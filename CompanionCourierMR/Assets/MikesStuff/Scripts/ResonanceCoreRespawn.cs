using UnityEngine;

/// <summary>
/// Handles the respawning of an object (like the Resonance Core) when it is
/// hit by another object with a specific tag (e.g., a PatrolBot).
/// </summary>
[RequireComponent(typeof(Rigidbody))] // This component requires a Rigidbody to function correctly.
public class ResonanceCoreRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [Tooltip("The tag of the object that will trigger a respawn when it collides with this object.")]
    public string respawnTriggerTag = "PatrolBot"; // Changed from BotProjectile to PatrolBot

    // --- Private Variables ---
    private Vector3 startingPosition;
    private Quaternion startingRotation;
    private Rigidbody rb;

    void Awake()
    {
        // Get the Rigidbody component once to be efficient.
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Store the initial position and rotation when the game begins.
        startingPosition = transform.position;
        startingRotation = transform.rotation;
        Debug.Log($"[{name}] Initial spawn position set to: {startingPosition}");
    }

    /// <summary>
    /// This method is called by Unity's physics engine whenever a collision occurs.
    /// </summary>
    /// <param name="collision">Information about the collision event.</param>
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the object we collided with has the tag we're looking for.
        if (collision.gameObject.CompareTag(respawnTriggerTag))
        {
            Debug.Log($"[{name}] was hit by an object with the '{respawnTriggerTag}' tag. Respawning now.", this);
            Respawn();
        }
    }

    /// <summary>
    /// Resets the object to its initial starting position and stops its movement.
    /// </summary>
    public void Respawn()
    {
        // Reset the position and rotation to where it started.
        transform.position = startingPosition;
        transform.rotation = startingRotation;

        // If there's a Rigidbody, reset its velocity and angular velocity to make it stop completely.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}