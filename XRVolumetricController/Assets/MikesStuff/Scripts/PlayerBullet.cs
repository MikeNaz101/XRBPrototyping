// PlayerBullet.cs
using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float damageAmount = 25f; // How much damage this bullet does
    public float speed = 50f;
    public float lifetime = 3f; // How long the bullet lives before self-destructing
    public GameObject hitEffectPrefab;

    void Start()
    {
        // If your bullet doesn't have a Rigidbody to move it via AddForce
        // (like in the HandMirrorVolume example), you might move it here.
        // However, the FireBullet method in your older script added force,
        // and PlayerWeaponController.Fire() also implies the bullet prefab handles its own movement.
        // If it's a Rigidbody bullet, the force is applied on instantiation.
        // If it's not, you'd add:
        // Rigidbody rb = GetComponent<Rigidbody>();
        // if (rb != null) rb.velocity = transform.forward * speed;
        // else transform.Translate(Vector3.forward * speed * Time.deltaTime); // Less ideal for continuous movement

        Destroy(gameObject, lifetime); // Self-destruct after a certain time
    }

    // This function is called when this collider/rigidbody has begun touching another rigidbody/collider
    void OnCollisionEnter(Collision collision)
    {
        // Check if the object we collided with has an Enemy script
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            // We hit an enemy!
            enemy.TakeDamage(damageAmount);

            // Optional: Instantiate a hit effect at the collision point
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, collision.contacts[0].point, Quaternion.LookRotation(collision.contacts[0].normal));
            }
        }

        // Destroy the bullet on any collision (or only on collision with an enemy, depending on your game design)
        Destroy(gameObject);
    }

    // Alternatively, if your enemies and/or bullets are triggers:
    void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damageAmount);
        }

        // Only destroy if it's not another trigger that shouldn't destroy the bullet
        // (e.g., a power-up pickup zone if bullets could pass through those)
        if (other.gameObject.layer != LayerMask.NameToLayer("IgnoreBulletCollision")) // Example layer
        {
             Destroy(gameObject);
        }
    }
}