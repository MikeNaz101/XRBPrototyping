using UnityEngine;

// Sphere can use Rigidbody or just Transform manipulation if collisions aren't complex.
// Using Transform for simplicity here, can be upgraded to Rigidbody if needed.
public class SphereEnemy : Enemy
{
    [Header("Sphere Specific Settings")]
    [Tooltip("Frequency of the sine wave oscillation.")]
    public float sineFrequency = 2f;
    [Tooltip("Amplitude (height) of the sine wave oscillation.")]
    public float sineAmplitude = 1f;
    [Tooltip("How quickly the sphere orients towards the player.")]
    public float rotationSpeed = 3f;

    private Vector3 _startPosition;
    private float _sineTime;
    private Vector3 _oscillationAxis; // The axis perpendicular to movement for sine wave

    protected override void Start()
    {
        base.Start();
        _startPosition = transform.position;
        _sineTime = Random.Range(0f, 2f * Mathf.PI); // Randomize start of sine wave

        // Determine an oscillation axis roughly perpendicular to initial direction to player
        if (playerShipTransform != null)
        {
            Vector3 directionToPlayer = (playerShipTransform.position - transform.position).normalized;
            _oscillationAxis = Vector3.Cross(directionToPlayer, Vector3.up).normalized;
            if (_oscillationAxis == Vector3.zero) // If player is directly above/below
            {
                _oscillationAxis = Vector3.right; // Default to right
            }
        }
        else
        {
            _oscillationAxis = Vector3.right; // Default if no player
        }
    }

    protected override void Move()
    {
        if (playerShipTransform == null) return;

        // --- Orientation ---
        Vector3 directionToPlayer = (playerShipTransform.position - transform.position).normalized;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Forward Movement ---
        Vector3 forwardMovement = transform.forward * baseSpeed * Time.deltaTime;

        // --- Sine Wave Oscillation ---
        _sineTime += Time.deltaTime * sineFrequency;
        float sineOffset = Mathf.Sin(_sineTime) * sineAmplitude;
        Vector3 oscillationMovement = _oscillationAxis * sineOffset * Time.deltaTime; // Scale by deltaTime if applying as velocity

        // --- Apply Movement (using Transform for simplicity) ---
        // For more robust physics, especially if colliding with other moving objects, use Rigidbody.MovePosition.
        // If using Rigidbody, the oscillation might need to be applied as a force or velocity change.
        transform.position += forwardMovement + oscillationMovement;


        // If you were to use Rigidbody for this movement:
        // if (rb != null)
        // {
        //     Vector3 targetVelocity = transform.forward * baseSpeed;
        //     // Add sine wave component to velocity (more complex to do smoothly with forces)
        //     // One way: Calculate target position including sine wave, then move towards it.
        //     Vector3 nextFramePosition = transform.position + forwardMovement;
        //     float nextSineValue = Mathf.Sin(_sineTime + Time.deltaTime * sineFrequency) * sineAmplitude;
        //     Vector3 sinePos = transform.position + (_oscillationAxis * nextSineValue); // This isn't quite right for velocity
        //
        //     // Simpler Rigidbody approach: direct velocity for forward, manipulate position for sine.
        //     // This is a bit of a hybrid and can feel unnatural if not tuned.
        //     rb.velocity = transform.forward * baseSpeed;
        //     // Then, adjust position based on sine wave (this part is tricky with direct velocity control)
        //     // A better way for Rigidbody might be to apply forces or use a PID controller to follow a path.
        //     // For now, sticking to Transform-based movement for this example due to sine wave complexity with RB.
        // }
    }
}