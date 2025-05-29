using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // --- GameplayLoopManager Script (Place on a dedicated GameManager GameObject) ---
public class GameplayLoopManager : MonoBehaviour
{
    public static GameplayLoopManager Instance { get; private set; }

    [Header("Level Settings")]
    [Tooltip("Initial time limit for the first level in seconds.")]
    public float initialTimeLimit = 120f;
    [Tooltip("How much the time limit decreases each subsequent level.")]
    public float timeDecrementPerLevel = 10f;
    [Tooltip("Minimum time limit, it won't go below this.")]
    public float minimumTimeLimit = 30f;
    [Tooltip("Base number of enemies for the first level.")]
    public int initialEnemyCount = 5;
    [Tooltip("How many more enemies are added each subsequent level.")]
    public int enemiesIncrementPerLevel = 2;
    [Tooltip("Duration in seconds to display the level complete screen.")]
    public float displayLevelCompleteScreenDuration = 3.0f;
    [Tooltip("Duration in seconds to display game over screen before returning to menu.")]
    public float gameOverToMenuDelay = 5.0f; 
    [Tooltip("Name of your start menu scene to load after game over.")]
    public string startMenuSceneName = "StartMenu"; 

    [Header("Scoring Factors")]
    [Tooltip("Multiplier for remaining time in score calculation.")]
    public float timeBonusFactor = 0.5f;
    [Tooltip("Multiplier for player's final health percentage in score calculation.")]
    public float healthBonusFactor = 100f;
    [Tooltip("Base XP awarded per point of final score.")]
    public float xpPerScorePoint = 0.25f;

    [Header("UI References (TextMeshPro)")]
    public TextMeshProUGUI timeLimitText;
    public TextMeshProUGUI enemiesRemainingText;
    public TextMeshProUGUI currentLevelText;
    public TextMeshProUGUI finalScoreText;
    public GameObject gameOverScreen; 
    public GameObject levelCompleteScreen; 

    [Header("Core References")]
    [Tooltip("Reference to the PlayerShip script.")]
    public PlayerShip playerShip;
    [Tooltip("Reference to the EnemySpawner script.")]
    public EnemySpawner enemySpawner; 
    [Tooltip("Reference to the GameManager for base enemy kill scores.")]
    public GameManager gameManager; 

    private float currentLevelTimeLimit;
    private float currentTimeRemaining;
    private int enemiesToSpawnThisLevel;
    private int enemiesRemainingThisLevel;
    private int currentOverallLevel = 1; 
    private bool isLevelActive = false;
    private Coroutine _levelCompleteCoroutine; 
    private Coroutine _gameOverCoroutine; 

    private float overallGameScore = 0f; // New: For accumulating total score

    public bool IsLevelActive => isLevelActive; 
    public float CurrentTimeRemaining => currentTimeRemaining;
    public float OverallGameScore => overallGameScore; // New: Public accessor

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (playerShip == null) Debug.LogError("GameplayLoopManager: PlayerShip reference not set!", this);
        if (enemySpawner == null) Debug.LogError("GameplayLoopManager: EnemySpawner reference not set!", this);
        if (gameManager == null) Debug.LogError("GameplayLoopManager: GameManager reference not set!", this);

        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (levelCompleteScreen) levelCompleteScreen.SetActive(false);
    }

    void Start()
    {
        if (playerShip != null)
        {
            playerShip.OnPlayerDeath.AddListener(HandlePlayerDeath);
        }
        StartNewGame(); 
    }

    public void StartNewGame() 
    {
        currentOverallLevel = 1;
        overallGameScore = 0f; // Reset overall score
        if (gameManager) gameManager.ResetScore(); 
        if (playerShip)
        {
            playerShip.ResetPlayerStateForNewGame(); 
        }
        StartLevel(currentOverallLevel);
    }

    void StartLevel(int levelNumber)
    {
        isLevelActive = true;
        currentOverallLevel = levelNumber; 

        currentLevelTimeLimit = Mathf.Max(minimumTimeLimit, initialTimeLimit - (timeDecrementPerLevel * (levelNumber - 1)));
        currentTimeRemaining = currentLevelTimeLimit;
        enemiesToSpawnThisLevel = initialEnemyCount + (enemiesIncrementPerLevel * (levelNumber - 1));
        enemiesRemainingThisLevel = enemiesToSpawnThisLevel;

        Debug.Log($"Starting Level {currentOverallLevel}. Time: {currentLevelTimeLimit}s, Enemies: {enemiesToSpawnThisLevel}");

        if (playerShip && !playerShip.gameObject.activeInHierarchy) 
        {
            playerShip.gameObject.SetActive(true); 
            playerShip.ResetPlayerStateForNewGame(); 
        }
        
        if (enemySpawner != null)
        {
            enemySpawner.StartNewWave(enemiesToSpawnThisLevel, currentOverallLevel); 
            Debug.Log($"GameplayLoopManager: Told EnemySpawner to start wave {currentOverallLevel} with {enemiesToSpawnThisLevel} enemies.");
        }
        else
        {
            Debug.LogError("GameplayLoopManager: EnemySpawner reference is missing. Cannot control enemy spawning for levels.");
        }


        if (gameOverScreen) gameOverScreen.SetActive(false);
        if (levelCompleteScreen) levelCompleteScreen.SetActive(false);

        UpdateLevelUI();
    }

    void Update()
    {
        if (!isLevelActive) return;

        currentTimeRemaining -= Time.deltaTime;
        if (currentTimeRemaining <= 0)
        {
            currentTimeRemaining = 0;
            HandleTimeUp();
        }
        UpdateLevelUI();
    }

    public void EnemyDestroyedByPlayer() 
    {
        if (!isLevelActive) return;

        enemiesRemainingThisLevel--;
        UpdateLevelUI();

        if (enemiesRemainingThisLevel <= 0)
        {
            enemiesRemainingThisLevel = 0; 
            LevelComplete();
        }
    }

    void HandlePlayerDeath() 
    {
        if (!isLevelActive && _gameOverCoroutine == null) return; 
        isLevelActive = false;
        Debug.Log("Game Over: Player Died!");
        CalculateAndShowFinalScore(false); 
        
        if (_gameOverCoroutine != null) StopCoroutine(_gameOverCoroutine);
        _gameOverCoroutine = StartCoroutine(GameOverSequence());
    }

    void HandleTimeUp()
    {
        if (!isLevelActive && _gameOverCoroutine == null) return; 
        isLevelActive = false;
        Debug.Log("Game Over: Time Ran Out!");
        CalculateAndShowFinalScore(false);

        if (_gameOverCoroutine != null) StopCoroutine(_gameOverCoroutine);
        _gameOverCoroutine = StartCoroutine(GameOverSequence());
    }

    IEnumerator GameOverSequence() 
    {
        if (gameOverScreen) gameOverScreen.SetActive(true);
        yield return new WaitForSeconds(gameOverToMenuDelay);
        Debug.Log($"Returning to menu: {startMenuSceneName}");
        if (!string.IsNullOrEmpty(startMenuSceneName))
        {
            SceneManager.LoadScene(startMenuSceneName);
        }
        else
        {
            Debug.LogError("Start Menu Scene Name is not set in GameplayLoopManager!");
        }
        _gameOverCoroutine = null;
    }

    void LevelComplete()
    {
        if (!isLevelActive && _levelCompleteCoroutine == null) return; 
        isLevelActive = false; 

        Debug.Log($"Level {currentOverallLevel} Complete!");
        CalculateAndShowFinalScore(true); 
        
        if (_levelCompleteCoroutine != null) StopCoroutine(_levelCompleteCoroutine);
        _levelCompleteCoroutine = StartCoroutine(LevelCompleteSequence());
    }

    IEnumerator LevelCompleteSequence()
    {
        if (levelCompleteScreen) levelCompleteScreen.SetActive(true);
        yield return new WaitForSeconds(displayLevelCompleteScreenDuration);
        if (levelCompleteScreen) levelCompleteScreen.SetActive(false);
        StartNextLevel();
        _levelCompleteCoroutine = null;
    }

    public void StartNextLevel() 
    {
        if (isLevelActive)
        {
            Debug.LogWarning("Attempted to start next level while current level is still active or processing completion.");
            return;
        }
        currentOverallLevel++;
        if (gameManager) gameManager.ResetScore(); 
        StartLevel(currentOverallLevel);
    }


    void CalculateAndShowFinalScore(bool levelWon)
    {
        if (playerShip == null || gameManager == null) 
        {
            Debug.LogError("Cannot calculate final score. PlayerShip or GameManager reference missing.");
            return;
        }

        float baseScoreFromKills = gameManager.currentScore;
        float timeBonus = levelWon ? (currentTimeRemaining * timeBonusFactor) : 0; 
        float healthPercentage = playerShip.CurrentHealth / playerShip.maxHealth;
        float healthBonus = healthPercentage * healthBonusFactor;
        float levelMultiplier = 1 + ((playerShip.CurrentLevel -1) * 0.1f); 

        float finalScoreForWave = (baseScoreFromKills + timeBonus + healthBonus) * levelMultiplier;
        finalScoreForWave = Mathf.Max(0, Mathf.FloorToInt(finalScoreForWave)); 

        Debug.Log($"Final Score Calculation for Wave {currentOverallLevel}: BaseKills={baseScoreFromKills}, TimeBonus={timeBonus}, HealthBonus={healthBonus}, PlayerLevelMulti={levelMultiplier} => FinalScoreForWave={finalScoreForWave}");

        if (finalScoreText != null)
        {
            finalScoreText.text = "Wave " + currentOverallLevel + " Score: " + finalScoreForWave.ToString();
        }

        if (levelWon) 
        {
            overallGameScore += finalScoreForWave; // Accumulate overall score
            Debug.Log($"Overall Game Score updated to: {overallGameScore}");

            float xpGained = finalScoreForWave * xpPerScorePoint;
            playerShip.AddXP(xpGained);
        }
    }

    void UpdateLevelUI()
    {
        if (timeLimitText != null)
        {
            timeLimitText.text = "Time: " + Mathf.CeilToInt(currentTimeRemaining).ToString();
        }
        if (enemiesRemainingText != null)
        {
            enemiesRemainingText.text = "Enemies: " + enemiesRemainingThisLevel.ToString();
        }
        if (currentLevelText != null) 
        {
            // Display overall game score on wrist, or wave score depending on what you prefer
            // For now, this displays the current wave number
            currentLevelText.text = "Wave: " + currentOverallLevel.ToString();
            // If you want WristUI to show overall score:
            // scoreText.text = "Total Score: " + overallGameScore.ToString(); 
            // (You'd need to pass this to WristUIController or have it get it from GameplayLoopManager.Instance.OverallGameScore)
        }
    }

    void OnDestroy()
    {
        if (playerShip != null)
        {
            playerShip.OnPlayerDeath.RemoveListener(HandlePlayerDeath);
        }
        if (_levelCompleteCoroutine != null) StopCoroutine(_levelCompleteCoroutine);
        if (_gameOverCoroutine != null) StopCoroutine(_gameOverCoroutine);
    }
}
