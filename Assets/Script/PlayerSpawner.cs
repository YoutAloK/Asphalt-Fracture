using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef carPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnSpacing = 10f;
    [SerializeField] private float spawnHeight = 2f;
    [SerializeField] private Vector3 spawnAreaCenter = Vector3.zero;
    
    [Header("Spawn Patterns")]
    [Tooltip("Как размещать игроков: Line, Circle, Grid")]
    [SerializeField] private SpawnPattern spawnPattern = SpawnPattern.Line;
    
    public enum SpawnPattern { Line, Circle, Grid }
    
    // Networked словарь для синхронизации заспавненных машин между клиентами
    [Networked, Capacity(10)]
    private NetworkDictionary<PlayerRef, NetworkObject> SpawnedCars => default;
    
    // Локальный кеш для быстрого доступа
    private Dictionary<PlayerRef, NetworkObject> localCarCache = new Dictionary<PlayerRef, NetworkObject>();
    
    public override void Spawned()
    {
        Debug.Log($"=== PlayerSpawner.Spawned() ===");
        Debug.Log($"IsServer: {Runner.IsServer}");
        Debug.Log($"IsMasterClient: {Runner.IsSharedModeMasterClient}");
        Debug.Log($"LocalPlayer: {Runner.LocalPlayer}");
        Debug.Log($"GameMode: {Runner.GameMode}");
        
        // Определяем, может ли этот клиент создавать объекты
        bool canSpawn = Runner.GameMode == GameMode.Shared 
            ? Runner.IsSharedModeMasterClient 
            : Runner.IsServer;
        
        if (canSpawn)
        {
            Debug.Log("✓ This client has spawn authority");
            
            // Спавним машины для всех уже подключенных игроков
            foreach (var player in Runner.ActivePlayers)
            {
                SpawnCarForPlayer(player);
            }
        }
        else
        {
            Debug.Log("Waiting for MasterClient/Server to spawn cars...");
        }
    }
    
    // Вызывается когда игрок подключается
    public void OnPlayerJoinedGame(PlayerRef player)
    {
        Debug.Log($"=== Player {player.PlayerId} joined the game ===");
        
        // Проверяем права на спавн
        bool canSpawn = Runner.GameMode == GameMode.Shared 
            ? Runner.IsSharedModeMasterClient 
            : Runner.IsServer;
        
        if (!canSpawn)
        {
            Debug.Log("Not spawning - we don't have spawn authority");
            return;
        }
        
        // Защита от двойного спавна
        if (SpawnedCars.ContainsKey(player))
        {
            Debug.LogWarning($"Car already exists for player {player.PlayerId}, skipping spawn");
            return;
        }
        
        SpawnCarForPlayer(player);
    }
    
    // Вызывается когда игрок отключается
    public void OnPlayerLeftGame(PlayerRef player)
    {
        Debug.Log($"=== Player {player.PlayerId} left the game ===");
        
        bool canDespawn = Runner.GameMode == GameMode.Shared 
            ? Runner.IsSharedModeMasterClient 
            : Runner.IsServer;
        
        if (canDespawn)
        {
            DespawnCarForPlayer(player);
        }
    }
    
    private void SpawnCarForPlayer(PlayerRef player)
    {
        // Дополнительная проверка на дубликаты
        if (SpawnedCars.ContainsKey(player))
        {
            Debug.LogWarning($"Preventing duplicate spawn for player {player.PlayerId}");
            return;
        }
        
        if (carPrefab == null)
        {
            Debug.LogError("❌ Car prefab is not assigned!");
            return;
        }
        
        // Вычисляем позицию спавна
        Vector3 spawnPosition = GetSpawnPosition(player);
        Quaternion spawnRotation = GetSpawnRotation(player);
        
        Debug.Log($"Spawning car for player {player.PlayerId} at {spawnPosition}");
        
        // Спавним машину с InputAuthority для этого игрока
        NetworkObject carObject = Runner.Spawn(
            carPrefab,
            spawnPosition,
            spawnRotation,
            player,  // КРИТИЧНО: Этот игрок получает control
            (runner, obj) =>
            {
                // Callback после спавна
                obj.name = $"Car_Player{player.PlayerId}";
                Debug.Log($"✓ Car spawned successfully!");
                Debug.Log($"  Name: {obj.name}");
                Debug.Log($"  InputAuthority: {obj.InputAuthority.PlayerId}");
                Debug.Log($"  Position: {obj.transform.position}");
            }
        );
        
        if (carObject != null)
        {
            // Добавляем в сетевой словарь (синхронизируется автоматически)
            SpawnedCars.Add(player, carObject);
            
            // Добавляем в локальный кеш
            localCarCache[player] = carObject;
            
            Debug.Log($"✓ Car registered for player {player.PlayerId}");
            Debug.Log($"Total cars spawned: {SpawnedCars.Count}");
        }
        else
        {
            Debug.LogError($"❌ Failed to spawn car for player {player.PlayerId}!");
        }
    }
    
    private void DespawnCarForPlayer(PlayerRef player)
    {
        NetworkObject carObject = null;
        
        // Пытаемся получить из сетевого словаря
        if (SpawnedCars.TryGet(player, out carObject))
        {
            if (carObject != null)
            {
                Debug.Log($"Despawning car for player {player.PlayerId}");
                Runner.Despawn(carObject);
            }
            
            // Удаляем из словаря
            SpawnedCars.Remove(player);
        }
        
        // Удаляем из локального кеша
        if (localCarCache.ContainsKey(player))
        {
            localCarCache.Remove(player);
        }
        
        Debug.Log($"Car removed for player {player.PlayerId}");
    }
    
    // Вспомогательный метод для подсчета активных игроков
    private int GetActivePlayerCount()
    {
        int count = 0;
        foreach (var player in Runner.ActivePlayers)
        {
            count++;
        }
        return count;
    }
    
    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        int playerIndex = player.PlayerId;
        
        switch (spawnPattern)
        {
            case SpawnPattern.Circle:
                return GetCircleSpawnPosition(playerIndex);
            
            case SpawnPattern.Grid:
                return GetGridSpawnPosition(playerIndex);
            
            case SpawnPattern.Line:
            default:
                return GetLineSpawnPosition(playerIndex);
        }
    }
    
    private Vector3 GetLineSpawnPosition(int playerIndex)
    {
        return spawnAreaCenter + new Vector3(
            playerIndex * spawnSpacing,
            spawnHeight,
            0f
        );
    }
    
    private Vector3 GetCircleSpawnPosition(int playerIndex)
    {
        int playerCount = GetActivePlayerCount();
        float angle = (360f / Mathf.Max(playerCount, 2)) * playerIndex;
        float radians = angle * Mathf.Deg2Rad;
        
        return spawnAreaCenter + new Vector3(
            Mathf.Cos(radians) * spawnSpacing,
            spawnHeight,
            Mathf.Sin(radians) * spawnSpacing
        );
    }
    
    private Vector3 GetGridSpawnPosition(int playerIndex)
    {
        int playerCount = GetActivePlayerCount();
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(playerCount));
        int row = playerIndex / gridSize;
        int col = playerIndex % gridSize;
        
        return spawnAreaCenter + new Vector3(
            col * spawnSpacing,
            spawnHeight,
            row * spawnSpacing
        );
    }
    
    private Quaternion GetSpawnRotation(PlayerRef player)
    {
        // В режиме круга машины смотрят в центр
        if (spawnPattern == SpawnPattern.Circle)
        {
            Vector3 spawnPos = GetSpawnPosition(player);
            Vector3 lookDirection = (spawnAreaCenter - spawnPos).normalized;
            lookDirection.y = 0; // Только горизонтальное вращение
            
            if (lookDirection != Vector3.zero)
            {
                return Quaternion.LookRotation(lookDirection);
            }
        }
        
        // По умолчанию все смотрят вперед
        return Quaternion.identity;
    }
    
    // Получить машину конкретного игрока
    public NetworkObject GetCarForPlayer(PlayerRef player)
    {
        // Сначала проверяем локальный кеш
        if (localCarCache.TryGetValue(player, out NetworkObject cachedCar))
        {
            if (cachedCar != null) return cachedCar;
        }
        
        // Если нет в кеше, проверяем сетевой словарь
        if (SpawnedCars.TryGet(player, out NetworkObject networkCar))
        {
            localCarCache[player] = networkCar;
            return networkCar;
        }
        
        return null;
    }
    
    // Диагностика - F2
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ShowSpawnedCarsList();
        }
    }
    
    private void ShowSpawnedCarsList()
    {
        Debug.Log("=== Spawned Cars List ===");
        Debug.Log($"Total in NetworkDictionary: {SpawnedCars.Count}");
        Debug.Log($"Total in Local Cache: {localCarCache.Count}");
        
        foreach (var kvp in SpawnedCars)
        {
            var player = kvp.Key;
            var car = kvp.Value;
            
            if (car != null)
            {
                bool isLocalPlayer = car.HasInputAuthority;
                string marker = isLocalPlayer ? "★ YOUR CAR" : "";
                
                Debug.Log($"Player {player.PlayerId} {marker}");
                Debug.Log($"  Name: {car.name}");
                Debug.Log($"  Position: {car.transform.position}");
                Debug.Log($"  InputAuthority: {car.InputAuthority.PlayerId}");
                Debug.Log($"  IsLocal: {isLocalPlayer}");
            }
            else
            {
                Debug.LogWarning($"Player {player.PlayerId}: Car is NULL!");
            }
        }
    }
}