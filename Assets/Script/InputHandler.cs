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
        
        // Отправляем в сеть
        input.Set(data);
    }
    
    // Когда игрок подключился
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[InputHandler] Player joined: {player.PlayerId}");
        
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
        Debug.Log($"[InputHandler] Player left: {player.PlayerId}");
        
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
        Debug.Log("[InputHandler] Connected to server!");
    }
    
    // Когда отключились от сервера
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[InputHandler] Disconnected from server: {reason}");
    }
    
    // Когда не удалось подключиться
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[InputHandler] Connection failed: {reason}");
    }
    
    // Запрос на подключение
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        // Можно добавить проверку пароля или других условий
        Debug.Log("[InputHandler] Connection request received");
    }
    
    // Когда игра завершается
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[InputHandler] Shutdown: {shutdownReason}");
    }
    
    // === Остальные обязательные методы интерфейса ===
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[InputHandler] Host migration started");
    }
    
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[InputHandler] Scene load done");
    }
    
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[InputHandler] Scene load start");
    }
    
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}