using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class MagnetBomb : MonoBehaviour
{
    [Header("Detonation Settings")]
    [Tooltip("Time in seconds before the bomb detonates if it doesn't hit anything.")]
    public float lifeTime = 5f;
    [Tooltip("Layers that the bomb will consider for detonation on impact.")]
    public LayerMask collisionDetonationMask; // E.g., "Environment", "Enemies"

    [Header("Magnetic Effect Settings")]
    [Tooltip("Radius of the magnetic pull effect.")]
    public float explosionRadius = 10f;
    [Tooltip("Force of the magnetic pull towards the center.")]
    public float pullForce = 50f;
    [Tooltip("Duration in seconds the magnetic pull effect lasts.")]
    public float effectDuration = 5f; // This is the "stuck together" duration
    [Tooltip("Layers that will be affected by the magnetic pull (should be your Enemy layer).")]
    public LayerMask enemyLayerMask;

    [Header("Visuals & Audio (Optional)")]
    [Tooltip("Prefab to instantiate at the detonation point (e.g., an explosion visual).")]
    public GameObject detonationEffectPrefab;
    [Tooltip("Sound to play on detonation.")]
    public AudioClip detonationSound;
    [Tooltip("AudioSource for playing detonation sound (can be on this prefab or an AudioSource will be added).")]
    public AudioSource audioSource;


    private Rigidbody rb;
    private bool hasDetonated = false;
    private Coroutine detonationTimerCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    void Start()
    {
        // Start a timer for self-detonation if it doesn't hit anything
        if (lifeTime > 0)
        {
            detonationTimerCoroutine = StartCoroutine(DetonateAfterTime(lifeTime));
        }
    }

    IEnumerator DetonateAfterTime(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!hasDetonated)
        {
            Detonate(transform.position);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the collision is with a layer that should trigger detonation
        if (!hasDetonated && ((1 << collision.gameObject.layer) & collisionDetonationMask) != 0)
        {
            if (detonationTimerCoroutine != null)
            {
                StopCoroutine(detonationTimerCoroutine);
            }
            Detonate(collision.contacts[0].point); // Detonate at impact point
        }
    }

    void Detonate(Vector3 detonationPoint)
    {
        if (hasDetonated) return;
        hasDetonated = true;

        // Debug.Log($"Magnet Bomb detonated at {detonationPoint}");

        // Instantiate detonation visual effect
        if (detonationEffectPrefab != null)
        {
            Instantiate(detonationEffectPrefab, detonationPoint, Quaternion.identity);
        }

        // Play detonation sound
        if (audioSource != null && detonationSound != null)
        {
            // Play sound at the detonation point if the AudioSource is not on a moving object
            // For simplicity, playing it from this object's AudioSource before it's fully disabled/destroyed.
            audioSource.PlayOneShot(detonationSound);
        }

        // Find all enemies within the radius
        Collider[] hitColliders = Physics.OverlapSphere(detonationPoint, explosionRadius, enemyLayerMask);
        List<Enemy> affectedEnemies = new List<Enemy>();
        List<Rigidbody> affectedEnemyRbs = new List<Rigidbody>();

        foreach (Collider hitCollider in hitColliders)
        {
            Enemy enemy = hitCollider.GetComponentInParent<Enemy>(); // Get Enemy script
            if (enemy != null && !affectedEnemies.Contains(enemy)) // Ensure each enemy is processed once
            {
                affectedEnemies.Add(enemy);
                Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    affectedEnemyRbs.Add(enemyRb);
                }
                enemy.SetMovementExternallyDisabled(true); // Disable their normal movement
                // Debug.Log($"Enemy {enemy.gameObject.name} caught in magnetic field.");
            }
        }

        // Start the magnetic pull effect
        if (affectedEnemies.Count > 0)
        {
            StartCoroutine(MagneticPullRoutine(detonationPoint, affectedEnemies, affectedEnemyRbs));
        }

        // Disable the bomb's visual and physics components, but don't destroy the GameObject yet
        // as it's running the coroutine. The coroutine will destroy it.
        Renderer rend = GetComponent<Renderer>();
        if (rend) rend.enabled = false;
        Collider col = GetComponent<Collider>();
        if (col) col.enabled = false;
        if (rb) rb.isKinematic = true; // Stop it from moving further
    }

    IEnumerator MagneticPullRoutine(Vector3 pullCenter, List<Enemy> enemies, List<Rigidbody> enemyRbs)
    {
        float startTime = Time.time;

        while (Time.time < startTime + effectDuration)
        {
            foreach (Rigidbody enemyRb in enemyRbs)
            {
                if (enemyRb != null) // Check if enemy still exists
                {
                    Vector3 directionToCenter = (pullCenter - enemyRb.position).normalized;
                    enemyRb.AddForce(directionToCenter * pullForce * Time.deltaTime, ForceMode.VelocityChange); // Apply force consistently

                    // Optional: Dampen velocity if they are trying to escape too fast or bounce off each other
                    if (enemyRb.linearVelocity.magnitude > pullForce * 0.1f) // Arbitrary damping threshold
                    {
                       // enemyRb.velocity *= 0.95f;
                    }
                }
            }
            yield return null; // Wait for the next frame
        }

        // Release enemies
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null) // Check if enemy still exists
            {
                enemy.SetMovementExternallyDisabled(false); // Re-enable their normal movement
                // Debug.Log($"Enemy {enemy.gameObject.name} released from magnetic field.");
                // Optionally, clear their velocity if you want them to stop dead after being released
                Rigidbody r = enemy.GetComponent<Rigidbody>();
                if(r) r.linearVelocity = Vector3.zero;
            }
        }

        // Debug.Log("Magnetic effect finished. Destroying bomb object.");
        Destroy(gameObject); // Now destroy the bomb GameObject
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
