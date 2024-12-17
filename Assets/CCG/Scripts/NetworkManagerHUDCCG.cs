using System;
using System.ComponentModel;
using UnityEngine;
using Mirror;
using System.Text;

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

    // Keep track of messages on the server to display on screen
    private readonly StringBuilder serverLog = new StringBuilder();
    // How many lines should we display?
    private const int maxLines = 50;

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

            // log only if this is a server
            // this uses the static function from the class in Mirror/Runtime/Logger
            // for some reason NetworkLogSettings is not a Monobehaviour, so i can't access it
            // and set the Log level directly
            LogFactory.GetLogger("NetworkManagerHUDCCG").filterLogType = LogType.Log;
            Application.logMessageReceived += HandleLog;
        }
    }

    // Log handler function for the server, to display the logs on screen
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if(NetworkServer.active)
        {
            if (serverLog.Length > 10000)
            {
                serverLog.Clear();
            }
            string appendString =  $"{DateTime.Now.ToShortTimeString()} [{type}] : " + logString + " ||\n";
            serverLog.Append(appendString);
        }
    }

    void OnGUI()
    {
        // if the server is active
        if (NetworkServer.active)
        {
            string text = serverLog.ToString();
            // only display last N lines
            string[] lines = text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            int linesToDraw = Math.Min(lines.Length, maxLines);
            StringBuilder builder = new StringBuilder();
            for (int i = lines.Length - linesToDraw; i < lines.Length; ++i)
            {
                builder.Append(lines[i]);
            }
            // display only those last N lines
            GUI.Label(new Rect(10, 10, Screen.width, Screen.height), builder.ToString());
        }
    }
}