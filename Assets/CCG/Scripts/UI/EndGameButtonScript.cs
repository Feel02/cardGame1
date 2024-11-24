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
        int playerScore  =  PlayerPrefs.GetInt("playerHealth");
            
        if (playerScore > 0)
        {
            endGameText.text = "You Win!";
        }
        //if the player is the loser
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
