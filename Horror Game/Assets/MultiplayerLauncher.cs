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

    [Header("Connection")]
    [SerializeField] private string ipAddress = "127.0.0.1";
    [SerializeField] private ushort port = 7777;

    private UnityTransport transport;
    private NetworkManager nm;

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
            yield break;
        }

        transport = nm.GetComponent<UnityTransport>();
        if (!transport)
        {
            Debug.LogError("NetworkManager is missing a UnityTransport component.");
        }
    }

    public void Host()
    {
        if (!Ready()) return;
        // Bind to all interfaces so other machines can join (overrides inspector Address).
        transport.SetConnectionData("0.0.0.0", port);
        if (!nm.StartHost()) Debug.LogError("Failed to start host.");
        else Debug.Log($"Hosting on UDP {port}");
    }

    public void ServerOnly()
    {
        if (!Ready()) return;
        transport.SetConnectionData("0.0.0.0", port);
        if (!nm.StartServer()) Debug.LogError("Failed to start server.");
        else Debug.Log($"Server started on UDP {port}");
    }

    public void Join()
    {
        if (!Ready()) return;
        if (ipAddressInput) ipAddress = ipAddressInput.text;
        if (portInput && ushort.TryParse(portInput.text, out var p)) port = p;

        transport.SetConnectionData(ipAddress, port);
        if (!nm.StartClient()) Debug.LogError("Failed to start client.");
        else Debug.Log($"Connecting to {ipAddress}:{port}");
    }

    public void ShutdownAll()
    {
        if (nm && nm.IsListening) nm.Shutdown();
    }

    bool Ready() => nm != null && transport != null && !nm.IsListening;
}
