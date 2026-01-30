using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private NetworkPrefabRef carPrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnSpacing = 5f;
    [SerializeField] private float spawnHeight = 2f;
    
    // Словарь для отслеживания заспавненных машин
    private Dictionary<PlayerRef, NetworkObject> spawnedCars = new Dictionary<PlayerRef, NetworkObject>();
    
    public override void Spawned()
    {
        Debug.Log($"=== PlayerSpawner.Spawned() ===");
        Debug.Log($"IsServer: {Runner.IsServer}");
        Debug.Log($"IsMasterClient: {Runner.IsSharedModeMasterClient}");
        Debug.Log($"LocalPlayer: {Runner.LocalPlayer}");
        
        // В Shared mode проверяем MasterClient, в Host mode - IsServer
        bool canSpawn = Runner.GameMode == GameMode.Shared 
            ? Runner.IsSharedModeMasterClient 
            : Runner.IsServer;
        
        if (canSpawn)
        {
            Debug.Log("This client can spawn players");
            
            // Спавним машины для всех уже подключенных игроков
            foreach (var player in Runner.ActivePlayers)
            {
                SpawnCarForPlayer(player);
            }
        }
        else
        {
            Debug.Log("This client cannot spawn (waiting for MasterClient/Server)");
        }
    }
    
    // Вызывается когда игрок подключается (вызывается из InputHandler)
    public void OnPlayerJoinedGame(PlayerRef player)
    {
        Debug.Log($"=== PlayerJoined: {player.PlayerId} ===");
        
        // Проверяем права на спавн в зависимости от режима
        bool canSpawn = Runner.GameMode == GameMode.Shared 
            ? Runner.IsSharedModeMasterClient 
            : Runner.IsServer;
        
        if (canSpawn)
        {
            SpawnCarForPlayer(player);
        }
        else
        {
            Debug.Log($"Not spawning - we are not MasterClient/Server");
        }
    }
    
    // Вызывается когда игрок отключается (вызывается из InputHandler)
    public void OnPlayerLeftGame(PlayerRef player)
    {
        Debug.Log($"=== PlayerLeft: {player.PlayerId} ===");
        
        // Проверяем права на деспавн
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
        // Проверяем что машина еще не заспавнена
        if (spawnedCars.ContainsKey(player))
        {
            Debug.LogWarning($"Car already exists for player {player.PlayerId}");
            return;
        }
        
        if (carPrefab == null)
        {
            Debug.LogError("Car prefab is not assigned!");
            return;
        }
        
        // Вычисляем позицию спавна
        Vector3 spawnPosition = GetSpawnPosition(player);
        
        Debug.Log($"Spawning car for player {player.PlayerId} at {spawnPosition}");
        
        // Спавним машину с правильным InputAuthority
        NetworkObject carObject = Runner.Spawn(
            carPrefab,
            spawnPosition,
            Quaternion.identity,
            player,  // ВАЖНО: Этот игрок будет управлять машиной
            (runner, obj) =>
            {
                Debug.Log($"✓ Car spawned: {obj.name} for player {player.PlayerId}");
                Debug.Log($"  - InputAuthority: {obj.InputAuthority}");
                Debug.Log($"  - StateAuthority: {obj.StateAuthority}");
                Debug.Log($"  - HasInputAuthority: {obj.HasInputAuthority}");
            }
        );
        
        if (carObject != null)
        {
            carObject.name = $"Car_Player{player.PlayerId}";
            spawnedCars[player] = carObject;
            Debug.Log($"✓ Successfully spawned car for player {player.PlayerId}");
        }
        else
        {
            Debug.LogError($"❌ Failed to spawn car for player {player.PlayerId}!");
        }
    }
    
    private void DespawnCarForPlayer(PlayerRef player)
    {
        if (spawnedCars.TryGetValue(player, out NetworkObject carObject))
        {
            if (carObject != null)
            {
                Debug.Log($"Despawning car for player {player.PlayerId}");
                Runner.Despawn(carObject);
            }
            spawnedCars.Remove(player);
        }
    }
    
    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        // Размещаем игроков в ряд
        return new Vector3(
            player.PlayerId * spawnSpacing,
            spawnHeight,
            0f
        );
    }
    
    // Дополнительный метод для получения машины конкретного игрока
    public NetworkObject GetCarForPlayer(PlayerRef player)
    {
        spawnedCars.TryGetValue(player, out NetworkObject car);
        return car;
    }
    
    // Диагностика - показывает список всех заспавненных машин
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("=== Spawned Cars List ===");
            foreach (var kvp in spawnedCars)
            {
                var player = kvp.Key;
                var car = kvp.Value;
                
                if (car != null)
                {
                    Debug.Log($"Player {player.PlayerId}: {car.name}");
                    Debug.Log($"  - Position: {car.transform.position}");
                    Debug.Log($"  - InputAuthority: {car.InputAuthority}");
                    Debug.Log($"  - IsLocal: {car.HasInputAuthority}");
                }
            }
            Debug.Log($"Total cars: {spawnedCars.Count}");
        }
    }
}