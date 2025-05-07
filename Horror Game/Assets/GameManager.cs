using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // Singleton instance
    public int level = 2; // current difficulty level
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Check if instance already exists
        if (instance == null)
        {
            instance = this; // Set the instance to this
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instance
        }
        DontDestroyOnLoad(gameObject); // Keep this object alive across scenes
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel; // Set the new difficulty level
    }
}
