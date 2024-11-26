using UnityEngine.EventSystems;
using UnityEngine;

public class PlayerField : MonoBehaviour, IDropHandler
{
    public Transform content;

    public Transform startingComp;

    public void OnDrop(PointerEventData eventData)
    {
        HandCard card = eventData.pointerDrag.transform.GetComponent<HandCard>();
        if (card == null) return;

        Player player = Player.localPlayer;
        int manaCost = card.cost.text.ToInt();

        // Check the starting point of the drag
        Transform startingParent = card.originalParent;

        // You can now make decisions based on the startingParent
        if (startingParent != startingComp)
        {
            Debug.Log("Card was not dragged from the player's wallet. Rejecting drop.");
            return;
        }

        if (player.IsOurTurn())
        {
            int index = card.handIndex;
            CardInfo cardInfo = player.deck.wallet[index];
            Player.gameManager.isSpawning = true;
            Player.gameManager.isHovering = false;
            Player.gameManager.CmdOnCardHover(0, index);
            player.deck.CmdPlayCard(cardInfo, index); // Summon card onto the board
        }
    }


    public void UpdateFieldCards()
    {
        Player.gameManager.isRefreshing = true;
        int cardCount = content.childCount;
        for (int i = 0; i < cardCount; ++i)
        {
            FieldCard card = content.GetChild(i).GetComponent<FieldCard>();
            card.CmdUpdateWaitTurn();
        }
    }
}
