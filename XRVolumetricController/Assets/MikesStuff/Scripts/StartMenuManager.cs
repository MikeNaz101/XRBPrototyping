using UnityEngine;
using UnityEngine.SceneManagement; // Required for scene management
using UnityEngine.UI; // If you need to interact with standard UI elements like Panels

public class StartMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    public string gameSceneName = "MainGameScene"; // Set this in the Inspector
    public string tutorialSceneName = "Tutorial"; // New: Set this in Inspector, defaults to "Tutorial"
    public string creditsSceneName = "CreditsScene"; // Optional: Set this in the Inspector
    // public string highScoresSceneName = "HighScoresScene"; // Optional

    [Header("UI Panels (Optional)")]
    public GameObject creditsPanel; // Assign if using a panel in the same scene
    public GameObject highScoresPanel; // Assign if using a panel in the same scene

    void Start()
    {
        // Ensure panels are initially hidden if they exist
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
        if (highScoresPanel != null)
        {
            highScoresPanel.SetActive(false);
        }
    }

    // --- Public methods to be called by button OnClick events ---

    public void StartGame()
    {
        Debug.Log("Start Game button clicked. Loading scene: " + gameSceneName);
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game Scene Name is not set in StartMenuManager!");
        }
    }

    public void StartTutorial() // New Method
    {
        Debug.Log("Tutorial button clicked. Loading scene: " + tutorialSceneName);
        if (!string.IsNullOrEmpty(tutorialSceneName))
        {
            SceneManager.LoadScene(tutorialSceneName);
        }
        else
        {
            Debug.LogError("Tutorial Scene Name is not set in StartMenuManager!");
        }
    }

    public void ShowCredits()
    {
        Debug.Log("Credits button clicked.");
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);
            // Optionally hide other main menu elements here if needed
        }
        else if (!string.IsNullOrEmpty(creditsSceneName))
        {
            SceneManager.LoadScene(creditsSceneName);
        }
        else
        {
            Debug.LogWarning("Credits Panel or Credits Scene Name is not set.");
        }
    }

    public void ShowHighScores()
    {
        Debug.Log("High Scores button clicked.");
        if (highScoresPanel != null)
        {
            highScoresPanel.SetActive(true);
            // Optionally hide other main menu elements here if needed
            // You would also populate the high scores text here
        }
        else
        {
            Debug.LogWarning("High Scores Panel or High Scores Scene Name is not set.");
        }
    }

    // --- Optional methods for panel-based UI ---

    public void HideCreditsPanel()
    {
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    public void HideHighScoresPanel()
    {
        if (highScoresPanel != null)
        {
            highScoresPanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game button clicked.");
        Application.Quit();

        #if UNITY_EDITOR
        // If running in the Unity Editor, stop playing
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
