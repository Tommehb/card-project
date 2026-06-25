using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

// near invisible, will not move unless the player stares for too long at it

public class GreenMannequin : MonoBehaviour
{
    public float stareTime = 3f; // Time the player must stare at the mannequin to trigger movement
    public float chaseDuration = 5f; // Time the mannequin will chase the player after being stared at
    public Transform player; // Reference to the player's transform
    public Camera playerCamera; // Reference to the player's camera
    public float speed = 2f; // Speed of the mannequin when chasing the player
    Rigidbody rb; // Reference to the Rigidbody component
    private float stareTimer = 0f; // Timer to track how long the player has been staring
    private bool isChasing = false; // Flag to indicate if the mannequin is currently chasing the player

    public float revealDistance = 8f; // Within this distance the mannequin reveals itself (the surprise)
    private Renderer[] renderers; // Cached renderers used to hide/reveal the mannequin
    private bool revealed = true; // Whether the mannequin is currently visible

    // --- Multiplayer authority helpers ---
    bool InSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    bool HasAuthority => !InSession || NetworkManager.Singleton.IsServer; // run AI in single-player or on the server

    private Transform[] cachedPlayers;
    private float playerCacheTimer;

    void RefreshPlayers()
    {
        Player[] ps = FindObjectsByType<Player>(FindObjectsInactive.Exclude);
        cachedPlayers = new Transform[ps.Length];
        for (int i = 0; i < ps.Length; i++) cachedPlayers[i] = ps[i].transform;
    }

    // Reveal visual is evaluated on EVERY client (positions are NetworkTransform-synced) so
    // all players see the same hidden -> revealed state.
    bool IsAnyPlayerWithin(float dist)
    {
        if (cachedPlayers == null) return false;
        float d2 = dist * dist;
        foreach (Transform t in cachedPlayers)
        {
            if (t == null) continue;
            if ((t.position - transform.position).sqrMagnitude <= d2) return true;
        }
        return false;
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
        return Vector3.Dot(p.forward, to.normalized) > 0.5f;
    }

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

    public bool IsLookingAtMannequin()
    {
        if (playerCamera == null) return false;
        Vector3 screenPoint = playerCamera.WorldToViewportPoint(transform.position);
        return screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;
    }

    // Hide or reveal the mannequin's visuals (the "disguise until close" behavior)
    void SetRevealed(bool show)
    {
        if (revealed == show) return;
        revealed = show;
        if (renderers != null)
            foreach (Renderer r in renderers)
                if (r != null) r.enabled = show;
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

    private void StopChasing()
    {
        isChasing = false; // Reset chasing flag after the chase duration
        stareTimer = 0f; // Reset the stare timer
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

        renderers = GetComponentsInChildren<Renderer>(true); // Cache for hide/reveal
    }

    // Update is called once per frame
    void Update()
    {
        // Refresh the local player list occasionally (works on server, clients, and single-player).
        playerCacheTimer -= Time.deltaTime;
        if (cachedPlayers == null || playerCacheTimer <= 0f) { RefreshPlayers(); playerCacheTimer = 0.5f; }

        // Hide/reveal is evaluated on EVERY client so all players see the surprise consistently.
        bool anyoneClose = (player != null && Vector3.Distance(transform.position, player.position) <= revealDistance)
            || IsAnyPlayerWithin(revealDistance);
        SetRevealed(anyoneClose);

        if (!HasAuthority) return; // AI is server-authoritative (single-player runs it locally)

        if (InSession)
        {
            Transform target = GetNearestLivePlayer();
            if (target != null) player = target;
        }
        if (player == null) return;

        if (!anyoneClose)
        {
            stareTimer = 0f;
            isChasing = false;
            return;
        }

        // Stare at it long enough (any player) and it starts chasing.
        if (IsWatchedByAnyone())
        {
            stareTimer += Time.deltaTime;
            if (stareTimer >= stareTime && !isChasing)
            {
                isChasing = true;
                Invoke("StopChasing", chaseDuration); // Stop chasing after a set duration
            }
        }
        else
        {
            stareTimer = 0f;
            isChasing = false;
        }

        // If the mannequin is currently chasing the player, move towards the player
        if (isChasing)
        {
            MoveTowardsPlayer();
            // Rotate to face the player
            Vector3 direction = (player.position - transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            lookRotation.x = 0; // Keep the rotation only on the y-axis
            lookRotation.z = 0; // Keep the rotation only on the y-axis
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * speed);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!HasAuthority) return; // only the server (or single-player) resolves catches
        if (other.CompareTag("Player"))
        {
            Player playerScript = other.GetComponent<Player>();
            if (playerScript == null) return;
            if (InSession) playerScript.ServerKill("Hide");
            else playerScript.Die("Hide");
        }
    }
}
