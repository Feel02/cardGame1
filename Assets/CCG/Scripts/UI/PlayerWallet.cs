using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using System.Linq;
using System;

public class PlayerWallet : MonoBehaviour, IDropHandler
{
    public GameObject panel;
    public HandCard cardPrefab;
    public Transform walletContent;
    public PlayerType playerType;
    private Player player;
    private PlayerInfo enemyInfo;
    private int cardCount = 0; // Amount of cards in hand
    public Transform startingComp;

    void Update()
    {
        player = Player.localPlayer;

        if (player && player.hasEnemy)
        {
            enemyInfo = player.enemyInfo;
        }

        if (IsEnemyHand())
        {
            // instantiate/destroy enough slots
            UIUtils.BalancePrefabs(cardPrefab.gameObject, enemyInfo.walletCount, walletContent);

            // refresh all members
            for (int i = 0; i < enemyInfo.walletCount; ++i)
            {
                HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();

                slot.AddCardBack();

                cardCount = enemyInfo.walletCount;
            }
        }
    }

    public void OnDrop(PointerEventData eventData){
        HandCard card = eventData.pointerDrag.transform.GetComponent<HandCard>();
        if (card == null) return;

        Player player = Player.localPlayer;
        int manaCost = card.cost.text.ToInt();

        // Check the starting point of the drag
        Transform startingParent = card.originalParent;

        // You can now make decisions based on the startingParent
        if (startingParent != startingComp)
        {
            Debug.Log("Card was not dragged from the player's hand. Rejecting drop.");
            return;
        }

        if (player.IsOurTurn() && player.deck.CanPlayCard(manaCost))
        {
            if(Player.gameManager.playerWallet.walletContent.childCount < 6){
                player.deck.CmdChangeMana(-manaCost);
                int index = card.handIndex;
                CheckIfSameCardExists(index);
            }
        }
    }

    public void CheckIfSameCardExists(int index){
        Debug.Log("Checking if same card exists");
        Player.gameManager.isSpawning = true;
        Player.gameManager.isHovering = false;
        Player.gameManager.CmdOnCardHover(0, index);
        CardInfo card = player.deck.hand[index];
        //Debug.Log("0Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
        //Debug.Log("Card name " + card.name);
        AddCard(index);
        //Debug.Log("1Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
        player.deck.CmdRemoveCardFromHand(index);
        //Debug.Log("2Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);

        //create a scriptablecards array from the starting deck.card items
        ScriptableCard[] startDeck = new ScriptableCard[player.deck.startingDeck.Length];
        for (int i = 0; i < player.deck.startingDeck.Length; i++)
        {
            startDeck[i] = player.deck.startingDeck[i].card;
        }

        for (int i = 0; i < walletContent.childCount - 1; ++i)
        {
            HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
            if(slot.cardName.text == card.name){
                if(Array.IndexOf(startDeck, card.data) == 0){ 
                    AddCardFromStartingDeck(1);
                    RemoveCard(walletContent.childCount - 2);
                    RemoveCard(i);
                    player.deck.CmdRemoveCardFromWallet(walletContent.childCount - 2);
                    player.deck.CmdRemoveCardFromWallet(i);
                    return;
                }
                else if(Array.IndexOf(startDeck, card.data) == 2){
                    AddCardFromStartingDeck(3);
                    RemoveCard(walletContent.childCount - 2);
                    RemoveCard(i);
                    player.deck.CmdRemoveCardFromWallet(walletContent.childCount - 2);
                    player.deck.CmdRemoveCardFromWallet(i);
                    return;
                }
            }
        }
    }

    public void AddCard(int index)
    {
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(walletContent, false);
        CardInfo card = player.deck.hand[index]; 
        HandCard slot = cardObj.GetComponent<HandCard>();
        //Debug.Log("Index Of " + card.name + " to wallet is " + player.deck.wallet.Count); 
        slot.AddCard(card, player.deck.wallet.Count, playerType);
        slot.cardOutline.gameObject.SetActive(false);
        slot.cardDragHover.canDrag = true;
        slot.isInWallet = true;
    }
    public void AddCardFromStartingDeck(int index){
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(walletContent, false);
        HandCard slot = cardObj.GetComponent<HandCard>();
        CardInfo cardInfo = new CardInfo(player.deck.startingDeck[index].card, 1);     
        slot.AddCard(cardInfo, player.deck.wallet.Count + 1, playerType);       //walletContent.childCount - 1
        slot.cardOutline.gameObject.SetActive(false);
        slot.cardDragHover.canDrag = true;
        slot.isInWallet = true;
        player.deck.CmdAddCardToWallet(index); 
    }

    public void RemoveCard(int index)
    {
        for (int i = index; i < walletContent.childCount; ++i)
        {
            HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
            int count = i;
            if (count == index){ slot.RemoveCard(); }
            else if (slot.handIndex > index) slot.handIndex--;
        }
    }

    bool IsEnemyHand() => player && player.hasEnemy && playerType == PlayerType.ENEMY && enemyInfo.handCount != cardCount;
    bool IsPlayerHand() => player && player.deck.spawnInitialCards && playerType == PlayerType.PLAYER;
}