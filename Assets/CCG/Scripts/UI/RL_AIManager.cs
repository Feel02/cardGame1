using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class RL_AIManager : MonoBehaviour
{
    public Player aiPlayer;
    public float aiTurnDelay = 2f;
    private bool aiInitialized = false;

    private List<int> usedCards = new List<int>();

    // Q-Learning Parameters
    private Dictionary<string, float> qTable = new Dictionary<string, float>();
    public float learningRate = 0.1f;
    public float discountFactor = 0.9f;
    public float explorationRate = 0.3f;  //Initial Exploration Rate
    public float explorationRateDecay = 0.001f; //How much to decay with each turn

    // Flag to determine if we use the pre-trained Q-table
    public bool usePretrainedQTable = true;

    // State Tracking
    private string previousState;
    private string previousAction;
    private int previousPlayerHealth;
    private int previousAIHealth;

    // Save/Load File Path (only used if not using the pre-trained Q-table)
    private string saveFilePath;
    private int turnCount = 0;

    void Start()
    {
        aiPlayer = GetComponent<Player>();
        if (aiPlayer == null)
        {
            Debug.LogError("AIManager: Player component missing!");
            enabled = false;
        }
        
        // If using a pre-trained Q-table, load from Resources.
        if (usePretrainedQTable)
        {
            LoadPretrainedQTable();
        }
        else
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "rl_agent_qtable.json");
            LoadQTable(); // Load Q-table from persistent data
        }
        
        Debug.Log("Q-Table initialization complete.");
    }

    void OnDestroy()
    {
        // Only save if we are not using the pre-trained Q-table.
        if (!usePretrainedQTable)
            SaveQTable();
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
            if (!IsInvoking("AITurn") && enabled)
            {
                Invoke("AITurn", aiTurnDelay);
                enabled = false;
            }
        }
    }

    void InitializeAI()
    {
        // Original initialization code...
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
        turnCount++;
        usedCards.Clear();
        // Decay exploration rate over time
        explorationRate = Mathf.Max(0.1f, explorationRate - explorationRateDecay);
        aiPlayer.mana += 1;

        int[] indexes = new int[3];
        aiPlayer.deck.RestartHand(indexes);
        aiPlayer.deck.CmdUpdateAIHand();

        previousPlayerHealth = Player.localPlayer.health;
        previousAIHealth = aiPlayer.health;

        string currentState = GetState();
        previousState = currentState;
        previousAction = null;
        float totalReward = 0;

        while (true)
        {
            List<string> possibleActions = GetPossibleActions();
            if (possibleActions.Count == 0)
            {
                Debug.Log("No possible actions. Ending turn.");
                break;
            }

            string chosenAction = ChooseAction(currentState, possibleActions);
            previousAction = chosenAction;
            Debug.Log("Executing action: " + chosenAction);
            ExecuteAction(chosenAction);
            float reward = CalculateReward();
            totalReward += reward;

            // Update Q-table only if not using the pre-trained version.
            UpdateQTable(previousState, previousAction, reward, currentState);

            previousState = currentState;
            currentState = GetState();

            if (chosenAction.Contains("end_turn"))
            {
                Debug.Log("End turn action executed. Ending turn.");
                break;
            }
        }
        EndTurn(totalReward);
    }

    void AttackPlayer(int attackerIndex)
    {
        FieldCard[] aiCreatures = GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>();
        if (attackerIndex < aiCreatures.Length)
        {
            FieldCard attacker = aiCreatures[attackerIndex];
            // Use networked command
            ((CreatureCard)attacker.card.data).Attack(attacker, Player.localPlayer);
            usedCards.Add(attacker.GetInstanceID()); // Mark the card as used
            Debug.Log(attacker.GetInstanceID() + " has been used!");
        }
    }

    string GetState()
    {
        string state = string.Format("({0}, {1}, {2}, {3}, {4})",
            aiPlayer.mana,
            aiPlayer.health,
            Player.localPlayer.health,
            GetAICreatureCount(),
            GetPlayerCreatureCount()
        );
        Debug.Log("Unity State: " + state);
        return state;
    }

    int GetAICreatureCount()
    {
        return GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>().Length;
    }

    int GetPlayerCreatureCount()
    {
        return GameObject.Find("PlayerFieldContent").GetComponentsInChildren<FieldCard>().Length;
    }
    List<string> GetPossibleActions()
    {
        List<string> actions = new List<string>();

        if (aiPlayer.deck.wallet.Count < 6)
        {
            for (int i = 0; i < aiPlayer.deck.hand.Count; i++)
            {   
                if (aiPlayer.deck.hand[i].cost.ToInt() <= aiPlayer.mana)
                {
                    actions.Add($"('buy', '{aiPlayer.deck.hand[i].data.CardID}')");
                }
            }
        }

        for (int i = 0; i < aiPlayer.deck.wallet.Count; i++)
        {
            actions.Add($"('play', '{aiPlayer.deck.wallet[i].data.CardID}')");
        }

        FieldCard[] aiCreatures = GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>();
        FieldCard[] playerCreatures = GameObject.Find("PlayerFieldContent").GetComponentsInChildren<FieldCard>();

        for (int i = 0; i < aiCreatures.Length; i++)
        {
            if(usedCards.Contains(aiCreatures[i].GetInstanceID())) continue;
            
            actions.Add($"('attack_player', {i})");
            for (int j = 0; j < playerCreatures.Length; j++)
            {
                actions.Add($"('attack_card', {i}, {j})");
            }
        }

        actions.Add("('end_turn',)");
        return actions;
    }
    string ChooseAction(string state, List<string> actions)
    {
        if (UnityEngine.Random.value < explorationRate)
        {
            string randomAction = actions[UnityEngine.Random.Range(0, actions.Count)];
            Debug.Log("Choosing random action: " + randomAction);
            return randomAction;
        }

        float maxQ = float.MinValue;
        string bestAction = actions[0];

        foreach (string action in actions)
        {
            string key = $"{state}_{action}";
            float qValue = qTable.ContainsKey(key) ? qTable[key] : 0;
            Debug.Log("State " + state + " QTable Key: " + key + " QTable Value: " + qValue);
            if (qValue > maxQ)
            {
                maxQ = qValue;
                bestAction = action;
            }
        }
        Debug.Log("Choosing best action: " + bestAction);
        return bestAction;
    }
    void ExecuteAction(string action)
    {
        Debug.Log($"RL AI executing action: {action}");
        string cleanedAction = action.Trim('(', ')');
        string[] parts = cleanedAction.Split(new char[] { ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
        string actionType = parts[0].Trim();
        Debug.Log("--------------------------------------");

        switch (actionType)
        {
            case "buy":
                string cardIDToBuy = parts[2].Trim();
                BuyCard(cardIDToBuy);
                break;
            case "play":
                string cardIDToPlay = parts[2].Trim();
                PlaySpecificCard(cardIDToPlay);
                break;
            case "attack_player":
                int attackerIndexPlayer = int.Parse(parts[1].Trim());
                AttackPlayer(attackerIndexPlayer);
                break;
            case "attack_card":
                int attackerIndexCard = int.Parse(parts[1].Trim());
                int targetIndexCard = int.Parse(parts[2].Trim());
                AttackCreature(attackerIndexCard, targetIndexCard);
                break;
            case "end_turn":
                // No action needed here; handled in AITurn.
                break;
        }
    }

    CardInfo? FindCardInHand(string cardID)
    {
        Debug.Log("Finding Card In Hand!");
        foreach (CardInfo card in aiPlayer.deck.hand)
        {
            Debug.Log("Comparing " + card.data.CardID + " " + cardID);
            if (card.data.CardID == cardID)
            {
                Debug.Log("Card has been found!");
                return card; // Found the card
            }
        }
        return null; // Card not found
    }

    CardInfo? FindCardInWallet(string cardID)
    {
        foreach (CardInfo card in aiPlayer.deck.wallet)
        {
            if (card.data.CardID == cardID)
            {
                return card; // Found the card
            }
        }
        return null; // Card not found
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
            usedCards.Add(attacker.GetInstanceID()); // Mark the card as used
        }
    }

    void BuyCard(string cardID)
    {
        Debug.Log("Buying card " + cardID);
        CardInfo? cardToBuy = FindCardInHand(cardID);

        if (cardToBuy != null && aiPlayer.mana >= cardToBuy?.cost.ToInt())
        {
            // Find the card's index in the hand
            int index = -1;
            for (int i = 0; i < aiPlayer.deck.hand.Count; i++)
            {
                if (aiPlayer.deck.hand[i].data.CardID == cardID)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                aiPlayer.mana -= cardToBuy.Value.cost.ToInt();
                aiPlayer.deck.wallet.Add(cardToBuy.Value);
                aiPlayer.deck.hand.RemoveAt(index);

                // Update UI
                aiPlayer.UpdateEnemyInfo();
                aiPlayer.deck.CmdUpdateAIBoughtCard();
                aiPlayer.deck.CmdUpdateAIHand();

                Debug.Log($"RL AI Bought {cardToBuy?.data.CardID}!");
            }
            else
            {
                Debug.LogWarning($"Card with ID {cardID} not found in hand!");
            }
        }
        else
        {
            Debug.LogWarning($"Card with ID {cardID} not found or not enough mana!");
        }
    }

    void PlaySpecificCard(string cardID)
    {
        CardInfo? cardToPlay = FindCardInWallet(cardID);

        if (cardToPlay != null)
        {
            Debug.Log("Playing Specific Card Called: " + cardToPlay?.data.CardID);
            aiPlayer.deck.CmdPlayCard(cardToPlay.Value, aiPlayer.deck.wallet.IndexOf(cardToPlay.Value));
        }
        else
        {
            Debug.LogWarning($"Card with ID {cardID} not found in Wallet!");
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

        // Mana efficiency (using more mana is good)
        reward += (aiPlayer.mana / 100f) * 0.1f;

        //Card efficiency(using more cards is good)
        reward += (aiPlayer.deck.hand.Count / 3f) * 0.05f;

        // Game termination
        if (Player.localPlayer.health <= 0) reward += 100f;
        if (aiPlayer.health <= 0) reward -= 100f;

        //Board control (number of creatures on ai side)
        reward += GetAICreatureCount() * 0.005f;
        reward -= GetPlayerCreatureCount() * 0.005f;

        return reward;
    }

    void UpdateQTable(string oldState, string action, float reward, string newState)
    {
        if (string.IsNullOrEmpty(oldState))
            return;

        // If using the pre-trained Q-table, skip updates.
        if (usePretrainedQTable)
            return;

        string key = $"{oldState}-{action}";
        float oldQ = qTable.ContainsKey(key) ? qTable[key] : 0;

        float maxFutureQ = 0;
        List<string> possibleActions = GetPossibleActions();
        if (possibleActions.Count > 0)
        {
            maxFutureQ = possibleActions
                .Select(a => qTable.ContainsKey($"{newState}-{a}") ? qTable[$"{newState}-{a}"] : 0)
                .Max();
        }

        float newQ = oldQ + learningRate * (reward + discountFactor * maxFutureQ - oldQ);
        qTable[key] = newQ;
    }

    void EndTurn(float totalReward)
    {
        string currentState = GetState();
        string key = $"{previousState}-('end_turn',)";
        float oldQ = qTable.ContainsKey(key) ? qTable[key] : 0;
        float newQ = oldQ + learningRate * (totalReward - oldQ);
        qTable[key] = newQ;
        previousState = null;
        previousAction = null;
        Player.gameManager.CmdEndTurn();
        enabled = true;
    }

    public void SaveQTable()
    {
        string json = JsonConvert.SerializeObject(qTable, Newtonsoft.Json.Formatting.Indented);
        try
        {
            File.WriteAllText(saveFilePath, json);
            Debug.Log("Q-Table Saved to JSON!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save Q-Table to JSON: {e.Message}");
        }
    }

    // Update LoadQTable to match Python format
    public void LoadQTable()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                JObject jsonObject = JObject.Parse(json);

                foreach (var entry in jsonObject)
                {
                    try
                    {
                        float value = (float)entry.Value;
                        qTable[entry.Key] = value;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Skipping invalid entry {entry.Key}: {e.Message}");
                    }
                }
                Debug.Log($"Loaded {qTable.Count} valid Q-table entries");
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e.Message}");
                qTable = new Dictionary<string, float>();
            }
        }
        else
        {
            Debug.Log("No Q-table found - using random actions");
            qTable = new Dictionary<string, float>();
        }
    }
    public void LoadPretrainedQTable()
    {
        TextAsset qTableAsset = Resources.Load<TextAsset>("rl_agent_qtable"); // Place your file in Resources folder without extension
        if (qTableAsset != null)
        {
            try
            {
                string json = qTableAsset.text;
                JObject jsonObject = JObject.Parse(json);

                foreach (var entry in jsonObject)
                {
                    try
                    {
                        float value = (float)entry.Value;
                        qTable[entry.Key] = value;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Skipping invalid entry {entry.Key}: {e.Message}");
                    }
                }
                Debug.Log($"Loaded {qTable.Count} pre-trained Q-table entries");
            }
            catch (Exception e)
            {
                Debug.LogError($"Pre-trained Q-table load failed: {e.Message}");
                qTable = new Dictionary<string, float>();
            }
        }
        else
        {
            Debug.Log("Pre-trained Q-table not found in Resources. Using an empty Q-table.");
            qTable = new Dictionary<string, float>();
        }
    }
}