using UnityEngine;
using UnityEngine.UI;

public class jumpscare : MonoBehaviour
{
    public GameObject jumpscareParent; // Reference to the parent GameObject containing the jumpscare UI elements
    public GameObject endGameUI; // Reference to the end game UI
    public Image jumpscareImage; // Reference to the UI Image component for the jumpscare
    public AudioSource jumpscareSound; // Reference to the AudioSource component for the jumpscare
    public float displayDuration = 2f; // Duration to display the jumpscare image
    private float timer = 0f; // Timer to track the display duration
    private bool isJumpscareActive = false; // Flag to indicate if the jumpscare
    public void TriggerJumpscare()
    {
        if (!isJumpscareActive)
        {
            // set parent active
            jumpscareParent.SetActive(true);

            jumpscareImage.enabled = true; // Show the jumpscare image
            jumpscareSound.Play(); // Play the jumpscare sound
            isJumpscareActive = true; // Set the flag to indicate the jumpscare is active
            timer = 0f; // Reset the timer

            // shake the camera
            Camera.main.GetComponent<CameraShake>().Shake(0.5f, 0.5f);

            Invoke("HideJumpscare", displayDuration); // Schedule hiding the jumpscare after the display duration
        }
    }

    private void HideJumpscare()
    {
        jumpscareParent.SetActive(false); // Hide the parent GameObject
        jumpscareImage.enabled = false; // Hide the jumpscare image
        isJumpscareActive = false; // Reset the flag
        endGameUI.SetActive(true); // Show the end game UI
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
