using UnityEngine;

/// <summary>
/// Автоматический настройщик для Network Manager
/// Добавьте этот скрипт на GameObject с NetworkRunnerHandler
/// </summary>
public class NetworkManagerSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoFindInputHandler = true;
    [SerializeField] private bool validateOnStart = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDetailedLogs = true;
    
    private void Awake()
    {
        // Автоматически находим или создаем InputHandler
        if (autoFindInputHandler)
        {
            var inputHandler = GetComponent<InputHandler>();
            if (inputHandler == null)
            {
                inputHandler = gameObject.AddComponent<InputHandler>();
                Log("✓ InputHandler automatically added");
            }
            else
            {
                Log("✓ InputHandler already exists");
            }
        }
    }
    
    private void Start()
    {
        if (validateOnStart)
        {
            ValidateSetup();
        }
    }
    
    private void ValidateSetup()
    {
        Log("=== Validating Network Setup ===");
        
        bool isValid = true;
        int warnings = 0;
        
        // Проверка 1: NetworkRunnerHandler
        var networkHandler = GetComponent<NetworkRunnerHandler>();
        if (networkHandler == null)
        {
            LogError("❌ NetworkRunnerHandler not found!");
            isValid = false;
        }
        else
        {
            Log("✓ NetworkRunnerHandler found");
        }
        
        // Проверка 2: InputHandler
        var inputHandler = GetComponent<InputHandler>();
        if (inputHandler == null)
        {
            LogError("❌ InputHandler not found!");
            isValid = false;
        }
        else
        {
            Log("✓ InputHandler found");
        }
        
        // Проверка 3: NetworkMenuUI
        var menuUI = FindFirstObjectByType<NetworkMenuUI>();
        if (menuUI == null)
        {
            LogWarning("⚠️ NetworkMenuUI not found! You need to create UI manually.");
            warnings++;
        }
        else
        {
            Log("✓ NetworkMenuUI found");
        }
        
        // Проверка 4: PlayerSpawner prefab
        if (networkHandler != null)
        {
            var spawnerField = networkHandler.GetType().GetField("playerSpawnerPrefab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (spawnerField != null)
            {
                var prefab = spawnerField.GetValue(networkHandler);
                if (prefab == null)
                {
                    LogWarning("⚠️ PlayerSpawner prefab not assigned in NetworkRunnerHandler!");
                    warnings++;
                }
                else
                {
                    Log("✓ PlayerSpawner prefab assigned");
                }
            }
        }
        
        // Проверка 5: Fusion App ID
        if (!Application.isEditor)
        {
            // В билде проверяем наличие Fusion settings
            LogWarning("⚠️ Make sure Photon Fusion AppId is configured in Project Settings!");
            warnings++;
        }
        
        // Итоговый результат
        Debug.Log("=================================");
        if (isValid && warnings == 0)
        {
            Log("✓✓✓ Network Manager setup is PERFECT!");
        }
        else if (isValid)
        {
            LogWarning($"⚠ Setup complete with {warnings} warning(s). Check above.");
        }
        else
        {
            LogError("❌ Setup FAILED! Fix errors above before playing.");
        }
        Debug.Log("=================================");
        
        // Подсказки для игры
        if (isValid)
        {
            Log("Controls:");
            Log("  WASD / Arrows - Drive");
            Log("  Space - Brake");
            Log("  F1 - Network Stats");
            Log("  F2 - Spawned Cars");
            Log("  F3 - Detailed Diagnostics");
            Log("  ESC - Toggle Menu");
        }
    }
    
    private void Log(string message)
    {
        if (showDetailedLogs)
        {
            Debug.Log($"[Setup] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        Debug.LogWarning($"[Setup] {message}");
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[Setup] {message}");
    }
    
    // Вспомогательный метод для ручной валидации
    [ContextMenu("Validate Setup")]
    public void ManualValidate()
    {
        ValidateSetup();
    }
}