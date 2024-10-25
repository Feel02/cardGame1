using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class TimerScript : NetworkBehaviour
{
    [SyncVar] public float TimeLeft;
    [SyncVar] public bool TimerOn = false;

    public float duration = 41;

    public Text TimerTxt;

    void Update()
    {
        if (TimerOn)
        {
            if (TimeLeft > 0)
            {
                TimeLeft -= Time.deltaTime;
                UpdateTimer(TimeLeft);
            }
            else
            {
                Debug.Log("Time is UP!");
                TimeLeft = 0;
                TimerOn = false;
                CmdNotifyTimeUp(); // Notify clients that time is up
            }
        }
    }

    void UpdateTimer(float currentTime)
    {
        currentTime += 1;

        //float minutes = Mathf.FloorToInt(currentTime / 60);
        float seconds = Mathf.FloorToInt(currentTime % 60);

        TimerTxt.text = seconds.ToString("00");
    }

    [Command (ignoreAuthority = true)]      //ClientRpc
    void CmdNotifyTimeUp()
    {
        //Player.gameManager.endTurnButton.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        //Player.gameManager.CmdEndTurn();
        Player.gameManager.timer.StopTimer();
        Player.gameManager.RpcTakeDamageToSelf(1);
        Player.gameManager.RpcSetTurn();
        //get access to parent class of Player which is Entity
        //Player.localPlayer.combat.CmdChangeHealth(-1);
    }

    public void StartTimer()
    {
        TimeLeft = duration;
        TimerOn = true;
    }

    public void StopTimer()
    {
        TimerOn = false;
    }
}
