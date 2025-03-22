using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class RL_AIManager : MonoBehaviour
{
    public Player aiPlayer;
    public float aiTurnDelay = 2f;
    private bool aiInitialized = false;

    // Q-Learning Parameters
    private Dictionary<string, float> qTable = new Dictionary<string, float>();
    private float learningRate = 0.1f;
    private float discountFactor = 0.9f;
    private float explorationRate = 0.3f;
    
    // State Tracking
    private string previousState;
    private string previousAction;
    private int previousPlayerHealth;
    private int previousAIHealth;

    void Update()
    {
        if (aiPlayer == null || !aiPlayer.gameObject.activeInHierarchy)
        {
            enabled = false;
            return;
        }

        if (Player.gameManager != null && !Player.gameManager.isOurTurn)
        {
            if (!aiInitialized) InitializeAI();
            Invoke("AITurn", aiTurnDelay);
            enabled = false;
        }
    }

    void InitializeAI()
    {
        // Initialization code from original AIManager
        for (int i = 0; i < aiPlayer.deck.startingDeck.Length; ++i)
        {
            CardAndAmount card = aiPlayer.deck.startingDeck[i];
            CreatureCard creature = (CreatureCard)card.card;
            for (int v = 0; v < creature.amount; v++)
            {
                aiPlayer.deck.deckList.Add(new CardInfo(card.card, 1));
            }
            if (aiPlayer.deck.hand.Count < 3) 
                aiPlayer.deck.hand.Add(new CardInfo(card.card, 1));
        }
        aiPlayer.deck.hand.Shuffle();
        aiInitialized = true;
    }

    void AITurn()
    {
        previousPlayerHealth = Player.localPlayer.health;
        previousAIHealth = aiPlayer.health;
        
        string currentState = GetState();
        List<string> possibleActions = GetPossibleActions();
        
        if (possibleActions.Count == 0)
        {
            EndTurn();
            return;
        }

        string chosenAction = ChooseAction(currentState, possibleActions);
        ExecuteAction(chosenAction);
        
        float reward = CalculateReward();
        UpdateQTable(previousState, previousAction, reward, currentState);
        
        previousState = currentState;
        previousAction = chosenAction;
        
        Invoke("EndTurn", 0.5f);
    }

    string GetState()
    {
        return string.Format("{0},{1},{2},{3},{4}",
            aiPlayer.mana,
            aiPlayer.health,
            Player.localPlayer.health,
            GetAICreatureCount(),
            GetPlayerCreatureCount()
        );
    }

    int GetAICreatureCount()
    {
        return GameObject.Find("EnemyFieldContent")
            .GetComponentsInChildren<FieldCard>().Length;
    }

    int GetPlayerCreatureCount()
    {
        return GameObject.Find("PlayerFieldContent")
            .GetComponentsInChildren<FieldCard>().Length;
    }

    List<string> GetPossibleActions()
    {
        List<string> actions = new List<string>();

        // Buy Actions
        for (int i = 0; i < aiPlayer.deck.hand.Count; i++)
        {
            if (aiPlayer.deck.hand[i].cost.ToInt() <= aiPlayer.mana)
                actions.Add($"BUY_{i}");
        }

        // Play Actions
        if (aiPlayer.deck.wallet.Count > 0)
            actions.Add("PLAY");

        // Attack Actions
        if (GetAICreatureCount() > 0)
            actions.Add("ATTACK");

        actions.Add("END");
        return actions;
    }

    string ChooseAction(string state, List<string> actions)
    {
        if (Random.value < explorationRate)
            return actions[Random.Range(0, actions.Count)];

        float maxQ = float.MinValue;
        string bestAction = actions[0];
        
        foreach (string action in actions)
        {
            string key = $"{state}-{action}";
            float qValue = qTable.ContainsKey(key) ? qTable[key] : 0;
            
            if (qValue > maxQ)
            {
                maxQ = qValue;
                bestAction = action;
            }
        }
        return bestAction;
    }

    void ExecuteAction(string action)
    {
        string[] parts = action.Split('_');
        
        switch (parts[0])
        {
            case "BUY":
                BuyCard(int.Parse(parts[1]));
                break;
            case "PLAY":
                PlayRandomCard();
                break;
            case "ATTACK":
                AttackRandom();
                break;
            case "END":
                break;
        }
    }

    void BuyCard(int index)
    {
        if (index < aiPlayer.deck.hand.Count && 
            aiPlayer.mana >= aiPlayer.deck.hand[index].cost.ToInt())
        {
            aiPlayer.mana -= aiPlayer.deck.hand[index].cost.ToInt();
            aiPlayer.deck.wallet.Add(aiPlayer.deck.hand[index]);
            aiPlayer.deck.hand.RemoveAt(index);
            aiPlayer.deck.CmdUpdateAIHand();
        }
    }

    void PlayRandomCard()
    {
        if (aiPlayer.deck.wallet.Count > 0)
        {
            int index = Random.Range(0, aiPlayer.deck.wallet.Count);
            aiPlayer.deck.CmdPlayCard(aiPlayer.deck.wallet[index], index);
        }
    }

    void AttackRandom()
    {
        FieldCard[] creatures = GameObject.Find("EnemyFieldContent")
            .GetComponentsInChildren<FieldCard>();
        
        if (creatures.Length == 0) return;

        foreach (FieldCard attacker in creatures){
            Entity[] targets = GameObject.Find("PlayerFieldContent")
                .GetComponentsInChildren<FieldCard>()
                .Cast<Entity>()
                .Append(Player.localPlayer)
                .ToArray();

            if (targets.Length == 0) return;

            Entity target = targets[Random.Range(0, targets.Length)];
            ((CreatureCard)attacker.card.data).Attack(attacker, target);
        }
    }

    float CalculateReward()
    {
        float reward = 0f;
        
        // Health changes
        reward += (previousPlayerHealth - Player.localPlayer.health) * 1f;
        reward -= (previousAIHealth - aiPlayer.health) * 0.8f;
        
        // Game termination
        if (Player.localPlayer.health <= 0) reward += 100f;
        if (aiPlayer.health <= 0) reward -= 100f;
        
        return reward;
    }

    void UpdateQTable(string oldState, string action, float reward, string newState)
    {
        if (string.IsNullOrEmpty(oldState)) return;

        string key = $"{oldState}-{action}";
        float oldQ = qTable.ContainsKey(key) ? qTable[key] : 0;
        
        // Calculate max future Q
        float maxFutureQ = GetPossibleActions()
            .Select(a => qTable.ContainsKey($"{newState}-{a}") ? qTable[$"{newState}-{a}"] : 0)
            .DefaultIfEmpty(0)
            .Max();

        float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
        qTable[key] = newQ;
    }

    void EndTurn()
    {
        Player.gameManager.CmdEndTurn();
        enabled = true;
    }
}