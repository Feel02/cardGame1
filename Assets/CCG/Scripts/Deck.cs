using UnityEngine;
using Mirror;
using System;
using System.Linq;

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
        if (type == 1) deckList[index] = newCard; //startingDeck'ten ismini bulup deckList'ten kartı bulup karıt çekmen lazım

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
        //Debug.Log("SDNVSDLLVSDMVKDSMKVMSDKLMLVSDKMVLSKDMVLKSLSMVD");
        wallet.RemoveAt(index);
        //Player.gameManager.playerWallet.RemoveCard(index);

        if (isServer) 
            RpcPlayCardField(boardCard,index); 
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

      [ClientRpc]
    void RpcClearClientHand(int[] indexes)
    {   

        if(Player.gameManager.isRefreshing){
            PlayerHand playerHand = Player.gameManager.playerHand;
            int size = playerHand.handContent.transform.childCount;
            for(int i = 0; i < size; i++)
            {
                playerHand.RemoveCard(i);
            }

            for(int i = 0; i < 3; i++)
            {
                playerHand.AddCardDirectly(deckList[indexes[i]], i);
            }
        }
    }


    [Command(ignoreAuthority = true)]
    public void CmdChangeMana(int amount)
    {
        // Increase mana by amount. If 3, increase by 3. If -3, reduce by 3.
        if (player.mana < player.maxMana) player.mana += amount;
    }

    [Command (ignoreAuthority = true)]
    public void CmdStartNewTurn()
    {
        if (player.mana < player.maxMana)
        {
            player.mana++;

            int[] indexes = new int[3];

            RestartHand(indexes);

            if (isServer) RpcClearClientHand(indexes);
        }
    }

    [Command (ignoreAuthority = true)]
    public void CmdRemoveCardFromHand(int index)
    {
        Debug.Log("0Removing card " + hand[index].name + " at index " + index + " from hand");
        wallet.Add(hand[index]);
        hand.RemoveAt(index);
        if (isServer) RpcRemoveCardFromHand(index);
       if(hand.Count == 0){
            int[] arr = new int[3];
            RestartHand(arr);
            RpcClearClientHand(arr);
        }
    }

    [ClientRpc]
    void RpcRemoveCardFromHand(int index)
    {   
        if(Player.gameManager.isSpawning){
            PlayerHand playerHand = Player.gameManager.playerHand;
            playerHand.RemoveCard(index);  
        }
    }

    [Command (ignoreAuthority = true)]
    public void CmdAddCardToWallet(int index)
    {
        String name = startingDeck[index].card.cardName;
        //create a namelist[] from the decklist's names
        String[] nameList = new String[deckList.Count];
        for (int i = 0; i < deckList.Count; i++)
        {
            nameList[i] = deckList[i].data.cardName;
        }
        int inx = Array.IndexOf(nameList, name);
        wallet.Add(deckList[inx]);
    }

    [Command (ignoreAuthority = true)]
    public void CmdRemoveCardFromWallet(int index)
    {

        wallet.RemoveAt(index);
    }

    public void PlayCardLocally(CardInfo card, int index)
    {
        CreatureCard creature = (CreatureCard)card.data;
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

        // Set card to be an enemy for the local player (since AI is the opponent)
        newCard.casterType = Target.ENEMIES;
        boardCard.transform.SetParent(Player.gameManager.enemyField.content, false);

        // Remove the card from the AI's wallet
        wallet.RemoveAt(index);
    }
    [Command (ignoreAuthority = true)]
    public void CmdUpdateAIBoughtCard()
    {
        if (isServer) RpcUpdateAIBoughtCard();
    }

    [ClientRpc]
    void RpcUpdateAIBoughtCard()
    {
        if(player.hasEnemy){
            Player.gameManager.enemyWallet.UpdateWalletUI();
        }
    }
    
     [Command (ignoreAuthority = true)]
    public void CmdUpdatePlayerHand()
    {
         if (isServer) RpcUpdatePlayerHand();
    }

    [ClientRpc]
    void RpcUpdatePlayerHand()
    {
        if(player.hasEnemy){
            Player.gameManager.playerHand.UpdateHandUI();
        }
        else {
            Player.gameManager.playerHand.UpdateHandUI();
        }
    }
    
    [Command (ignoreAuthority = true)]
    public void CmdUpdateAIHand()
    {
         if (isServer) RpcUpdateAIHand();
    }

    [ClientRpc]
    void RpcUpdateAIHand()
    {
        if(player.hasEnemy){
            Player.gameManager.enemyHand.UpdateHandUI();
        }
    }
}