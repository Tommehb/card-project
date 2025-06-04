using UnityEngine;

public class Player : MonoBehaviour
{
    public float walkSpeed = 5f; // Speed of the player when walking
    public float runSpeed = 10f; // Speed of the player when running
    public float jumpForce = 5f; // Force applied when the player jumps
    public Camera playerCamera; // Reference to the player's camera
    public float cameraRotationSpeed = 2f; // Speed of camera rotation
    public GameHandler gameHandler; // Reference to the GameHandler script

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        Vector3 cameraRotation = playerCamera.transform.eulerAngles;
        cameraRotation.x -= mouseY * cameraRotationSpeed; // Rotate up and down
        cameraRotation.y += mouseX * cameraRotationSpeed; // Rotate left and right
        // cameraRotation.x = Mathf.Clamp(cameraRotation.x, -90f, 90f); // Clamp vertical rotation to prevent flipping
        // Apply the rotation to the camera
        // and the player object
        playerCamera.transform.eulerAngles = cameraRotation;

        // WASD relative to camera
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = playerCamera.transform.right * moveHorizontal + playerCamera.transform.forward * moveVertical;
        moveDirection.y = 0; // Prevent vertical movement
        moveDirection.Normalize(); // Normalize to prevent faster diagonal movement
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed; // Check if running
        transform.position += moveDirection * speed * Time.deltaTime;
        // Jumping
        if (Input.GetKeyDown(KeyCode.Space) && Mathf.Approximately(transform.position.y, 0f)) // Simple ground check
        {
            GetComponent<Rigidbody>().AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        // Prevent camera from going through the ground
        if (transform.position.y < 0f)
        {
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        }
        // Prevent camera from going through walls
        RaycastHit hit;
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, 1f))
        {
            if (hit.collider.CompareTag("Wall"))
            {
                transform.position = new Vector3(hit.point.x, transform.position.y, hit.point.z);
            }
        }
        // Prevent camera from going through the ceiling
        if (transform.position.y > 10f) // Assuming 10f is the height of the ceiling
        {
            transform.position = new Vector3(transform.position.x, 10f, transform.position.z);
        }
        // Prevent camera from going through the floor
        if (transform.position.y < 0f) // Assuming 0f is the height of the floor
        {
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        }
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Key"))
        {
            gameHandler.KeyFound(); // Call the KeyFound method in GameHandler
            Destroy(collision.gameObject); // Destroy the key GameObject
        }
    }

    // Death function
    public void Die()
    {
        // Handle player death (e.g., respawn, game over, etc.)
        Debug.Log("Player has died.");
        // You can add more logic here, such as restarting the level or showing a game over screen
    }
}
