using UnityEngine;
using Mirror;

// Doesnt do anything special but it's set up to be built-upon
[AddComponentMenu("Network Manager CCG")]
public class NetworkManagerCCG : NetworkManager
{
    // Called when Player connects to the server and joins the game
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Transform startPos = GetStartPosition();
        GameObject player = Instantiate(playerPrefab);
        NetworkServer.AddPlayerForConnection(conn, player);

        // Offline mode: Add AI player
        if (PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            GameObject aiPlayer = Instantiate(playerPrefab);
            NetworkServer.Spawn(aiPlayer); // Spawn without a connection

            // Initialize AI Player (name, etc.)
            aiPlayer.GetComponent<Player>().InitializeAI();
        }
    }
   
}
