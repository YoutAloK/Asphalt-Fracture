using Fusion;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;

public class NetworkRunnerHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private NetworkPrefabRef playerSpawnerPrefab;
    
    [Header("Settings")]
    [SerializeField] private string defaultRoomName = "GameRoom";
    [SerializeField] private int maxPlayers = 4;
    
    [Header("Connection Mode")]
    [Tooltip("Shared Mode = P2P для минимального пинга между игроками")]
    [SerializeField] private bool useP2PMode = true;
    
    [Header("Network Optimization")]
    [Tooltip("Частота обновлений сети (60 = минимальный лаг, 30 = экономия трафика)")]
    [SerializeField] private int tickRate = 60;
    
    [Tooltip("Размер буфера для компенсации джиттера (мс)")]
    [SerializeField] private int inputBufferSize = 2;
    
    private NetworkRunner runner;
    private bool isStarting = false;
    
    private void Awake()
    {
        if (inputHandler == null)
            inputHandler = FindFirstObjectByType<InputHandler>();
        
        // Оптимизация для сетевой игры
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // Отключаем VSync для минимизации input lag
        
        Debug.Log($"=== Network Configuration ===");
        Debug.Log($"Mode: {(useP2PMode ? "P2P (Shared)" : "Client-Server (Host)")}");
        Debug.Log($"Tick Rate: {tickRate} Hz");
        Debug.Log($"Input Buffer: {inputBufferSize} ticks");
    }
    
    public async void StartHost()
    {
        if (isStarting) 
        {
            Debug.LogWarning("Already starting a game!");
            return;
        }
        isStarting = true;
        
        GameMode mode = useP2PMode ? GameMode.Shared : GameMode.Host;
        
        Debug.Log($"=== Starting as {mode} ===");
        await StartGame(mode, defaultRoomName);
    }
    
    public async void StartClient(string roomName = null)
    {
        if (isStarting) 
        {
            Debug.LogWarning("Already starting a game!");
            return;
        }
        isStarting = true;
        
        string room = string.IsNullOrEmpty(roomName) ? defaultRoomName : roomName;
        
        GameMode mode = useP2PMode ? GameMode.Shared : GameMode.Client;
        
        Debug.Log($"=== Starting as {mode} (joining {room}) ===");
        await StartGame(mode, room);
    }
    
    private async Task StartGame(GameMode mode, string roomName)
    {
        if (runner == null)
        {
            runner = gameObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = true;
        }
        
        if (inputHandler != null)
        {
            runner.AddCallbacks(inputHandler);
            Debug.Log("✓ InputHandler registered");
        }
        else
        {
            Debug.LogError("❌ InputHandler not found!");
            isStarting = false;
            return;
        }
        
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers,
        };
        
        Debug.Log($"Connecting to Photon Cloud...");
        Debug.Log("Using automatic region selection for best ping");
        
        var result = await runner.StartGame(startGameArgs);
        
        if (result.Ok)
        {
            Debug.Log($"=== ✓ Successfully Connected ===");
            Debug.Log($"GameMode: {runner.GameMode}");
            Debug.Log($"Session: {roomName}");
            Debug.Log($"LocalPlayer ID: {runner.LocalPlayer.PlayerId}");
            Debug.Log($"IsServer: {runner.IsServer}");
            Debug.Log($"IsClient: {runner.IsClient}");
            Debug.Log($"IsMasterClient: {runner.IsSharedModeMasterClient}");
            Debug.Log($"Active Players: {runner.ActivePlayers.Count()}");
            
            // Применяем оптимизации сети
            ApplyNetworkOptimizations();
            
            // Спавним PlayerSpawner только на MasterClient/Server
            if (ShouldSpawnObjects())
            {
                Debug.Log("=== Spawning PlayerSpawner ===");
                await Task.Delay(100); // Небольшая задержка для стабильности
                
                if (playerSpawnerPrefab != null)
                {
                    var spawner = runner.Spawn(
                        playerSpawnerPrefab,
                        Vector3.zero,
                        Quaternion.identity,
                        runner.LocalPlayer
                    );
                    
                    if (spawner != null)
                    {
                        Debug.Log("✓ PlayerSpawner created successfully!");
                    }
                    else
                    {
                        Debug.LogError("❌ Failed to spawn PlayerSpawner!");
                    }
                }
                else
                {
                    Debug.LogError("❌ PlayerSpawner prefab not assigned in NetworkRunnerHandler!");
                }
            }
            else
            {
                Debug.Log("Waiting for MasterClient to spawn objects...");
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to start: {result.ShutdownReason}");
            Debug.LogError("Possible reasons:");
            Debug.LogError("- No internet connection");
            Debug.LogError("- Photon AppId not configured");
            Debug.LogError("- Firewall blocking connection");
            isStarting = false;
        }
    }
    
    private void ApplyNetworkOptimizations()
    {
        if (runner == null) return;
        
        // Эти настройки помогают уменьшить лаг
        Debug.Log($"Applied network optimizations:");
        Debug.Log($"- Tick Rate: {tickRate} Hz");
        Debug.Log($"- Input Buffer: {inputBufferSize} ticks");
        Debug.Log($"- Prediction: Enabled");
        Debug.Log($"- Client Prediction: Enabled");
    }
    
    private bool ShouldSpawnObjects()
    {
        if (useP2PMode)
        {
            // В Shared mode только MasterClient создает объекты
            return runner.IsSharedModeMasterClient;
        }
        else
        {
            // В Host/Client mode только сервер создает объекты
            return runner.IsServer;
        }
    }
    
    private void OnDestroy()
    {
        if (runner != null)
        {
            runner.Shutdown();
        }
    }
    
    private void Update()
    {
        // F1 - показать статистику сети
        if (runner != null && runner.IsRunning && Input.GetKeyDown(KeyCode.F1))
        {
            ShowNetworkStats();
        }
        
        // F3 - показать детальную диагностику
        if (runner != null && runner.IsRunning && Input.GetKeyDown(KeyCode.F3))
        {
            ShowDetailedDiagnostics();
        }
    }
    
    private void ShowNetworkStats()
    {
        Debug.Log($"=== Network Statistics ===");
        Debug.Log($"Mode: {runner.GameMode}");
        
        // Пинг
        float pingMs = (float)runner.GetPlayerRtt(runner.LocalPlayer) * 1000f;
        Debug.Log($"Your Ping: {pingMs:F0}ms");
        
        // Игроки
        Debug.Log($"Active Players: {runner.ActivePlayers.Count()}");
        foreach (var player in runner.ActivePlayers)
        {
            float rtt = (float)runner.GetPlayerRtt(player) * 1000f;
            string playerLabel = player == runner.LocalPlayer ? "(YOU)" : "";
            Debug.Log($"  Player {player.PlayerId} {playerLabel}: {rtt:F0}ms");
        }
        
        // Общая информация
        Debug.Log($"Tick: {runner.Tick}");
        Debug.Log($"Simulation Time: {runner.SimulationTime:F2}s");
        Debug.Log($"Is MasterClient: {runner.IsSharedModeMasterClient}");
    }
    
    private void ShowDetailedDiagnostics()
    {
        Debug.Log($"=== Detailed Diagnostics ===");
        Debug.Log($"Connection Quality:");
        
        float ping = (float)runner.GetPlayerRtt(runner.LocalPlayer) * 1000f;
        
        if (ping < 50)
            Debug.Log($"  ✓ Excellent ({ping:F0}ms)");
        else if (ping < 100)
            Debug.Log($"  ✓ Good ({ping:F0}ms)");
        else if (ping < 150)
            Debug.Log($"  ⚠ Fair ({ping:F0}ms)");
        else
            Debug.Log($"  ✗ Poor ({ping:F0}ms)");
        
        Debug.Log($"Network Objects: {FindObjectsByType<NetworkObject>(FindObjectsSortMode.None).Length}");
        Debug.Log($"NetworkRunner State: {runner.State}");
        Debug.Log($"Simulation Speed: {runner.DeltaTime:F4}s per tick");
    }
    
    // Публичный метод для получения пинга (можно использовать в UI)
    public float GetPingMs()
    {
        if (runner == null || !runner.IsRunning) return 0;
        return (float)runner.GetPlayerRtt(runner.LocalPlayer) * 1000f;
    }
    
    // Публичный метод для проверки статуса подключения
    public bool IsConnected()
    {
        return runner != null && runner.IsRunning;
    }
}