using UnityEngine;
using System.Collections.Generic; // Added for List

// Assuming IDamageable interface is defined elsewhere:
// public interface IDamageable { void TakeDamage(float amount); }

[RequireComponent(typeof(Collider))] // Enemies should be able to be hit
public abstract class Enemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    [Tooltip("How much health this enemy has.")]
    public float health = 100f;
    [Tooltip("How many points this enemy is worth when destroyed.")]
    public int scoreValue = 10;
    [Tooltip("Movement speed, if applicable directly (some enemies might use forces).")]
    public float baseSpeed = 2f;

    [Header("References")]
    [Tooltip("Transform of the player's ship. Can be auto-assigned by tag if null.")]
    public Transform playerShipTransform;
    [Tooltip("Reference to the GameManager. Can be auto-assigned if null.")]
    public GameManager gameManager; // Assuming GameManager script exists
    public float collisionDamageToPlayer = 10f; // How much damage this enemy does on collision

    [Header("Power-Up Drop Settings (Optional)")]
    [Tooltip("Chance for this enemy to drop a power-up (0 to 1). 0 means never, 1 means always.")]
    [Range(0f, 1f)]
    public float powerUpDropChance = 0.1f;
    [Tooltip("List of power-up item prefabs this enemy can drop. Assign your PowerUpItem prefabs here.")]
    public List<GameObject> possiblePowerUpDrops;


    protected Rigidbody rb;
    public bool IsMovementExternallyDisabled { get; private set; } = false; // New flag for magnet bomb

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    protected virtual void Start()
    {
        // Auto-assign player ship transform if not set, by looking for a "Player" tag
        if (playerShipTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerShipTransform = playerObject.transform;
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Player ship transform not found! Enemy may not behave correctly. Ensure player has 'Player' tag.", this);
            }
        }

        // Auto-assign GameManager instance if not set
        if (gameManager == null)
        {
            if (GameManager.Instance != null) // Assuming GameManager uses a singleton pattern
            {
                gameManager = GameManager.Instance;
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] GameManager instance not found! Score will not be tracked. Ensure GameManager exists in the scene.", this);
            }
        }
    }

    protected virtual void Update()
    {
        // Check if movement is externally disabled (e.g., by magnet bomb)
        if (IsMovementExternallyDisabled)
        {
            // When movement is disabled, the enemy's own Move() logic is skipped.
            // The MagnetBomb script will handle its movement by applying forces.
            return;
        }

        // Ensure the enemy is active and player exists before moving
        if (playerShipTransform != null && health > 0)
        {
            Move();
        }
    }

    // Abstract method that all derived enemy types MUST implement
    protected abstract void Move();

    // New method to allow external scripts (like MagnetBomb) to disable/enable movement
    public void SetMovementExternallyDisabled(bool isDisabled)
    {
        IsMovementExternallyDisabled = isDisabled;
        if (isDisabled && rb != null)
        {
            // When disabled by magnet, we might want to let the magnet bomb control its velocity entirely.
            // Setting velocity to zero here might fight with the magnet's pull initially.
            // Consider if rb.velocity = Vector3.zero; is needed or if the magnet's pull is sufficient.
            // For now, we'll just set the flag and let MagnetBomb apply forces.
        }
        // If re-enabling (isDisabled is false), their normal Move() method in Update() will take over.
    }


    public virtual void TakeDamage(float amount)
    {
        if (health <= 0) return; // Already dead

        health -= amount;
        // Debug.Log($"{gameObject.name} took {amount} damage, health: {health}");

        if (health <= 0)
        {
            Die();
        }
        // else
        // {
        //     // Optional: Play a hit effect or sound
        // }
    }

    protected virtual void Die()
    {
        // Debug.Log($"{gameObject.name} has been destroyed.");

        if (gameManager != null)
        {
            gameManager.EnemyDestroyed(scoreValue);
        }
        
        if (GameplayLoopManager.Instance != null)
        {
            GameplayLoopManager.Instance.EnemyDestroyedByPlayer();
        }

        // --- Power-Up Drop Logic ---
        if (possiblePowerUpDrops != null && possiblePowerUpDrops.Count > 0)
        {
            if (Random.value <= powerUpDropChance) // Check if a drop should occur based on chance
            {
                int randomIndex = Random.Range(0, possiblePowerUpDrops.Count);
                GameObject powerUpToDrop = possiblePowerUpDrops[randomIndex];
                if (powerUpToDrop != null)
                {
                    Instantiate(powerUpToDrop, transform.position, Quaternion.identity); // Spawn the power-up at enemy's position
                    // Debug.Log($"{gameObject.name} dropped power-up: {powerUpToDrop.name}");
                }
            }
        }
        // --- End Power-Up Drop Logic ---

        // Optional: Instantiate explosion effect, play death sound
        // if (explosionPrefab != null) Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        // Check if the object we collided with is the PlayerShip
        PlayerShip player = collision.gameObject.GetComponent<PlayerShip>();
        if (player != null)
        {
            // We hit the player ship!
            player.TakeDamage(collisionDamageToPlayer);

            // Optional: Enemy might also take damage or be destroyed on impact
            // TakeDamage(someSelfDamageAmount);
            // Die(); // Or just destroy itself
        }

        // Example: Check if hit by a player bullet (if bullets are non-triggers)
        // if (collision.gameObject.CompareTag("PlayerBullet"))
        // {
        //     PlayerBullet bullet = collision.gameObject.GetComponent<PlayerBullet>();
        //     if (bullet != null)
        //     {
        //         TakeDamage(bullet.damageAmount);
        //         Destroy(collision.gameObject); // Destroy the bullet
        //     }
        // }
    }

     protected virtual void OnTriggerEnter(Collider other)
    {
        // Example: Check if hit by a player bullet (if bullets are triggers)
        // if (other.CompareTag("PlayerBullet"))
        // {
        //     Bullet bullet = other.GetComponent<Bullet>();
        //     if (bullet != null)
        //     {
        //         TakeDamage(bullet.damageAmount);
        //         Destroy(other.gameObject); // Destroy the bullet
        //     }
        // }
    }
}
