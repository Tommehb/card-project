// MultiplayerLauncher.cs
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Collections;

public class MultiplayerLauncher : MonoBehaviour
{
    [Header("UI Elements (Optional)")]
    [SerializeField] private TMP_InputField ipAddressInput;
    [SerializeField] private TMP_InputField portInput;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text statusText;

    [Header("Connection")]
    [SerializeField] private string ipAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    [Header("Scene Flow")]
    [SerializeField] private string menuSceneName = "Home";
    [SerializeField] private string lobbySceneName = "MultiplayTest";
    [SerializeField] private string gameplaySceneName = "Eastwood Elementary School";

    private UnityTransport transport;
    private NetworkManager nm;
    private LanSessionManager session;

    // Wait a frame so NetworkManager.Awake() runs and sets Singleton.
    private IEnumerator Start()
    {
        // If NetworkManager is in another scene/prefab that spawns this frame,
        // give it time to initialize.
        yield return null;

        nm = NetworkManager.Singleton ?? FindFirstObjectByType<NetworkManager>();
        if (nm == null)
        {
            Debug.LogError("No NetworkManager in scene. Add one or use a bootstrap (see below).");
            UpdateStatus("No NetworkManager found in the scene.");
            yield break;
        }

        transport = nm.GetComponent<UnityTransport>();
        if (!transport)
        {
            Debug.LogError("NetworkManager is missing a UnityTransport component.");
            UpdateStatus("NetworkManager is missing a UnityTransport component.");
            yield break;
        }

        session = nm.GetComponent<LanSessionManager>();
        if (session == null)
        {
            session = nm.gameObject.AddComponent<LanSessionManager>();
        }

        session.ConfigureSceneFlow(menuSceneName, lobbySceneName, gameplaySceneName);
        UpdateStatus(session.StatusMessage);
    }

    public void Host()
    {
        if (!Ready()) return;
        ReadInputs();

        if (session != null)
        {
            session.ConfigureSceneFlow(menuSceneName, lobbySceneName, gameplaySceneName);
            session.SetLocalPlayerName(ReadPlayerName());
            var started = session.StartHostSession(ipAddress, port);
            UpdateStatus(session.StatusMessage);
            if (!started)
            {
                Debug.LogError("Failed to start LAN host.");
            }
            return;
        }

        // Bind to all interfaces so other machines can join (overrides inspector Address).
        transport.SetConnectionData("0.0.0.0", port);
        if (!nm.StartHost()) Debug.LogError("Failed to start host.");
        else Debug.Log($"Hosting on UDP {port}");
    }

    public void ServerOnly()
    {
        if (!Ready()) return;
        ReadInputs();

        if (session != null)
        {
            var started = session.StartServerOnlySession(port);
            UpdateStatus(session.StatusMessage);
            if (!started)
            {
                Debug.LogError("Failed to start LAN server.");
            }
            return;
        }

        transport.SetConnectionData("0.0.0.0", port);
        if (!nm.StartServer()) Debug.LogError("Failed to start server.");
        else Debug.Log($"Server started on UDP {port}");
    }

    public void Join()
    {
        if (!Ready()) return;
        ReadInputs();

        if (session != null)
        {
            session.ConfigureSceneFlow(menuSceneName, lobbySceneName, gameplaySceneName);
            session.SetLocalPlayerName(ReadPlayerName());
            var started = session.StartClientSession(ipAddress, port);
            UpdateStatus(session.StatusMessage);
            if (!started)
            {
                Debug.LogError($"Failed to connect to {ipAddress}:{port}");
            }
            return;
        }

        transport.SetConnectionData(ipAddress, port);
        if (!nm.StartClient()) Debug.LogError("Failed to start client.");
        else Debug.Log($"Connecting to {ipAddress}:{port}");
    }

    public void ShutdownAll()
    {
        if (session != null)
        {
            session.ShutdownAndReturnToMenu();
            UpdateStatus(session.StatusMessage);
            return;
        }

        if (nm && nm.IsListening) nm.Shutdown();
    }

    private void Update()
    {
        if (session != null)
        {
            UpdateStatus(session.StatusMessage);
        }
    }

    private void ReadInputs()
    {
        if (ipAddressInput != null && !string.IsNullOrWhiteSpace(ipAddressInput.text))
        {
            ipAddress = ipAddressInput.text.Trim();
        }

        if (portInput != null && ushort.TryParse(portInput.text, out var parsedPort))
        {
            port = parsedPort;
        }
    }

    private string ReadPlayerName()
    {
        if (playerNameInput == null || string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            return "Player";
        }

        return playerNameInput.text.Trim();
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = string.IsNullOrWhiteSpace(message) ? "Offline" : message;
        }
    }

    bool Ready() => nm != null && transport != null && !nm.IsListening;
}
