using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class EndGameButtonScript : MonoBehaviour
{
    public Text endGameText;
    // get the background and change the color to green if winning
    public Image backgroundImage;
    public Color winColor;

    public void Start()
    {
        //int isPlayerWinner = PlayerPrefs.GetInt("isPlayerWinner", -1);

        string winnerName = PlayerPrefs.GetString("winnerName", "No Winner");

        if (winnerName == "AI Player")
        {
            endGameText.text = "You Lose!";
        }
        else if (winnerName == Player.localPlayer.username)
        {
            endGameText.text = "You Win!";
            // Change the background color to green
            backgroundImage.color = winColor;
        }
        else if (winnerName == Player.localPlayer.enemyInfo.username)
        {
            endGameText.text = "You Lose!";
        }
        else
        {
            endGameText.text = "You Lose!";
        }
    }

    public void EndGame()
    {
        // Stop the network host/server/client if running
        if (NetworkManager.singleton != null)
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopHost();
            }
            else if (NetworkServer.active)
            {
                NetworkManager.singleton.StopServer();
            }
            else if (NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
            }
            // Destroy the NetworkManager GameObject to ensure a clean state
            Destroy(NetworkManager.singleton.gameObject);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
