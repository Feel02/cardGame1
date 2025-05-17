using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OfflineStartGameButtonScript : MonoBehaviour
{
    public TMP_InputField username;
    public GameObject aiSelectionPanel; // Reference to new AI selection panel
    public Button offlineButton; // Reference to the offline button

    public void Start()
    {
        // Only disable and gray out the offline button on Android devices at runtime
        if (Application.platform == RuntimePlatform.Android)
        {
            if (offlineButton != null)
            {
                offlineButton.interactable = false;
                ColorBlock colors = offlineButton.colors;
                colors.normalColor = colors.disabledColor;
                colors.highlightedColor = colors.disabledColor;
                colors.pressedColor = colors.disabledColor;
                colors.selectedColor = colors.disabledColor;
                offlineButton.colors = colors;
            }
        }
        if (PlayerPrefs.GetString("Name") != null)
            username.text = PlayerPrefs.GetString("Name");
    }

    public void ShowAISelection()
    {
        if(username.text == "")
        {
            username.text = "Player";
        }
        PlayerPrefs.SetString("Name", username.text);
        offlineButton.gameObject.SetActive(false); // Hide the offline button
        aiSelectionPanel.SetActive(true); // Show AI selection
    }

    public void StartGame(bool useRLAgent)
    {
        PlayerPrefs.SetInt("UseRLAgent", useRLAgent ? 1 : 0);
        PlayerPrefs.SetString("InputServerIp", "localhost");
        PlayerPrefs.SetInt("isClient", 0);
        PlayerPrefs.SetInt("offlineMode", 1);

        UnityEngine.SceneManagement.SceneManager.LoadScene(1);
    }
}