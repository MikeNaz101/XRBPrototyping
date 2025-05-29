using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    public PowerUpType powerUpType = PowerUpType.Shield;
    [Tooltip("Duration of the power-up in seconds. 0 means default or infinite/single use.")]
    public float duration = 10f; // Can be overridden by PlayerShip defaults if 0

    // Optional: Add visuals or simple bobbing animation here
    void Start()
    {
        // Ensure it has a trigger collider to be picked up
        Collider col = GetComponent<Collider>();
        if (col && !col.isTrigger)
        {
            Debug.LogWarning($"PowerUpItem '{gameObject.name}' collider should be a trigger.", this);
            col.isTrigger = true;
        }
        else if (!col)
        {
            Debug.LogError($"PowerUpItem '{gameObject.name}' is missing a Collider component.", this);
        }
    }
}