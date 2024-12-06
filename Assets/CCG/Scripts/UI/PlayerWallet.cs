using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using UnityEngine.EventSystems;
using UnityEngine.XR;

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
                player.combat.CmdChangeMana(-manaCost);
                int index = card.handIndex;
                Player.gameManager.isSpawning = true;
                Player.gameManager.isHovering = false;
                Player.gameManager.CmdOnCardHover(0, index);
                AddCard(index);
                player.deck.CmdRemoveCard(index);
            }
        }
    }

    public void AddCard(int index)
    {
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(walletContent, false);
        Debug.Log("Adding card to wallet " + index + "size " + Player.gameManager.playerWallet.walletContent.childCount);
        CardInfo card = player.deck.hand[index];
        Debug.Log("Adding card to wallet " + card.name);
        HandCard slot = cardObj.GetComponent<HandCard>();
        slot.AddCard(card, Player.gameManager.playerWallet.walletContent.childCount - 1, playerType);
        slot.cardOutline.gameObject.SetActive(false);
        slot.cardDragHover.canDrag = true;
        slot.isInWallet = true;
        slot.handIndex = Player.gameManager.playerWallet.walletContent.childCount - 1;
    }

    public void AddCardDirectly(CardInfo card,int index){
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(walletContent, false);

        HandCard slot = cardObj.GetComponent<HandCard>();

        slot.AddCard(card, index, playerType);
    }

    public void RemoveCard(int index)
    {
        for (int i = index; i < walletContent.childCount; ++i)
        {
            HandCard slot = walletContent.GetChild(i).GetComponent<HandCard>();
            int count = i;
            if (count == index) slot.RemoveCard();
            else if (slot.handIndex > index) slot.handIndex--;
        }
    }

    

    bool IsEnemyHand() => player && player.hasEnemy && player.deck.hand.Count == 3 && playerType == PlayerType.ENEMY && enemyInfo.handCount != cardCount;
    bool IsPlayerHand() => player && player.deck.spawnInitialCards && playerType == PlayerType.PLAYER;
}