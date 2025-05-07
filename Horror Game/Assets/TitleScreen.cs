using UnityEngine;
using UnityEngine.SceneManagement; // Import the SceneManagement namespace
using UnityEngine.UI; // Import the UI namespace

public class TitleScreen : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        // Load the game scene
        SceneManager.LoadScene("Eastwood Elementary School");
    }

    public void QuitGame()
    {
        // Quit the application
        Application.Quit();
        Debug.Log("Game is exiting");
    }

    

}
