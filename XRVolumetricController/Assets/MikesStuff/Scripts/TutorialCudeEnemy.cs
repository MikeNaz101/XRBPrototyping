using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TutorialCubeEnemy : TutorialEnemyBase // Inherits from TutorialEnemyBase
{
    [Header("Cube Specific Settings")]
    [Tooltip("The force applied during each pulse towards the player.")]
    public float pulseForce = 10f;
    [Tooltip("How often (in seconds) the enemy pulses its engines.")]
    public float pulseInterval = 3f;
    [Tooltip("How long (in seconds) the pulse force is applied.")]
    public float pulseDuration = 0.2f;
    [Tooltip("Maximum speed to prevent excessive velocity from repeated pulses.")]
    public float maxSpeed = 5f;
    [Tooltip("How quickly the cube orients itself towards the player before pulsing.")]
    public float rotationSpeed = 5f;

    private float _pulseTimer;
    private bool _isPulsing;
    private float _currentPulseTime;

    protected override void Awake()
    {
        base.Awake(); // Call the base Awake from TutorialEnemyBase
        if (rb == null) // rb is initialized in TutorialEnemyBase.Awake()
        {
            // This should ideally not happen if base.Awake() is called and works.
            Debug.LogError("Rigidbody not found on TutorialCubeEnemy, attempting to get it again.", this);
            rb = GetComponent<Rigidbody>();
        }
        rb.useGravity = false;
        // Using Rigidbody.drag and Rigidbody.angularDrag is generally preferred over linearDamping/angularDamping
        rb.linearDamping = 0.5f; 
        rb.angularDamping = 0.5f;
    }

    protected override void Start()
    {
        base.Start(); // Call the base Start from TutorialEnemyBase
        _pulseTimer = Random.Range(0, pulseInterval);
        _isPulsing = false;
    }

    protected override void Move()
    {
        if (playerShipTransform == null) return;

        Vector3 directionToPlayer = (playerShipTransform.position - transform.position).normalized;

        if (!_isPulsing)
        {
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            _pulseTimer -= Time.deltaTime;
            if (_pulseTimer <= 0)
            {
                _isPulsing = true;
                _currentPulseTime = 0f;
                _pulseTimer = pulseInterval;
            }
        }
        else // Is Pulsing
        {
            _currentPulseTime += Time.deltaTime;
            if (_currentPulseTime <= pulseDuration)
            {
                rb.AddForce(transform.forward * pulseForce, ForceMode.Acceleration);

                if (rb.linearVelocity.magnitude > maxSpeed) // Changed from linearVelocity
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
            else
            {
                _isPulsing = false;
            }
        }
    }

    // No need to override Die() unless you have specific visual/audio for tutorial cube death
    // The base TutorialEnemyBase.Die() will handle Destroy(gameObject)
}