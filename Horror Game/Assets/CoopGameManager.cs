using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/*
    CoopGameManager — the server-authoritative co-op layer for LAN multiplayer.

    Lives on the same GameObject as GameHandler (which must also have a NetworkObject).
    It is only ever active inside a LAN session: in single-player it is never spawned,
    so GameHandler.IsCoop is false and the original local logic runs untouched.

    Responsibilities (server authority, replicated to all clients):
      - shared key objective    (keysFound / totalKeys NetworkVariables)
      - deterministic key layout (keySpawnIndices NetworkList, synced on spawn)
      - networked key pickup     (client -> ServerRpc -> count++ + despawn for everyone)
      - safe-zone unlock state
      - team win/lose            (win when a survivor escapes; lose when all are down)

    GameHandler exposes the scene references + GameObject manipulation; this class owns
    the network state and RPCs and calls back into GameHandler.

    NOTE: late-join after gameplay has started is not supported in this slice (consistent
    with the LAN checklist); all players are expected to load the gameplay scene together.
*/
[RequireComponent(typeof(GameHandler))]
public class CoopGameManager : NetworkBehaviour
{
    // game state constants (NetworkVariable<int> avoids enum-serialization quirks)
    public const int StateInProgress = 0;
    public const int StateWon = 1;
    public const int StateLost = 2;

    private readonly NetworkVariable<int> keysFound = new(0);
    private readonly NetworkVariable<int> totalKeys = new(0);
    private readonly NetworkVariable<int> alivePlayers = new(0);
    private readonly NetworkVariable<int> state = new(StateInProgress);
    private readonly NetworkVariable<bool> safeZoneUnlocked = new(false);

    // Per-key spawn-point index chosen by the server; replicated to every client (and to
    // late-spawning client objects) as part of normal NetworkList synchronization.
    private readonly NetworkList<int> keySpawnIndices = new();

    private GameHandler handler;

    // Server-side set of clients that are downed (spectating). Mannequins skip them.
    private readonly HashSet<ulong> downedClients = new();

    public static CoopGameManager Instance { get; private set; }
    public bool IsClientDowned(ulong clientId) => downedClients.Contains(clientId);

    public bool IsActive => IsSpawned;
    public int KeysFound => keysFound.Value;
    public int TotalKeys => totalKeys.Value;
    public bool AllKeysFound => totalKeys.Value > 0 && keysFound.Value >= totalKeys.Value;

    private void Awake()
    {
        handler = GetComponent<GameHandler>();
    }

    public override void OnNetworkSpawn()
    {
        Instance = this;
        keysFound.OnValueChanged += HandleKeysChanged;
        totalKeys.OnValueChanged += HandleKeysChanged;
        state.OnValueChanged += HandleStateChanged;
        safeZoneUnlocked.OnValueChanged += HandleSafeZoneChanged;
        keySpawnIndices.OnListChanged += HandleKeyLayoutChanged;

        // Every client builds the local scene (mannequins, key-collected tracking), then
        // applies the current replicated state.
        handler.CoopClientSetup();
        ApplyKeyLayout();
        handler.SetSafeZoneEnabled(safeZoneUnlocked.Value);
        handler.OnCoopObjectiveChanged(keysFound.Value, totalKeys.Value);

        if (IsServer)
        {
            ServerStartRun();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (Instance == this) Instance = null;
        keysFound.OnValueChanged -= HandleKeysChanged;
        totalKeys.OnValueChanged -= HandleKeysChanged;
        state.OnValueChanged -= HandleStateChanged;
        safeZoneUnlocked.OnValueChanged -= HandleSafeZoneChanged;
        keySpawnIndices.OnListChanged -= HandleKeyLayoutChanged;
    }

    private void ServerStartRun()
    {
        int level = GameManager.instance != null ? GameManager.instance.level : 1;
        totalKeys.Value = level == 2 ? 10 : level == 3 ? 20 : 5;

        // Server chooses where the keys go; writing the NetworkList replicates the layout
        // to all clients so every screen shows the same keys (addressed by their index in
        // GameHandler.keys, which is identical scene data on every client).
        keySpawnIndices.Clear();
        int count = Mathf.Min(totalKeys.Value, handler.KeyCount);
        int spawnPointCount = handler.SpawnPointCount;
        for (int i = 0; i < count; i++)
        {
            keySpawnIndices.Add(spawnPointCount > 0 ? Random.Range(0, spawnPointCount) : 0);
        }

        alivePlayers.Value = Mathf.Max(1, NetworkManager.ConnectedClientsList.Count);
    }

    private void ApplyKeyLayout()
    {
        int n = keySpawnIndices.Count;
        if (n == 0) return;
        var indices = new int[n];
        for (int i = 0; i < n; i++) indices[i] = keySpawnIndices[i];
        handler.PlaceCoopKeys(indices);
    }

    private void HandleKeyLayoutChanged(NetworkListEvent<int> _)
    {
        ApplyKeyLayout();
    }

    // ---- key pickup (called from the owning client's collision) ----
    [ServerRpc(RequireOwnership = false)]
    public void RequestPickupServerRpc(int keyIndex)
    {
        if (state.Value != StateInProgress) return;
        if (keyIndex < 0 || keyIndex >= handler.KeyCount) return;
        if (handler.IsKeyCollected(keyIndex)) return;

        handler.MarkKeyCollectedServer(keyIndex);
        keysFound.Value = Mathf.Min(keysFound.Value + 1, totalKeys.Value);
        CollectKeyClientRpc(keyIndex);

        if (AllKeysFound)
        {
            safeZoneUnlocked.Value = true;
        }
    }

    [ClientRpc]
    private void CollectKeyClientRpc(int keyIndex)
    {
        handler.DeactivateKey(keyIndex);
    }

    // ---- escape / team win (called from the owning client at the safe zone) ----
    [ServerRpc(RequireOwnership = false)]
    public void RequestEscapeServerRpc()
    {
        if (state.Value != StateInProgress) return;
        if (!AllKeysFound) return;
        state.Value = StateWon;
    }

    // ---- death (called from the dying player's client) ----
    [ServerRpc(RequireOwnership = false)]
    public void ReportDeathServerRpc(ServerRpcParams rpcParams = default)
    {
        if (state.Value != StateInProgress) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (!downedClients.Add(sender)) return; // already counted this player

        alivePlayers.Value = Mathf.Max(0, alivePlayers.Value - 1);
        if (alivePlayers.Value <= 0)
        {
            state.Value = StateLost;
        }
    }

    private void HandleKeysChanged(int previous, int current)
    {
        handler.OnCoopObjectiveChanged(keysFound.Value, totalKeys.Value);
    }

    private void HandleSafeZoneChanged(bool previous, bool current)
    {
        handler.SetSafeZoneEnabled(current);
    }

    private void HandleStateChanged(int previous, int current)
    {
        if (current == StateWon) handler.OnCoopWon();
        else if (current == StateLost) handler.OnCoopLost();
    }
}
