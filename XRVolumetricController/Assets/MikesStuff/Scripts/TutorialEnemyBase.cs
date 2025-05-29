using UnityEngine;
using System.Collections.Generic; // For possible future use with drops, though tutorial manager handles it now

// Assuming IDamageable interface is defined elsewhere:
// public interface IDamageable { void TakeDamage(float amount); }

[RequireComponent(typeof(Collider))]
public abstract class TutorialEnemyBase : MonoBehaviour, IDamageable
{
    [Header("Tutorial Enemy Stats")]
    [Tooltip("How much health this tutorial enemy has.")]
    public float health = 50f; // Can be lower for tutorial
    [Tooltip("Movement speed, if applicable directly.")]
    public float baseSpeed = 2f;

    [Header("References")]
    [Tooltip("Transform of the player's ship. TutorialManager should assign this.")]
    public Transform playerShipTransform; // TutorialManager will set this

    protected Rigidbody rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        if (playerShipTransform == null)
        {
            // TutorialManager is responsible for assigning this.
            // If still null, it might be an issue with TutorialManager's setup.
            Debug.LogWarning($"[{gameObject.name}] PlayerShipTransform not assigned by TutorialManager. Enemy might not behave correctly.", this);
        }
    }

    protected virtual void Update()
    {
        if (playerShipTransform != null && health > 0)
        {
            Move();
        }
    }

    protected abstract void Move();

    public virtual void TakeDamage(float amount)
    {
        if (health <= 0) return; // Already dead

        health -= amount;
        // Debug.Log($"Tutorial Enemy {gameObject.name} took {amount} damage, health: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // Debug.Log($"Tutorial Enemy {gameObject.name} has been destroyed.");
        // No GameManager or GameplayLoopManager notification needed for tutorial enemies.
        // TutorialManager detects death by checking if the spawned enemy GameObject is null.

        // Optional: Instantiate a simple explosion effect for tutorial
        // if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject); // This is what TutorialManager looks for
    }

    // Collision/Trigger logic for taking damage from player bullets
    // (Similar to your main Enemy.cs, but simplified - no game manager interactions)
    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Example: Check if hit by a player bullet
        // This assumes your player bullets have a script (e.g., PlayerBullet) and a tag.
        if (collision.gameObject.CompareTag("Bullet")) // Make sure your player bullets are tagged "PlayerBullet"
        {
            PlayerBullet bullet = collision.gameObject.GetComponent<PlayerBullet>();
            if (bullet != null)
            {
                TakeDamage(bullet.damageAmount);
            }
            Destroy(collision.gameObject); // Destroy the bullet
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            PlayerBullet bullet = other.GetComponent<PlayerBullet>();
            if (bullet != null)
            {
                TakeDamage(bullet.damageAmount);
            }
            Destroy(other.gameObject);
        }
    }
}
