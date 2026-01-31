using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class InputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    // Собираем input от игрока
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();
        
        // Собираем данные клавиатуры/геймпада
        data.Horizontal = Input.GetAxis("Horizontal");
        data.Vertical = Input.GetAxis("Vertical");
        data.IsBraking = Input.GetKey(KeyCode.Space); // Ручное торможение
        
        // Отправляем в сеть
        input.Set(data);
    }
    
    // Когда игрок подключился
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[InputHandler] ✓ Player {player.PlayerId} joined the game!");
        
        // Находим PlayerSpawner и сообщаем о новом игроке
        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null && spawner.Object != null)
        {
            spawner.OnPlayerJoinedGame(player);
        }
    }
    
    // Когда игрок отключился
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[InputHandler] ✗ Player {player.PlayerId} left the game");
        
        // Находим PlayerSpawner и сообщаем об отключении
        var spawner = FindFirstObjectByType<PlayerSpawner>();
        if (spawner != null && spawner.Object != null)
        {
            spawner.OnPlayerLeftGame(player);
        }
    }
    
    // Когда подключились к серверу
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[InputHandler] ✓ Connected to Photon server!");
    }
    
    // Когда отключились от сервера
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[InputHandler] ✗ Disconnected from server: {reason}");
    }
    
    // Когда не удалось подключиться
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[InputHandler] ❌ Connection failed: {reason}");
    }
    
    // Запрос на подключение
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        Debug.Log("[InputHandler] Connection request received - accepting");
    }
    
    // Когда игра завершается
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[InputHandler] Shutdown: {shutdownReason}");
    }
    
    // === Остальные обязательные методы интерфейса ===
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) 
    { 
        // Пропущенный input - может происходить при высоком пинге
        Debug.LogWarning($"[InputHandler] Input missing for player {player.PlayerId}");
    }
    
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) 
    {
        Debug.Log($"[InputHandler] Sessions available: {sessionList.Count}");
    }
    
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[InputHandler] Host migration started - game will continue");
    }
    
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[InputHandler] ✓ Scene loaded successfully");
    }
    
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[InputHandler] Scene loading...");
    }
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}