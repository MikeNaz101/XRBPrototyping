using UnityEngine;
using System.Collections;
using TMPro; // Required for TextMeshProUGUI
using UnityEngine.SceneManagement; // Required for loading scenes
using UnityEngine.Events; // Required for UnityEvent
using static OVRInput; // Assuming OVRInput for button presses

public class TutorialManager : MonoBehaviour
{
    public enum TutorialStep
    {
        Welcome,
        ExplainRightTriggerShoot,
        ExplainLeftTriggerMove,
        ExplainRightControllerAim,
        ExplainWristUI,
        PromptSpawnFirstEnemy,    
        WaitForFirstEnemyKill,    
        InformPowerUpSpawned,     
        WaitForPowerUpPickup,     
        ExplainPowerUpUse,        
        PromptSpawnSecondEnemy,   
        WaitForSecondEnemyKill,   
        TutorialComplete,
        ReturningToMenu
    }

    [Header("UI References")]
    [Tooltip("TextMeshProUGUI element to display tutorial instructions.")]
    public TextMeshProUGUI instructionText;
    [Tooltip("Optional: GameObject for a 'Next' button or prompt if not using controller input for next.")]
    public GameObject nextPromptUI;

    [Header("Tutorial Content")]
    [Tooltip("The enemy prefab to spawn for the tutorial tasks.")]
    public GameObject tutorialEnemyPrefab;
    [Tooltip("The transform where the FIRST tutorial enemy will be spawned.")]
    public Transform firstEnemySpawnPoint;
    [Tooltip("The transform where the SECOND tutorial enemy will be spawned (can be same as first).")]
    public Transform secondEnemySpawnPoint;
    [Tooltip("The PowerUpItem prefab to spawn for the tutorial (e.g., Magnet Bomb pickup).")]
    public GameObject tutorialPowerUpPrefab;
    [Tooltip("The specific PowerUpType this tutorialPowerUpPrefab represents (e.g., MagnetBomb_SingleUse).")]
    public PowerUpType tutorialPowerUpType = PowerUpType.MagnetBomb_SingleUse;
    
    [Header("Tutorial Settings")]
    [Tooltip("Button to press to advance most tutorial steps (e.g., A button).")]
    public Button advanceTutorialButton = Button.One; 
    [Tooltip("Button the player needs to press to use the specific tutorial power-up (e.g., SecondaryHandTrigger for Magnet Bomb).")]
    public Button usePowerUpButton = Button.SecondaryHandTrigger;
    [Tooltip("Name of your start menu scene.")]
    public string startMenuSceneName = "StartMenu";
    [Tooltip("Delay before returning to menu after tutorial completion.")]
    public float delayBeforeReturnToMenu = 5f;

    [Header("Player References")]
    [Tooltip("Reference to the player's ship. Used to check if it's active and for power-up events.")]
    public PlayerShip playerShip;

    private TutorialStep currentStep;
    private GameObject spawnedTutorialEnemy; 
    private GameObject spawnedTutorialPowerUp;
    private bool waitingForInputToAdvance = false;
    private bool waitingForPowerUpPickup = false; // This flag is set in UpdateTutorialUI
    private bool waitingForPowerUpUse = false;
    private Vector3 firstEnemyDeathLocation; 

    void Start()
    {
        if (instructionText == null) { Debug.LogError("TUT_ERR: InstructionText not assigned!", this); enabled = false; return; }
        if (tutorialEnemyPrefab == null) Debug.LogError("TUT_ERR: TutorialEnemyPrefab not assigned!", this);
        if (firstEnemySpawnPoint == null) Debug.LogError("TUT_ERR: FirstEnemySpawnPoint not assigned!", this);
        if (secondEnemySpawnPoint == null) Debug.LogWarning("TUT_WARN: SecondEnemySpawnPoint not assigned! Using FirstEnemySpawnPoint as fallback.", this);
        if (tutorialPowerUpPrefab == null) Debug.LogError("TUT_ERR: TutorialPowerUpPrefab not assigned!", this);
        if (string.IsNullOrEmpty(startMenuSceneName)) Debug.LogError("TUT_ERR: StartMenuSceneName not assigned!", this);

        if (playerShip == null)
        {
            playerShip = FindObjectOfType<PlayerShip>();
            if (playerShip == null) Debug.LogError("TUT_ERR: PlayerShip not found in scene! Powerup collection event won't work.", this);
        }

        if (playerShip != null)
        {
            Debug.Log("TUT_Debug: Attempting to subscribe to PlayerShip.OnPowerUpCollected.");
            playerShip.OnPowerUpCollected.AddListener(HandlePowerUpCollected);
        }
        else
        {
            Debug.LogError("TUT_ERR: Cannot subscribe to PlayerShip events because playerShip reference is null.");
        }


        if (nextPromptUI) nextPromptUI.SetActive(false);
        StartTutorial();
    }

    void StartTutorial()
    {
        currentStep = TutorialStep.Welcome;
        UpdateTutorialUI();
    }

    void Update()
    {
        if (waitingForInputToAdvance)
        {
            if (GetDown(advanceTutorialButton, Controller.Active))
            {
                // Debug.Log($"TUT_Debug: Advance button pressed. Current step: {currentStep}");
                AdvanceStep();
            }
        }
        else if (waitingForPowerUpUse && GetDown(usePowerUpButton, Controller.Active))
        {
            Debug.Log("TUT_Debug: Player pressed the 'use power-up' button for tutorial step.");
            waitingForPowerUpUse = false;
            AdvanceStep();
        }

        if (currentStep == TutorialStep.WaitForFirstEnemyKill)
        {
            if (spawnedTutorialEnemy != null)
            {
                firstEnemyDeathLocation = spawnedTutorialEnemy.transform.position;
            }
            else 
            {
                Debug.Log("TUT_Debug: First tutorial enemy detected as null (killed).");
                currentStep = TutorialStep.InformPowerUpSpawned;
                SpawnTutorialPowerUp(firstEnemyDeathLocation); 
                UpdateTutorialUI(); 
                return; 
            }
        }
        else if (currentStep == TutorialStep.WaitForSecondEnemyKill)
        {
            if (spawnedTutorialEnemy == null)
            {
                Debug.Log("TUT_Debug: Second tutorial enemy detected as null (killed).");
                AdvanceStep();
            }
        }
    }

    void AdvanceStep()
    {
        waitingForInputToAdvance = false;
        if (nextPromptUI) nextPromptUI.SetActive(false);

        int nextStepIndex = (int)currentStep + 1;
        // Ensure we don't go out of bounds before TutorialComplete handles the end
        if (nextStepIndex < (int)TutorialStep.TutorialComplete +1 ) // +1 because TutorialComplete can start a coroutine
        {
            currentStep = (TutorialStep)nextStepIndex;
            Debug.Log($"TUT_Debug: Advanced to step: {currentStep}");
        } else if (currentStep != TutorialStep.TutorialComplete && currentStep != TutorialStep.ReturningToMenu) {
             Debug.LogWarning($"TUT_WARN: Tried to advance past known steps. Current: {currentStep}, Next Index: {nextStepIndex}");
             currentStep = TutorialStep.TutorialComplete; // Go to complete if out of bounds
        }
        
        UpdateTutorialUI();
    }

    void UpdateTutorialUI()
    {
        Debug.Log($"TUT_Debug: UpdateTutorialUI called for step: {currentStep}");
        string message = "";
        waitingForInputToAdvance = false; // Reset flags, they will be set by cases if needed
        waitingForPowerUpPickup = false;
        waitingForPowerUpUse = false;

        switch (currentStep)
        {
            case TutorialStep.Welcome:
                message = "Welcome to the MR Volumetric Defender Tutorial!\nPress (A) to continue.";
                waitingForInputToAdvance = true;
                break;
            case TutorialStep.ExplainRightTriggerShoot:
                message = "To SHOOT: Squeeze the RIGHT INDEX TRIGGER.\nTry it now, then press (A).";
                waitingForInputToAdvance = true;
                break;
            case TutorialStep.ExplainLeftTriggerMove:
                message = "To MOVE FORWARD: Squeeze and hold the LEFT INDEX TRIGGER.\nTry it now, then press (A).";
                waitingForInputToAdvance = true;
                break;
            case TutorialStep.ExplainRightControllerAim:
                message = "AIM your ship by rotating your RIGHT CONTROLLER.\nYour ship will mirror its orientation.\nPress (A) to continue.";
                waitingForInputToAdvance = true;
                break;
            case TutorialStep.ExplainWristUI:
                message = "Look at your LEFT WRIST to see your Health, Score, XP, and Level.\nPress (A) to continue.";
                waitingForInputToAdvance = true;
                break;
            case TutorialStep.PromptSpawnFirstEnemy:
                message = "Next, an enemy will appear. Destroy it, and it will drop a power-up!\nPress (A) to spawn the first enemy.";
                waitingForInputToAdvance = true;
                break;
            case TutorialStep.WaitForFirstEnemyKill:
                if (spawnedTutorialEnemy == null) 
                {
                    SpawnTutorialEnemy(firstEnemySpawnPoint);
                }
                message = "First enemy spawned! Destroy it!";
                break;
            case TutorialStep.InformPowerUpSpawned:
                message = "Good job! The enemy dropped a Magnet Bomb!\nFly into it to collect it.";
                waitingForPowerUpPickup = true; 
                break;
            case TutorialStep.WaitForPowerUpPickup:
                if (spawnedTutorialPowerUp != null) {
                     message = "Magnet Bomb is available! Fly into it to collect it.";
                } else {
                    Debug.LogWarning("TUT_WARN: In WaitForPowerUpPickup, but spawnedTutorialPowerUp is null. This means it was likely collected and HandlePowerUpCollected advanced the step. If not, this is an issue.");
                    // If HandlePowerUpCollected worked, currentStep would have changed.
                    // If it didn't, this message will keep showing.
                    // The primary advance logic is in HandlePowerUpCollected.
                }
                waitingForPowerUpPickup = true; 
                break;
            case TutorialStep.ExplainPowerUpUse:
                message = $"Nice! You collected the Magnet Bomb.\nTo USE it, press the RIGHT CONTROLLER {(usePowerUpButton == Button.SecondaryHandTrigger ? "Y/B button" : usePowerUpButton.ToString())}.\nTry using it now to proceed!";
                waitingForPowerUpUse = true; 
                break;
            case TutorialStep.PromptSpawnSecondEnemy:
                message = "Now, let's practice combat with another enemy.\nUse your weapons or the Magnet Bomb!\nPress (A) to spawn the enemy.";
                waitingForInputToAdvance = true;
                break;
            case TutorialStep.WaitForSecondEnemyKill:
                if (spawnedTutorialEnemy == null)
                {
                     SpawnTutorialEnemy(secondEnemySpawnPoint != null ? secondEnemySpawnPoint : firstEnemySpawnPoint);
                }
                message = "Second enemy spawned! Destroy it!";
                break;
            case TutorialStep.TutorialComplete:
                message = "Great job! You've completed the tutorial.";
                if (nextPromptUI) nextPromptUI.SetActive(false); 
                StartCoroutine(CompleteTutorialAndReturn());
                break;
            case TutorialStep.ReturningToMenu:
                message = "Returning to Start Menu...";
                break;
            default:
                Debug.LogError("TUT_ERR: Tutorial step not recognized: " + currentStep);
                message = "Error in tutorial flow.";
                break;
        }

        if (instructionText != null) instructionText.text = message;
        if (nextPromptUI != null)
        {
            nextPromptUI.SetActive(waitingForInputToAdvance);
        }
    }

    void SpawnTutorialPowerUp(Vector3 spawnPosition)
    {
        if (tutorialPowerUpPrefab != null)
        {
            if (spawnedTutorialPowerUp != null) Destroy(spawnedTutorialPowerUp);
            spawnedTutorialPowerUp = Instantiate(tutorialPowerUpPrefab, spawnPosition, Quaternion.identity);
            Debug.Log("TUT_Debug: Tutorial Power-Up spawned at: " + spawnPosition + " (" + spawnedTutorialPowerUp.name + ")");
            
            PowerUpItem item = spawnedTutorialPowerUp.GetComponent<PowerUpItem>();
            if(item != null && item.powerUpType != tutorialPowerUpType)
            {
                Debug.LogWarning($"TUT_WARN: Tutorial PowerUp Prefab '{spawnedTutorialPowerUp.name}' has type {item.powerUpType}, but tutorial expects {tutorialPowerUpType}. Ensure prefab is configured correctly.");
            }
        }
        else
        {
            Debug.LogError("TUT_ERR: Cannot spawn tutorial power-up. Prefab missing.");
            AdvanceStep(); 
        }
    }

    public void HandlePowerUpCollected(PowerUpType collectedType)
    {
        Debug.Log($"TUT_Debug: HandlePowerUpCollected received event. Current Step: {currentStep}, Collected Type: {collectedType}, Expected Tutorial Type: {tutorialPowerUpType}");
        
        // MODIFIED Condition: Allow processing if current step is InformPowerUpSpawned OR WaitForPowerUpPickup
        if ((currentStep == TutorialStep.InformPowerUpSpawned || currentStep == TutorialStep.WaitForPowerUpPickup) 
            && collectedType == tutorialPowerUpType)
        {
            Debug.Log("TUT_Debug: Player collected the correct power-up for the tutorial! Advancing step.");
            if (spawnedTutorialPowerUp != null)
            {
                spawnedTutorialPowerUp = null; 
            }
            waitingForPowerUpPickup = false; 
            // Ensure we definitively move to the next logical step if we were in InformPowerUpSpawned
            if (currentStep == TutorialStep.InformPowerUpSpawned) 
            {
                currentStep = TutorialStep.WaitForPowerUpPickup; // Formally enter the "waiting" state's intended next step
            }
            AdvanceStep(); // This will now advance from WaitForPowerUpPickup to ExplainPowerUpUse
        }
        else if (currentStep == TutorialStep.WaitForPowerUpPickup || currentStep == TutorialStep.InformPowerUpSpawned)
        {
            Debug.LogWarning($"TUT_WARN: Player collected a power-up ({collectedType}), but it wasn't the expected one ({tutorialPowerUpType}) for the current tutorial step ({currentStep}). No advancement.");
        }
        else
        {
            Debug.Log($"TUT_Debug: Power-up collected ({collectedType}), but tutorial not currently in a state to process it (current step: {currentStep}).");
        }
    }

    void SpawnTutorialEnemy(Transform spawnPointToUse)
    {
        if (tutorialEnemyPrefab != null && spawnPointToUse != null)
        {
            if (spawnedTutorialEnemy != null) Destroy(spawnedTutorialEnemy); 
            spawnedTutorialEnemy = Instantiate(tutorialEnemyPrefab, spawnPointToUse.position, spawnPointToUse.rotation);
            Debug.Log("TUT_Debug: Tutorial enemy spawned: " + spawnedTutorialEnemy.name + " at " + spawnPointToUse.name);

            TutorialEnemyBase enemyScript = spawnedTutorialEnemy.GetComponent<TutorialEnemyBase>(); 
            if (enemyScript != null && playerShip != null)
            {
                enemyScript.playerShipTransform = playerShip.transform;
            }
            else if (enemyScript == null)
            {
                Debug.LogError($"TUT_ERR: Spawned tutorial enemy '{spawnedTutorialEnemy.name}' does not have a TutorialEnemyBase (or derived) component!");
            }
        }
        else
        {
            Debug.LogError("TUT_ERR: Cannot spawn tutorial enemy. Prefab or spawn point missing.");
            AdvanceStep(); 
        }
    }

    IEnumerator CompleteTutorialAndReturn()
    {
        yield return new WaitForSeconds(delayBeforeReturnToMenu);
        currentStep = TutorialStep.ReturningToMenu;
        UpdateTutorialUI();
        yield return new WaitForSeconds(1f);
        if (!string.IsNullOrEmpty(startMenuSceneName))
        {
            SceneManager.LoadScene(startMenuSceneName);
        }
        else
        {
            Debug.LogError("TUT_ERR: Start Menu Scene Name is not set. Cannot return to menu.");
        }
    }

    void OnDestroy()
    {
        if (playerShip != null)
        {
            playerShip.OnPowerUpCollected.RemoveListener(HandlePowerUpCollected);
            Debug.Log("TUT_Debug: Unsubscribed from PlayerShip.OnPowerUpCollected.");
        }
    }
}