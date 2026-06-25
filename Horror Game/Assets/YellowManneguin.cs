using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class YellowManneguin : MonoBehaviour
{
    public Transform player;
    Rigidbody rb;
    public float speed = 2f; // Speed of the mannequin
    public Camera playerCamera; // Reference to the player's camera

    // --- Multiplayer authority helpers ---
    bool InSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    bool HasAuthority => !InSession || NetworkManager.Singleton.IsServer; // run AI in single-player or on the server

    private bool motionDisabled;
    // On non-authoritative clients, stop the NavMeshAgent/Rigidbody so they don't fight NetworkTransform.
    void DisableLocalMotion()
    {
        if (motionDisabled) return;
        motionDisabled = true;
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        if (rb != null) rb.isKinematic = true;
    }

    Transform GetNearestLivePlayer()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return null;
        Transform nearest = null; float best = float.MaxValue;
        foreach (NetworkClient client in nm.ConnectedClientsList)
        {
            if (CoopGameManager.Instance != null && CoopGameManager.Instance.IsClientDowned(client.ClientId)) continue;
            NetworkObject po = client.PlayerObject;
            if (po == null) continue;
            float d = Vector3.Distance(transform.position, po.transform.position);
            if (d < best) { best = d; nearest = po.transform; }
        }
        return nearest;
    }

    // Server-side proxy for "is this player looking at me" (the server has no client camera).
    bool IsLookedAtBy(Transform p)
    {
        if (p == null) return false;
        Vector3 to = transform.position - p.position; to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return true;
        return Vector3.Dot(p.forward, to.normalized) > 0.5f; // ~60-degree front cone
    }

    // A watched Weeping-Angel freezes if ANY live player is looking at it.
    bool IsWatchedByAnyone()
    {
        if (!InSession) return IsLookingAtMannequin(); // single-player: accurate camera viewport check
        var nm = NetworkManager.Singleton;
        if (nm == null) return false;
        foreach (NetworkClient client in nm.ConnectedClientsList)
        {
            if (CoopGameManager.Instance != null && CoopGameManager.Instance.IsClientDowned(client.ClientId)) continue;
            NetworkObject po = client.PlayerObject;
            if (po == null) continue;
            if (IsLookedAtBy(po.transform)) return true;
        }
        return false;
    }

    // function to check if the player is looking at the mannequin (within the viewport)
    public bool IsLookingAtMannequin()
    {
        if (playerCamera == null) return false;
        Vector3 screenPoint = playerCamera.WorldToViewportPoint(transform.position);
        return screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Vector3 targetPosition = transform.position + direction * speed * Time.deltaTime;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // If no NavMeshAgent, move manually
            rb.MovePosition(targetPosition);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component

        // Resolve the player + camera at runtime (prefab refs can't point at a scene object)
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (playerCamera == null) playerCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (!HasAuthority) { DisableLocalMotion(); return; } // clients receive position via NetworkTransform
        if (InSession)
        {
            Transform target = GetNearestLivePlayer();
            if (target != null) player = target;
        }
        if (player == null) return;

        // If not being looked at by anyone, move towards the nearest player
        if (!IsWatchedByAnyone())
        {
            MoveTowardsPlayer();
            // rotate about the y axis to face the player
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            lookRotation.x = 0; // Keep the rotation only on the y-axis
            lookRotation.z = 0; // Keep the rotation only on the y-axis
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * speed);
        }
        else
        {
            // stop moving when being looked at
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.SetDestination(transform.position);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasAuthority) return; // only the server (or single-player) resolves catches
        if (other.CompareTag("Player"))
        {
            Player playerScript = other.GetComponent<Player>();
            if (playerScript == null) return;
            if (InSession) playerScript.ServerKill("Blink");
            else playerScript.Die("Blink");
        }
    }
}
