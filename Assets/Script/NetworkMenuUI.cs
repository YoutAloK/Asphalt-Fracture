using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NetworkMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private TextMeshProUGUI statusText;
    
    [Header("In-Game UI (Optional)")]
    [SerializeField] private GameObject inGamePanel;
    [SerializeField] private TextMeshProUGUI pingText;
    [SerializeField] private TextMeshProUGUI playersText;
    
    [Header("Network")]
    [SerializeField] private NetworkRunnerHandler networkHandler;
    
    [Header("Settings")]
    [SerializeField] private bool showPingInGame = true;
    [SerializeField] private float pingUpdateInterval = 0.5f;
    
    private float pingUpdateTimer = 0f;
    
    private void Start()
    {
        // Находим NetworkRunnerHandler если не назначен
        if (networkHandler == null)
        {
            networkHandler = FindFirstObjectByType<NetworkRunnerHandler>();
            
            if (networkHandler == null)
            {
                Debug.LogError("❌ NetworkRunnerHandler not found!");
            }
        }
        
        // Настраиваем кнопки
        if (hostButton != null)
        {
            hostButton.onClick.AddListener(OnHostClicked);
        }
        
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinClicked);
        }
        
        // Показываем меню
        ShowMenu(true);
        ShowInGameUI(false);
        UpdateStatus("Choose Host or Join");
        
        // Устанавливаем placeholder для комнаты
        if (roomNameInput != null && roomNameInput.placeholder != null)
        {
            var placeholder = roomNameInput.placeholder.GetComponent<TextMeshProUGUI>();
            if (placeholder != null)
            {
                placeholder.text = "Room Name (optional)";
            }
        }
    }
    
    private void Update()
    {
        // Обновляем пинг и статистику во время игры
        if (showPingInGame && networkHandler != null && networkHandler.IsConnected())
        {
            pingUpdateTimer += Time.deltaTime;
            
            if (pingUpdateTimer >= pingUpdateInterval)
            {
                UpdateInGameStats();
                pingUpdateTimer = 0f;
            }
        }
        
        // ESC для возврата в меню
        if (Input.GetKeyDown(KeyCode.Escape) && !menuPanel.activeSelf)
        {
            ToggleMenu();
        }
    }
    
    private void OnHostClicked()
    {
        if (networkHandler == null)
        {
            UpdateStatus("ERROR: NetworkHandler not found!");
            return;
        }
        
        UpdateStatus("Starting as Host...");
        DisableButtons();
        
        networkHandler.StartHost();
        
        // Показываем игровой UI через секунду
        Invoke(nameof(ShowGameUI), 1f);
    }
    
    private void OnJoinClicked()
    {
        if (networkHandler == null)
        {
            UpdateStatus("ERROR: NetworkHandler not found!");
            return;
        }
        
        string roomName = roomNameInput != null ? roomNameInput.text.Trim() : "";
        
        if (string.IsNullOrEmpty(roomName))
        {
            UpdateStatus("Joining default room...");
            networkHandler.StartClient();
        }
        else
        {
            UpdateStatus($"Joining room: {roomName}...");
            networkHandler.StartClient(roomName);
        }
        
        DisableButtons();
        
        // Показываем игровой UI через секунду
        Invoke(nameof(ShowGameUI), 1f);
    }
    
    private void ShowGameUI()
    {
        ShowMenu(false);
        ShowInGameUI(true);
    }
    
    private void ShowMenu(bool show)
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(show);
        }
    }
    
    private void ShowInGameUI(bool show)
    {
        if (inGamePanel != null)
        {
            inGamePanel.SetActive(show);
        }
    }
    
    private void ToggleMenu()
    {
        if (menuPanel != null)
        {
            bool newState = !menuPanel.activeSelf;
            menuPanel.SetActive(newState);
            
            // Приостанавливаем игру если меню открыто
            Time.timeScale = newState ? 0f : 1f;
        }
    }
    
    private void DisableButtons()
    {
        if (hostButton != null) hostButton.interactable = false;
        if (joinButton != null) joinButton.interactable = false;
    }
    
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[MenuUI] {message}");
    }
    
    private void UpdateInGameStats()
    {
        if (networkHandler == null) return;
        
        // Обновляем пинг
        if (pingText != null)
        {
            float ping = networkHandler.GetPingMs();
            
            // Цвет в зависимости от пинга
            Color pingColor = Color.green;
            if (ping > 100) pingColor = Color.yellow;
            if (ping > 200) pingColor = Color.red;
            
            pingText.text = $"Ping: {ping:F0}ms";
            pingText.color = pingColor;
        }
        
        // Обновляем количество игроков (опционально)
        if (playersText != null)
        {
            // Можно добавить подсчет игроков через Runner.ActivePlayers
            playersText.text = "Players: Connected";
        }
    }
}