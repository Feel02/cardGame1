using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using Unity.VisualScripting;

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

    public ScriptableCard[] startDeck;
    void Update()
    {
        player = Player.localPlayer;

        if (player && player.hasEnemy)
        {
            enemyInfo = player.enemyInfo;
        }
        UpdateWalletUI();
    }

    public void OnDrop(PointerEventData eventData){
        UpdateStartDeck();
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
        //Debug.Log("Checking if same card exists");
        Player.gameManager.isSpawning = true;
        Player.gameManager.isHovering = false;
        Player.gameManager.CmdOnCardHover(0, index);
        CardInfo card = player.deck.hand[index];
        //Debug.Log("0Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
        //Debug.Log("Card name " + card.name);
        AddCard(card, player.deck.wallet.Count, playerType);
        //Debug.Log("1Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
        player.deck.CmdRemoveCardFromHand(index);
        //Debug.Log("2Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
        //create a scriptablecards array from the starting deck.card items
        

        for (int i = 0; i < walletContent.childCount - 1; ++i)
        {
            HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
            if(slot.cardName.text == card.name){
                if(Array.IndexOf(startDeck, card.data) == 0){
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count); 
                    //Debug.Log("Removing card " + walletContent.GetChild(player.deck.wallet.Count-1).GetComponent<HandCard>().cardName.text + " from wallet");
                    RemoveCardById(walletContent.GetChild(player.deck.wallet.Count-1).GetComponent<HandCard>().GetInstanceID());
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
                    //Debug.Log("Removing card " + slot.cardName.text + " from wallet");
                    RemoveCardById(slot.GetInstanceID());
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
                    StartCoroutine(DelayedStartHost(1));
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
                    //player.deck.CmdUpdatePlayerHand();
                    cardCount--;
                    UpdateWalletUI();
                    //Debug.Log("Updating wallet UI " + player.deck.wallet.Count + " " + walletContent.childCount);
                    return;
                }
                else if(Array.IndexOf(startDeck, card.data) == 2){
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count); 
                    //Debug.Log("Removing card " + walletContent.GetChild(player.deck.wallet.Count-1).GetComponent<HandCard>().cardName.text + " from wallet");
                    RemoveCardById(walletContent.GetChild(player.deck.wallet.Count-1).GetComponent<HandCard>().GetInstanceID());
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
                    //Debug.Log("Removing card " + slot.cardName.text + " from wallet");
                    RemoveCardById(slot.GetInstanceID());
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
                    StartCoroutine(DelayedStartHost(3));
                    //Debug.Log("5Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
                    //player.deck.CmdUpdatePlayerHand();
                    cardCount--;
                    UpdateWalletUI();
                    //Debug.Log("Updating wallet UI " + player.deck.wallet.Count + " " + walletContent.childCount);
                    return;
                }
            }
        }
        //player.deck.CmdUpdatePlayerHand();
        
    }

    IEnumerator DelayedStartHost(int num)
    {
        // Wait for half a second before starting the host
        yield return new WaitForSeconds(0.1f);
        AddCardFromStartingDeck(num);
        //yield return new WaitForSeconds(1f);
    }

    public void AddCard(CardInfo card, int index, PlayerType type)
    {
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(walletContent, false);
        HandCard slot = cardObj.GetComponent<HandCard>();
        slot.AddCard(card, index, type);
        slot.cardOutline.gameObject.SetActive(false);
        slot.cardDragHover.canDrag = true;
        slot.isInWallet = true;
    }
    public void AddCardFromStartingDeck(int index){
        //Debug.Log("4Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(walletContent, false);
        HandCard slot = cardObj.GetComponent<HandCard>();
        CardInfo cardInfo = new CardInfo(player.deck.startingDeck[index].card, 1);
        slot.AddCard(cardInfo, player.deck.wallet.Count, playerType);       //walletContent.childCount - 1
        slot.cardOutline.gameObject.SetActive(false);
        slot.cardDragHover.canDrag = true;
        slot.isInWallet = true;
        player.deck.CmdAddCardToWallet(index);
        //Debug.Log("4Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
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

    public void RemoveCardById(int id)
    {
        //Debug.Log("3Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
        for (int i = 0; i < walletContent.childCount; ++i)
        {
            HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
            if (slot.GetInstanceID() == id){
                player.deck.CmdRemoveCardFromWallet(i);
                for (int j = i; j < walletContent.childCount; ++j)
                {
                    HandCard slot2 = walletContent.GetChild(j).GetComponent<HandCard>();
                    int count = j;
                    if (count == i) { slot2.RemoveCard(); }
                    else if (slot2.handIndex > i) slot2.handIndex--;
                }
                //Debug.Log("33Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);

                return;
            }
        }
        //Debug.Log("3Size of The wallet " + walletContent.childCount + " " + player.deck.wallet.Count);
    }

    bool IsEnemyHand() => player && player.hasEnemy && playerType == PlayerType.ENEMY && enemyInfo.walletCount != cardCount;
    bool IsPlayerHand() => player && playerType == PlayerType.PLAYER;
    public void UpdateStartDeck(){
        player = Player.localPlayer;

        if (startDeck != null) Array.Clear(startDeck, 0, startDeck.Length);
        startDeck = new ScriptableCard[player.deck.startingDeck.Length];
        for (int i = 0; i < player.deck.startingDeck.Length; i++)
        {
            //Debug.Log("Adding card " + player.deck.startingDeck[i].card.name + " to start deck");
            startDeck[i] = player.deck.startingDeck[i].card;
        }
    }
    public void UpdateWalletUI(){
        if (player && player.hasEnemy && playerType == PlayerType.ENEMY){
            if(enemyInfo.walletCount != cardCount){
                cardCount = enemyInfo.walletCount;
                UIUtils.BalancePrefabs(cardPrefab.gameObject, enemyInfo.walletCount, walletContent);
                for (int i = 0; i < enemyInfo.walletCount; ++i){
                    HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
                    slot.AddCardBack();
                }
            }
        }
        else if (player && playerType == PlayerType.PLAYER){
            if (player.deck.wallet.Count != walletContent.childCount){
                //Debug.Log("------------------------------------------");
                //Debug.Log("Updating wallet UI " + player.deck.wallet.Count + " " + walletContent.childCount);
                /* for(int i = 0; i < player.deck.wallet.Count; ++i){
                    Debug.Log("Wallet card " + player.deck.wallet[i].data.cardName);
                }
                for (int i = 0; i < walletContent.childCount; ++i){
                    Debug.Log("Wallet card " + walletContent.GetChild(i).GetComponent<HandCard>().cardName.text);    
                } */
                //Debug.Log("------------------------------------------");
                int cardCount = player.deck.wallet.Count;
                // Create new cards based on the current hand size
                UIUtils.BalancePrefabs(cardPrefab.gameObject, player.deck.wallet.Count, walletContent);
                for (int i = 0; i < player.deck.wallet.Count; i++){
                    HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
                    slot.AddCard(player.deck.wallet[i], i, playerType);
                    slot.cardOutline.gameObject.SetActive(false);
                    slot.cardDragHover.canDrag = true;
                    slot.isInWallet = true;
                }
            }
            /* if(player.deck.wallet.Count != cardCount){
                cardCount = player.deck.wallet.Count;
                UIUtils.BalancePrefabs(cardPrefab.gameObject, player.deck.wallet.Count, walletContent);
                for (int i = 0; i < player.deck.wallet.Count; i++){
                    HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
                    slot.AddCard(player.deck.wallet[i], i, playerType);
                    slot.cardOutline.gameObject.SetActive(false);
                    slot.cardDragHover.canDrag = true;
                    slot.isInWallet = true;
                }
            } */
        }
    }
}