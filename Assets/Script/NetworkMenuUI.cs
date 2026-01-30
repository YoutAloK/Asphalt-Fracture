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
    
    [Header("Network")]
    [SerializeField] private NetworkRunnerHandler networkHandler;
    
    private void Start()
    {
        // Находим NetworkRunnerHandler если не назначен
        if (networkHandler == null)
        {
            networkHandler = FindFirstObjectByType<NetworkRunnerHandler>();
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
        UpdateStatus("Choose Host or Join");
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
        
        // Скрываем меню через секунду
        Invoke(nameof(HideMenu), 1f);
    }
    
    private void OnJoinClicked()
    {
        if (networkHandler == null)
        {
            UpdateStatus("ERROR: NetworkHandler not found!");
            return;
        }
        
        string roomName = roomNameInput != null ? roomNameInput.text : null;
        
        UpdateStatus($"Joining room: {(string.IsNullOrEmpty(roomName) ? "default" : roomName)}...");
        DisableButtons();
        
        networkHandler.StartClient(roomName);
        
        // Скрываем меню через секунду
        Invoke(nameof(HideMenu), 1f);
    }
    
    private void ShowMenu(bool show)
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(show);
        }
    }
    
    private void HideMenu()
    {
        ShowMenu(false);
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
}