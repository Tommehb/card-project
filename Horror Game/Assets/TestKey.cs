using UnityEngine;

public class TestKey : MonoBehaviour
{
    public Rigidbody rb; // Reference to the Rigidbody component
    public float force = 1f; // Force to apply when throwing the key

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component attached to this GameObject
    }

    // Update is called once per frame
    void Update()
    {
        // every 5 seconds, throw the key in a random direction
        // if (Time.time % 5 < 0.1f) // Check if 5 seconds have passed
        // {
        //     ThrowInRandomDirection(); // Call the method to throw the key
        // }
        
        if (transform.position.y < -10f)
        {
            // set y position to 10
            transform.position = new Vector3(transform.position.x, 10f, transform.position.z); // Reset the y position
        }
    }

    // void ThrowInRandomDirection()
    // {
    //     // Generate a random direction
    //     Vector3 randomDirection = Random.insideUnitSphere; // Get a random point inside a sphere
    //     // Cap y so that only some upward movement is allowed
    //     randomDirection.y = Mathf.Clamp(randomDirection.y, 0f, 0.5f); // Limit the upward movement
    //     randomDirection.Normalize(); // Normalize the direction vector

    //     // Apply force in the random direction
    //     rb.AddForce(randomDirection * force, ForceMode.Impulse); // Apply an impulse force
    // }
}
