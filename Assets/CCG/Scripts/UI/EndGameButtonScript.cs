using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class EndGameButtonScript : MonoBehaviour
{
    public Text endGameText;
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
        UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
