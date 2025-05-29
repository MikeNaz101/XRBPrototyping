using UnityEngine;

[RequireComponent(typeof(Rigidbody))] // Cube enemy needs a Rigidbody for force-based movement
public class CubeEnemy : Enemy
{
    [Header("Cube Specific Settings")]
    [Tooltip("The force applied during each pulse towards the player.")]
    public float pulseForce = 10f;
    [Tooltip("How often (in seconds) the enemy pulses its engines.")]
    public float pulseInterval = 3f;
    [Tooltip("How long (in seconds) the pulse force is applied.")]
    public float pulseDuration = 0.2f; // Keep this short for a "pulse" feel
    [Tooltip("Maximum speed to prevent excessive velocity from repeated pulses.")]
    public float maxSpeed = 5f;
    [Tooltip("How quickly the cube orients itself towards the player before pulsing.")]
    public float rotationSpeed = 5f;


    private float _pulseTimer;
    private bool _isPulsing;
    private float _currentPulseTime;

    protected override void Awake()
    {
        base.Awake(); // Call the base Awake
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        rb.useGravity = false; // Usually, space enemies don't use gravity
        rb.linearDamping = 0.5f; // Some drag to prevent infinite sliding and help control max speed
        rb.angularDamping = 0.5f;
    }

    protected override void Start()
    {
        base.Start(); // Call the base Start
        _pulseTimer = Random.Range(0, pulseInterval); // Stagger initial pulses
        _isPulsing = false;
    }

    protected override void Move()
    {
        if (playerShipTransform == null) return;

        Vector3 directionToPlayer = (playerShipTransform.position - transform.position).normalized;

        if (!_isPulsing)
        {
            // Orient towards the player before pulsing
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            _pulseTimer -= Time.deltaTime;
            if (_pulseTimer <= 0)
            {
                _isPulsing = true;
                _currentPulseTime = 0f;
                _pulseTimer = pulseInterval; // Reset timer for next pulse
            }
        }
        else // Is Pulsing
        {
            _currentPulseTime += Time.deltaTime;
            if (_currentPulseTime <= pulseDuration)
            {
                // Apply force in the current forward direction (which should be towards player due to prior orientation)
                rb.AddForce(transform.forward * pulseForce, ForceMode.Acceleration);

                // Clamp velocity to maxSpeed
                if (rb.linearVelocity.magnitude > maxSpeed)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
                }
            }
            else
            {
                _isPulsing = false; // End pulse
            }
        }
    }

    // Optional: Override Die if Cube has specific death behavior
    // protected override void Die()
    // {
    //     base.Die();
    //     // Add cube-specific explosion, etc.
    // }
}
