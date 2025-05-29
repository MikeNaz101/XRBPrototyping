using UnityEngine;
using System.Collections;
using static OVRInput; // Allows GetDown() instead of OVRInput.GetDown()

public class DirectHandRotationShipController1 : MonoBehaviour
{
    [Header("Target Object")]
    [Tooltip("The Rigidbody of the ship or object to control.")]
    public Rigidbody controlledObjectRigidbody;

    [Header("Hand Tracking Source")]
    [Tooltip("Assign the Transform of the GameObject that represents your tracked hand's orientation.")]
    public Transform handTransformToTrack; // Assign this in the Inspector

    [Header("OVRInput Control")]
    [Tooltip("Controller to listen to for input (Active, LTouch, RTouch).")]
    public Controller controllerForInput = Controller.Active;
    [Tooltip("OVRInput Button for moving forward.")]
    public Button moveForwardButton = Button.PrimaryHandTrigger;
    [Tooltip("OVRInput Button for firing.")]
    public Button fireButton = Button.PrimaryIndexTrigger;

    [Header("Movement Control")]
    [Tooltip("Speed of the object when moving forward.")]
    public float moveSpeed = 2.0f;

    [Header("Optional Lift (Currently not auto-triggered)")]
    [Tooltip("How high the object lifts. Requires a custom trigger if used.")]
    public float liftHeight = 0.5f;
    [Tooltip("How long the lifting animation takes.")]
    public float liftDuration = 1.0f;

    [Header("Firing")]
    [Tooltip("The bullet prefab to instantiate.")]
    public GameObject bulletPrefab;
    [Tooltip("The two fire points (Transforms) on the ship. Assign exactly two.")]
    public Transform[] firePoints = new Transform[2];

    [Header("Effects - Particles")]
    public GameObject engineEffectPrefab;
    public Transform engineEffectPoint; // Should be on the ship
    public GameObject muzzleFlashPrefab;

    [Header("Effects - Audio")]
    public AudioSource engineAudioSource; // Should be on or associated with the ship
    public AudioSource effectsAudioSource; // Should be on or associated with the ship
    public AudioClip engineLoopSound;
    public AudioClip fireSoundClip;

    // --- Private Variables ---
    private bool isMovingForward = false;
    private bool isLifting = false; // Kept for logic, but not auto-triggered
    private Coroutine liftCoroutine;
    private int currentFirePointIndex = 0;
    private GameObject currentEngineEffectInstance = null;

    void Awake()
    {
        if (controlledObjectRigidbody == null) { Debug.LogError($"[{nameof(DirectHandRotationShipController)}] Controlled Object Rigidbody not assigned!", this); enabled = false; return; }
        if (handTransformToTrack == null) { Debug.LogError($"[{nameof(DirectHandRotationShipController)}] Hand Transform To Track not assigned! This is required for rotation.", this); enabled = false; return; }
        if (firePoints.Length != 2 || firePoints[0] == null || firePoints[1] == null) { Debug.LogError($"[{nameof(DirectHandRotationShipController)}] Exactly two valid Fire Points must be assigned!", this); enabled = false; return; }
        if (bulletPrefab == null) { Debug.LogError($"[{nameof(DirectHandRotationShipController)}] Bullet Prefab not assigned!", this); enabled = false; return; }

        // Validate Effects Assignments (Warnings only)
        ValidateEffectAssignments();
        currentEngineEffectInstance = null;
        EnablePhysicsOnShip(); // Ensure physics are on initially
    }

    void ValidateEffectAssignments()
    {
        if (engineEffectPrefab == null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Effect Prefab not assigned.", this); }
        if (engineEffectPoint == null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Effect Point Transform (on ship) not assigned.", this); }
        if (muzzleFlashPrefab == null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Muzzle Flash Prefab not assigned.", this); }
        if (engineAudioSource == null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Audio Source not assigned.", this); } else if (engineLoopSound == null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Engine Loop Sound clip not assigned.", this); }
        if (effectsAudioSource == null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Effects Audio Source not assigned.", this); } else if (fireSoundClip == null) { Debug.LogWarning($"[{nameof(DirectHandRotationShipController)}] Fire Sound clip not assigned.", this); }
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
        if (handTransformToTrack == null || controlledObjectRigidbody == null)
        {
            if(isMovingForward) // Stop effects if critical references are lost
            {
                isMovingForward = false;
                StopEngineEffects();
            }
            return;
        }

        // --- Continuous Rotation Control from assigned handTransformToTrack ---
        AlignShipToHandRotation(handTransformToTrack.rotation);

        // --- OVRInput Handling for Movement and Firing ---
        HandleOVRInput();

        // --- Movement Physics (Only if NOT lifting AND isMovingForward is true) ---
        if (!isLifting && isMovingForward)
        {
            Vector3 forwardDirection = controlledObjectRigidbody.rotation * Vector3.forward;
            Vector3 movement = forwardDirection * moveSpeed * Time.deltaTime;
            controlledObjectRigidbody.MovePosition(controlledObjectRigidbody.position + movement);
        }
    }

    private void HandleOVRInput()
    {
        // Move Forward Input
        if (Get(moveForwardButton, controllerForInput))
        {
            if (!isMovingForward && !isLifting)
            {
                isMovingForward = true;
                StartEngineEffects();
                // Optional: if you want the ship to ignore physics bumps while actively moving via button
                // DisablePhysicsOnShipAndStopMovement();
            }
        }
        else
        {
            if (isMovingForward)
            {
                isMovingForward = false;
                StopEngineEffects();
                // Optional: ensure physics are on if you disabled them for active movement
                // EnablePhysicsOnShip();
            }
        }

        // Fire Input
        if (GetDown(fireButton, controllerForInput))
        {
            if (!isLifting)
            {
                FireBullet();
            }
        }

        // Example: Trigger lift with a button (e.g., SecondaryHandTrigger / "B" or "Y" button)
        // if (GetDown(Button.SecondaryHandTrigger, controllerForInput) && !isLifting && liftCoroutine == null)
        // {
        //     liftCoroutine = StartCoroutine(LiftObjectCoroutine());
        // }
    }

    private void AlignShipToHandRotation(Quaternion currentHandRotation)
    {
        Vector3 handWorldForward = currentHandRotation * Vector3.forward;
        Vector3 handWorldUp = currentHandRotation * Vector3.up;
        Quaternion targetShipOrientation = Quaternion.LookRotation(handWorldForward, handWorldUp);
        controlledObjectRigidbody.MoveRotation(targetShipOrientation);
    }

    private void FireBullet()
    {
        if (firePoints.Length < 2 || firePoints[0] == null || firePoints[1] == null || bulletPrefab == null) return;
        Transform currentFirePointToUse = firePoints[currentFirePointIndex];

        if (muzzleFlashPrefab != null)
        {
            Instantiate(muzzleFlashPrefab, currentFirePointToUse.position, currentFirePointToUse.rotation);
        }
        if (effectsAudioSource != null && fireSoundClip != null)
        {
            effectsAudioSource.PlayOneShot(fireSoundClip);
        }
        GameObject bullet = Instantiate(bulletPrefab, currentFirePointToUse.position, currentFirePointToUse.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();
        if (bulletRb != null)
        {
            bulletRb.AddForce(currentFirePointToUse.forward * 20f, ForceMode.Impulse);
        }
        currentFirePointIndex = 1 - currentFirePointIndex;
    }

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
        DisablePhysicsOnShipAndStopMovement(); // Make ship kinematic during lift

        Vector3 startPosition = controlledObjectRigidbody.position;
        Vector3 targetPosition = startPosition + Vector3.up * liftHeight; // Or use handTransformToTrack.up if lift should be relative to hand
        float startTime = Time.time;
        float journeyProgress = 0f;

        while (journeyProgress < 1.0f && handTransformToTrack != null) // Continue if hand is still valid
        {
            float timeSinceStarted = Time.time - startTime;
            journeyProgress = timeSinceStarted / liftDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, journeyProgress);
            controlledObjectRigidbody.MovePosition(Vector3.Lerp(startPosition, targetPosition, smoothT));
            yield return null;
        }

        if (handTransformToTrack != null) // Snap to position if hand still valid
        {
             controlledObjectRigidbody.MovePosition(targetPosition);
        }

        isLifting = false;

        // Decide ship's state after lifting
        if (handTransformToTrack != null && Get(moveForwardButton, controllerForInput))
        {
            if (!isMovingForward) // Start movement if button held
            {
                isMovingForward = true;
                StartEngineEffects();
                // Optionally keep kinematic if active movement means no physics bumps
                // DisablePhysicsOnShipAndStopMovement();
            }
        }
        else
        {
            // If not moving via button after lift, ensure physics are on
            EnablePhysicsOnShip();
        }
        liftCoroutine = null;
    }
}