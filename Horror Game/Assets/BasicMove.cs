// BasicMove.cs
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkObject))]
public class BasicMove : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float rotateSpeed = 180f;

    private CharacterController _cc;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        // Give local player a color cue
        if (IsOwner)
        {
            var mr = GetComponentInChildren<MeshRenderer>();
            if (mr) mr.material.color = new Color(0.3f, 0.8f, 1f, 1f);
            gameObject.name = "Player (Local)";
        }
        else
        {
            gameObject.name = $"Player #{OwnerClientId}";
        }
    }

    void Update()
    {
        if (!IsOwner) return; // only the local owner drives input

        float h = Input.GetAxis("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxis("Vertical");   // W/S or Up/Down

        // Simple WASD move + Rotate with QE
        Vector3 move = (transform.right * h + transform.forward * v) * moveSpeed;

        if (_cc != null) _cc.SimpleMove(move);
        else transform.position += move * Time.deltaTime;

        float yaw = 0f;
        if (Input.GetKey(KeyCode.Q)) yaw -= 1f;
        if (Input.GetKey(KeyCode.E)) yaw += 1f;
        transform.Rotate(0f, yaw * rotateSpeed * Time.deltaTime, 0f);
    }
}
