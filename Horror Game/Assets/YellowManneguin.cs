using UnityEngine;
using UnityEngine.AI;

public class YellowManneguin : MonoBehaviour
{
    public Transform player;
    Rigidbody rb;
    public float speed = 2f; // Speed of the mannequin
    public Camera playerCamera; // Reference to the player's camera

    // function to check if the player is looking at the mannequin (within the viewport)
    public bool IsLookingAtMannequin()
    {
        Vector3 screenPoint = playerCamera.WorldToViewportPoint(transform.position);
        return screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;
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
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log("Is looking at mannequin: " + IsLookingAtMannequin());

        // If not being looked at, move towards player
        if (!IsLookingAtMannequin())
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
}
