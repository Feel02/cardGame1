using UnityEngine;
using Mirror;
using System;

public class Deck : NetworkBehaviour
{
    [Header("Player")]
    public Player player;
    [HideInInspector] public int deckSize = 10;
    [HideInInspector] public int handSize = 3;

    [Header("Decks")]
    public SyncListCard deckList = new SyncListCard(); // DeckList used during the match. Contains all cards in the deck. This is where we'll be drawing card froms.
    public SyncListCard graveyard = new SyncListCard(); // Cards in player graveyard.
    public SyncListCard hand = new SyncListCard(); // Cards in player's hand during the match.

    [Header("Battlefield")]
    public SyncListCard playerField = new SyncListCard(); // Field where we summon creatures.
    public SyncListCard wallet = new SyncListCard(); // Cards in player's hand during the match.

    [Header("Starting Deck")]
    public CardAndAmount[] startingDeck;

    [HideInInspector] public bool spawnInitialCards = true;

    public void OnDeckListChange(SyncListCard.Operation op, int index, CardInfo oldCard, CardInfo newCard)
    {
        UpdateDeck(index, 1, newCard);
    }

    public void OnHandChange(SyncListCard.Operation op, int index, CardInfo oldCard, CardInfo newCard)
    {
        UpdateDeck(index, 2, newCard);
    }

    public void OnGraveyardChange(SyncListCard.Operation op, int index, CardInfo oldCard, CardInfo newCard)
    {
        UpdateDeck(index, 3, newCard);
    }

    public void UpdateDeck(int index, int type, CardInfo newCard)
    {
        // Deck List
        if (type == 1) deckList[index] = newCard;

        // Hand
        if (type == 2) hand[index] = newCard;

        // Gaveyard
        if (type == 3) graveyard[index] = newCard;

    }


    ///////////////
    public bool CanPlayCard(int manaCost)
    {
        return player.mana >= manaCost && player.health > 0;
    }

    public void DrawCard(int amount)
    {
        PlayerHand playerHand = Player.gameManager.playerHand;
        for (int i = 0; i < amount; ++i)
        {
            int index = i;
            playerHand.AddCard(index);
        }
        spawnInitialCards = false;
    }

    [Command (ignoreAuthority = true)]
    public void CmdPlayCard(CardInfo card, int index)
    {
        CreatureCard creature = (CreatureCard)card.data;
        //Debug.Log("Playing card " + card.name + " at index " + index + " card in the hand " + hand[index].name);
        GameObject boardCard = Instantiate(creature.cardPrefab.gameObject);
        FieldCard newCard = boardCard.GetComponent<FieldCard>();
        newCard.card = new CardInfo(card.data); // Save Card Info so we can re-access it later if we need to.
        newCard.cardName.text = card.name;
        newCard.health = creature.health;
        newCard.strength = creature.strength;
        newCard.image.sprite = card.image;
        newCard.image.color = Color.white;

        // If creature has charge, reduce waitTurn to 0 so they can attack right away.
        if (creature.hasCharge) newCard.waitTurn = 0;

        // Update the Card Info that appears when hovering
        newCard.cardHover.UpdateFieldCardInfo(card);

        // Spawn it
        NetworkServer.Spawn(boardCard);

        /* // Remove card from hand
        hand.RemoveAt(index); */
        wallet.RemoveAt(index);

        if (isServer) 
            RpcPlayCardField(boardCard,index); 
    }

    public void RestartHand(int[] indexes){

        for(int i = 0; i < 3; i++)
        {
            indexes[i] = UnityEngine.Random.Range(0, player.deck.deckList.Count);
        }
        
        if(hand.Count != 0)
            hand.Clear();

        for(int i = 0; i < 3; i++)
        {
            hand.Add(deckList[indexes[i]]);
        }
    }

    [Command (ignoreAuthority = true)]
    public void CmdRestartHandAndPlayerHand()
    {
        int[] indexes = new int[3];
        RestartHand(indexes);
        if (isServer) RpcClearClientHand(indexes);
    }

    [Command (ignoreAuthority = true)]
    public void CmdStartNewTurn()
    {
        if (player.mana < player.maxMana)
        {
            player.mana++;

            int[] indexes = new int[3];

            RestartHand(indexes);

            /* for (int i = 0; i < player.deck.hand.Count; i++)
            {
                Debug.Log(player.username + " has " + hand[i].name + " in hand as " + i);
            } */

            /* Debug.Log("server side size " + player.deck.hand.Count + " first card " + player.deck.hand[0].name);
            Debug.Log(player.username + " gained 1 mana. Total mana: " + player.mana);
            Debug.Log(player.username + " has " + player.deck.hand.Count + " cards " + player.deck.hand[0].name +" " + player.deck.hand[1].name + " " + player.deck.hand[2].name + " in hand"); */

            if (isServer) RpcClearClientHand(indexes);
        }
    }

    [ClientRpc]
    void RpcClearClientHand(int[] indexes)
    {   

        //Debug.Log(Player.localPlayer.username + " " + player.username + " " + Player.gameManager.isRefreshing);

        if(Player.gameManager.isRefreshing){

            //Debug.Log("client side size " + hand.Count + " first card " + hand[0].name);

            PlayerHand playerHand = Player.gameManager.playerHand;
            int size = playerHand.handContent.transform.childCount;

            /* for (int i = 0; i < hand.Count; i++)
            {
                Debug.Log(player.username + " has " + hand[i].name + " in hand as " + i);
            } */

            for(int i = 0; i < size; i++)
            {
                playerHand.RemoveCard(i);
            }

            for(int i = 0; i < 3; i++)
            {
                playerHand.AddCardDirectly(deckList[indexes[i]], i);
            }

            //Player.gameManager.isRefreshing = false;  
        }
    }

    [ClientRpc]
    public void RpcPlayCardField(GameObject boardCard, int index)
    {
        if (Player.gameManager.isSpawning)
        {
            // Set our FieldCard as a FRIENDLY creature for our local player, and ENEMY for our opponent.
            boardCard.GetComponent<FieldCard>().casterType = Target.FRIENDLIES;
            boardCard.transform.SetParent(Player.gameManager.playerField.content, false);
            Player.gameManager.playerWallet.RemoveCard(index); // Update player's wallet
            Player.gameManager.isSpawning = false;
            
        }
        else if (player.hasEnemy)
        {
            boardCard.GetComponent<FieldCard>().casterType = Target.ENEMIES;
            boardCard.transform.SetParent(Player.gameManager.enemyField.content, false);
            //Player.gameManager.enemyWallet.RemoveCard(index);
        }
    }

    [Command (ignoreAuthority = true)]
    public void CmdRemoveCard(int index)
    {
        Debug.Log("Removing card " + hand[index].name + " at index " + index);
        wallet.Add(hand[index]);
        hand.RemoveAt(index);
        if (isServer) RpcRemoveCard(index);

        if(hand.Count == 0){
            int[] arr = new int[3];
            RestartHand(arr);
            RpcClearClientHand(arr);
        }
    }

    [ClientRpc]
    void RpcRemoveCard(int index)
    {   
        Debug.Log("1Removing card " + hand[index].name + " at index " + index);
        if(Player.gameManager.isSpawning){
            Debug.Log("2Removing card " + hand[index].name + " at index " + index);
            PlayerHand playerHand = Player.gameManager.playerHand;
            playerHand.RemoveCard(index);  
        }
    }

}
