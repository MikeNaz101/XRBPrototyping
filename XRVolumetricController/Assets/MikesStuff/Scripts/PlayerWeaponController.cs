// --- PlayerWeaponController Script (Place on your ship or weapon mount GameObject) ---
// (This script remains the same as the last version in the Canvas)
using UnityEngine; 

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Weapon Settings")]
    [Tooltip("The current bullet prefab this weapon will fire.")]
    public GameObject currentBulletPrefab; 
    [Tooltip("Current type of bullet/weapon active. Set by PlayerShip.")]
    public BulletType currentBulletType { get; private set; } 
    [Tooltip("Array of Transforms where bullets will be instantiated from.")]
    public Transform[] firePoints;
    [Tooltip("Optional: Muzzle flash particle effect prefab to instantiate. Ensure it has self-destruction or a short duration & 'Stop Action: Destroy'.")]
    public GameObject muzzleFlashPrefab; 
    [Tooltip("Optional: Sound effect for firing.")]
    public AudioClip fireSound;
    [Tooltip("Optional: AudioSource to play the fire sound.")]
    public AudioSource weaponAudioSource;
    [Tooltip("Speed for projectile bullets. Laser might ignore this or use it differently.")]
    public float projectileSpeed = 50f;

    private int _currentFirePointIndex = 0;
    private PlayerShip _playerShip; 

    void Awake()
    {
        _playerShip = GetComponentInParent<PlayerShip>();
        if (_playerShip == null)
        {
            Debug.LogError("PlayerWeaponController could not find PlayerShip component in parent hierarchy. Laser pulse lifetime might not be correctly fetched.", this);
        }
    }

    public void SetBulletPrefab(GameObject newPrefab, BulletType type)
    {
        if (newPrefab != null)
        {
            currentBulletPrefab = newPrefab;
            currentBulletType = type;
        }
        else
        {
            Debug.LogWarning($"Attempted to set a null bullet prefab on PlayerWeaponController for type {type}.");
        }
    }

    public void Fire()
    {
        if (currentBulletPrefab == null) return;
        if (firePoints == null || firePoints.Length == 0) return;

        int firePointIndexToUse = _currentFirePointIndex % firePoints.Length;
        Transform firePointToUse = firePoints[firePointIndexToUse];
        
        if (firePointToUse == null)
        {
            Debug.LogError($"PlayerWeaponController: FirePoint at index {firePointIndexToUse} is null.");
            _currentFirePointIndex++; 
            return;
        }

        if (currentBulletType == BulletType.Laser)
        {
            GameObject laserInstanceObject = Instantiate(currentBulletPrefab, firePointToUse.position, firePointToUse.rotation);
            LaserBeamInstance laserBeam = laserInstanceObject.GetComponent<LaserBeamInstance>();
            if (laserBeam != null)
            {
                float pulseLifetime = _playerShip != null ? _playerShip.laserPulseLifetime : 0.2f; 
                laserBeam.Initialize(pulseLifetime, firePointToUse.position, firePointToUse.forward);
            }
            else
            {
                Debug.LogError("LaserBulletPrefab does not have a LaserBeamInstance component!", laserInstanceObject);
                Destroy(laserInstanceObject); 
            }
        }
        else 
        {
            GameObject newBullet = Instantiate(currentBulletPrefab, firePointToUse.position, firePointToUse.rotation);
            Rigidbody bulletRb = newBullet.GetComponent<Rigidbody>();
            if (bulletRb != null)
            {
                bulletRb.linearVelocity = firePointToUse.forward * projectileSpeed;
            }
            else
            {
                Debug.LogWarning($"Bullet prefab '{currentBulletPrefab.name}' is missing a Rigidbody. It might not move as expected unless its own script handles movement.", newBullet);
            }
        }

        if (muzzleFlashPrefab != null)
        {
            GameObject flashInstance = Instantiate(muzzleFlashPrefab, firePointToUse.position, firePointToUse.rotation);
            flashInstance.transform.SetParent(firePointToUse, true); 
        }

        if (weaponAudioSource != null && fireSound != null)
        {
            weaponAudioSource.PlayOneShot(fireSound);
        }

        _currentFirePointIndex++; 
    }
}
