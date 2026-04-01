using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class LanNetworkPlayer : NetworkBehaviour
{
    private readonly NetworkVariable<FixedString64Bytes> playerName = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<bool> ready = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<int> slotIndex = new(
        -1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public string PlayerName => string.IsNullOrWhiteSpace(playerName.Value.ToString())
        ? $"Player {OwnerClientId}"
        : playerName.Value.ToString();

    public bool IsReady => ready.Value;
    public int SlotIndex => slotIndex.Value;

    public override void OnNetworkSpawn()
    {
        LanSessionManager.Instance?.RegisterPlayer(this);

        if (IsServer && string.IsNullOrWhiteSpace(playerName.Value.ToString()))
        {
            playerName.Value = MakeFixedString(PlayerName);
        }

        if (IsOwner)
        {
            SubmitLocalPlayerSettings();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (LanSessionManager.Instance != null)
        {
            LanSessionManager.Instance.UnregisterPlayer(this);
        }
    }

    public void SubmitLocalPlayerSettings()
    {
        if (!IsOwner)
        {
            return;
        }

        var session = LanSessionManager.Instance;
        var localName = session != null ? session.LocalPlayerName : string.Empty;
        SetPlayerNameServerRpc(MakeFixedString(string.IsNullOrWhiteSpace(localName) ? PlayerName : localName));
    }

    public void SetReady(bool isReady)
    {
        if (!IsOwner)
        {
            return;
        }

        SetReadyServerRpc(isReady);
    }

    public void ToggleReady()
    {
        SetReady(!IsReady);
    }

    public void SetSlotIndex(int newSlotIndex)
    {
        if (!IsServer)
        {
            return;
        }

        slotIndex.Value = Mathf.Max(0, newSlotIndex);
    }

    public void ResetReadyState()
    {
        if (!IsServer)
        {
            return;
        }

        ready.Value = false;
    }

    public void TeleportOwnerTo(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (!IsServer)
        {
            return;
        }

        var sendParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { OwnerClientId }
            }
        };

        TeleportOwnerClientRpc(worldPosition, worldRotation.eulerAngles, sendParams);
    }

    [ServerRpc]
    private void SetPlayerNameServerRpc(FixedString64Bytes desiredName)
    {
        var sanitized = SanitizeName(desiredName.ToString(), OwnerClientId);
        playerName.Value = MakeFixedString(sanitized);
    }

    [ServerRpc]
    private void SetReadyServerRpc(bool isReady)
    {
        ready.Value = isReady;
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 worldPosition, Vector3 worldEulerAngles, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner)
        {
            return;
        }

        ApplyTeleport(worldPosition, Quaternion.Euler(worldEulerAngles));
    }

    private void ApplyTeleport(Vector3 worldPosition, Quaternion worldRotation)
    {
        if (TryGetComponent<CharacterController>(out var controller))
        {
            controller.enabled = false;
        }

        if (TryGetComponent<Rigidbody>(out var body))
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.position = worldPosition;
            body.rotation = worldRotation;
        }
        else
        {
            transform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    private static string SanitizeName(string rawName, ulong ownerClientId)
    {
        var trimmed = rawName == null ? string.Empty : rawName.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? $"Player {ownerClientId}" : trimmed;
    }

    private static FixedString64Bytes MakeFixedString(string value)
    {
        var fixedString = new FixedString64Bytes();
        fixedString.Append(value ?? string.Empty);
        return fixedString;
    }
}
