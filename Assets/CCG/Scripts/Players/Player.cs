using System;
using UnityEngine;
using Mirror;
using UnityEditor;
using Unity.VisualScripting;

// Useful for UI. Whether the player is, well, a player or an enemy.
public enum PlayerType { PLAYER, ENEMY };

[RequireComponent(typeof(Deck))]
[Serializable]
public class Player : Entity
{
    [Header("Player Info")]
    [SyncVar(hook = nameof(UpdatePlayerName))] public string username; // SyncVar hook to call a command whenever a username changes (like when players load in initially).

    [Header("Portrait")]
    public Sprite portrait; // For the player's icon at the top left of the screen & in the PartyHUD.

    [Header("Deck")]
    public Deck deck;
    public Sprite cardback;
    [SyncVar, HideInInspector] public int tauntCount = 0; // Amount of taunt creatures on your side of the board.

    [Header("Mana")]
    [SyncVar] public int maxMana = 100;
    [SyncVar] public int currentMax = 1;
    [SyncVar] public int _mana = 1;
    public int mana
    {
        get { return Mathf.Min(_mana, maxMana); }
        set { _mana = Mathf.Clamp(value, 0, maxMana); }
    }


    // Quicker access for UI scripts
    [HideInInspector] public static Player localPlayer;
    [HideInInspector] public bool hasEnemy = false; // If we have set an enemy.
    [HideInInspector] public PlayerInfo enemyInfo; // We can't pass a Player class through the Network, but we can pass structs. 
    // We store all our enemy's info in a PlayerInfo struct so we can pass it through the network when needed.
    [HideInInspector] public static GameManager gameManager;
    [SyncVar, HideInInspector] public bool firstPlayer = false; // Is it player 1, player 2, etc.
    [SyncVar, HideInInspector] public bool isAI = false;

    public void InitializeAI()
    {
        username = "AI Player";
        isAI = true;
        hasEnemy = true;

        bool useRLAgent = PlayerPrefs.GetInt("UseRLAgent", 0) == 1;

        if (useRLAgent)
        {
            gameObject.AddComponent<RL_AIManager>();
        }
        else
        {
            gameObject.AddComponent<AIManager>();
        }

        if (GetComponent<NetworkIdentity>() != null)
        {
            GetComponent<NetworkIdentity>().enabled = false;
        }
        if (GetComponent<NetworkTransform>() != null)
        {
            GetComponent<NetworkTransform>().enabled = false;
        }
    }
    public override void OnStartLocalPlayer()
    {
        localPlayer = this;
        // Get and update the player's username and stats
        CmdLoadPlayer(PlayerPrefs.GetString("Name"));
        CmdLoadDeck();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        deck.deckList.Callback += deck.OnDeckListChange;
         //deck.hand.Callback += deck.OnHandChange;
        deck.graveyard.Callback += deck.OnGraveyardChange;
    }

    [Command]
    public void CmdLoadPlayer(string user)
    {
        // Update the player's username, which calls a SyncVar hook.
        // Learn more here : https://mirror-networking.com/docs/Guides/Sync/SyncVarHook.html
        username = user;
    }

    // Update the player's username, as well as the box above the player's head where their name is displayed.
    void UpdatePlayerName(string oldUser, string newUser)
    {
        // Update username
        username = newUser;

        // Update game object's name in editor (only useful for debugging).
        gameObject.name = newUser;
    }

    [Command]
    public void CmdLoadDeck()
    {
        // Fill deck from startingDeck array
        for (int i = 0; i < deck.startingDeck.Length; ++i)
        {
            CardAndAmount card = deck.startingDeck[i];
            CreatureCard creature = (CreatureCard)card.card;
            for (int v = 0; v < creature.amount; v++)                     //card.amount instead of 3
            {
                deck.deckList.Add(card.amount > 0 ? new CardInfo(card.card, 1) : new CardInfo());
            }
            if (deck.hand.Count < 3) deck.hand.Add(new CardInfo(card.card, 1));
        }
        if (deck.hand.Count == 3)
        {
            deck.hand.Shuffle();
        }
    }

    [Command]
    public void CmdStartGame()
    {
        gameManager.StartGame();
    }

    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        health = gameManager.maxHealth;
        maxMana = gameManager.maxMana;
        deck.deckSize = gameManager.deckSize;
        deck.handSize = gameManager.handSize;

        if (isServerOnly)
        {
            System.Random rnd = new System.Random();
            Boolean random = rnd.NextDouble() <= 0.5 ? true : false;
            firstPlayer = random;
        }
        // Offline mode: Determine first player randomly for the human player
        else if (isServer && !isAI && PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            firstPlayer = true;
        }
        // Ensure AI is never the first player in offline mode
        else if (isAI)
        {
            firstPlayer = false;
        }
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();

        // Get EnemyInfo as soon as another player connects. Only start updating once our Player has been loaded in properly (username will be set if loaded in).
        if (!hasEnemy && username != "")
        {
            UpdateEnemyInfo();
        }
    }

    public void UpdateEnemyInfo()
    {
        if (PlayerPrefs.GetInt("offlineMode", 0) == 1 && isAI) return; // AI doesn't need enemy info

        // Find all Players and add them to the list.
        Player[] onlinePlayers = FindObjectsOfType<Player>();

        // Loop through all online Players (should just be one other Player)
        foreach (Player player in onlinePlayers)
        {
            if(isServerOnly){
                if(gameManager.players.Count == 2){
                    gameManager.players[0].player.GetComponent<Player>().firstPlayer = firstPlayer;
                    gameManager.players[1].player.GetComponent<Player>().firstPlayer = !firstPlayer;
                }
            }
            else
            {
                gameManager.CmdAddPlayerToPlayersList(new PlayerInfo(player.gameObject));
                if(player != null && player.username != ""){ 
                    // Make sure the players are loaded properly (we load the usernames first)
                    // There should only be one other Player online, so if it's not us then it's the enemy.
                    if (player != this)
                    {
                        // Get & Set PlayerInfo from our Enemy's gameObject
                        PlayerInfo currentPlayer = new PlayerInfo(player.gameObject);
                        enemyInfo = currentPlayer;
                        hasEnemy = true;
                        enemyInfo.data.casterType = Target.OPPONENT;
                        gameManager.StartGame();
                    }
                }
            }
        }
    }
    
    public void ChangeFirstPlayer(bool v)
    {
        firstPlayer = v;
    }

    void OnDestroy()
{
    if (isAI)
    {
        AIManager aiManager = FindObjectOfType<AIManager>();
        if (aiManager != null)
        {
            aiManager.aiPlayer = null;
        }
    }
}

    public bool IsOurTurn() => gameManager.isOurTurn;               
    public bool IsRefresh() => gameManager.isRefreshing;
}