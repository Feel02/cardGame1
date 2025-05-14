using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class WaitingScreen : MonoBehaviour
{
    [Header("Waiting Screen")]
    public GameObject waitingPanel;
    public Text waitingText;
    public Image loadingIcon;
    
    [Header("Settings")]
    public float rotationSpeed = 90f; // Degrees per second
    public float textBlinkInterval = 0.8f; // Time in seconds between text blinks
    
    private bool isWaiting = true;
    private NetworkManagerCCG networkManager;
    private Coroutine blinkCoroutine;
    private bool isCustomMessage = false;
    private string customMessageBase = "";
    
    // Static instance for easy access
    public static WaitingScreen Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Find the network manager
        networkManager = FindObjectOfType<NetworkManagerCCG>();
        
        // Ensure the waiting screen is initially visible if online mode is enabled and not a server
        bool isOfflineMode = PlayerPrefs.GetInt("offlineMode", 0) == 1;
        bool isServer = NetworkServer.active;
        bool isClient = NetworkClient.isConnected && NetworkClient.active;
        
        // Only show on pure clients - hide on servers or hosts
        bool shouldShow = !isOfflineMode && !isServer && isClient;
        
        waitingPanel.SetActive(shouldShow);
        
        if (shouldShow)
        {
            // Start rotating the loading icon
            StartCoroutine(RotateLoadingIcon());
            
            // Start blinking text
            blinkCoroutine = StartCoroutine(BlinkText());
            
            // We'll use the Update method to check connection status instead of events
            Debug.Log("Waiting for connection and players...");
        }
    }
    
    void OnDestroy()
    {
        // Stop coroutines
        if (blinkCoroutine != null)
            StopCoroutine(blinkCoroutine);
            
        if (Instance == this)
            Instance = null;
    }
    
    void Update()
    {
        // Skip custom message updates (like for RL_AI loading)
        if (isCustomMessage)
            return;
            
        // If we're in offline mode or we're a server, don't show the waiting screen
        bool isOfflineMode = PlayerPrefs.GetInt("offlineMode", 0) == 1;
        bool isServer = NetworkServer.active;
        
        if (isOfflineMode || isServer)
        {
            if (waitingPanel.activeSelf)
                waitingPanel.SetActive(false);
            return;
        }
        
        // Check if we're connected to the server
        bool isConnected = NetworkClient.isConnected;
        
        // Check if local player exists and has connected to another player
        if (Player.localPlayer != null)
        {
            if (Player.localPlayer.hasEnemy && isWaiting)
            {
                // Another player has connected, hide the waiting screen
                waitingPanel.SetActive(false);
                isWaiting = false;
                Debug.Log("Second player connected. Hiding waiting screen.");
            }
            else if (!Player.localPlayer.hasEnemy && !isWaiting)
            {
                // Lost connection to second player, show waiting screen again
                waitingPanel.SetActive(true);
                isWaiting = true;
                Debug.Log("Second player disconnected. Showing waiting screen.");
            }
        }
        else if (isConnected && waitingText != null)
        {
            // Connected to server but Player.localPlayer not set yet
            waitingText.text = "WAITING FOR OPPONENT...";
        }
    }
    
    // Public methods to show/hide with custom message
    public void ShowWithMessage(string message)
    {
        isCustomMessage = true;
        customMessageBase = message;
        waitingPanel.SetActive(true);
        
        if (waitingText != null)
            waitingText.text = message;
            
        // Make sure coroutines are running
        if (blinkCoroutine == null)
            blinkCoroutine = StartCoroutine(BlinkText());
            
        StartCoroutine(RotateLoadingIcon());
    }
    
    public void Hide()
    {
        waitingPanel.SetActive(false);
        isCustomMessage = false;
    }
    
    IEnumerator RotateLoadingIcon()
    {
        while (true)
        {
            if (loadingIcon != null)
            {
                loadingIcon.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
            }
            yield return null;
        }
    }
    
    IEnumerator BlinkText()
    {
        bool visible = true;
        
        while (true)
        {
            if (waitingText != null)
            {
                // Toggle text visibility
                visible = !visible;
                
                if (isCustomMessage)
                {
                    waitingText.text = visible ? customMessageBase + "..." : customMessageBase;
                }
                else
                {
                    waitingText.text = visible ? "WAITING FOR OPPONENT..." : "WAITING FOR OPPONENT";
                }
            }
            
            yield return new WaitForSeconds(textBlinkInterval);
        }
    }
} 