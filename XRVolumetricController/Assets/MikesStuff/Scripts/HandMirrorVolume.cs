using UnityEngine;
using System.Collections;      // Required for Coroutines
// using UnityEngine.InputSystem; // REMOVED for OVRInput

// It's often convenient to use OVRInput statically if you use it a lot
using static OVRInput; // Allows you to write GetDown() instead of OVRInput.GetDown()

[RequireComponent(typeof(Collider))]
public class HandMirrorVolume : MonoBehaviour
{
    // --- Public Fields (Assign in Inspector) ---

    [Header("Target Object")]
    [Tooltip("The GameObject to control (MUST have a Rigidbody). Drag its Rigidbody here.")]
    public Rigidbody controlledObjectRigidbody;

    [Header("Hand Tracking")]
    [Tooltip("The specific tag assigned to your hand tracking GameObject (e.g., 'PlayerHand'). For Meta Building Blocks, you'll replace this with MetaXRHand detection.")]
    public string handTag = "PlayerHand"; // Keep for now, but remember to adapt with MetaXRHand as discussed

    // NEW: OVRInput Configuration
    [Header("OVRInput Control")]
    [Tooltip("Controller to listen to for input (Active, LTouch, RTouch).")]
    public Controller controllerForInput = Controller.Active; // Default to active controller
    [Tooltip("OVRInput Button for moving forward.")]
    public Button moveForwardButton = Button.PrimaryHandTrigger; // Example: Grip Trigger
    [Tooltip("OVRInput Button for firing.")]
    public Button fireButton = Button.PrimaryIndexTrigger;       // Example: Index Trigger

    [Header("Movement Control")]
    [Tooltip("Speed of the object when moving forward.")]
    public float moveSpeed = 2.0f;

    [Header("Entry/Exit Behavior")]
    [Tooltip("How high the object lifts off its current position upon hand entry.")]
    public float liftHeight = 0.5f;
    [Tooltip("How long the lifting animation takes.")]
    public float liftDuration = 1.0f;

    [Header("Firing")]
    [Tooltip("The bullet prefab to instantiate.")]
    public GameObject bulletPrefab;
    [Tooltip("The two fire points (Transforms) the bullets will come from. Assign exactly two.")]
    public Transform[] firePoints = new Transform[2];

    [Header("Effects - Particles")]
    [Tooltip("Engine effect prefab to instantiate during forward movement. Should probably be Looping, Play On Awake OFF.")]
    public GameObject engineEffectPrefab;
    [Tooltip("The Transform where the engine effect prefab should be instantiated.")]
    public Transform engineEffectPoint;
    [Tooltip("Muzzle flash prefab to instantiate on firing. Must have self-destruction.")]
    public GameObject muzzleFlashPrefab;

    [Header("Effects - Audio")]
    [Tooltip("AudioSource for the looping engine sound.")]
    public AudioSource engineAudioSource;
    [Tooltip("AudioSource for one-shot sounds like firing.")]
    public AudioSource effectsAudioSource;
    [Tooltip("The looping sound for the engine.")]
    public AudioClip engineLoopSound;
    [Tooltip("The sound effect for firing a bullet.")]
    public AudioClip fireSoundClip;

    // --- Private Variables ---
    private Transform currentHandTransform;
    private bool isHandInside = false;
    private bool isMovingForward = false;
    private bool isLifting = false;
    private Coroutine liftCoroutine;
    private int currentFirePointIndex = 0;
    private GameObject currentEngineEffectInstance = null;

    // --- Unity Methods ---

    void Awake()
    {
        if (controlledObjectRigidbody == null) { Debug.LogError("Controlled Object Rigidbody not assigned!", this); enabled = false; return; }
        Collider col = GetComponent<Collider>();
        if (col == null) { Debug.LogError("This GameObject needs a Collider component.", this); enabled = false; return; }
        if (!col.isTrigger) { Debug.LogWarning("This GameObject's Collider needs 'Is Trigger' enabled. Attempting to fix.", this); col.isTrigger = true; }
        if (firePoints.Length != 2 || firePoints[0] == null || firePoints[1] == null) { Debug.LogError("Exactly two valid Fire Points must be assigned!", this); enabled = false; return; }
        if (bulletPrefab == null) { Debug.LogError("Bullet Prefab not assigned!", this); enabled = false; return; }

        if (engineEffectPrefab == null) { Debug.LogWarning("Engine Effect Prefab not assigned.", this); }
        if (engineEffectPoint == null) { Debug.LogWarning("Engine Effect Point Transform not assigned.", this); }
        if (muzzleFlashPrefab == null) { Debug.LogWarning("Muzzle Flash Prefab not assigned.", this); }
        if (engineAudioSource == null) { Debug.LogWarning("Engine Audio Source not assigned.", this); } else if (engineLoopSound == null) { Debug.LogWarning("Engine Loop Sound clip not assigned.", this); }
        if (effectsAudioSource == null) { Debug.LogWarning("Effects Audio Source not assigned.", this); } else if (fireSoundClip == null) { Debug.LogWarning("Fire Sound clip not assigned.", this); }

        currentEngineEffectInstance = null;
    }

    // OnEnable is no longer needed for InputSystem subscriptions
    // void OnEnable() { }

    void OnDisable()
    {
        // This cleanup logic is still important
        if (isHandInside)
        {
            if (isMovingForward) // If moving when disabled, stop effects
            {
                StopEngineEffects();
                isMovingForward = false;
            }
            if (controlledObjectRigidbody != null) EnablePhysics(); // Ensure physics is re-enabled
            isHandInside = false; // Reset state
            currentHandTransform = null;
        }
        if (liftCoroutine != null)
        {
            StopCoroutine(liftCoroutine);
            isLifting = false;
        }
        // Debug.Log("HandMirrorVolume Disabled: State cleaned up.");
    }

    // --- Trigger Volume Logic ---
    // NOTE: Remember to update this section to use MetaXRHand detection
    // as previously discussed, instead of handTag.
    void OnTriggerEnter(Collider other)
    {
        if (!isHandInside && other.CompareTag(handTag)) // TODO: Replace with MetaXRHand detection
        {
            isHandInside = true;
            currentHandTransform = other.transform;

            if (controlledObjectRigidbody != null && currentHandTransform != null)
            {
                ApplyPerpendicularRotation(currentHandTransform.rotation);
            }
            DisablePhysicsAndStopMovement();
            if (liftCoroutine != null) StopCoroutine(liftCoroutine);
            liftCoroutine = StartCoroutine(LiftObjectCoroutine());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (isHandInside && currentHandTransform != null && other.transform == currentHandTransform) // TODO: Replace with MetaXRHand detection
        {
            isHandInside = false;
            currentHandTransform = null;

            if (isMovingForward)
            {
                isMovingForward = false;
                StopEngineEffects();
            }
            if (liftCoroutine != null)
            {
                StopCoroutine(liftCoroutine);
                isLifting = false;
            }
            EnablePhysics();
        }
    }

    // --- Main Update Logic ---
    void Update()
    {
        if (!isHandInside || currentHandTransform == null || controlledObjectRigidbody == null)
        {
            // If hand leaves while moving forward (e.g. OnTriggerExit didn't catch it fast enough or disabled)
            // ensure effects are stopped.
            if(isMovingForward)
            {
                isMovingForward = false;
                StopEngineEffects();
            }
            return;
        }

        // --- Continuous Rotation Control ---
        ApplyPerpendicularRotation(currentHandTransform.rotation);

        // --- OVRInput Handling ---
        HandleOVRInput();

        // --- Movement Control (Only if NOT lifting AND isMovingForward is true) ---
        if (!isLifting && isMovingForward)
        {
            Vector3 forwardDirection = controlledObjectRigidbody.rotation * Vector3.forward;
            Vector3 movement = forwardDirection * moveSpeed * Time.deltaTime;
            controlledObjectRigidbody.MovePosition(controlledObjectRigidbody.position + movement);
        }
    }

    // --- NEW: OVRInput Handling Method ---
    private void HandleOVRInput()
    {
        // Move Forward Input (Held button)
        if (Get(moveForwardButton, controllerForInput)) // Using static import for OVRInput
        {
            if (!isMovingForward && !isLifting) // Start only if not already moving and not lifting
            {
                isMovingForward = true;
                StartEngineEffects();
            }
        }
        else
        {
            if (isMovingForward) // Stop if was moving
            {
                isMovingForward = false;
                StopEngineEffects();
            }
        }

        // Fire Input (Button press down)
        if (GetDown(fireButton, controllerForInput)) // Using static import for OVRInput
        {
            if (!isLifting) // Can fire even if not moving forward, but not while lifting
            {
                FireBullet();
            }
        }
    }


    // --- Core Action Methods ---
    private void ApplyPerpendicularRotation(Quaternion handRotation)
    {
        // Get the world-space forward and up directions from the input handRotation
        Vector3 handForward = handRotation * Vector3.forward;
        Vector3 handUp = handRotation * Vector3.up;

        // Create a rotation where the object's local Z+ (forward) aligns with handForward,
        // and its local Y+ (up) aligns with handUp.
        // This makes the object directly mirror the hand's orientation.
        Quaternion targetShipOrientation = Quaternion.LookRotation(handForward, handUp);

        // The original problematic rotations:
        // Quaternion basePerpendicularRotation = Quaternion.LookRotation(handForward, handUp); // This was already good if mirroring is desired
        // Quaternion rotationAfterYAdjust = basePerpendicularRotation * Quaternion.Euler(0f, 90f, 0f); // This introduces an unwanted offset
        // Quaternion finalRotation = rotationAfterYAdjust * Quaternion.Euler(180f, 0f, 0f);       // This introduces another unwanted offset

        // Corrected: Set the final rotation directly to the desired mirrored orientation
        Quaternion finalRotation = targetShipOrientation;

        controlledObjectRigidbody.MoveRotation(finalRotation);
    }

    private void FireBullet()
    {
        if (firePoints.Length < 2 || firePoints[0] == null || firePoints[1] == null || bulletPrefab == null) return;
        Transform currentFirePoint = firePoints[currentFirePointIndex];

        if (muzzleFlashPrefab != null)
        {
            Instantiate(muzzleFlashPrefab, currentFirePoint.position, currentFirePoint.rotation);
        }
        if (effectsAudioSource != null && fireSoundClip != null)
        {
            effectsAudioSource.PlayOneShot(fireSoundClip);
        }
        GameObject bullet = Instantiate(bulletPrefab, currentFirePoint.position, currentFirePoint.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.AddForce(currentFirePoint.forward * 20f, ForceMode.Impulse);
        }
        currentFirePointIndex = 1 - currentFirePointIndex;
    }

    // --- Effect Control Methods ---
    private void StartEngineEffects()
    {
        if (engineEffectPrefab != null && engineEffectPoint != null && currentEngineEffectInstance == null)
        {
            currentEngineEffectInstance = Instantiate(engineEffectPrefab, engineEffectPoint.position, engineEffectPoint.rotation, engineEffectPoint);
        }
        if (engineAudioSource != null && engineLoopSound != null && !engineAudioSource.isPlaying)
        {
            engineAudioSource.clip = engineLoopSound;
            engineAudioSource.loop = true;
            engineAudioSource.Play();
        }
    }

    private void StopEngineEffects()
    {
        if (currentEngineEffectInstance != null)
        {
            Destroy(currentEngineEffectInstance);
            currentEngineEffectInstance = null;
        }
        if (engineAudioSource != null && engineAudioSource.isPlaying)
        {
            engineAudioSource.Stop();
        }
    }

    // --- Physics State Management ---
    void DisablePhysicsAndStopMovement()
    {
        if (controlledObjectRigidbody != null)
        {
            controlledObjectRigidbody.isKinematic = true;
            controlledObjectRigidbody.linearVelocity = Vector3.zero; // Use velocity for direct assignment
            controlledObjectRigidbody.angularVelocity = Vector3.zero;
        }
    }

    void EnablePhysics()
    {
        if (controlledObjectRigidbody != null)
        {
            controlledObjectRigidbody.isKinematic = false;
        }
    }

    // --- Coroutine for Lifting ---
    IEnumerator LiftObjectCoroutine()
    {
        isLifting = true;
        Vector3 startPosition = controlledObjectRigidbody.position;
        Vector3 targetPosition = startPosition + Vector3.up * liftHeight; // Use controlledObjectRigidbody.transform.up if you want lift relative to its orientation
        float startTime = Time.time;
        float journeyProgress = 0f;

        while (journeyProgress < 1.0f && isHandInside) // Check isHandInside here too
        {
            float timeSinceStarted = Time.time - startTime;
            journeyProgress = timeSinceStarted / liftDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, journeyProgress);
            controlledObjectRigidbody.MovePosition(Vector3.Lerp(startPosition, targetPosition, smoothT));
            yield return null;
        }

        if (isHandInside) // Only snap to target if hand is still inside
        {
             controlledObjectRigidbody.MovePosition(targetPosition);
        }
        // else if hand exited, OnDisable or OnTriggerExit should handle reverting/enabling physics

        isLifting = false; // Mark lifting as done

        // Check if move button is held AFTER lift and start effects if so
        if (isHandInside && Get(moveForwardButton, controllerForInput)) // Check current OVRInput state
        {
            if (!isMovingForward) // To avoid double-starting if it somehow was true
            {
                isMovingForward = true;
                StartEngineEffects();
            }
        }
        liftCoroutine = null;
    }

} // End of class HandMirrorVolume