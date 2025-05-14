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

        Sprite[] sprites = Resources.LoadAll<Sprite>("Portraits/Trainer");
        print(sprites.Length);
        aiPlayer.portrait = sprites[8];
        aiPlayer.username = "AI Player";
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

        // --- Q-LEARNING ACTION SELECTION (ONLY BUY, END_TURN, PASS_BY) ---
        bool qActionTakenThisLoop;
        do
        {
            qActionTakenThisLoop = false;
            List<string> possibleActions = GetPossibleActions();
            if (possibleActions.Count == 0)
            {
                Debug.Log("No possible Q-actions.");
                break; 
            }

            string chosenAction = ChooseAction(currentState, possibleActions);
            
            if (chosenAction.Contains("end_turn") || chosenAction.Contains("pass_by"))
            {
                Debug.Log("Q-Learning chose to end turn or pass by. Proceeding to greedy attack phase.");
                break; // Exit Q-action loop, proceed to greedy attack phase
            }
            
            previousAction = chosenAction; // Store action if not end_turn
            Debug.Log("Executing Q-action: " + chosenAction);
            ExecuteAction(chosenAction); // This might use a card, affecting 'usedCards'
            float reward = CalculateReward();
            totalReward += reward;
            UpdateQTable(previousState, previousAction, reward, currentState);
            previousState = currentState;
            currentState = GetState();
            qActionTakenThisLoop = true;

        } while (qActionTakenThisLoop && previousAction != null && !previousAction.Contains("end_turn") && !previousAction.Contains("pass_by"));

        // --- ALWAYS PLAY ALL CARDS IN WALLET BEFORE ATTACKING ---
        // Play as many cards from wallet as possible (until board is full or wallet is empty)
        while (aiPlayer.deck.wallet.Count > 0 && aiPlayer.deck.playerField.Count < 6)
        {
            // Always play the first card in wallet
            aiPlayer.deck.CmdPlayCard(aiPlayer.deck.wallet[0], 0);
        }

        // --- GREEDY ATTACK PHASE ---
        GreedyAttackPhase();
        EndTurn(totalReward);
    }

    void GreedyAttackPhase()
    {
        FieldCard[] aiCreaturesAll = GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>();
        FieldCard[] oppCreaturesAll = GameObject.Find("PlayerFieldContent").GetComponentsInChildren<FieldCard>();
        List<FieldCard> aiAttackers = aiCreaturesAll.Where(c => !usedCards.Contains(c.GetInstanceID())).ToList();
        List<FieldCard> oppCreatures = oppCreaturesAll.ToList();

        // One-shot check: if AI can win immediately, do so
        int aiTotalAttack = aiAttackers.Sum(c => c.card.strength);
        int enemyHealth = Player.localPlayer.health;
        if (aiTotalAttack >= enemyHealth)
        {
            foreach (var attacker in aiAttackers)
                AttackPlayer(attacker);
            return;
        }

        // Helper to recalc total attack
        int GetTotalAttack(List<FieldCard> cards) => cards.Sum(c => c.card.strength);

        while (aiAttackers.Count > 0 && oppCreatures.Count > 0)
        {
            int aiTotalAttackLoop = GetTotalAttack(aiAttackers);
            int oppTotalAttack = GetTotalAttack(oppCreatures);

            // If AI's total attack > opponent's, attack player with all remaining attackers
            if (aiTotalAttackLoop > oppTotalAttack)
            {
                foreach (var attacker in aiAttackers)
                {
                    AttackPlayer(attacker);
                }
                return; // Stop trading, attack player only
            }

            // For each AI attacker, find the best kill (highest attack, lowest health)
            FieldCard bestAttacker = null;
            FieldCard bestTarget = null;
            int bestTargetAttack = -1;
            int bestTargetHealth = int.MaxValue;

            foreach (var attacker in aiAttackers)
            {
                foreach (var target in oppCreatures)
                {
                    if (attacker.card.strength >= target.card.health)
                    {
                        // Prefer highest attack target, then lowest health
                        if (target.card.strength > bestTargetAttack || (target.card.strength == bestTargetAttack && target.card.health < bestTargetHealth))
                        {
                            bestAttacker = attacker;
                            bestTarget = target;
                            bestTargetAttack = target.card.strength;
                            bestTargetHealth = target.card.health;
                        }
                    }
                }
            }

            if (bestAttacker != null && bestTarget != null)
            {
                AttackCreature(bestAttacker, bestTarget);
                aiAttackers.Remove(bestAttacker);
                oppCreatures.Remove(bestTarget);
            }
            else
            {
                break; // No more kills possible
            }
        }
        // Attack player with any remaining attackers
        foreach (var attacker in aiAttackers)
        {
            AttackPlayer(attacker);
        }
    }

    private struct PotentialEngagement
    {
        public FieldCard Target;
        public TradeOutcome Outcome;
        public bool IsDangerous;
        public int TargetStrength;
    }

    private int GetCustomOutcomeScore(TradeOutcome outcome, bool isTargetDangerous)
    {
        if (isTargetDangerous)
        {
            if (outcome == TradeOutcome.Best) return 4;
            if (outcome == TradeOutcome.Good) return 3;
            if (outcome == TradeOutcome.Okay) return 2;
            if (outcome == TradeOutcome.Bad) return 1;
        }
        else // Not Dangerous
        {
            if (outcome == TradeOutcome.Best) return 4;
            if (outcome == TradeOutcome.Good) return 3; // Still allow Good for non-dangerous if it's the top pick.
                                                      // The selection logic will filter this if only Best is desired for non-dangerous.
                                                      // The user case: AI 4/1 vs Player 1/1 (Good, NonDangerous) -> score 3.
                                                      // Selection logic `if (scoreForBestChoice == 4)` filters this out.
            if (outcome == TradeOutcome.Okay) return -1;
            if (outcome == TradeOutcome.Bad) return -2;
        }
        return -99; // Should not be reached if all outcomes are covered
    }

    private enum TradeOutcome
    {
        Bad,        // Attacker dies, Target survives
        Okay,       // Both survive OR Attacker survives, Target takes damage but survives
        Good,       // Attacker dies, Target dies (mutual destruction)
        Best        // Attacker survives, Target dies
    }

    private bool IsCardDangerous(FieldCard card)
    {
        return card.card.strength >= 3 || card.card.health >= 4 || (card.card.strength >= 3 && card.card.health >= 2);
    }

    private TradeOutcome CalculateTradeOutcome(FieldCard attacker, FieldCard target)
    {
        int attackerStrength = attacker.card.strength;
        int attackerHealth = attacker.card.health; // Current health
        int targetStrength = target.card.strength;
        int targetHealth = target.card.health;   // Current health

        bool attackerDies = targetStrength >= attackerHealth;
        bool targetDies = attackerStrength >= targetHealth;

        if (targetDies && !attackerDies) return TradeOutcome.Best;
        if (targetDies && attackerDies) return TradeOutcome.Good;
        // For "Okay", we consider if attacker survives, even if target doesn't die.
        // Or if both survive.
        if (!attackerDies) return TradeOutcome.Okay; // Attacker survives (target may or may not die, but not "Best")
        // If attackerDies is true, and targetDies is false, it's Bad.
        // This also covers the case where both survive but we already handled !attackerDies for Okay.
        // So if we reach here, attackerDies must be true.
        if (attackerDies && !targetDies) return TradeOutcome.Bad;
        
        return TradeOutcome.Okay; // Default for any other scenario, e.g. both survive (covered by !attackerDies)
                                  // or if logic is somehow incomplete, err on side of not Bad.
                                  // Re-evaluating "Okay": Attacker survives, target doesn't die OR both survive.
                                  // If attackerDies is false, it is Okay or Best. Best is handled.
                                  // So if !attackerDies, it's Okay.
                                  // If attackerDies is true:
                                  //  - if targetDies is true -> Good (handled)
                                  //  - if targetDies is false -> Bad
    }

    void AttackPlayer(FieldCard attacker)
    {
        if (attacker != null)
        {
            // Use networked command
            ((CreatureCard)attacker.card.data).Attack(attacker, Player.localPlayer);
            usedCards.Add(attacker.GetInstanceID()); // Mark the card as used
            Debug.Log(attacker.card.data.CardID + " (" + attacker.GetInstanceID() + ") attacked player.");
        }
    }

    void AttackCreature(FieldCard attacker, FieldCard target)
    {
        if (attacker != null && target != null)
        {
            // Use networked command
            ((CreatureCard)attacker.card.data).Attack(attacker, target);
            usedCards.Add(attacker.GetInstanceID()); // Mark the card as used
            Debug.Log(attacker.card.data.CardID + " (" + attacker.GetInstanceID() + ") attacked " + target.card.data.CardID + " (" + target.GetInstanceID() + ")");
        }
    }

    string GetState()
    {
        List<int> state = new List<int>();
        state.Add(aiPlayer.mana); // coins
        state.Add(aiPlayer.health); // your_health
        state.Add(Player.localPlayer.health); // opp_health

        // Hand cards (up to 3)
        for (int i = 0; i < 3; i++)
        {
            if (i < aiPlayer.deck.hand.Count)
            {
                var card = aiPlayer.deck.hand[i];
                state.Add(card.strength); // attack
                state.Add(card.health);   // health
                state.Add(card.price);    // price
            }
            else
            {
                state.Add(0); state.Add(0); state.Add(0);
            }
        }
        string stateString = "(" + string.Join(", ", state) + ")";
        Debug.Log("Unity State: " + stateString);
        return stateString;
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

        // Only allow Q-table to choose buy, end_turn, and pass_by
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
        actions.Add("('end_turn',)");
        actions.Add("('pass_by',)");
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

        FieldCard[] aiCreatures = GameObject.Find("EnemyFieldContent").GetComponentsInChildren<FieldCard>();
        FieldCard[] playerCreatures = GameObject.Find("PlayerFieldContent").GetComponentsInChildren<FieldCard>();

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
                if (attackerIndexPlayer < aiCreatures.Length)
                {
                    AttackPlayer(aiCreatures[attackerIndexPlayer]);
                }
                else
                {
                    Debug.LogWarning($"Attacker index {attackerIndexPlayer} out of bounds for AI creatures.");
                }
                break;
            case "attack_card":
                int attackerIndexCard = int.Parse(parts[1].Trim());
                int targetIndexCard = int.Parse(parts[2].Trim());
                if (attackerIndexCard < aiCreatures.Length && targetIndexCard < playerCreatures.Length)
                {
                    AttackCreature(aiCreatures[attackerIndexCard], playerCreatures[targetIndexCard]);
                }
                else
                {
                    Debug.LogWarning($"Attacker index {attackerIndexCard} or target index {targetIndexCard} out of bounds.");
                }
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