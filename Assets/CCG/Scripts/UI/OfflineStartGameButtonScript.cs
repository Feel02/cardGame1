using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Unity.VisualScripting;

public class OfflineStartGameButtonScript : MonoBehaviour
{
    public TMPro.TMP_InputField username;

    public void Start(){
        if (PlayerPrefs.GetString("Name") != null) username.text = PlayerPrefs.GetString("Name");
    }
    public void StartGame()
    {
        if(username.text == ""){
            username.text = "AI Opponent"; // Or some default AI name
        }
        PlayerPrefs.SetString("Name", username.text);
        PlayerPrefs.SetString("InputServerIp", "localhost");
        PlayerPrefs.SetInt("isClient", 0);
        PlayerPrefs.SetInt("offlineMode", 1);

        UnityEngine.SceneManagement.SceneManager.LoadScene(1, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}