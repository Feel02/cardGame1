using UnityEngine.EventSystems;
using UnityEngine;

public class PlayerWallet : MonoBehaviour, IDropHandler
{
    public Transform content;

    public void OnDrop(PointerEventData eventData)
    {
        HandCard card = eventData.pointerDrag.transform.GetComponent<HandCard>();
        Player player = Player.localPlayer;
        int manaCost = card.cost.text.ToInt();

        //
        if (player.IsOurTurn() && player.deck.CanPlayCard(manaCost))
        {
            int index = card.handIndex;
            //Debug.Log("dsvsdvdfdfv " + Player.gameManager.playerHand.handContent.transform.GetChild(0).GetComponent<HandCard>().cardName.text);
            CardInfo cardInfo = player.deck.hand[index];
            Debug.LogError(index + " / " + cardInfo.name);
            Player.gameManager.isSpawning = true;
            Player.gameManager.isHovering = false;
            Player.gameManager.CmdOnCardHover(0, index);
            player.deck.CmdPlayCard(cardInfo, index, false); // Summon card onto the board
            player.combat.CmdChangeMana(-manaCost); // Reduce player's mana
        }
    }

    public void RemoveCard(int index)
    {
        for (int i = index; i < content.childCount; ++i)
        {
            HandCard slot = content.GetChild(i).GetComponent<HandCard>();
            int count = i;
            if (count == index) slot.RemoveCard();
            else if (slot.handIndex > index) slot.handIndex--;
        }
    }
}
