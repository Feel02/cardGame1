using UnityEngine;
using Mirror;

// Doesnt do anything special but it's set up to be built-upon
[AddComponentMenu("Network Manager CCG")]
public class NetworkManagerCCG : NetworkManager
{
    public override void Start()
    {
        base.Start();

        // Check if offline mode is enabled
        if (PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            // Start as host (server and client)
            this.StartHost();
        }
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
    }

    // Called when Player connects to the server and joins the game
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Transform startPos = GetStartPosition();
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        if (PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            GameObject aiPlayer = Instantiate(playerPrefab);
            NetworkServer.Spawn(aiPlayer); 
            aiPlayer.GetComponent<Player>().InitializeAI();
            aiPlayer.SetActive(true);
        }
    }
}