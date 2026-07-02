using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;
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
    public GameObject winUI; // Optional UI panel shown when the player escapes
    public AudioClip ambientClip; // Optional ambient soundscape (else auto-loaded from Resources)

    // start time
    public float startTime;

    private CoopGameManager coop; // present only when the LAN co-op layer is wired in
    private bool[] keyCollected;  // replicated key-collection tracking (co-op)

    // True while a server-authoritative LAN co-op game is running.
    public bool IsCoop => coop != null && coop.IsActive;
    public int KeyCount => keys != null ? keys.Count : 0;
    public int SpawnPointCount => spawnPoints != null ? spawnPoints.Count : 0;
    public bool CoopAllKeysFound => coop != null && coop.AllKeysFound;
    public void CoopRequestPickup(int keyIndex) { if (coop != null) coop.RequestPickupServerRpc(keyIndex); }
    public void CoopRequestEscape() { if (coop != null) coop.RequestEscapeServerRpc(); }
    public void CoopReportDeath() { if (coop != null) coop.ReportDeathServerRpc(); }

    void Awake()
    {
        coop = GetComponent<CoopGameManager>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        EnsureSupportSystems(); // Auto-create the logger / safe-zone trigger / ambience so no manual wiring is needed

        // Hide the cursor and lock it to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        startTime = Time.time; // Record the start time

        // In a LAN session the server-authoritative CoopGameManager drives setup (key
        // placement, objective, safe zone) via OnNetworkSpawn. Outside a session, run the
        // original single-player setup below.
        bool inNetworkSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (inNetworkSession && coop != null)
        {
            return;
        }

        SinglePlayerSetup();
    }

    // Original single-player key spawning + mannequin setup.
    void SinglePlayerSetup()
    {
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
        if (IsCoop) return; // objective UI is driven by CoopGameManager replication

        objectiveText.text = "Objective: Find all keys (" + keysFound + "/" + totalKeys + ")"; // Update the objective text
        if (keysFound >= totalKeys) // Check if all keys have been found
        {
            objectiveText.text = "Objective: Go to the safe zone!"; // Update the objective text
        }
    }

    // ---- Co-op (LAN) hooks called by CoopGameManager ----

    // Per-client scene setup at network spawn: enable mannequins locally and reset key tracking.
    public void CoopClientSetup()
    {
        keyCollected = new bool[KeyCount];
        // In co-op every placed mannequin stays active; its AI is server-authoritative
        // (see the mannequin scripts) and its position syncs to clients via NetworkTransform.
    }

    // Place keys deterministically on every client using the server-chosen spawn indices.
    public void PlaceCoopKeys(int[] spawnIndices)
    {
        if (keys == null) return;
        if (keyCollected == null || keyCollected.Length != keys.Count) keyCollected = new bool[keys.Count];

        for (int i = 0; i < keys.Count; i++)
            if (keys[i] != null) keys[i].SetActive(false);

        for (int i = 0; i < spawnIndices.Length && i < keys.Count; i++)
        {
            GameObject key = keys[i];
            if (key == null) continue;
            key.SetActive(true);

            int sp = SpawnPointCount > 0 ? Mathf.Clamp(spawnIndices[i], 0, SpawnPointCount - 1) : -1;
            if (sp >= 0) key.transform.position = spawnPoints[sp].position;

            Rigidbody rb = key.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // deterministic: no physics throw to desync across clients
            }
        }
    }

    public bool IsKeyCollected(int index) =>
        keyCollected != null && index >= 0 && index < keyCollected.Length && keyCollected[index];

    public void MarkKeyCollectedServer(int index)
    {
        if (keyCollected != null && index >= 0 && index < keyCollected.Length) keyCollected[index] = true;
    }

    public void DeactivateKey(int index)
    {
        if (keyCollected != null && index >= 0 && index < keyCollected.Length) keyCollected[index] = true;
        if (keys != null && index >= 0 && index < keys.Count && keys[index] != null) keys[index].SetActive(false);
    }

    public void SetSafeZoneEnabled(bool enabled)
    {
        if (safeZone != null) safeZone.enabled = enabled;
    }

    public void OnCoopObjectiveChanged(int found, int total)
    {
        keysFound = found;
        totalKeys = total;
        if (objectiveText == null) return;
        objectiveText.text = (total > 0 && found >= total)
            ? "Objective: Go to the safe zone!"
            : "Objective: Find all keys (" + found + "/" + total + ")";
    }

    // Broadcast to all clients when a survivor escapes (team win).
    public void OnCoopWon()
    {
        if (objectiveText != null) objectiveText.text = "Your team escaped!";
        if (reasonText != null) reasonText.text = "You reached the safe zone.";
        if (timeText != null) timeText.text = "Time Survived: " + (Time.time - startTime).ToString("F2") + " seconds";
        if (winUI != null) winUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PlayerBehaviorLogger.Instance != null)
        {
            PlayerBehaviorLogger.Instance.RecordEscape(Time.time - startTime);
            PlayerBehaviorLogger.Instance.FinalizeAndExport();
        }
    }

    // Broadcast to all clients when every player is down (team loss).
    public void OnCoopLost()
    {
        if (objectiveText != null) objectiveText.text = "Your team was caught.";
        if (reasonText != null) reasonText.text = "Everyone was caught by the mannequins.";
        if (winUI != null) winUI.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (PlayerBehaviorLogger.Instance != null)
            PlayerBehaviorLogger.Instance.FinalizeAndExport();
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
        AutoCollectMannequins(); // Populate the lists from the scene if left empty in the Inspector

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

    // Create the telemetry logger, safe-zone win trigger, and ambient/tension audio at
    // runtime if they aren't already present, so the scene needs no manual wiring.
    void EnsureSupportSystems()
    {
        // Post-run behavior logger
        if (FindAnyObjectByType<PlayerBehaviorLogger>() == null)
            gameObject.AddComponent<PlayerBehaviorLogger>();

        if (GetComponent<GamePauseMenu>() == null)
            gameObject.AddComponent<GamePauseMenu>();

        // Safe-zone escape/win trigger on the existing SafeZone object
        if (safeZone != null && safeZone.GetComponent<SafeZoneTrigger>() == null)
        {
            SafeZoneTrigger trigger = safeZone.gameObject.AddComponent<SafeZoneTrigger>();
            trigger.gameHandler = this;
        }

        // Ambient soundscape + rising tension
        if (FindAnyObjectByType<TensionDirector>() == null)
        {
            AudioClip clip = ambientClip != null
                ? ambientClip
                : Resources.Load<AudioClip>("dark-horror-soundscape-345814");

            GameObject ambienceGO = new GameObject("Ambience (auto)");
            AudioSource source = ambienceGO.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D
            source.clip = clip;

            TensionDirector director = ambienceGO.AddComponent<TensionDirector>();
            director.ambient = source;
            director.soundscape = clip;
        }
    }

    // If the mannequin lists were left empty in the Inspector, discover the placed
    // mannequin instances in the scene by their behavior script so level scaling works.
    void AutoCollectMannequins()
    {
        if (chaseMannequins == null) chaseMannequins = new List<GameObject>();
        if (blinkMannequins == null) blinkMannequins = new List<GameObject>();
        if (hideMannequins == null) hideMannequins = new List<GameObject>();

        if (chaseMannequins.Count == 0)
            foreach (RedMannequin m in FindObjectsByType<RedMannequin>(FindObjectsInactive.Include))
                chaseMannequins.Add(m.gameObject);

        if (blinkMannequins.Count == 0)
            foreach (YellowManneguin m in FindObjectsByType<YellowManneguin>(FindObjectsInactive.Include))
                blinkMannequins.Add(m.gameObject);

        if (hideMannequins.Count == 0)
            foreach (GreenMannequin m in FindObjectsByType<GreenMannequin>(FindObjectsInactive.Include))
                hideMannequins.Add(m.gameObject);
    }

    public void RestartGame() // Method to restart the game
    {
        if (LanSessionManager.Instance != null && LanSessionManager.Instance.IsSessionActive)
        {
            if (!LanSessionManager.Instance.RestartCurrentNetworkScene())
            {
                Debug.LogWarning("Only the host can restart a LAN scene.");
            }
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // Reload the current scene
    }

    public void ExitToTitle() // Method to exit to the title screen
    {
        if (LanSessionManager.Instance != null && LanSessionManager.Instance.IsSessionActive)
        {
            LanSessionManager.Instance.ShutdownAndReturnToMenu();
            return;
        }

        SceneManager.LoadScene("Home"); // Load the title screen scene
    }

    public void PlayerDied(float timeSurvived, string reason)
    {
        Debug.Log("Player died after surviving for " + timeSurvived + " seconds. Reason: " + reason);
        // You can add more logic here, such as recording stats or updating a leaderboard

        timeText.text = "Time Survived: " + timeSurvived.ToString("F2") + " seconds";
        reasonText.text = "And got attacked by a: " + reason + " mannequin";

        // Unlock and show the cursor for the death screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Persist this run's behavior data for post-run analysis
        if (PlayerBehaviorLogger.Instance != null)
            PlayerBehaviorLogger.Instance.FinalizeAndExport();
    }

    public void PlayerEscaped(float timeSurvived) // Player reached the safe zone and won the run
    {
        Debug.Log("Player escaped after surviving for " + timeSurvived + " seconds.");

        if (objectiveText != null) objectiveText.text = "You escaped!";
        if (timeText != null) timeText.text = "Time Survived: " + timeSurvived.ToString("F2") + " seconds";
        if (reasonText != null) reasonText.text = "You reached the safe zone.";
        if (winUI != null) winUI.SetActive(true);

        // Unlock and show the cursor for the win screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Stop the player moving behind the end screen
        Player player = FindAnyObjectByType<Player>();
        if (player != null) player.EndRun();

        // Persist this run's behavior data for post-run analysis
        if (PlayerBehaviorLogger.Instance != null)
        {
            PlayerBehaviorLogger.Instance.RecordEscape(timeSurvived);
            PlayerBehaviorLogger.Instance.FinalizeAndExport();
        }
    }
}
