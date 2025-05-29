using UnityEngine;
using System.Collections;
using static OVRInput; // Allows GetDown() instead of OVRInput.GetDown()

public class DirectHandRotationShipController : MonoBehaviour
{
    [Header("Target Object & Player Ship")]
    [Tooltip("The Rigidbody of the ship or object to control.")]
    public Rigidbody controlledObjectRigidbody;
    [Tooltip("Reference to the PlayerShip script on your ship GameObject. This is crucial for firing and power-ups.")]
    public PlayerShip playerShipReference;

    [Header("Hand Tracking Source")]
    [Tooltip("Assign the Transform of the GameObject that represents your tracked hand's orientation.")]
    public Transform handTransformToTrack;

    [Header("OVRInput Control")]
    [Tooltip("Controller to listen to for input (Active, LTouch, RTouch).")]
    public Controller controllerForInput = Controller.Active;
    [Tooltip("OVRInput Button for moving forward.")]
    public Button moveForwardButton = Button.PrimaryHandTrigger;
    [Tooltip("OVRInput Button for firing.")]
    public Button fireButton = Button.PrimaryIndexTrigger;
    [Tooltip("OVRInput Button for using special abilities like Magnet Bomb.")]
    public Button specialAbilityButton = Button.SecondaryHandTrigger;
    [Tooltip("Second OVRInput Button to be HELD ALONG WITH MoveForwardButton to activate speed boost.")]
    public Button speedBoostActivationButton = Button.SecondaryIndexTrigger; // Example: Right Index if move is Left Grip

    [Header("Movement Control")]
    [Tooltip("Speed of the object when moving forward.")]
    public float moveSpeed = 2.0f;

    [Header("Optional Lift (Currently not auto-triggered)")]
    public float liftHeight = 0.5f;
    public float liftDuration = 1.0f;

    [Header("Effects - Particles (Movement & Generic)")]
    [Tooltip("The engine particle effect prefab to instantiate. Ensure 'Play on Awake' is checked on its ParticleSystem.")]
    public GameObject engineEffectPrefab; 
    [Tooltip("The Transform where the engine effect prefab should be instantiated and parented.")]
    public Transform engineEffectPoint;   

    [Header("Effects - Audio (Movement & Generic)")]
    public AudioSource engineAudioSource;
    public AudioClip engineLoopSound;
    [Tooltip("Pitch for engine sound when speed boost is active.")]
    public float speedBoostEnginePitch = 1.3f; // New
    [Tooltip("Volume for engine sound when speed boost is active (relative to original if original isn't 1).")]
    public float speedBoostEngineVolumeFactor = 1.2f; // New: Multiplies original volume
    private float originalEnginePitch; // New
    private float originalEngineVolume; // New
    private bool engineAudioModifiedForBoost = false; // New


    // --- Private Variables ---
    private bool isMovingForward = false;
    private bool isLifting = false;
    private Coroutine liftCoroutine;
    private GameObject currentEngineEffectInstance = null; 

    void Awake()
    {
        if (controlledObjectRigidbody == null) { Debug.LogError($"[{nameof(DirectHandRotationShipController)}] Controlled Object Rigidbody not assigned!", this); enabled = false; return; }
        if (handTransformToTrack == null) { Debug.LogError($"[{nameof(DirectHandRotationShipController)}] Hand Transform To Track not assigned!", this); enabled = false; return; }
        if (playerShipReference == null) { Debug.LogError($"[{nameof(DirectHandRotationShipController)}] PlayerShip Reference not assigned!", this); enabled = false; return; }
        
        if (engineEffectPrefab == null)
        {
            Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Effect Prefab not assigned. Engine visual effects will not play.", this);
        }
        if (engineEffectPoint == null)
        {
            Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Effect Point not assigned. Engine effect will spawn at ship's root if used.", this);
        }

        if (engineAudioSource != null)
        {
            originalEnginePitch = engineAudioSource.pitch;
            originalEngineVolume = engineAudioSource.volume;
        }
        else
        {
            Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Audio Source not assigned. Speed boost audio changes will not work.", this);
            originalEnginePitch = 1f; // Default
            originalEngineVolume = 1f; // Default
        }
        if (engineLoopSound == null && engineAudioSource != null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Loop Sound clip not assigned.", this); }


        EnablePhysicsOnShip();
        currentEngineEffectInstance = null; 
    }

    void OnDisable()
    {
        if (isMovingForward)
        {
            StopEngineEffects(); 
            isMovingForward = false;
        }
        if (liftCoroutine != null)
        {
            StopCoroutine(liftCoroutine);
            if (isLifting && controlledObjectRigidbody != null && controlledObjectRigidbody.isKinematic) {
                 EnablePhysicsOnShip();
            }
            isLifting = false;
        }
    }

    void Update()
    {
        if (handTransformToTrack == null || controlledObjectRigidbody == null || playerShipReference == null)
        {
            if(isMovingForward)
            {
                isMovingForward = false;
                StopEngineEffects();
            }
            return;
        }

        AlignShipToHandRotation(handTransformToTrack.rotation);
        HandleOVRInput();

        if (!isLifting && isMovingForward)
        {
            float currentActualSpeed = moveSpeed * (playerShipReference != null ? playerShipReference.CurrentSpeedMultiplier : 1f);
            Vector3 forwardDirection = controlledObjectRigidbody.rotation * Vector3.forward;
            Vector3 movement = forwardDirection * currentActualSpeed * Time.deltaTime;
            controlledObjectRigidbody.MovePosition(controlledObjectRigidbody.position + movement);
        }

        // Continuously update engine audio based on speed boost state if engine is running
        UpdateEngineAudioForBoost();
    }

    private void HandleOVRInput()
    {
        bool moveButtonPressed = Get(moveForwardButton, controllerForInput);
        bool speedBoostInputHeld = Get(speedBoostActivationButton, controllerForInput); // Check if the second button is HELD

        // Speed Boost Activation Attempt
        // Activates if move button is pressed, speed boost button is pressed,
        // player has the power-up, and boost isn't already active.
        if (moveButtonPressed && speedBoostInputHeld && playerShipReference != null && !playerShipReference.IsSpeedBoostActive)
        {
            if (playerShipReference.TryActivateSpeedBoost())
            {
                // PlayerShip now handles setting its IsSpeedBoostActive to true.
                // Audio changes will be picked up by UpdateEngineAudioForBoost or StartEngineEffects.
                Debug.Log($"[{nameof(DirectHandRotationShipController)}] Speed Boost activation successful via input.");
            }
        }

        // Engine sound and particle control based on moveButtonPressed
        if (moveButtonPressed)
        {
            if (!isMovingForward && !isLifting)
            {
                isMovingForward = true;
                StartEngineEffects(); 
            }
        }
        else 
        {
            if (isMovingForward)
            {
                isMovingForward = false;
                StopEngineEffects(); 
            }
        }

        if (GetDown(fireButton, controllerForInput))
        {
            if (!isLifting && playerShipReference.playerWeaponController != null)
            {
                playerShipReference.playerWeaponController.Fire();
            }
            else if (playerShipReference.playerWeaponController == null)
            {
                 Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] PlayerWeaponController reference on PlayerShip is missing. Cannot fire.");
            }
        }

        if (GetDown(specialAbilityButton, controllerForInput))
        {
            if (!isLifting)
            {
                playerShipReference.UseMagnetBomb();
            }
        }
    }

    private void AlignShipToHandRotation(Quaternion currentHandRotation)
    {
        Vector3 handWorldForward = currentHandRotation * Vector3.forward;
        Vector3 handWorldUp = currentHandRotation * Vector3.up;
        Quaternion targetShipOrientation = Quaternion.LookRotation(handWorldForward, handWorldUp);
        controlledObjectRigidbody.MoveRotation(targetShipOrientation);
    }

    private void StartEngineEffects()
    {
        if (engineEffectPrefab != null && currentEngineEffectInstance == null)
        {
            Transform spawnPoint = engineEffectPoint != null ? engineEffectPoint : controlledObjectRigidbody.transform; 
            currentEngineEffectInstance = Instantiate(engineEffectPrefab, spawnPoint.position, spawnPoint.rotation);
            currentEngineEffectInstance.transform.SetParent(spawnPoint, true); 
            // Debug.Log($"[{nameof(DirectHandRotationShipController)}] Instantiated engine effect '{engineEffectPrefab.name}'.");
        }
        else if (engineEffectPrefab == null)
        {
            Debug.LogError($"[{nameof(DirectHandRotationShipController)}] engineEffectPrefab is NULL in StartEngineEffects! Cannot play visual effect.");
        }

        if (engineAudioSource != null && engineLoopSound != null)
        {
            if (!engineAudioSource.isPlaying)
            {
                engineAudioSource.clip = engineLoopSound;
                engineAudioSource.loop = true;
                engineAudioSource.Play();
                // Debug.Log($"[{nameof(DirectHandRotationShipController)}] Engine audio started.");
            }
            // Initial audio adjustment will be handled by UpdateEngineAudioForBoost
            UpdateEngineAudioForBoost(); 
        }
    }

    private void StopEngineEffects()
    {
        if (currentEngineEffectInstance != null)
        {
            Destroy(currentEngineEffectInstance);
            currentEngineEffectInstance = null; 
            // Debug.Log($"[{nameof(DirectHandRotationShipController)}] Destroyed engine effect instance.");
        }

        if (engineAudioSource != null && engineAudioSource.isPlaying)
        {
            engineAudioSource.Stop();
            // Debug.Log($"[{nameof(DirectHandRotationShipController)}] Engine audio stopped.");
            // Reset pitch and volume if they were modified for boost
            if (engineAudioModifiedForBoost)
            {
                engineAudioSource.pitch = originalEnginePitch;
                engineAudioSource.volume = originalEngineVolume;
                engineAudioModifiedForBoost = false;
                // Debug.Log($"[{nameof(DirectHandRotationShipController)}] Engine audio RESTORED on stop: Pitch {originalEnginePitch}, Vol {originalEngineVolume}");
            }
        }
    }

    private void UpdateEngineAudioForBoost()
    {
        if (engineAudioSource == null || playerShipReference == null) return;

        if (engineAudioSource.isPlaying) // Only modify if engine sound is actually playing
        {
            if (playerShipReference.IsSpeedBoostActive && !engineAudioModifiedForBoost)
            {
                engineAudioSource.pitch = speedBoostEnginePitch;
                engineAudioSource.volume = originalEngineVolume * speedBoostEngineVolumeFactor; // Modify based on original
                engineAudioModifiedForBoost = true;
                // Debug.Log($"[{nameof(DirectHandRotationShipController)}] Engine audio BOOSTED: Pitch {engineAudioSource.pitch}, Vol {engineAudioSource.volume}");
            }
            else if (!playerShipReference.IsSpeedBoostActive && engineAudioModifiedForBoost)
            {
                engineAudioSource.pitch = originalEnginePitch;
                engineAudioSource.volume = originalEngineVolume;
                engineAudioModifiedForBoost = false;
                // Debug.Log($"[{nameof(DirectHandRotationShipController)}] Engine audio RESTORED: Pitch {originalEnginePitch}, Vol {originalEngineVolume}");
            }
        }
        else if (engineAudioModifiedForBoost) // If engine stopped but audio was still in boosted state
        {
            // Ensure it's reset if StopEngineEffects didn't catch it or if state changed weirdly
            engineAudioSource.pitch = originalEnginePitch;
            engineAudioSource.volume = originalEngineVolume;
            engineAudioModifiedForBoost = false;
        }
    }


    void DisablePhysicsOnShipAndStopMovement()
    {
        if (controlledObjectRigidbody != null)
        {
            controlledObjectRigidbody.isKinematic = true;
            controlledObjectRigidbody.linearVelocity = Vector3.zero;
            controlledObjectRigidbody.angularVelocity = Vector3.zero;
        }
    }

    void EnablePhysicsOnShip()
    {
        if (controlledObjectRigidbody != null)
        {
            controlledObjectRigidbody.isKinematic = false;
        }
    }

    IEnumerator LiftObjectCoroutine()
    {
        isLifting = true;
        DisablePhysicsOnShipAndStopMovement();

        Vector3 startPosition = controlledObjectRigidbody.position;
        Vector3 targetPosition = startPosition + Vector3.up * liftHeight;
        float startTime = Time.time;
        float journeyProgress = 0f;

        while (journeyProgress < 1.0f && handTransformToTrack != null)
        {
            float timeSinceStarted = Time.time - startTime;
            journeyProgress = timeSinceStarted / liftDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, journeyProgress);
            controlledObjectRigidbody.MovePosition(Vector3.Lerp(startPosition, targetPosition, smoothT));
            yield return null;
        }

        if (handTransformToTrack != null)
        {
             controlledObjectRigidbody.MovePosition(targetPosition);
        }

        isLifting = false;

        if (handTransformToTrack != null && Get(moveForwardButton, controllerForInput))
        {
            if (!isMovingForward)
            {
                isMovingForward = true;
                StartEngineEffects();
            }
        }
        else
        {
            EnablePhysicsOnShip();
        }
        liftCoroutine = null;
    }
}
