using UnityEngine;
using UnityEngine.AI;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
    }

    // Update is called once per frame
    void Update()
    {
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
        if (other.CompareTag("Player"))
        {
            // Handle collision with the player
            Debug.Log("Red Mannequin collided with the player!");

            //call the Die function in Player
            Player playerScript = other.GetComponent<Player>();
            if (playerScript != null)
            {
                playerScript.Die(); // Call the Die function in Player
            }
        }
    }
}
