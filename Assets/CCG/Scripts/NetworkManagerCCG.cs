using UnityEngine;
using Mirror;
using System.Net;
using System.Net.Sockets;
using System.Collections;

[AddComponentMenu("Network Manager CCG")]
public class NetworkManagerCCG : NetworkManager
{
    public override void Start()
    {
        base.Start();

        // Check if offline mode is enabled
        if (PlayerPrefs.GetInt("offlineMode", 0) == 1)
        {
            #if UNITY_ANDROID
            try 
            {
                networkAddress = GetLocalIPAddress();
                Debug.Log("Using local IP for networkAddress: " + networkAddress);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to retrieve local IP address: " + ex.Message);
            }
            // Start host after a short delay
            StartCoroutine(DelayedStartHost());
            #else
            // For non-Android builds, just start host immediately
            StartHost();
            #endif
        }
    }

    IEnumerator DelayedStartHost()
    {
        // Wait for half a second before starting the host
        yield return new WaitForSeconds(0.5f);
        StartHost();
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
    }

    // Called when a player connects to the server and joins the game
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

    #if UNITY_ANDROID
    // Retrieves the device's local IP address by creating a UDP socket.
    public static string GetLocalIPAddress()
    {
        string localIP = "";
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            // Connect to a known external address to determine the local endpoint.
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
        return localIP;
    }
    #endif
}
