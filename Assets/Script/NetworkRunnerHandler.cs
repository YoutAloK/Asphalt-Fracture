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
    [Tooltip("Shared Mode = P2P (минимальный пинг), Host Mode = клиент-сервер")]
    [SerializeField] private bool useP2PMode = true; // Включено для минимального пинга
    
    [Header("Photon Settings")]
    [SerializeField] private string photonRegion = ""; // Пустое = auto (лучший регион автоматически)
    
    private NetworkRunner runner;
    private bool isStarting = false;
    
    private void Awake()
    {
        if (inputHandler == null)
            inputHandler = FindFirstObjectByType<InputHandler>();
        
        // Оптимизация для сетевой игры
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        
        Debug.Log($"=== Network Mode: {(useP2PMode ? "P2P (Shared)" : "Client-Server (Host)")} ===");
    }
    
    public async void StartHost()
    {
        if (isStarting) return;
        isStarting = true;
        
        GameMode mode = useP2PMode ? GameMode.Shared : GameMode.Host;
        
        Debug.Log($"=== Starting as {mode} ===");
        await StartGame(mode, defaultRoomName);
    }
    
    public async void StartClient(string roomName = null)
    {
        if (isStarting) return;
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
            Debug.Log("InputHandler registered");
        }
        
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers,
        };
        
        // Для минимального пинга используем auto-регион (не задаем fixed)
        Debug.Log("Using auto (best) Photon region for minimal ping");
        
        Debug.Log($"Attempting to connect to Photon in {mode} mode...");
        Debug.Log("P2P Mode advantages:");
        Debug.Log("✓ Direct connection between players");
        Debug.Log("✓ Lower latency (no relay through host)");
        Debug.Log("✓ Better for 2-4 players");
        
        var result = await runner.StartGame(startGameArgs);
        
        if (result.Ok)
        {
            Debug.Log($"=== NetworkRunner Started Successfully ===");
            Debug.Log($"GameMode: {runner.GameMode}");
            Debug.Log($"Session: {roomName}");
            Debug.Log($"LocalPlayer: {runner.LocalPlayer}");
            Debug.Log($"IsServer: {runner.IsServer}");
            Debug.Log($"IsClient: {runner.IsClient}");
            Debug.Log($"IsSharedModeMasterClient: {runner.IsSharedModeMasterClient}");
            Debug.Log($"ActivePlayers: {runner.ActivePlayers.Count()}");
            
            if (ShouldSpawnObjects())
            {
                Debug.Log("=== Spawning PlayerSpawner (as MasterClient) ===");
                await Task.Delay(100);
                
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
                    Debug.LogError("❌ PlayerSpawner prefab not assigned!");
                }
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to start: {result.ShutdownReason}");
            isStarting = false;
        }
    }
    
    private bool ShouldSpawnObjects()
    {
        if (useP2PMode)
        {
            return runner.IsSharedModeMasterClient;
        }
        else
        {
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
        if (runner != null && runner.IsRunning && Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"=== Network Stats ===");
            Debug.Log($"Mode: {runner.GameMode}");
            
            float pingMs = (float)runner.GetPlayerRtt(runner.LocalPlayer) * 1000f;
            Debug.Log($"Ping: {pingMs:F0}ms");
            
            Debug.Log($"Active Players: {runner.ActivePlayers.Count()}");
            Debug.Log($"Tick: {runner.Tick}");
            
            Debug.Log($"Delta Time: {(float)runner.DeltaTime}");
            
            Debug.Log($"Is MasterClient: {runner.IsSharedModeMasterClient}");
            
            foreach (var player in runner.ActivePlayers)
            {
                float rtt = (float)runner.GetPlayerRtt(player) * 1000f;
                Debug.Log($"Player {player.PlayerId} RTT: {rtt:F0}ms");
            }
        }
    }
}