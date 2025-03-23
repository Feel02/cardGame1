using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

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

    // Save/Load File Path (Persistent Data Path is platform-specific, good for builds)
    private string saveFilePath;

    void Start()
    {
        aiPlayer = GetComponent<Player>();
        if (aiPlayer == null)
        {
            Debug.LogError("AIManager: Player component missing!");
            enabled = false;
        }
        saveFilePath = Path.Combine(Application.persistentDataPath, "rl_agent_qtable.bin");
        LoadQTable(); // Load Q-table at start
        print(saveFilePath);
    }

    void OnDestroy()
    {
        SaveQTable(); // Save Q-table when the AI script is destroyed (e.g., game ends)
    }


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
        // Add these lines from the original AIManager
        aiPlayer.mana += 1;

        // 1. Restart hand
        int[] indexes = new int[3];
        aiPlayer.deck.RestartHand(indexes);
        aiPlayer.deck.CmdUpdateAIHand();

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

    void AttackPlayer(int attackerIndex)
    {
        FieldCard[] aiCreatures = GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>();
        if (attackerIndex < aiCreatures.Length)
        {
            FieldCard attacker = aiCreatures[attackerIndex];
            // Use networked command
            ((CreatureCard)attacker.card.data).Attack(attacker, Player.localPlayer);
        }
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

        // Buy Actions (Specific Card)
        for (int i = 0; i < aiPlayer.deck.hand.Count; i++)
        {
            if (aiPlayer.deck.hand[i].cost.ToInt() <= aiPlayer.mana)
                actions.Add($"BUY_{i}");
        }

        // Play Actions (Specific Card from Wallet)
        for (int i = 0; i < aiPlayer.deck.wallet.Count; i++)
        {
            actions.Add($"PLAY_{i}");
        }

        // Attack Actions (Specific Attacker, Specific Target)
        FieldCard[] aiCreatures = GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>();
        FieldCard[] playerCreatures = GameObject.Find("PlayerFieldContent").GetComponentsInChildren<FieldCard>();

        for (int attackerIndex = 0; attackerIndex < aiCreatures.Length; attackerIndex++)
        {
            actions.Add($"ATTACK_{attackerIndex}_PLAYER"); // Attack Player

            for (int targetIndex = 0; targetIndex < playerCreatures.Length; targetIndex++)
            {
                actions.Add($"ATTACK_{attackerIndex}_CREATURE_{targetIndex}"); // Attack Creature
            }
        }


        actions.Add("END");
        return actions;
    }

    string ChooseAction(string state, List<string> actions)
    {
        if (UnityEngine.Random.value < explorationRate)
            return actions[UnityEngine.Random.Range(0, actions.Count)];

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
        Debug.Log($"RL AI executing action: {action}");

        string[] parts = action.Split('_');

        switch (parts[0])
        {
            case "BUY":
                BuyCard(int.Parse(parts[1]));
                break;
            case "PLAY":
                PlaySpecificCard(int.Parse(parts[1])); // Play specific card from wallet
                break;
            case "ATTACK":
                if (parts[2] == "PLAYER")
                {
                    AttackPlayer(int.Parse(parts[1])); // Attack player with specific creature
                }
                else if (parts[2] == "CREATURE")
                {
                    AttackCreature(int.Parse(parts[1]), int.Parse(parts[3])); // Attack creature with specific creature
                }
                break;
            case "END":
                break;
        }
    }

    void PlaySpecificCard(int index)
    {
        if (index < aiPlayer.deck.wallet.Count)
        {
            aiPlayer.deck.CmdPlayCard(aiPlayer.deck.wallet[index], index);
        }
    }
    void AttackCreature(int attackerIndex, int targetIndex)
    {
        FieldCard[] aiCreatures = GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>();
        FieldCard[] playerCreatures = GameObject.Find("PlayerFieldContent").GetComponentsInChildren<FieldCard>();

        if (attackerIndex < aiCreatures.Length && targetIndex < playerCreatures.Length)
        {
            FieldCard attacker = aiCreatures[attackerIndex];
            FieldCard target = playerCreatures[targetIndex];
            // Use networked command
            ((CreatureCard)attacker.card.data).Attack(attacker, target);
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
            int index = UnityEngine.Random.Range(0, aiPlayer.deck.wallet.Count);
            aiPlayer.deck.CmdPlayCard(aiPlayer.deck.wallet[index], index);
        }
    }

    void AttackRandom()
    {
        FieldCard[] creatures = GameObject.Find("EnemyFieldContent")
            .GetComponentsInChildren<FieldCard>();

        if (creatures.Length == 0) return;

        foreach (FieldCard attacker in creatures)
        {
            Entity[] targets = GameObject.Find("PlayerFieldContent")
                .GetComponentsInChildren<FieldCard>()
                .Cast<Entity>()
                .Append(Player.localPlayer)
                .ToArray();

            if (targets.Length == 0) return;

            Entity target = targets[UnityEngine.Random.Range(0, targets.Length)];
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

        float maxFutureQ = 0;
        if (GetPossibleActions().Count > 0)
        {
            maxFutureQ = GetPossibleActions()
                .Select(a => qTable.ContainsKey($"{newState}-{a}") ? qTable[$"{newState}-{a}"] : 0)
                .Max();
        }         

        float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
        qTable[key] = newQ;
    }

    void EndTurn()
    {
        // Clear previous state
        previousState = null;
        previousAction = null;
        
        // End turn properly
        Player.gameManager.CmdEndTurn();
        enabled = true;
    }

    // --- Save/Load Functionality ---

    public void SaveQTable()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(saveFilePath);
        bf.Serialize(file, qTable);
        file.Close();
        Debug.Log("Q-Table Saved!");
    }

    public void LoadQTable()
    {
        if (File.Exists(saveFilePath))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(saveFilePath, FileMode.Open);
            try
            {
                qTable = (Dictionary<string, float>)bf.Deserialize(file);
                Debug.Log("Q-Table Loaded!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load Q-Table: {e.Message}");
                qTable = new Dictionary<string, float>(); // Initialize empty Q-table if loading fails
            }
            file.Close();
        }
        else
        {
            Debug.Log("No Q-Table save file found. Starting with an empty Q-Table.");
            qTable = new Dictionary<string, float>(); // Initialize empty Q-table if no file exists
        }
    }
}