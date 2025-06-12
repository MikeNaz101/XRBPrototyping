// Health.cs
using UnityEngine;

// This component makes any GameObject take damage and be destroyed.
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [Tooltip("The maximum and starting health for this object.")]
    public int maxHealth = 100;

    [Header("Optional Effects")]
    [Tooltip("Particle effect to instantiate when the object is destroyed.")]
    public GameObject destructionEffectPrefab;

    private int _currentHealth;
    private HexCell _parentHex; // A reference to the hex this object occupies

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    /// <summary>
    /// Sets the parent hex cell so this object can vacate it upon destruction.
    /// This should be called by the HexCell when this object occupies it.
    /// </summary>
    public void SetParentHex(HexCell hex)
    {
        _parentHex = hex;
    }

    /// <summary>
    /// This method is called by attackers to deal damage.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (_currentHealth <= 0) return; // Already destroyed

        _currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage, has {_currentHealth}/{maxHealth} health remaining.");

        // TODO: Add feedback here, like flashing the material red.

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Handles the destruction of this GameObject.
    /// </summary>
    private void Die()
    {
        Debug.Log($"{gameObject.name} has been destroyed.");

        // Vacate the hex cell it was on so a new object can be placed there.
        if (_parentHex != null)
        {
            _parentHex.Vacate();
        }

        // Play a destruction particle effect if one is assigned.
        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Remove the GameObject from the scene.
        Destroy(gameObject);
    }
}
