using UnityEngine;

/// <summary>
/// Автоматический настройщик для NetworkManager
/// Добавь этот скрипт на GameObject с NetworkRunnerHandler
/// </summary>
public class NetworkManagerSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [SerializeField] private bool autoFindInputHandler = true;
    
    private void Awake()
    {
        // Автоматически находим или создаем InputHandler
        if (autoFindInputHandler)
        {
            var inputHandler = GetComponent<InputHandler>();
            if (inputHandler == null)
            {
                inputHandler = gameObject.AddComponent<InputHandler>();
                Debug.Log("[Setup] InputHandler automatically added");
            }
        }
    }
    
    private void Start()
    {
        // Проверяем наличие всех необходимых компонентов
        ValidateSetup();
    }
    
    private void ValidateSetup()
    {
        bool isValid = true;
        
        var networkHandler = GetComponent<NetworkRunnerHandler>();
        if (networkHandler == null)
        {
            Debug.LogError("[Setup] ❌ NetworkRunnerHandler not found!");
            isValid = false;
        }
        
        var inputHandler = GetComponent<InputHandler>();
        if (inputHandler == null)
        {
            Debug.LogError("[Setup] ❌ InputHandler not found!");
            isValid = false;
        }
        
        var menuUI = FindFirstObjectByType<NetworkMenuUI>();
        if (menuUI == null)
        {
            Debug.LogWarning("[Setup] ⚠️ NetworkMenuUI not found! You need to create UI.");
        }
        
        if (isValid)
        {
            Debug.Log("[Setup] ✓ Network Manager setup is complete!");
        }
        else
        {
            Debug.LogError("[Setup] ❌ Setup incomplete! Check errors above.");
        }
    }
}