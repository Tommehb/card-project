using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

/*
    The Red Mannequin chases the player if they are within a certain distance.
    If the player is within view (raycast), the mannequin will move towards the player.
    If the player is not in view, it will wander around randomly.
*/

public class RedMannequin : MonoBehaviour
{
    public Transform player; // Reference to the player's transform
    public float chaseDistance = 10f; // Distance at which the mannequin starts chasing the player
    public float speed = 2f; // Speed of the mannequin
    public float wanderDistance = 5f; // Distance for random wandering

    Rigidbody rb; // Reference to the Rigidbody component

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

    // In a co-op session, target the nearest non-downed player (evaluated on the server).
    Transform GetNearestLivePlayer()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return null;
        Transform nearest = null;
        float best = float.MaxValue;
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component

        // Resolve the player at runtime (prefab refs can't point at a scene object)
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
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
        // Call the function to update the mannequin's behavior
        UpdateBehavior();
    }

    // Function to check if the player is within chase distance
    private bool IsPlayerInChaseDistance()
    {
        return Vector3.Distance(transform.position, player.position) < chaseDistance;
    }

    // Function to check if the player is in view
    private bool IsPlayerInView()
    {
        RaycastHit hit;
        Vector3 directionToPlayer = player.position - transform.position;

        // Check if the raycast hits the player
        if (Physics.Raycast(transform.position, directionToPlayer, out hit))
        {
            return hit.transform == player;
        }
        return false;
    }

    // Function to move the mannequin towards the player
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

    // Function to wander randomly
    private void WanderRandomly()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderDistance;
        randomDirection += transform.position;

        NavMeshHit hit;
        NavMesh.SamplePosition(randomDirection, out hit, wanderDistance, 1);
        Vector3 finalPosition = hit.position;

        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.SetDestination(finalPosition);
        }
        else
        {
            // If no NavMeshAgent, move manually
            Vector3 direction = (finalPosition - transform.position).normalized;
            Vector3 targetPosition = transform.position + direction * speed * Time.deltaTime;

            rb.MovePosition(targetPosition);
        }
    }

    // Function to update the mannequin's behavior
    private void UpdateBehavior()
    {
        if (IsPlayerInChaseDistance())
        {
            if (IsPlayerInView())
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
                WanderRandomly();
            }
        }
        else
        {
            WanderRandomly();
        }
    }

    // Function to handle collision with the player
    private void OnTriggerEnter(Collider other)
    {
        if (!HasAuthority) return; // only the server (or single-player) resolves catches
        if (other.CompareTag("Player"))
        {
            Player playerScript = other.GetComponent<Player>();
            if (playerScript == null) return;
            if (InSession) playerScript.ServerKill("Chase"); // server tells that player's client to die
            else playerScript.Die("Chase");
        }
    }
}
