using UnityEngine;
using UnityEngine.UI; // If you want to display score on a UI Text element

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int currentScore { get; private set; }

    [Header("UI (Optional)")]
    [Tooltip("Assign a UI Text or TextMeshProUGUI element to display the score.")]
    public Text scoreText; // For legacy UI Text
    // public TMPro.TextMeshProUGUI scoreTextTMP; // For TextMeshPro

    void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: if your GameManager needs to persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        currentScore = 0;
        UpdateScoreUI();
    }

    public void AddScore(int amount)
    {
        if (amount <= 0) return; // Don't add negative or zero scores this way

        currentScore += amount;
        UpdateScoreUI();
        Debug.Log($"Score: {currentScore}");
    }

    public void EnemyDestroyed(int scoreValue)
    {
        AddScore(scoreValue);
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore.ToString();
        }
        // if (scoreTextTMP != null)
        // {
        //     scoreTextTMP.text = "Score: " + currentScore.ToString();
        // }
    }

    // Example method to reset score (e.g., for a new game)
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }
}