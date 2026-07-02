using UnityEngine;
using UnityEngine.SceneManagement; // Import the SceneManagement namespace

public class TitleScreen : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        GamePauseMenu.ResetGlobalState();
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

    public void StartMultiplayer()
    {
        var launcher = FindAnyObjectByType<MultiplayerLauncher>();
        if (launcher != null)
        {
            launcher.Host();
            return;
        }

        Debug.LogError("No MultiplayerLauncher found on the title screen.");
    }

}
