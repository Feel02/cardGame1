using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine.Analytics;
using System.Linq;

public class GameManager : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 30;

    [Header("Mana")]
    public int maxMana = 100;

    [Header("Hand")]
    public int handSize = 3;
    public PlayerHand playerHand;
    public PlayerHand enemyHand;

    [Header("Deck")]
    public int deckSize = 10; // Maximum deck size
    public int identicalCardCount = 2; // How many identical cards we allow to have in a deck

    [Header("Battlefield")]
    public PlayerField playerField;
    public PlayerField enemyField;

    public PlayerWallet playerWallet;
    public PlayerWallet enemyWallet;

    [Header("Turn Management")]
    public GameObject endTurnButton;
    [HideInInspector] public bool isOurTurn = false;
    [SyncVar, HideInInspector] public int turnCount = 1; // Start at 1

    // isHovering is only set to true on the Client that called the OnCardHover function.
    // We only want the hovering to appear on the enemy's Client, so we must exclude the OnCardHover caller from the Rpc call.
    [HideInInspector] public bool isHovering = false;
    [HideInInspector] public bool isHoveringField = false;
    [HideInInspector] public bool isSpawning = false;
    [HideInInspector] public bool isRefreshing = false;
    public TimerScript timer; // Reference to the TimerScript

    public SyncListPlayerInfo players = new SyncListPlayerInfo(); // Information of all players online. One is player, other is opponent.

    // Not sent from Player / Object with Authority, so we need to ignoreAuthority. 
    // We could also have this command run on the Player instead
    [Command(ignoreAuthority = true)]
    public void CmdOnCardHover(float moveBy, int index)
    {
        // Only move cards if there are any in our opponent's opponent's hand (our hand from our opponent's point of view).
        if (enemyHand.handContent.transform.childCount > 0 && isServer) RpcCardHover(moveBy, index);
    }

    [ClientRpc]
    public void RpcCardHover(float moveBy, int index)
    {
        // Only move card for the player that isn't currently hovering
        if (!isHovering)
        {
            HandCard card = enemyHand.handContent.transform.GetChild(index).GetComponent<HandCard>();
            card.transform.localPosition = new Vector2(card.transform.localPosition.x, moveBy);
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdOnFieldCardHover(GameObject cardObject, bool activateShine, bool targeting)
    {
        /*
        FieldCard card = cardObject.GetComponent<Card>();
        card.shine.gameObject.SetActive(true);*/
        if (isServer) RpcFieldCardHover(cardObject, activateShine, targeting);
    }

    [ClientRpc]
    public void RpcFieldCardHover(GameObject cardObject, bool activateShine, bool targeting)
    {
        if (!isHoveringField)
        {
            FieldCard card = cardObject.GetComponent<FieldCard>();
            Color shine = activateShine ? card.hoverColor : Color.clear;
            card.shine.color = targeting ? card.targetColor : shine;
            //card.shine.gameObject.SetActive(activateShine);
        }
    }

    public void EndTurn()
    {
        timer.StopTimer();
        RpcSetTurn();
    }

    // Ends our turn and starts our opponent's turn.
    [Command(ignoreAuthority = true)]
    public void CmdEndTurn()
    {
        timer.StopTimer();
        RpcSetTurn();
    }

    [ClientRpc]
    public void RpcSetTurn()
    {
        // If isOurTurn was true, set it false. If it was false, set it true.
        isOurTurn = !isOurTurn;
        endTurnButton.SetActive(isOurTurn);

        // If isOurTurn (after updating the bool above)
        if (isOurTurn)
        {
            Player.localPlayer.deck.CmdStartNewTurn();
            isRefreshing = true;
            playerField.UpdateFieldCards();
            timer.StartTimer();
        }
        else{
            isRefreshing = false;
            timer.StopTimer();
        }
    }

    [ClientRpc]
    public void RpcTakeDamageToSelf(int amount)
    {
        if(isOurTurn)
            Player.localPlayer.combat.CmdChangeHealth(-amount);
    }    

    [Command(ignoreAuthority = true)]
    public void CmdChangeFirstPlayer(bool firstPlayer)
    {
        players[0].player.GetComponent<Player>().ChangeFirstPlayer(firstPlayer);
        players[1].player.GetComponent<Player>().ChangeFirstPlayer(!firstPlayer);
    }

    [Command(ignoreAuthority = true)]
    public void CmdAddPlayerToPlayersList(PlayerInfo player){
        if(!players.Contains(player)){
            player.data.mana = 1;
            players.Add(player);
        }
    }

    public void StartGame()
    {
        Player player = Player.localPlayer;
        try{
            //player.mana = 1;
            player.enemyInfo.data.mana = 1;
            
            if(player.firstPlayer){
                endTurnButton.SetActive(true);
                isOurTurn = true;
                isRefreshing = true;
                timer.StartTimer();
            }
        } catch {
            Debug.Log("A player trying to access somewhere they shouldn't but don't worry, I can fix her.");
        }
    }
}