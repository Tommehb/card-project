using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(UnityTransport))]
public class LanSessionManager : MonoBehaviour
{
    public static LanSessionManager Instance { get; private set; }

    [Header("Scene Flow")]
    [SerializeField] private string menuSceneName = "Home";
    [SerializeField] private string lobbySceneName = "MultiplayTest";
    [SerializeField] private string gameplaySceneName = "Eastwood Elementary School";
    [SerializeField] private bool loadLobbySceneAfterHosting = true;

    [Header("Connection Defaults")]
    [SerializeField] private string defaultAddress = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;
    [SerializeField] private string defaultPlayerName = "Player";

    private readonly List<LanNetworkPlayer> registeredPlayers = new();

    private NetworkManager networkManager;
    private UnityTransport transport;
    private bool callbacksRegistered;
    private bool isShuttingDown;
    private string localPlayerName;
    private string statusMessage = "Offline";
    private string currentAddress;
    private ushort currentPort;

    public string StatusMessage => statusMessage;
    public string LocalPlayerName => string.IsNullOrWhiteSpace(localPlayerName) ? defaultPlayerName : localPlayerName;
    public string MenuSceneName => menuSceneName;
    public string LobbySceneName => lobbySceneName;
    public string GameplaySceneName => gameplaySceneName;
    public bool IsSessionActive => networkManager != null && networkManager.IsListening;
    public bool IsHost => networkManager != null && networkManager.IsHost;
    public bool IsServer => networkManager != null && networkManager.IsServer;
    public bool IsClient => networkManager != null && networkManager.IsClient;
    public bool IsInLobbyScene => UnitySceneManager.GetActiveScene().name == lobbySceneName;
    public bool IsInGameplayScene => UnitySceneManager.GetActiveScene().name == gameplaySceneName;

    public LanNetworkPlayer LocalPlayer
    {
        get
        {
            if (networkManager == null || !networkManager.IsConnectedClient || networkManager.LocalClient == null)
            {
                return null;
            }

            var localPlayerObject = networkManager.LocalClient.PlayerObject;
            return localPlayerObject != null ? localPlayerObject.GetComponent<LanNetworkPlayer>() : null;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        networkManager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
        localPlayerName = string.IsNullOrWhiteSpace(localPlayerName) ? defaultPlayerName : localPlayerName;
        currentAddress = defaultAddress;
        currentPort = defaultPort;

        RegisterCallbacks();
    }

    private void OnDestroy()
    {
        UnregisterCallbacks();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ConfigureSceneFlow(string menuScene, string lobbyScene, string gameplayScene)
    {
        if (!string.IsNullOrWhiteSpace(menuScene))
        {
            menuSceneName = menuScene;
        }

        if (!string.IsNullOrWhiteSpace(lobbyScene))
        {
            lobbySceneName = lobbyScene;
        }

        if (!string.IsNullOrWhiteSpace(gameplayScene))
        {
            gameplaySceneName = gameplayScene;
        }
    }

    public void SetLocalPlayerName(string desiredName)
    {
        localPlayerName = string.IsNullOrWhiteSpace(desiredName) ? defaultPlayerName : desiredName.Trim();

        var localPlayer = LocalPlayer;
        if (localPlayer != null)
        {
            localPlayer.SubmitLocalPlayerSettings();
        }
    }

    public bool StartHostSession(string address, ushort port)
    {
        if (!CanStartSession())
        {
            return false;
        }

        currentAddress = string.IsNullOrWhiteSpace(address) ? defaultAddress : address.Trim();
        currentPort = port;
        ConfigureHostTransport(port);

        if (!networkManager.StartHost())
        {
            SetStatus("Failed to start LAN host.");
            return false;
        }

        SetStatus($"Hosting LAN session on UDP {port}.");

        if (loadLobbySceneAfterHosting && !string.IsNullOrWhiteSpace(lobbySceneName) && UnitySceneManager.GetActiveScene().name != lobbySceneName)
        {
            LoadNetworkScene(lobbySceneName);
        }

        return true;
    }

    public bool StartClientSession(string address, ushort port)
    {
        if (!CanStartSession())
        {
            return false;
        }

        currentAddress = string.IsNullOrWhiteSpace(address) ? defaultAddress : address.Trim();
        currentPort = port;
        ConfigureClientTransport(address, port);

        if (!networkManager.StartClient())
        {
            SetStatus($"Failed to connect to {address}:{port}.");
            return false;
        }

        SetStatus($"Connecting to {address}:{port}...");
        return true;
    }

    public bool StartServerOnlySession(ushort port)
    {
        if (!CanStartSession())
        {
            return false;
        }

        currentPort = port;
        ConfigureHostTransport(port);

        if (!networkManager.StartServer())
        {
            SetStatus("Failed to start LAN server.");
            return false;
        }

        SetStatus($"LAN server started on UDP {port}.");
        return true;
    }

    public void ShutdownAndReturnToMenu()
    {
        ShutdownSession(true);
    }

    public void ShutdownSession(bool loadMenuScene)
    {
        if (networkManager == null)
        {
            return;
        }

        isShuttingDown = true;
        ClearRegisteredPlayers();

        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        SetStatus("Offline");

        if (loadMenuScene && !string.IsNullOrWhiteSpace(menuSceneName) && UnitySceneManager.GetActiveScene().name != menuSceneName)
        {
            UnitySceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
        }
    }

    public bool TryStartGame()
    {
        if (!CanLocalPlayerStartGame())
        {
            SetStatus("The host can only start once every player is ready.");
            return false;
        }

        ResetAllReadyStates();
        SetStatus($"Loading {gameplaySceneName}...");
        return LoadNetworkScene(gameplaySceneName);
    }

    public bool ReturnToLobby()
    {
        if (!IsServer || string.IsNullOrWhiteSpace(lobbySceneName))
        {
            return false;
        }

        ResetAllReadyStates();
        SetStatus($"Returning to {lobbySceneName}...");
        return LoadNetworkScene(lobbySceneName);
    }

    public bool RestartCurrentNetworkScene()
    {
        if (!IsServer)
        {
            return false;
        }

        var currentScene = UnitySceneManager.GetActiveScene().name;
        if (string.IsNullOrWhiteSpace(currentScene))
        {
            return false;
        }

        SetStatus($"Reloading {currentScene}...");
        return LoadNetworkScene(currentScene);
    }

    public bool CanLocalPlayerStartGame()
    {
        return IsHost && IsInLobbyScene && AreAllPlayersReady();
    }

    public bool AreAllPlayersReady()
    {
        var players = GetRegisteredPlayersSorted();
        if (players.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < players.Count; i++)
        {
            if (!players[i].IsReady)
            {
                return false;
            }
        }

        return true;
    }

    public string BuildLobbyRosterText()
    {
        var players = GetRegisteredPlayersSorted();
        if (players.Count == 0)
        {
            return "No network players have spawned yet.";
        }

        var builder = new StringBuilder();
        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var role = player.OwnerClientId == NetworkManager.ServerClientId ? "Host" : "Client";
            builder.Append(i + 1);
            builder.Append(". ");
            builder.Append(player.PlayerName);
            builder.Append(" (");
            builder.Append(role);
            builder.Append(") - ");
            builder.Append(player.IsReady ? "Ready" : "Not Ready");
            if (i < players.Count - 1)
            {
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    public void RegisterPlayer(LanNetworkPlayer player)
    {
        if (player == null || registeredPlayers.Contains(player))
        {
            return;
        }

        registeredPlayers.Add(player);
        SortPlayersAndAssignSlots();

        if (IsServer)
        {
            StartCoroutine(AssignSceneSpawnsNextFrame(UnitySceneManager.GetActiveScene().name));
        }
    }

    public void UnregisterPlayer(LanNetworkPlayer player)
    {
        if (player == null)
        {
            return;
        }

        if (registeredPlayers.Remove(player))
        {
            SortPlayersAndAssignSlots();
        }
    }

    private void RegisterCallbacks()
    {
        if (callbacksRegistered || networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback += HandleClientConnected;
        networkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            networkManager.SceneManager.OnSynchronizeComplete += HandleSynchronizeComplete;
        }

        callbacksRegistered = true;
    }

    private void UnregisterCallbacks()
    {
        if (!callbacksRegistered || networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback -= HandleClientConnected;
        networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;

        if (networkManager.SceneManager != null)
        {
            networkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            networkManager.SceneManager.OnSynchronizeComplete -= HandleSynchronizeComplete;
        }

        callbacksRegistered = false;
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (networkManager == null)
        {
            return;
        }

        if (clientId == networkManager.LocalClientId)
        {
            SetStatus(IsHost ? $"Hosting LAN session on UDP {currentPort}." : $"Connected to LAN host at {currentAddress}:{currentPort}.");
        }
        else if (IsServer)
        {
            SetStatus($"Client {clientId} joined the LAN session.");
        }
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        if (networkManager == null)
        {
            return;
        }

        if (isShuttingDown)
        {
            return;
        }

        if (clientId == networkManager.LocalClientId)
        {
            SetStatus("Disconnected from LAN session.");

            if (!string.IsNullOrWhiteSpace(menuSceneName) && UnitySceneManager.GetActiveScene().name != menuSceneName)
            {
                UnitySceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
            }

            return;
        }

        if (IsServer)
        {
            SetStatus($"Client {clientId} left the LAN session.");
            SortPlayersAndAssignSlots();
        }
    }

    private void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer)
        {
            return;
        }

        StartCoroutine(AssignSceneSpawnsNextFrame(sceneName));
    }

    private void HandleSynchronizeComplete(ulong clientId)
    {
        if (!IsServer)
        {
            return;
        }

        StartCoroutine(AssignSceneSpawnsNextFrame(UnitySceneManager.GetActiveScene().name));
    }

    private IEnumerator AssignSceneSpawnsNextFrame(string sceneName)
    {
        yield return null;
        AssignSceneSpawns(sceneName);
    }

    private void AssignSceneSpawns(string sceneName)
    {
        var spawnType = GetSpawnPointTypeForScene(sceneName);
        if (spawnType == LanSpawnPointType.None)
        {
            return;
        }

        var spawnPoints = GetSpawnPointsForScene(sceneName, spawnType);
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"No {spawnType} spawn points were found in scene '{sceneName}'.");
            return;
        }

        var players = GetRegisteredPlayersSorted();
        for (var i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var spawnPoint = spawnPoints[i % spawnPoints.Count];
            player.TeleportOwnerTo(spawnPoint.transform.position, spawnPoint.transform.rotation);
        }
    }

    private List<LanSpawnPoint> GetSpawnPointsForScene(string sceneName, LanSpawnPointType spawnType)
    {
        var points = new List<LanSpawnPoint>();
        var foundPoints = FindObjectsByType<LanSpawnPoint>(FindObjectsSortMode.None);
        for (var i = 0; i < foundPoints.Length; i++)
        {
            var point = foundPoints[i];
            if (point == null)
            {
                continue;
            }

            if (point.gameObject.scene.name != sceneName || point.SpawnType != spawnType)
            {
                continue;
            }

            points.Add(point);
        }

        points.Sort((left, right) => left.SlotIndex.CompareTo(right.SlotIndex));
        return points;
    }

    private LanSpawnPointType GetSpawnPointTypeForScene(string sceneName)
    {
        if (sceneName == lobbySceneName)
        {
            return LanSpawnPointType.Lobby;
        }

        if (sceneName == gameplaySceneName)
        {
            return LanSpawnPointType.Gameplay;
        }

        return LanSpawnPointType.None;
    }

    private void SortPlayersAndAssignSlots()
    {
        registeredPlayers.Sort((left, right) => left.OwnerClientId.CompareTo(right.OwnerClientId));

        if (!IsServer)
        {
            return;
        }

        for (var i = 0; i < registeredPlayers.Count; i++)
        {
            registeredPlayers[i].SetSlotIndex(i);
        }
    }

    private List<LanNetworkPlayer> GetRegisteredPlayersSorted()
    {
        registeredPlayers.RemoveAll(player => player == null);
        registeredPlayers.Sort((left, right) => left.OwnerClientId.CompareTo(right.OwnerClientId));
        return registeredPlayers;
    }

    private void ResetAllReadyStates()
    {
        if (!IsServer)
        {
            return;
        }

        var players = GetRegisteredPlayersSorted();
        for (var i = 0; i < players.Count; i++)
        {
            players[i].ResetReadyState();
        }
    }

    private void ClearRegisteredPlayers()
    {
        registeredPlayers.Clear();
    }

    private void ConfigureHostTransport(ushort port)
    {
        transport.SetConnectionData("0.0.0.0", port);
    }

    private void ConfigureClientTransport(string address, ushort port)
    {
        transport.SetConnectionData(string.IsNullOrWhiteSpace(address) ? defaultAddress : address.Trim(), port);
    }

    private bool CanStartSession()
    {
        if (networkManager == null || transport == null)
        {
            SetStatus("LAN session manager is missing a NetworkManager or UnityTransport.");
            return false;
        }

        if (networkManager.IsListening)
        {
            SetStatus("A LAN session is already running.");
            return false;
        }

        isShuttingDown = false;
        return true;
    }

    private bool LoadNetworkScene(string sceneName)
    {
        if (networkManager == null || !networkManager.IsServer)
        {
            return false;
        }

        var result = networkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        return result == SceneEventProgressStatus.Started;
    }

    private void SetStatus(string newStatus)
    {
        statusMessage = string.IsNullOrWhiteSpace(newStatus) ? "Offline" : newStatus;
    }
}
