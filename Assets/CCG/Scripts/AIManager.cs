using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using System;

public class AIManager : MonoBehaviour
{
    public Player aiPlayer;
    public float aiTurnDelay = 2f;
    private bool aiInitialized = false;

    void Start()
    {
        aiPlayer = GetComponent<Player>();
        if (aiPlayer == null)
        {
            Debug.LogError("AIManager: Player component missing!");
            enabled = false;
        }
        
        Sprite[] sprites = Resources.LoadAll<Sprite>("Portraits/Trainer");
        print(sprites.Length);
        aiPlayer.portrait = sprites[5];
        aiPlayer.username = "AI Player";
    }

    void Update()
    {
        // Check if aiPlayer is null or destroyed to prevent MissingReferenceException
        if (aiPlayer == null || aiPlayer.gameObject == null)
        {
            enabled = false;
            return;
        }

        if (Player.gameManager != null && !Player.gameManager.isOurTurn)
        {
            if (!aiInitialized)
            {
                InitializeAI();
            }

            Invoke("AITurn", aiTurnDelay);
            enabled = false;
        }
    }

    void InitializeAI()
    {
        // Fill deck from startingDeck array
        for (int i = 0; i < aiPlayer.deck.startingDeck.Length; ++i)
        {
            CardAndAmount card = aiPlayer.deck.startingDeck[i];
            CreatureCard creature = (CreatureCard)card.card;
            for (int v = 0; v < creature.amount; v++)                     //card.amount instead of 3
            {
                aiPlayer.deck.deckList.Add(card.amount > 0 ? new CardInfo(card.card, 1) : new CardInfo());
            }
            if (aiPlayer.deck.hand.Count < 3) aiPlayer.deck.hand.Add(new CardInfo(card.card, 1));
        }
        if (aiPlayer.deck.hand.Count == 3)
        {
            aiPlayer.deck.hand.Shuffle();
        }

        DrawInitialHand();
        aiInitialized = true;
    }

    void DrawInitialHand()
    {
       
        int[] indexes = new int[3];

        for (int i = 0; i < 3; i++)
        {
            indexes[i] = UnityEngine.Random.Range(0, aiPlayer.deck.deckList.Count);
        }

        if (aiPlayer.deck.hand.Count != 0)
            aiPlayer.deck.hand.Clear();

        for (int i = 0; i < 3; i++)
        {
            aiPlayer.deck.hand.Add(aiPlayer.deck.deckList[indexes[i]]);
        }
    }

    void AITurn()
    {
        // Check again if aiPlayer is null before proceeding
        if (aiPlayer == null || aiPlayer.gameObject == null)
        {
            enabled = false;
            return;
        }
        aiPlayer.mana += 1;

        // 1. Restart hand
        int[] indexes = new int[3];
        aiPlayer.deck.RestartHand(indexes);
        aiPlayer.deck.CmdUpdateAIHand();

        // 2. Buy and Play Cards from Wallet
        BuyAndPlayCards();

        //Attack with random creature on field
        AttackWithRandomCreature();

        // 3. End Turn
        Player.gameManager.CmdEndTurn();
        enabled = true;
    }

   void BuyAndPlayCards()
    {
        bool isItNewCard = false;
        if(aiPlayer.deck.wallet.Count < 6)
        {
            bool cardBoughtAndPlayed = BuyAndPlayCard();
            isItNewCard = cardBoughtAndPlayed;
            if(UnityEngine.Random.Range(0, 10) < 4) PlayRandomCardFromWallet();
        }

        if(!isItNewCard)
            if(UnityEngine.Random.Range(0, 10) < 5) 
                PlayRandomCardFromWallet();
    }

    bool BuyAndPlayCard()
    {

        List<int> affordableCardIndices = new List<int>();
        List<CardInfo> handCopy = new List<CardInfo>(aiPlayer.deck.hand);

        for(int i = 0; i < handCopy.Count; i++)
        {
            if (handCopy[i].cost.ToInt() <= aiPlayer.mana)
            {
                affordableCardIndices.Add(i);
            }
        }

        foreach(int index in affordableCardIndices)
        {
            if(aiPlayer.deck.wallet.Count == 6) continue;

            int cardCost = handCopy[index].cost.ToInt();
            // Check if the AI can afford the card
            if (aiPlayer.mana >= cardCost)
            {
                aiPlayer.mana -= cardCost;

                aiPlayer.deck.wallet.Add(handCopy[index]);
                //AI must remove the card from the hand, the player must see it
                aiPlayer.deck.hand.RemoveAt(index);
                aiPlayer.UpdateEnemyInfo(); // Ensure UI updates
                aiPlayer.deck.CmdUpdateAIBoughtCard(); // Updates Player's wallet to show card has moved.
                return true;
            }
        }
        return false; // No card can be bought and played
    }

   void PlayRandomCardFromWallet()
    {
        if (aiPlayer.deck.wallet.Count > 0)
        {
            int randomCardIndex = UnityEngine.Random.Range(0, aiPlayer.deck.wallet.Count);

            Player.gameManager.isSpawning = true;
            Player.gameManager.isHovering = false;
            //aiPlayer.deck.PlayCardLocally(aiPlayer.deck.wallet[randomCardIndex], randomCardIndex);
            aiPlayer.deck.CmdPlayCard(aiPlayer.deck.wallet[randomCardIndex], randomCardIndex);

            aiPlayer.deck.playerField.Clear();
            //get the field cards from the component named as EnemyFieldContent
            FieldCard[] fieldcards = GameObject.Find("EnemyFieldContent").GetComponent<Transform>().GetComponentsInChildren<FieldCard>();
            aiPlayer.deck.playerField.AddRange(fieldcards.Select(fc => new CardInfo(fc.card.data, 1)));
        }

    }

    void AttackWithRandomCreature()
    {
        FieldCard[] aiCreatures = GameObject.Find("EnemyFieldContent").GetComponent<Transform>().GetComponentsInChildren<FieldCard>();

        if (aiCreatures.Length > 0)
        {
            foreach (FieldCard attacker in aiCreatures)
            {
                List<Entity> potentialTargets = new List<Entity>();
                potentialTargets.Add(Player.localPlayer);
                potentialTargets.AddRange(GameObject.Find("PlayerFieldContent").GetComponent<Transform>().GetComponentsInChildren<FieldCard>());

                if (potentialTargets.Count > 0)
                {
                    Entity target = potentialTargets[UnityEngine.Random.Range(0, potentialTargets.Count)];

                    /* bool canTarget = target.casterType.CanTarget(attacker.card.acceptableTargets);
                    if (canTarget) */
                    ((CreatureCard)attacker.card.data).Attack(attacker, target);
                }   
            }
        }
    }
}