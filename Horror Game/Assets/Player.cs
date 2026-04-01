using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody))]
public class Player : NetworkBehaviour
{
    public bool isAlive = true; // Player's alive status
    public float walkSpeed = 5f; // Speed of the player when walking
    public float runSpeed = 10f; // Speed of the player when running
    public float jumpForce = 5f; // Force applied when the player jumps
    public Camera playerCamera; // Reference to the player's camera
    public float cameraRotationSpeed = 5f; // Speed of camera rotation
    public GameHandler gameHandler; // Reference to the GameHandler script
    public jumpscare jumpscareHandler; // Reference to the jumpscare script

    private Rigidbody playerBody;
    private AudioListener playerAudioListener;
    private float pitch;

    void Awake()
    {
        playerBody = GetComponent<Rigidbody>();

        if (playerCamera != null)
        {
            playerAudioListener = playerCamera.GetComponent<AudioListener>();
            pitch = playerCamera.transform.localEulerAngles.x;
            if (pitch > 180f)
            {
                pitch -= 360f;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check mouse sensitivity from GameManager
        if (GameManager.instance != null)
        {
            cameraRotationSpeed = GameManager.instance.mouseSensitivity; // Set camera rotation speed based on mouse sensitivity
        }

        ApplyOwnershipState();
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkObject == null || !NetworkObject.IsPlayerObject)
        {
            if (IsServer)
            {
                Debug.LogWarning("Despawning a scene-placed Player object. Multiplayer should use spawned player objects instead.");
                NetworkObject.Despawn(false);
            }

            return;
        }

        ApplyOwnershipState();
    }

    public override void OnGainedOwnership()
    {
        ApplyOwnershipState();
    }

    public override void OnLostOwnership()
    {
        ApplyOwnershipState();
    }

    // Update is called once per frame
    void Update()
    {
        if (!HasLocalControl() || !isAlive)
        {
            return;
        }

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        transform.Rotate(Vector3.up * mouseX * cameraRotationSpeed);

        if (playerCamera != null)
        {
            pitch = Mathf.Clamp(pitch - mouseY * cameraRotationSpeed, -85f, 85f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // WASD relative to camera
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        Vector3 moveDirection = transform.right * moveHorizontal + transform.forward * moveVertical;
        moveDirection.y = 0; // Prevent vertical movement
        moveDirection.Normalize(); // Normalize to prevent faster diagonal movement
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed; // Check if running

        var targetPosition = transform.position + moveDirection * speed * Time.deltaTime;
        if (playerBody != null)
        {
            playerBody.MovePosition(targetPosition);
        }
        else
        {
            transform.position = targetPosition;
        }

        // Jumping
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded()) // Simple ground check
        {
            playerBody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        // Prevent camera from going through the ground
        if (transform.position.y < 0f)
        {
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
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
        if (!HasLocalControl())
        {
            return;
        }

        if (collision.gameObject.CompareTag("Key"))
        {
            gameHandler.KeyFound(); // Call the KeyFound method in GameHandler
            Destroy(collision.gameObject); // Destroy the key GameObject
        }
    }

    // Death function
    public void Die(string reason)
    {
        if (!isAlive) return; // Prevent multiple deaths
        if (!HasLocalControl()) return;
        // Handle player death (e.g., respawn, game over, etc.)
        Debug.Log("Player has died: " + reason);
        // You can add more logic here, such as restarting the level or showing a game over screen
        jumpscareHandler.TriggerJumpscare(); // Trigger the jumpscare

        // emit death event
        float timeSurvived = Time.time - gameHandler.startTime; // Calculate time survived
        gameHandler.PlayerDied(timeSurvived, reason); // Notify GameManager of player death

        isAlive = false; // Mark player as dead
    }

    private bool HasLocalControl()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !IsSpawned)
        {
            return true;
        }

        return IsOwner;
    }

    private bool IsGrounded()
    {
        if (playerBody == null)
        {
            return Mathf.Approximately(transform.position.y, 0f);
        }

        return Mathf.Abs(playerBody.linearVelocity.y) < 0.05f && transform.position.y <= 1.15f;
    }

    private void ApplyOwnershipState()
    {
        var localControl = HasLocalControl();

        if (playerCamera != null)
        {
            playerCamera.enabled = localControl;
        }

        if (playerAudioListener != null)
        {
            playerAudioListener.enabled = localControl;
        }

        if (playerBody != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)
        {
            playerBody.isKinematic = !localControl;
        }
    }
}
