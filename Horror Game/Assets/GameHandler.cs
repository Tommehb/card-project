using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public class GameHandler : MonoBehaviour
{
    public TextMeshProUGUI objectiveText; // Reference to the TextMeshProUGUI component
    public TextMeshProUGUI timeText; // Reference to the TextMeshProUGUI component for time display
    public TextMeshProUGUI reasonText;
    public int keysFound = 0; // Number of keys found
    public int totalKeys = 5; // Total number of keys to find

    public List<GameObject> keys; // List of key GameObjects
    public List<GameObject> blinkMannequins; // List of blink mannequins
    public List<GameObject> hideMannequins; // List of blink mannequins
    public List<GameObject> chaseMannequins; // List of chase mannequins

    public List<Transform> spawnPoints; // List of spawn points for the keys

    public BoxCollider safeZone; // Reference to the safe zone collider

    // start time
    public float startTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startTime = Time.time; // Record the start time
        if (GameManager.instance != null) {
            switch (GameManager.instance.level)
            {
                case 1:
                    objectiveText.text = "Objective: Find all keys (0/5)";
                    totalKeys = 5; // Set total keys for level 1
                    break;
                case 2:
                    objectiveText.text = "Objective: Find all keys (0/10)";
                    totalKeys = 10; // Set total keys for level 2
                    break;
                case 3:
                    objectiveText.text = "Objective: Find all keys (0/20)";
                    totalKeys = 20; // Set total keys for level 3
                    break;
                default:
                    objectiveText.text = "Objective: Find all keys (0/5)";
                    totalKeys = 5; // Default to level 1 if no valid level is set
                    break;
            }

            // Go through the list and disable all keys
            foreach (GameObject key in keys)
            {
                key.SetActive(false); // Disable the key GameObject
            }
            // Enable the keys based on the level
            for (int i = 0; i < totalKeys; i++)
            {
                if (i < keys.Count)
                {
                    keys[i].SetActive(true); // Enable the key GameObject

                    // Randomly Spawn in the following steps:
                    // 1. 50/50 choice between inside or outside
                    int choice = Random.Range(0, 2);
                    // 2. Randomly choose a spawn point from the list
                    int spawnPointIndex = Random.Range(1, spawnPoints.Count);
                    if (choice == 0) {
                        spawnPointIndex = 0; // outside
                        Debug.Log("Spawned outside");
                    } else {
                        Debug.Log("Spawned inside");
                    }

                    // 3. Set the position of the key to the chosen spawn point
                    keys[i].transform.position = spawnPoints[spawnPointIndex].position; // Set the position of the key

                    // 4. Throw the key in a random direction and force
                    Rigidbody rb = keys[i].GetComponent<Rigidbody>(); // Get the Rigidbody component attached to the key
                    if (rb != null)
                    {
                        Vector3 randomDirection = Random.insideUnitSphere; // Get a random point inside a sphere
                        randomDirection.y = Mathf.Clamp(randomDirection.y, 0f, 0.5f); // Limit the upward movement
                        randomDirection.Normalize(); // Normalize the direction vector

                        float force = Random.Range(1f, 5f); // Random force between 1 and 5
                        rb.AddForce(randomDirection * force, ForceMode.Impulse); // Apply an impulse force
                    }
                    else
                    {
                        Debug.LogWarning("Rigidbody component not found on key GameObject.");
                    }
                }
            }
            safeZone.enabled = false; // Disable the safe zone collider

            SetupMannequins(); // Call the method to set up mannequins
        }
    }

    // Update is called once per frame
    void Update()
    {
        objectiveText.text = "Objective: Find all keys (" + keysFound + "/" + totalKeys + ")"; // Update the objective text
        if (keysFound >= totalKeys) // Check if all keys have been found
        {
            objectiveText.text = "Objective: Go to the safe zone!"; // Update the objective text
        }
    }

    public void KeyFound() // Method to call when a key is found
    {
        keysFound++; // Increment the number of keys found
        if (keysFound >= totalKeys) // Check if all keys have been found
        {
            safeZone.enabled = true; // Enable the safe zone collider
        }
    }

    void SetupMannequins()
    {
        // Disable all the mannequins initially
        foreach (GameObject mannequin in blinkMannequins)
        {
            mannequin.SetActive(false); // Disable the blink mannequins
        }
        foreach (GameObject mannequin in hideMannequins)
        {
            mannequin.SetActive(false); // Disable the hide mannequins
        }
        foreach (GameObject mannequin in chaseMannequins)
        {
            mannequin.SetActive(false); // Disable the chase mannequins
        }

        if (GameManager.instance.level == 1) { // 2 of each
            for (int i = 0; i < 2; i++)
            {
                if (i < blinkMannequins.Count)
                {
                    blinkMannequins[i].SetActive(true); // Enable the blink mannequin
                }
                if (i < hideMannequins.Count)
                {
                    hideMannequins[i].SetActive(true); // Enable the hide mannequin
                }
                if (i < chaseMannequins.Count)
                {
                    chaseMannequins[i].SetActive(true); // Enable the chase mannequin
                }
            }
        } else if (GameManager.instance.level == 2) { // 4 of each
            for (int i = 0; i < 4; i++)
            {
                if (i < blinkMannequins.Count)
                {
                    blinkMannequins[i].SetActive(true); // Enable the blink mannequin
                }
                if (i < hideMannequins.Count)
                {
                    hideMannequins[i].SetActive(true); // Enable the hide mannequin
                }
                if (i < chaseMannequins.Count)
                {
                    chaseMannequins[i].SetActive(true); // Enable the chase mannequin
                }
            }
        } else if (GameManager.instance.level == 3) { // 6 of each
            for (int i = 0; i < 6; i++)
            {
                if (i < blinkMannequins.Count)
                {
                    blinkMannequins[i].SetActive(true); // Enable the blink mannequin
                }
                if (i < hideMannequins.Count)
                {
                    hideMannequins[i].SetActive(true); // Enable the hide mannequin
                }
                if (i < chaseMannequins.Count)
                {
                    chaseMannequins[i].SetActive(true); // Enable the chase mannequin
                }
            }
        } else {
            Debug.LogWarning("Invalid level set in GameManager.instance.level");
        }
    }

    public void RestartGame() // Method to restart the game
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload the current scene
    }

    public void ExitToTitle() // Method to exit to the title screen
    {
        SceneManager.LoadScene("Home"); // Load the title screen scene
    }

    public void PlayerDied(float timeSurvived, string reason)
    {
        Debug.Log("Player died after surviving for " + timeSurvived + " seconds. Reason: " + reason);
        // You can add more logic here, such as recording stats or updating a leaderboard

        timeText.text = "Time Survived: " + timeSurvived.ToString("F2") + " seconds";
        reasonText.text = "And got attacked by a: " + reason + " mannequin";
    }
}
