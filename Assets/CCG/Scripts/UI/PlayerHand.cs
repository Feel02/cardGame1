using UnityEngine;
using Mirror;
using Unity.VisualScripting;

public class PlayerHand : MonoBehaviour
{
    public GameObject panel;
    public HandCard cardPrefab;
    public Transform handContent;
    public PlayerType playerType;
    private Player player;
    private PlayerInfo enemyInfo;

    private bool isStarted = false;

    void Update()
    {
        player = Player.localPlayer;
         if (player && player.hasEnemy)
        {
           enemyInfo = player.enemyInfo;
          if (!isStarted && IsPlayerHand())
            {
                isStarted = true;
                player.deck.DrawCard(3);
            }
         }
        UpdateHandUI();
    }

    public void AddCard(int index)
    {
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(handContent, false);
        Debug.Log("Adding card to hand " + index + "size " + player.deck.hand.Count);
        CardInfo card = player.deck.hand[index];
        Debug.Log("Adding card to hand " + card.name);
        HandCard slot = cardObj.GetComponent<HandCard>();

        slot.AddCard(card, index, playerType);
    }

     public void AddCardDirectly(CardInfo card,int index){
        GameObject cardObj = Instantiate(cardPrefab.gameObject);
        cardObj.transform.SetParent(handContent, false);

        HandCard slot = cardObj.GetComponent<HandCard>();

        slot.AddCard(card, index, playerType);
    }

    public void RemoveCard(int index)
    {
        for (int i = index; i < handContent.childCount; ++i)
        {
            HandCard slot = handContent.GetChild(i).GetComponent<HandCard>();
            int count = i;
            if (count == index) slot.RemoveCard();
            else if (slot.handIndex > index) slot.handIndex--;
        }
    }

   bool IsEnemyHand() => player && player.hasEnemy && playerType == PlayerType.ENEMY;
   bool IsPlayerHand() => player && playerType == PlayerType.PLAYER;
      public void UpdateHandUI()
    {
        if (player == null) return;

         if (player.hasEnemy && playerType == PlayerType.ENEMY)
        {
              if (enemyInfo.handCount != handContent.childCount && enemyInfo.player != null && player != null)
                {
                   int cardCount = enemyInfo.handCount;
                    UIUtils.BalancePrefabs(cardPrefab.gameObject, enemyInfo.handCount, handContent);
                    for (int i = 0; i < enemyInfo.handCount; ++i)
                    {
                    HandCard slot = handContent.GetChild(i).GetComponent<HandCard>();
                    slot.AddCardBack();
                    }
                }
        }
         else if (playerType == PlayerType.PLAYER)
           {
                 if (player.deck.hand.Count != handContent.childCount)
                {
                   int cardCount = player.deck.hand.Count;
                    UIUtils.BalancePrefabs(cardPrefab.gameObject, player.deck.hand.Count, handContent);
                    for (int i = 0; i < player.deck.hand.Count; i++)
                    {
                         HandCard slot = handContent.GetChild(i).GetComponent<HandCard>();
                         slot.AddCard(player.deck.hand[i], i, playerType);
                    }
                }
           }
    }
}