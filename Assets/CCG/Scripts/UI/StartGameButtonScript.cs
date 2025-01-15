using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using Unity.VisualScripting;

public class StartGameButtonScript : MonoBehaviour
{
    public TMPro.TMP_InputField serverIp;
    public TMPro.TMP_InputField username;

    public void Start(){
        if (PlayerPrefs.GetString("Name") != null) username.text = PlayerPrefs.GetString("Name");
        if (PlayerPrefs.GetString("InputServerIp") != null) serverIp.text = PlayerPrefs.GetString("InputServerIp");
    }
    public void StartGameClient()
    {
        if(serverIp.text == ""){
            serverIp.text = "localhost";
        }
        if(username.text == ""){
            username.text = "player";
        }
        PlayerPrefs.SetString("Name", username.text);
        PlayerPrefs.SetString("InputServerIp", serverIp.text);
        PlayerPrefs.SetInt("isClient", 0);

        UnityEngine.SceneManagement.SceneManager.LoadScene(1, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public void StartGameServer()
    {
        if(serverIp.text == ""){
            serverIp.text = "localhost";
        }
        if(username.text == ""){
            username.text = "server";
        }
        PlayerPrefs.SetString("Name", username.text);
        PlayerPrefs.SetString("InputServerIp", serverIp.text);
        PlayerPrefs.SetInt("isClient", 1);

        UnityEngine.SceneManagement.SceneManager.LoadScene(1, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
