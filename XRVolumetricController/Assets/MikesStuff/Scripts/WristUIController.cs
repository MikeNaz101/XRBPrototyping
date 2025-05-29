using UnityEngine;
using UnityEngine.UI; // Keep for Slider, remove if you also switch sliders to a TMP equivalent if one exists
using TMPro; // Required for TextMeshProUGUI

public class WristUIController : MonoBehaviour
{
    [Header("Player Data References")]
    [Tooltip("Reference to the PlayerShip script.")]
    public PlayerShip playerShip;

    [Header("Gameplay Data References")]
    [Tooltip("Reference to the GameplayLoopManager script.")]
    public GameplayLoopManager gameplayLoopManager;
    [Tooltip("Reference to the GameManager for running kill score (current wave score).")]
    public GameManager gameManager; 

    [Header("UI Text Elements (TextMeshPro)")]
    public TextMeshProUGUI healthText;
    public Slider healthSlider; // Standard UI Slider still works fine
    public TextMeshProUGUI scoreText; // This will show running kill score from GameManager for the current wave
    public TextMeshProUGUI overallScoreText; // New: To display overall game score from GameplayLoopManager
    public TextMeshProUGUI timeLimitText; 
    public TextMeshProUGUI xpText;
    public Slider xpSlider; // Standard UI Slider still works fine
    public TextMeshProUGUI currentLevelText; // Player's overall level from PlayerShip
    public TextMeshProUGUI currentWaveText; // Current wave number from GameplayLoopManager

    [Header("Controller Tracking (Optional - if UI is not a direct child)")]
    [Tooltip("Assign the Left Controller's Transform if this UI isn't a direct child and needs to follow it.")]
    public Transform leftControllerAnchor;
    public Vector3 positionOffset = new Vector3(0, 0.05f, 0.1f); // Example offset from anchor
    public Vector3 rotationOffset = new Vector3(45, 0, 0);   // Example rotation offset

    void Start()
    {
        if (playerShip == null)
        {
            playerShip = FindObjectOfType<PlayerShip>();
            if (playerShip == null) Debug.LogError("WristUIController: PlayerShip reference not set and not found!", this);
        }
        if (gameplayLoopManager == null)
        {
            gameplayLoopManager = GameplayLoopManager.Instance;
            if (gameplayLoopManager == null) Debug.LogError("WristUIController: GameplayLoopManager reference not set and not found!", this);
        }
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
            if (gameManager == null) Debug.LogError("WristUIController: GameManager reference not set and not found!", this);
        }

        UpdateAllUI();
    }

    void Update()
    {
        FollowController(); 
        UpdateAllUI();
    }

    void FollowController()
    {
        if (leftControllerAnchor != null)
        {
            transform.position = leftControllerAnchor.TransformPoint(positionOffset);
            transform.rotation = leftControllerAnchor.rotation * Quaternion.Euler(rotationOffset);
        }
    }

    void UpdateAllUI()
    {
        // Player Ship Data
        if (playerShip != null)
        {
            if (healthText != null)
            {
                healthText.text = $"HP: {playerShip.CurrentHealth:F0}/{playerShip.maxHealth:F0}";
            }
            if (healthSlider != null)
            {
                healthSlider.maxValue = playerShip.maxHealth;
                healthSlider.value = playerShip.CurrentHealth;
            }
            if (xpText != null)
            {
                xpText.text = $"XP: {playerShip.CurrentXP:F0}/{playerShip.XPToNextLevel:F0}";
            }
            if (xpSlider != null)
            {
                xpSlider.maxValue = playerShip.XPToNextLevel;
                xpSlider.value = playerShip.CurrentXP;
            }
            if (currentLevelText != null)
            {
                currentLevelText.text = $"LVL: {playerShip.CurrentLevel}";
            }
        }

        // Gameplay Loop Data
        if (gameplayLoopManager != null)
        {
            if (timeLimitText != null && gameplayLoopManager.IsLevelActive)
            {
                timeLimitText.text = "Time: " + Mathf.CeilToInt(gameplayLoopManager.CurrentTimeRemaining).ToString();
            }
            else if (timeLimitText != null)
            {
                timeLimitText.text = "Time: --";
            }

            if (currentWaveText != null)
            {
                // Assuming GameplayLoopManager has a way to get currentOverallLevel
                // (which it does from the version in the Canvas)
                // If GameplayLoopManager.Instance.currentOverallLevel was public or had a property:
                // currentWaveText.text = "Wave: " + GameplayLoopManager.Instance.currentOverallLevel;
                // For now, let's assume you'll add a public property to GameplayLoopManager for this.
                // As a placeholder, or if GameplayLoopManager updates its own text field:
                // currentWaveText.text = "Wave: " + gameplayLoopManager.GetCurrentWaveNumber(); // Example
            }
            
            if (overallScoreText != null) // Display overall game score
            {
                overallScoreText.text = "Total Score: " + gameplayLoopManager.OverallGameScore.ToString("F0");
            }
        }
        
        // Score from GameManager (running kill score for the current wave)
        if (gameManager != null)
        {
            if (scoreText != null)
            {
                scoreText.text = "Wave Score: " + gameManager.currentScore.ToString();
            }
        }
    }
}
