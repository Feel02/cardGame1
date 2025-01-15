using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using System;

// Add this component to a GameObject in your scene
public class AIManager : MonoBehaviour
{
    public Player aiPlayer;

    public float aiTurnDelay = 2f; // Delay before AI takes its turn

    void Start()
    {
        if (PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            // Find the AI player in the scene
            Player[] players = FindObjectsOfType<Player>();
            foreach (Player p in players)
            {
                if (p.isAI)
                {
                    aiPlayer = p;
                    break;
                }
            }
        }
    }

    void Update()
    {
        // Check if it's the AI's turn and the game has started
        if (Player.gameManager != null && !Player.gameManager.isOurTurn && aiPlayer.hasEnemy && Player.gameManager.players.Count == 2 )
        {
            // Start the AI turn after a delay
            Invoke("AITurn", aiTurnDelay);
            // Ensure the AI only takes one turn per player turn
            enabled = false; 
        }
    }

    void AITurn()
    {
        // 1. Buy Cards (if possible)
        BuyAffordableCards();

        // 2. Play Cards Randomly (if possible)
        PlayRandomCardFromWallet();

        // 3. End Turn
        //Player.gameManager.CmdEndTurn(); //Change this line with below one
        Player.gameManager.EndTurn();

        // Re-enable the script for the next AI turn
        enabled = true;
    }



    void BuyAffordableCards()
    {
        //Debug.Log("AI buying cards...");
        if (aiPlayer.deck.wallet.Count < 6)                                            //Added if statement
        {
            List<int> affordableCardIndices = new List<int>();
            for (int i = 0; i < aiPlayer.deck.startingDeck.Length; i++)
            {
                if (aiPlayer.deck.startingDeck[i].card.cost <= aiPlayer.mana)
                {
                    affordableCardIndices.Add(i);
                }
            }

            if (affordableCardIndices.Count > 0)
            {
                int randomCardIndex = affordableCardIndices[UnityEngine.Random.Range(0, affordableCardIndices.Count)];
                int cardCost = aiPlayer.deck.startingDeck[randomCardIndex].card.cost;
                //Debug.Log("AI attempting to buy card: " + aiPlayer.deck.startingDeck[randomCardIndex].card.name);

                aiPlayer.deck.CmdChangeMana(-cardCost);
                aiPlayer.deck.CmdAddCardToWallet(randomCardIndex);
                aiPlayer.UpdateEnemyInfo();// Add the bought card to the AI's wallet

            }
            else{
                //Debug.Log("AI can't afford any cards.");
            }
        }
    }



    void PlayRandomCardFromWallet()
    {
        //Debug.Log("AI playing cards...");
        if (aiPlayer.deck.wallet.Count > 0)
        {

            int randomCardIndex = UnityEngine.Random.Range(0, aiPlayer.deck.wallet.Count);
            //Debug.Log("AI attempting to play card: " + aiPlayer.deck.wallet[randomCardIndex].name);

            Player.gameManager.isSpawning = true;
            Player.gameManager.isHovering = false;            
            aiPlayer.deck.CmdPlayCard(aiPlayer.deck.wallet[randomCardIndex], randomCardIndex); // Play card from Wallet onto board
            //Debug.Log("AI played card: " + aiPlayer.deck.wallet[randomCardIndex].name);

            //Attack with random creature on field
            AttackWithRandomCreature();

        }
        else{
            //Debug.Log("AI has no cards to play.");
        }
    }


    void AttackWithRandomCreature()
    {
        //Debug.Log("AI attacking with creature...");
        // Find all AI creatures on the field
        FieldCard[] aiCreatures = FindObjectsOfType<FieldCard>().Where(c => c.casterType == Target.ENEMIES && c.CanAttack()).ToArray();

        if (aiCreatures.Length > 0)
        {
            // Choose a random creature to attack with
            FieldCard attacker = aiCreatures[UnityEngine.Random.Range(0, aiCreatures.Length)];

            // Find a random target (opponent or enemy creature)
            List<Entity> potentialTargets = new List<Entity>();
            potentialTargets.Add(Player.localPlayer); // Add the player as a potential target
            potentialTargets.AddRange(FindObjectsOfType<FieldCard>().Where(c => c.casterType == Target.FRIENDLIES));

            if (potentialTargets.Count > 0)
            {
                Entity target = potentialTargets[UnityEngine.Random.Range(0, potentialTargets.Count)];

                bool canTarget = target.casterType.CanTarget(attacker.card.acceptableTargets);
                if (canTarget)
                    // Attack the target
                    ((CreatureCard)attacker.card.data).Attack(attacker, target);
            }
        }
        else{
            //Debug.Log("AI has no creatures to attack with.");
        }
    }
}