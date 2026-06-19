using UnityEngine;
using UnityEngine.AI;

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
        if (player == null) return;

        // Stay hidden/disguised until the player comes close, then reveal (the surprise)
        bool close = Vector3.Distance(transform.position, player.position) <= revealDistance;
        SetRevealed(close);
        if (!close)
        {
            stareTimer = 0f;
            isChasing = false;
            return;
        }

        if (IsLookingAtMannequin())
        {
            // If the player is looking at the mannequin, increase the stare timer
            stareTimer += Time.deltaTime;

            // If the stare timer exceeds the required time, start chasing the player
            if (stareTimer >= stareTime && !isChasing)
            {
                isChasing = true;
                Invoke("StopChasing", chaseDuration); // Stop chasing after a set duration
            }
        }
        else
        {
            // If the player stops looking at the mannequin, reset the stare timer
            stareTimer = 0f;
            isChasing = false; // Stop chasing if not being looked at
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
        if (other.CompareTag("Player"))
        {
            // Handle collision with the player
            Debug.Log("Green Mannequin collided with the player!");

            //call the Die function in Player
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.Die("Hide"); // Call the Die function in Player
            }
        }
    }
}
