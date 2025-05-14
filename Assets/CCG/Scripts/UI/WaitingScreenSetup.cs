using UnityEngine;
using UnityEngine.UI;

// This class is used to create the waiting screen UI in the scene if it doesn't exist
public class WaitingScreenSetup : MonoBehaviour
{
    // Call this method from another script to ensure the waiting screen exists
    public static WaitingScreen EnsureWaitingScreenExists()
    {
        // Check if waiting screen already exists
        WaitingScreen existingScreen = FindObjectOfType<WaitingScreen>();
        if (existingScreen != null)
            return existingScreen;
            
        // Find the main canvas - we'll attach our waiting screen to it
        Canvas mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("No Canvas found in the scene. Cannot create waiting screen.");
            return null;
        }
        
        // Create waiting panel
        GameObject waitingPanel = new GameObject("WaitingPanel");
        waitingPanel.transform.SetParent(mainCanvas.transform, false);
        RectTransform panelRect = waitingPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Add background image
        Image panelImage = waitingPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        // Create a container for centered content
        GameObject contentContainer = new GameObject("Content");
        contentContainer.transform.SetParent(waitingPanel.transform, false);
        RectTransform contentRect = contentContainer.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(400, 200);
        
        // Create waiting text
        GameObject textObj = new GameObject("WaitingText");
        textObj.transform.SetParent(contentContainer.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0, 40);
        textRect.sizeDelta = new Vector2(300, 50);
        Text waitingText = textObj.AddComponent<Text>();
        waitingText.text = "WAITING FOR OPPONENT...";
        waitingText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        waitingText.fontSize = 24;
        waitingText.alignment = TextAnchor.MiddleCenter;
        waitingText.color = Color.white;
        
        // Create loading icon
        GameObject loadingObj = new GameObject("LoadingIcon");
        loadingObj.transform.SetParent(contentContainer.transform, false);
        RectTransform loadingRect = loadingObj.AddComponent<RectTransform>();
        loadingRect.anchorMin = new Vector2(0.5f, 0.5f);
        loadingRect.anchorMax = new Vector2(0.5f, 0.5f);
        loadingRect.anchoredPosition = new Vector2(0, -40);
        loadingRect.sizeDelta = new Vector2(50, 50);
        Image loadingIcon = loadingObj.AddComponent<Image>();
        
        // Try to load a circle sprite for the loading icon or create a basic one
        Sprite circleSprite = Resources.Load<Sprite>("UI/circle");
        if (circleSprite != null)
        {
            loadingIcon.sprite = circleSprite;
        }
        else
        {
            // If no sprite is available, we'll use a white square as fallback
            loadingIcon.color = Color.white;
        }
        
        // Add the WaitingScreen component and configure it
        WaitingScreen waitingScreen = waitingPanel.AddComponent<WaitingScreen>();
        waitingScreen.waitingPanel = waitingPanel;
        waitingScreen.waitingText = waitingText;
        waitingScreen.loadingIcon = loadingIcon;
        
        return waitingScreen;
    }
} 