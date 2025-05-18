// File: IndividualBeeBehavior.cs
// Purpose: Controls the behavior of an individual bee, including random flight,
// landing on VR hands, and reacting to being swatted.
// Instructions: Attach this script to your bee prefab/GameObject.
// Ensure the bee GameObject has a Collider (set to IsTrigger=true) and a Rigidbody.
// The Rigidbody can be kinematic if you control all movement via script, or non-kinematic
// if you want to use physics forces (though this script primarily uses transform manipulation).

using UnityEngine;
using System.Collections;

public class IndividualBeeBehavior : MonoBehaviour
{
    public enum BeeState
    {
        Wandering,
        ApproachingHand,
        Landed,
        Agitated,
        TakingOff
    }

    [Header("Bee State")]
    public BeeState currentState = BeeState.Wandering;

    [Header("Movement Parameters")]
    public float wanderSpeed = 0.5f;
    public float approachSpeed = 1.0f;
    public float agitatedSpeed = 2.0f;
    public float rotationSpeed = 2.0f;
    [Tooltip("How far the bee will look for a new wander target.")]
    public float wanderRadius = 3.0f;
    [Tooltip("Optional anchor point for wandering. If null, wanders around its start position or current area.")]
    public Transform wanderAnchor;
    [Tooltip("Minimum distance to a wander target before picking a new one.")]
    public float minWanderTargetDistance = 0.1f;
    [Tooltip("How high above a landing spot the bee aims for before final descent.")]
    public float landingApproachHeight = 0.1f;
    [Tooltip("Distance to hand to consider it 'landed'.")]
    public float landingDistanceThreshold = 0.02f;


    [Header("Interaction Parameters")]
    [Tooltip("Tag assigned to player's VR hands.")]
    public string playerHandTag = "PlayerHand";
    [Tooltip("Velocity magnitude of a hand to be considered a 'swat'.")]
    public float swatVelocityThreshold = 1.5f;
    [Tooltip("How long the bee stays agitated after being swatted (seconds).")]
    public float agitatedDuration = 5.0f;
    [Tooltip("Cooldown after being agitated before it might try to land again (seconds).")]
    public float postAgitationCooldown = 10.0f;
    [Tooltip("Minimum time between deciding to land on a hand.")]
    public float landAttemptCooldown = 5.0f;


    [Header("Landing Spot")]
    [Tooltip("Name of the child GameObject on the hand to target for landing. E.g., 'BeeLandingSpot'.")]
    public string handLandingSpotName = "BeeLandingSpot";

    // Private internal variables
    private Vector3 _currentTargetPosition;
    private Transform _targetHand; // The hand the bee is trying to land on
    private Transform _landingSpotOnTargetHand;
    private Rigidbody _beeRigidbody;
    private Collider _beeCollider;

    private float _timeSinceLastAgitation = 0f;
    private float _timeSinceLastLandAttempt = 0f;
    private bool _isAgitatedRecently = false;
    private Coroutine _agitationCoroutine;
    private Coroutine _postAgitationCooldownCoroutine;
    private Vector3 _initialLocalScale;


    void Start()
    {
        _beeRigidbody = GetComponent<Rigidbody>();
        _beeCollider = GetComponent<Collider>();
        _initialLocalScale = transform.localScale; // Store initial scale for landing/unlanding

        if (_beeCollider == null)
        {
            Debug.LogError("IndividualBeeBehavior: Bee needs a Collider component!", this);
            enabled = false;
            return;
        }
        if (!_beeCollider.isTrigger)
        {
            Debug.LogWarning("IndividualBeeBehavior: Bee's Collider is not set to IsTrigger=true. This might affect hand detection.", this);
        }

        // If no wander anchor, use its own starting position as a loose anchor concept for first target
        if (wanderAnchor == null)
        {
            wanderAnchor = new GameObject(gameObject.name + "_WanderAnchor").transform;
            wanderAnchor.position = transform.position;
        }

        SetState(BeeState.Wandering);
        _timeSinceLastLandAttempt = landAttemptCooldown; // Allow landing attempt early
    }

    void Update()
    {
        _timeSinceLastAgitation += Time.deltaTime;
        _timeSinceLastLandAttempt += Time.deltaTime;

        switch (currentState)
        {
            case BeeState.Wandering:
                HandleWandering();
                break;
            case BeeState.ApproachingHand:
                HandleApproachingHand();
                break;
            case BeeState.Landed:
                HandleLanded();
                break;
            case BeeState.Agitated:
                HandleAgitated();
                break;
            case BeeState.TakingOff:
                HandleTakingOff();
                break;
        }
    }

    void SetState(BeeState newState)
    {
        if (currentState == newState) return;

        // Exit logic for previous state
        if (currentState == BeeState.Landed)
        {
            UnLand();
        }
        if (currentState == BeeState.Agitated && _agitationCoroutine != null)
        {
            StopCoroutine(_agitationCoroutine);
            _agitationCoroutine = null;
        }


        currentState = newState;
        // Debug.Log($"Bee {gameObject.name} changed state to: {currentState}");

        // Entry logic for new state
        switch (currentState)
        {
            case BeeState.Wandering:
                PickNewWanderTarget();
                break;
            case BeeState.ApproachingHand:
                // Target hand and landing spot should already be set by AttemptLand
                if (_landingSpotOnTargetHand == null) {
                    Debug.LogWarning("ApproachingHand state entered without a landing spot. Returning to Wander.", this);
                    SetState(BeeState.Wandering);
                }
                break;
            case BeeState.Landed:
                // Movement stops, parenting happens in HandleLanded
                break;
            case BeeState.Agitated:
                PickNewWanderTarget(); // Pick an immediate flee direction
                if (_agitationCoroutine != null) StopCoroutine(_agitationCoroutine);
                _agitationCoroutine = StartCoroutine(AgitationTimer());
                _isAgitatedRecently = true;
                if (_postAgitationCooldownCoroutine != null) StopCoroutine(_postAgitationCooldownCoroutine);
                _postAgitationCooldownCoroutine = StartCoroutine(PostAgitationCooldownTimer());
                break;
            case BeeState.TakingOff:
                PickNewWanderTarget(); // Pick a direction away from the hand
                break;
        }
    }

    #region State Handlers
    void HandleWandering()
    {
        MoveTowardsTarget(_currentTargetPosition, wanderSpeed);
        if (Vector3.Distance(transform.position, _currentTargetPosition) < minWanderTargetDistance)
        {
            PickNewWanderTarget();
        }
    }

    void HandleApproachingHand()
    {
        if (_targetHand == null || _landingSpotOnTargetHand == null)
        {
            SetState(BeeState.Wandering); // Target lost
            return;
        }

        Vector3 targetPosWithOffset = _landingSpotOnTargetHand.position + (_targetHand.up * landingApproachHeight); // Approach slightly above
        MoveTowardsTarget(targetPosWithOffset, approachSpeed);

        // Check if close enough to "land"
        if (Vector3.Distance(transform.position, _landingSpotOnTargetHand.position) < landingDistanceThreshold)
        {
            SetState(BeeState.Landed);
        }
    }

    void HandleLanded()
    {
        if (_targetHand == null) // Hand disappeared or was destroyed
        {
            SetState(BeeState.Wandering);
            return;
        }
        // Stay parented and positioned. Movement is handled by the hand.
        // If not parented yet (first frame of Landed)
        if (transform.parent != _targetHand)
        {
            transform.SetParent(_targetHand, true); // Parent to hand, keep world position temporarily
            transform.position = _landingSpotOnTargetHand.position; // Snap to landing spot
            transform.rotation = _landingSpotOnTargetHand.rotation; // Align with landing spot
            transform.localScale = _initialLocalScale; // Ensure scale is correct after parenting

            if (_beeRigidbody != null) _beeRigidbody.isKinematic = true; // Stop physics while landed
        }
    }

    void HandleAgitated()
    {
        MoveTowardsTarget(_currentTargetPosition, agitatedSpeed);
        if (Vector3.Distance(transform.position, _currentTargetPosition) < minWanderTargetDistance * 2f) // React faster
        {
            PickNewWanderTarget(true); // Pick more erratic targets
        }
    }

    void HandleTakingOff()
    {
        MoveTowardsTarget(_currentTargetPosition, approachSpeed); // Use approach speed for controlled takeoff
        if (Vector3.Distance(transform.position, _currentTargetPosition) > wanderRadius * 0.5f) // Flew away a bit
        {
            SetState(BeeState.Wandering);
        }
    }
    #endregion

    #region Movement & Targeting
    void MoveTowardsTarget(Vector3 targetPosition, float speed)
    {
        if (targetPosition == null) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void PickNewWanderTarget(bool isAgitatedTarget = false)
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        if(wanderAnchor != null) {
            _currentTargetPosition = wanderAnchor.position + randomDirection;
        } else {
             _currentTargetPosition = transform.position + randomDirection;
        }


        // Optional: Add some height variation if your wander anchor is on the ground
        // _currentTargetPosition.y = Mathf.Max(_currentTargetPosition.y, wanderAnchor.position.y + 0.1f); // Ensure it's not underground

        if (isAgitatedTarget && _targetHand != null) // If agitated, try to move away from where the hand was
        {
            Vector3 awayFromHand = (transform.position - _targetHand.position).normalized * wanderRadius;
            _currentTargetPosition = transform.position + awayFromHand + Random.insideUnitSphere * (wanderRadius * 0.5f);
        }
    }

    void UnLand()
    {
        if (transform.parent != null && (wanderAnchor == null || transform.parent != wanderAnchor.parent)) // Check if parented to something other than a general scene anchor
        {
            transform.SetParent(wanderAnchor != null ? wanderAnchor.parent : null, true); // Unparent, keep world position
            transform.localScale = _initialLocalScale; // Restore original scale
        }
        if (_beeRigidbody != null) _beeRigidbody.isKinematic = false; // Or true if you continue to manage all movement
        _targetHand = null;
        _landingSpotOnTargetHand = null;
    }
    #endregion

    #region Interaction & Triggers
    void OnTriggerStay(Collider other) // Using OnTriggerStay for continuous check
    {
        if (!other.CompareTag(playerHandTag)) return;

        Rigidbody handRb = other.GetComponentInParent<Rigidbody>(); // Assumes Rigidbody is on a parent of the hand collider

        // SWAT DETECTION
        if (handRb != null && handRb.linearVelocity.magnitude > swatVelocityThreshold)
        {
            if (currentState == BeeState.Landed)
            {
                // Force takeoff and then agitate
                // Debug.Log("Swatted while landed!");
                SetState(BeeState.TakingOff); // This will call UnLand()
                // After TakingOff completes, it will transition to Wandering, then we can agitate
                // Or, we can have a direct path from TakingOff to Agitated if swatted
                StartCoroutine(DelayedAgitationAfterSwat());
            }
            else if (currentState != BeeState.Agitated)
            {
                // Debug.Log("Swatted while flying!");
                SetState(BeeState.Agitated);
            }
            return; // Prioritize swat over landing
        }

        // LANDING ATTEMPT
        if (currentState == BeeState.Wandering && !_isAgitatedRecently && _timeSinceLastLandAttempt >= landAttemptCooldown)
        {
            Transform potentialLandingSpot = FindChildRecursive(other.transform, handLandingSpotName);
            if (potentialLandingSpot == null) {
                 // If no specific spot, consider landing on the hand collider itself (less precise)
                potentialLandingSpot = other.transform;
            }

            if (potentialLandingSpot != null)
            {
                // Debug.Log("Attempting to land on hand: " + other.gameObject.name);
                _targetHand = other.transform.root; // Get the root of the hand (OVRHand, controller, etc.)
                _landingSpotOnTargetHand = potentialLandingSpot;
                SetState(BeeState.ApproachingHand);
                _timeSinceLastLandAttempt = 0f; // Reset cooldown
            }
        }
    }

    public void PlayerInitiatedTakeOff() // Call this if player shakes hand, etc.
    {
        if (currentState == BeeState.Landed)
        {
            SetState(BeeState.TakingOff);
        }
    }
    #endregion

    #region Coroutines
    IEnumerator AgitationTimer()
    {
        yield return new WaitForSeconds(agitatedDuration);
        if (currentState == BeeState.Agitated) // Only transition if still agitated
        {
            SetState(BeeState.Wandering);
        }
    }

    IEnumerator PostAgitationCooldownTimer()
    {
        yield return new WaitForSeconds(postAgitationCooldown);
        _isAgitatedRecently = false;
    }

    IEnumerator DelayedAgitationAfterSwat()
    {
        // Wait a brief moment for the bee to take off before agitating
        yield return new WaitForSeconds(0.5f); // Adjust delay as needed
        SetState(BeeState.Agitated);
    }

    #endregion

    // Helper to find a named child transform recursively
    Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }
}
