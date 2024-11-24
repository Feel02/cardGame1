using System.ComponentModel;
using UnityEngine;
using Mirror;

/// <summary>
/// An extension for the NetworkManager that displays a default HUD for controlling the network state of the game.
/// <para>This component also shows useful internal state for the networking system in the inspector window of the editor. It allows users to view connections, networked objects, message handlers, and packet statistics. This information can be helpful when debugging networked games.</para>
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Network/NetworkManagerHUD CCG")]
[RequireComponent(typeof(NetworkManager))]
[EditorBrowsable(EditorBrowsableState.Never)]
[HelpURL("https://mirror-networking.com/docs/Components/NetworkManagerHUD.html")]
public class NetworkManagerHUDCCG : MonoBehaviour
{
    NetworkManager manager;

    string username = "";

    void Awake()
    {
        manager = GetComponent<NetworkManager>();

        // Set last username used (if any) in the username's input field
        if (PlayerPrefs.GetString("Name") != null) username = PlayerPrefs.GetString("Name");
        else username = "Player";
        if (PlayerPrefs.GetString("InputServerIp") != null) manager.networkAddress = PlayerPrefs.GetString("InputServerIp");
        else manager.networkAddress = "localhost";
        
        int isClient = PlayerPrefs.GetInt("isClient");
        if(isClient == 0)
        {
            manager.StartClient();
        }
        else{
            manager.StartServer();
        }
    }
}

    /* void OnGUI()
    {
        if (!NetworkClient.isConnected && !NetworkServer.active)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }

        // client ready
        if (NetworkClient.isConnected && !ClientScene.ready)
        {
            ClientScene.Ready(NetworkClient.connection);

            if (ClientScene.localPlayer == null)
            {
                ClientScene.AddPlayer(NetworkClient.connection);
            }
        }

        StopButtons();

        GUILayout.EndArea();
    }

    void StartButtons()
    {
        if (!NetworkClient.active)
        {
            // Server + Client
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                if (GUILayout.Button("Host (Server + Client)"))
                {
                    manager.StartHost();

                    // Save the player's username
                    PlayerPrefs.SetString("Name", username);

                    // Hide GUI
                    showGUI = false;
                }
            }

            // Client + IP
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Client"))
            {
                manager.StartClient();

                // Save the player's username
                PlayerPrefs.SetString("Name", username);

                PlayerPrefs.SetInt("playerHealth", 30);

                // Hide GUI
                showGUI = false;
            }
            manager.networkAddress = GUILayout.TextField(manager.networkAddress);
            GUILayout.EndHorizontal();

            // Username field
            username = GUILayout.TextField(username);


            // Server Only
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // cant be a server in webgl build
                GUILayout.Box("(  WebGL cannot be server  )");
            }
            else
            {
                if (GUILayout.Button("Server Only")) manager.StartServer();
            }
        }
        else
        {
            // Connecting
            GUILayout.Label("Connecting to " + manager.networkAddress + "..");
            if (GUILayout.Button("Cancel Connection Attempt"))
            {
                manager.StopClient();
            }
        }
    }

    void StatusLabels()
    {
        // server / client status message
        if (NetworkServer.active)
        {
            GUILayout.Label("Server: active. Transport: " + Transport.activeTransport);
        }
        if (NetworkClient.isConnected)
        {
            GUILayout.Label("Client: address=" + manager.networkAddress);
        }
    }

    void StopButtons()
    {
        // stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            if (GUILayout.Button("Stop Host"))
            {
                manager.StopHost();
            }
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            if (GUILayout.Button("Stop Client"))
            {
                manager.StopClient();
            }
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            if (GUILayout.Button("Stop Server"))
            {
                manager.StopServer();
            }
        }
    }
} */