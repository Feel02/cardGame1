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
        int isPlayerWinner = PlayerPrefs.GetInt("isPlayerWinner", -1);
        
        if (isPlayerWinner == 1)
        {
            endGameText.text = "You Win!";
        }
        else if (isPlayerWinner == 0)
        {
            endGameText.text = "You Lose!";
        }
        else
        {
            endGameText.text = "Game Over!";
        }
    }

    public void EndGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}
