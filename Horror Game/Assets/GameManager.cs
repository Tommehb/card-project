using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance; // Singleton instance
    public int level = 2; // current difficulty level
    public Slider levelSlider; // UI slider for difficulty level
    public int mouseSensitivity = 2; // mouse sensitivity
    public Slider sensitivitySlider; // UI slider for mouse sensitivity

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

        // Initialize sliders if they exist
        if (levelSlider != null)
        {
            levelSlider.value = level; // Set slider to current level
            levelSlider.onValueChanged.AddListener(delegate { SetLevel((int)levelSlider.value); }); // Add listener to update level on slider change
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = mouseSensitivity; // Set slider to current mouse sensitivity
            sensitivitySlider.onValueChanged.AddListener(delegate { SetMouseSensitivity((int)sensitivitySlider.value); }); // Add listener to update mouse sensitivity on slider change 
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLevel(int newLevel)
    {
        Debug.Log("Setting level to " + newLevel);
        level = newLevel; // Set the new difficulty level
    }

    public void SetMouseSensitivity(int newSensitivity)
    {
        Debug.Log("Setting mouse sensitivity to " + newSensitivity);
        mouseSensitivity = newSensitivity; // Set the new mouse sensitivity
    }
    
}
