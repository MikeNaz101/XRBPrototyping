using UnityEngine;

public class PyramidEnemy : Enemy
{
    [Header("Pyramid Specific Settings")]
    [Tooltip("How quickly the pyramid turns to face the player.")]
    public float rotationSpeed = 2f;
    // BaseSpeed from Enemy class will be used for its forward movement.

    protected override void Awake()
    {
        base.Awake();
        // If using Rigidbody for movement, configure it here
        if (rb != null)
        {
            rb.useGravity = false;
            rb.linearDamping = 1.0f; // Some drag for smoother deceleration if it overshoots
        }
    }
    protected override void Move()
    {
        if (playerShipTransform == null) return;

        // --- Orientation ---
        Vector3 directionToPlayer = (playerShipTransform.position - transform.position).normalized;
        if (directionToPlayer != Vector3.zero) // Avoid LookRotation error if direction is zero
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Smooth Forward Movement ---
        // Using transform.Translate for simple, smooth movement.
        // If you need physics interactions (bouncing off things), use Rigidbody.MovePosition or rb.velocity.
        transform.Translate(Vector3.forward * baseSpeed * Time.deltaTime);

        // Example using Rigidbody for movement (if you add/enable Rigidbody):
        // if (rb != null)
        // {
        //     // Calculate desired velocity
        //     Vector3 desiredVelocity = transform.forward * baseSpeed;
        //     // Smoothly change current velocity towards desired velocity
        //     // This requires careful tuning of rb.drag or applying counter-forces for deceleration.
        //     // A simpler approach for smooth follow with Rigidbody:
        //     Vector3 targetPosition = transform.position + transform.forward * baseSpeed * Time.deltaTime;
        //     rb.MovePosition(targetPosition);
        //
        //     // Or direct velocity control:
        //     // rb.velocity = transform.forward * baseSpeed;
        // }
    }
}
