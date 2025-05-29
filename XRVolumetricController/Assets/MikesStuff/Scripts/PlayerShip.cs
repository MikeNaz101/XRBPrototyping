using UnityEngine;
using System.Collections;
using UnityEngine.Events; // For UnityEvents like OnDeath
using UnityEngine.SceneManagement; // Required for loading scenes
using TMPro; // Required for TextMeshProUGUI


// Enum for different types of power-ups
public enum PowerUpType
{
    Shield,
    BulletUpgrade_RapidFire,
    BulletUpgrade_SpreadShot,
    BulletUpgrade_LaserFire,
    MagnetBomb_SingleUse,
    HealthPack,
    DualThrusters // Player collects this to ENABLE speed boost activation
}

// Enum for different bullet types, used by PlayerShip to manage active upgrades
public enum BulletType
{
    Default,
    RapidFire,
    SpreadShot,
    Laser
}

public class PlayerShip : MonoBehaviour
{
    [Header("Player Stats")]
    [Tooltip("Maximum health of the player's ship.")]
    public float maxHealth = 100f;
    [Tooltip("Current health of the player's ship.")]
    [SerializeField] private float currentHealth;
    [Tooltip("Current level of the player.")]
    [SerializeField] private int currentLevel = 1;
    [Tooltip("Current experience points.")]
    [SerializeField] private float currentXP = 0f;
    [Tooltip("Experience points needed for the next level.")]
    [SerializeField] private float xpToNextLevel = 100f;

    [Header("Damage & Invulnerability")]
    [Tooltip("Duration in seconds the player is invulnerable after taking a hit.")]
    public float invulnerabilityDuration = 1.0f; 
    [Tooltip("How fast the ship blinks when invulnerable (seconds per on/off cycle). 0 for no blink.")]
    public float invulnerabilityBlinkRate = 0.1f; 
    [Tooltip("Renderer for the main ship model to apply blinking effect. If null, tries to find one in children.")]
    public Renderer shipModelRenderer; 

    [Header("Core References")]
    [Tooltip("Reference to the PlayerWeaponController script for changing bullet types and initiating fire.")]
    public PlayerWeaponController playerWeaponController;

    [Header("Shield Power-Up")]
    [Tooltip("GameObject representing the shield visual effect.")]
    public GameObject shieldVisual;
    [Tooltip("Default duration for the shield power-up in seconds.")]
    public float defaultShieldDuration = 10f;

    [Header("Bullet Power-Ups")]
    [Tooltip("Default bullet prefab used by the PlayerWeaponController.")]
    public GameObject defaultBulletPrefab;
    [Tooltip("Bullet prefab for Rapid Fire upgrade.")]
    public GameObject rapidFireBulletPrefab;
    [Tooltip("Bullet prefab for Spread Shot upgrade.")]
    public GameObject spreadShotBulletPrefab;
    [Tooltip("Bullet prefab for Laser Fire upgrade. This prefab should have the LaserBeamInstance script.")]
    public GameObject laserBulletPrefab;
    [Tooltip("Default duration for bullet upgrades in seconds.")]
    public float defaultBulletUpgradeDuration = 15f;
    [Tooltip("For pulsed lasers, how long each laser 'shot' instance lasts.")]
    public float laserPulseLifetime = 0.2f;

    [Header("Magnet Bomb Power-Up")]
    [Tooltip("Prefab for the Magnet Bomb effect or projectile. Must have MagnetBomb.cs script.")]
    public GameObject magnetBombPrefab;
    [Tooltip("Transform from where the magnet bomb is launched/activated.")]
    public Transform magnetBombLaunchPoint;
    [Tooltip("Initial speed for the magnet bomb projectile.")]
    public float magnetBombLaunchSpeed = 20f;
    [SerializeField] private int magnetBombCharges = 0;

    [Header("Dual Thrusters (Speed Boost) Power-Up")]
    [Tooltip("Movement speed multiplier when Dual Thrusters are active.")]
    public float speedBoostMultiplier = 1.5f;
    [Tooltip("Default duration for the speed boost in seconds (used when activated).")]
    public float defaultSpeedBoostDuration = 10f;
    [SerializeField] private bool hasSpeedBoostPowerUp = false; 
    private float collectedSpeedBoostDuration = 0f; 

    [Header("Effects & Audio")]
    [Tooltip("Particle effect for when the ship takes damage.")]
    public GameObject damageEffectPrefab;
    [Tooltip("Particle effect for when the ship is destroyed.")]
    public GameObject deathEffectPrefab;
    [Tooltip("AudioClip for taking damage.")]
    public AudioClip damageSound;
    [Tooltip("AudioClip for ship destruction.")]
    public AudioClip deathSound;
    [Tooltip("AudioClip for collecting a power-up.")]
    public AudioClip powerUpSound;
    [Tooltip("AudioSource for playing one-shot sound effects like damage, power-up pickup.")]
    public AudioSource effectsAudioSource;
    [Tooltip("AudioClip for leveling up.")]
    public AudioClip levelUpSound;
    [Tooltip("AudioClip for low health warning (should be a loop).")] 
    public AudioClip lowHealthWarningSound; 
    [Tooltip("AudioSource for playing the looping low health warning. If null, one will be added.")] 
    public AudioSource warningAudioSource; 
    [Tooltip("Health threshold (percentage, 0 to 1) to trigger low health warning.")] 
    [Range(0f, 1f)] public float lowHealthThreshold = 0.2f; 


    [Header("Events")]
    public UnityEvent OnPlayerDeath;
    public UnityEvent OnPlayerLevelUp;
    public UnityEvent<PowerUpType> OnPowerUpCollected; 

    // Private state variables
    private bool isShieldActive = false;
    private Coroutine shieldCoroutine;

    private Coroutine bulletUpgradeCoroutine;
    private BulletType currentActiveBulletUpgrade = BulletType.Default;

    private bool _isSpeedBoostActive = false; 
    private Coroutine speedBoostCoroutine;

    private bool isInvulnerable = false; 
    private Coroutine invulnerabilityCoroutine; 
    private bool isLowHealthSoundPlaying = false; 


    public float CurrentHealth => currentHealth;
    public int CurrentLevel => currentLevel;
    public float CurrentXP => currentXP;
    public float XPToNextLevel => xpToNextLevel;
    public float CurrentSpeedMultiplier => _isSpeedBoostActive ? speedBoostMultiplier : 1f;
    public bool IsSpeedBoostActive => _isSpeedBoostActive; 

    void Awake()
    {
        currentHealth = maxHealth;
        currentLevel = 1;
        currentXP = 0;
        xpToNextLevel = CalculateXPForNextLevel(currentLevel);

        if (playerWeaponController == null)
        {
            playerWeaponController = GetComponentInChildren<PlayerWeaponController>();
            if (playerWeaponController == null)
                Debug.LogError($"[{nameof(PlayerShip)}] PlayerWeaponController not found or assigned! Firing and bullet upgrades will not work.", this);
        }

        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }

        if (playerWeaponController != null && defaultBulletPrefab != null)
        {
            playerWeaponController.SetBulletPrefab(defaultBulletPrefab, BulletType.Default);
        }
        else if (playerWeaponController != null && defaultBulletPrefab == null)
        {
            Debug.LogWarning($"[{nameof(PlayerShip)}] DefaultBulletPrefab not assigned. Weapon controller might not have a starting bullet.", this);
        }

        if (shipModelRenderer == null)
        {
            shipModelRenderer = GetComponentInChildren<Renderer>();
        }

        if (warningAudioSource == null)
        {
            warningAudioSource = gameObject.AddComponent<AudioSource>();
            warningAudioSource.playOnAwake = false;
            warningAudioSource.loop = true; 
        }
        if (lowHealthWarningSound != null && warningAudioSource != null)
        {
            warningAudioSource.clip = lowHealthWarningSound;
        }
        else if (lowHealthWarningSound == null)
        {
            Debug.LogWarning($"[{nameof(PlayerShip)}] Low Health Warning Sound not assigned. This feature will be disabled.", this);
        }

        if (OnPowerUpCollected == null) 
        {
            OnPowerUpCollected = new UnityEvent<PowerUpType>();
        }
    }

    public void TakeDamage(float amount)
    {
        if (isInvulnerable || isShieldActive || currentHealth <= 0)
        {
            if(isInvulnerable) Debug.Log("Player is invulnerable, no damage taken.");
            if(isShieldActive) Debug.Log("Damage absorbed by shield!");
            return;
        }

        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Current Health: {currentHealth}");

        if (damageEffectPrefab != null)
        {
            Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
        }
        if (effectsAudioSource != null && damageSound != null)
        {
            effectsAudioSource.PlayOneShot(damageSound);
        }

        if (invulnerabilityCoroutine != null) StopCoroutine(invulnerabilityCoroutine);
        invulnerabilityCoroutine = StartCoroutine(InvulnerabilityRoutine());

        CheckLowHealthSound(); 

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    IEnumerator InvulnerabilityRoutine() 
    {
        isInvulnerable = true;

        if (shipModelRenderer != null && invulnerabilityBlinkRate > 0)
        {
            float endTime = Time.time + invulnerabilityDuration;
            bool rendererEnabledState = shipModelRenderer.enabled; 
            int blinkCount = 0;
            while (Time.time < endTime)
            {
                shipModelRenderer.enabled = !shipModelRenderer.enabled;
                yield return new WaitForSeconds(invulnerabilityBlinkRate / 2f); 
                blinkCount++;
            }
            shipModelRenderer.enabled = rendererEnabledState; 
            if (blinkCount % 2 != 0 && !rendererEnabledState) 
            {
                shipModelRenderer.enabled = true;
            }
        }
        else
        {
            yield return new WaitForSeconds(invulnerabilityDuration);
        }

        isInvulnerable = false;
        invulnerabilityCoroutine = null;
    }

    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
        CheckLowHealthSound(); 
    }

    void Die()
    {
        Debug.Log("Player Ship Destroyed!");
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }
        if (effectsAudioSource != null && deathSound != null)
        {
            effectsAudioSource.PlayOneShot(deathSound);
        }

        StopLowHealthSound(); 
        OnPlayerDeath.Invoke();
        gameObject.SetActive(false);
    }

    void CheckLowHealthSound() 
    {
        if (lowHealthWarningSound == null || warningAudioSource == null) return;

        bool isCurrentlyLowHealth = (currentHealth / maxHealth) <= lowHealthThreshold;

        if (isCurrentlyLowHealth && !isLowHealthSoundPlaying && currentHealth > 0) 
        {
            warningAudioSource.Play();
            isLowHealthSoundPlaying = true;
        }
        else if (!isCurrentlyLowHealth && isLowHealthSoundPlaying)
        {
            StopLowHealthSound();
        }
    }

    void StopLowHealthSound() 
    {
        if (warningAudioSource != null && warningAudioSource.isPlaying)
        {
            warningAudioSource.Stop();
        }
        isLowHealthSoundPlaying = false;
    }

    public void CollectPowerUp(PowerUpType type, float duration = 0f)
    {
        if (effectsAudioSource != null && powerUpSound != null)
        {
            effectsAudioSource.PlayOneShot(powerUpSound);
        }
        OnPowerUpCollected?.Invoke(type); 

        switch (type)
        {
            case PowerUpType.Shield:
                ActivateShield(duration > 0 ? duration : defaultShieldDuration);
                break;
            case PowerUpType.BulletUpgrade_RapidFire:
                ActivateBulletUpgrade(BulletType.RapidFire, rapidFireBulletPrefab, duration > 0 ? duration : defaultBulletUpgradeDuration);
                break;
            case PowerUpType.BulletUpgrade_SpreadShot:
                ActivateBulletUpgrade(BulletType.SpreadShot, spreadShotBulletPrefab, duration > 0 ? duration : defaultBulletUpgradeDuration);
                break;
            case PowerUpType.BulletUpgrade_LaserFire:
                ActivateBulletUpgrade(BulletType.Laser, laserBulletPrefab, duration > 0 ? duration : defaultBulletUpgradeDuration);
                break;
            case PowerUpType.MagnetBomb_SingleUse:
                AddMagnetBombCharge();
                break;
            case PowerUpType.HealthPack:
                Heal(maxHealth * 0.25f);
                break;
            case PowerUpType.DualThrusters:
                hasSpeedBoostPowerUp = true; 
                collectedSpeedBoostDuration = (duration > 0 ? duration : defaultSpeedBoostDuration);
                Debug.Log($"[{nameof(PlayerShip)}] Dual Thrusters power-up collected! Duration: {collectedSpeedBoostDuration}s. Ready for activation.");
                break;
        }
    }

    void ActivateShield(float duration)
    {
        if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
        shieldCoroutine = StartCoroutine(ShieldRoutine(duration));
    }

    IEnumerator ShieldRoutine(float duration)
    {
        isShieldActive = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);
        yield return new WaitForSeconds(duration);
        DeactivateShield();
    }

    void DeactivateShield()
    {
        isShieldActive = false;
        if (shieldVisual != null) shieldVisual.SetActive(false);
        shieldCoroutine = null;
    }

    void ActivateBulletUpgrade(BulletType newType, GameObject newBulletPrefab, float duration)
    {
        if (playerWeaponController == null) { Debug.LogWarning("PlayerWeaponController not assigned to PlayerShip."); return; }
        if (newBulletPrefab == null) { Debug.LogWarning($"Prefab for {newType} not assigned."); return; }

        if (bulletUpgradeCoroutine != null) StopCoroutine(bulletUpgradeCoroutine);
        
        if (currentActiveBulletUpgrade != BulletType.Default && currentActiveBulletUpgrade != newType)
        {
             playerWeaponController.SetBulletPrefab(defaultBulletPrefab, BulletType.Default);
        }

        playerWeaponController.SetBulletPrefab(newBulletPrefab, newType);
        currentActiveBulletUpgrade = newType;
        bulletUpgradeCoroutine = StartCoroutine(BulletUpgradeRoutine(duration));
    }

    IEnumerator BulletUpgradeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        RevertToDefaultBullet();
    }

    void RevertToDefaultBullet()
    {
        if (playerWeaponController != null && defaultBulletPrefab != null)
        {
            playerWeaponController.SetBulletPrefab(defaultBulletPrefab, BulletType.Default);
        }
        currentActiveBulletUpgrade = BulletType.Default;
        bulletUpgradeCoroutine = null;
    }

    void AddMagnetBombCharge()
    {
        magnetBombCharges++;
    }

    public void UseMagnetBomb()
    {
        if (magnetBombCharges > 0)
        {
            magnetBombCharges--;
            ActivateMagnetBombActual();
        }
    }

    void ActivateMagnetBombActual()
    {
        if (magnetBombPrefab != null)
        {
            Transform spawnTransform = magnetBombLaunchPoint != null ? magnetBombLaunchPoint : transform;
            GameObject bombInstance = Instantiate(magnetBombPrefab, spawnTransform.position, spawnTransform.rotation);
            
            Rigidbody bombRb = bombInstance.GetComponent<Rigidbody>();
            if (bombRb != null)
            {
                bombRb.AddForce(spawnTransform.forward * magnetBombLaunchSpeed, ForceMode.Impulse);
            }
        }
    }

    public bool TryActivateSpeedBoost() 
    {
        if (hasSpeedBoostPowerUp && !_isSpeedBoostActive)
        {
            ActivateSpeedBoostActual(collectedSpeedBoostDuration);
            hasSpeedBoostPowerUp = false; 
            collectedSpeedBoostDuration = 0f;
            return true;
        }
        if (_isSpeedBoostActive)
        {
            Debug.Log($"[{nameof(PlayerShip)}] Speed boost already active.");
        }
        if (!hasSpeedBoostPowerUp)
        {
            Debug.Log($"[{nameof(PlayerShip)}] No speed boost power-up available to activate.");
        }
        return false;
    }

    private void ActivateSpeedBoostActual(float duration) 
    {
        if (speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
        speedBoostCoroutine = StartCoroutine(SpeedBoostRoutine(duration));
    }

    IEnumerator SpeedBoostRoutine(float duration)
    {
        _isSpeedBoostActive = true;
        Debug.Log($"[{nameof(PlayerShip)}] Speed Boost ACTIVATED. Duration: {duration}s. Multiplier: {speedBoostMultiplier}");
        yield return new WaitForSeconds(duration);
        DeactivateSpeedBoost();
    }

    void DeactivateSpeedBoost()
    {
        _isSpeedBoostActive = false;
        speedBoostCoroutine = null;
        Debug.Log($"[{nameof(PlayerShip)}] Speed Boost DEACTIVATED.");
    }

    public void AddXP(float amount) 
    {
        if (amount <= 0) return;
        currentXP += amount;
        CheckForLevelUp();
    }

    void CheckForLevelUp() 
    {
        while (currentXP >= xpToNextLevel && xpToNextLevel > 0) 
        {
            currentXP -= xpToNextLevel; 
            IncreaseLevelInternal(); 
        }
    }

    private void IncreaseLevelInternal(int levels = 1) 
    {
        currentLevel += levels;
        xpToNextLevel = CalculateXPForNextLevel(currentLevel);
        Debug.Log($"Player Leveled Up to Level {currentLevel}! XP to next: {xpToNextLevel}. Current XP: {currentXP}");
        OnPlayerLevelUp.Invoke();
        if (effectsAudioSource != null && levelUpSound != null)
        {
            effectsAudioSource.PlayOneShot(levelUpSound);
        }
    }
    
    public void GrantLevels(int levels = 1)
    {
        if (levels <=0) return;
        currentLevel += levels;
        currentXP = 0; 
        xpToNextLevel = CalculateXPForNextLevel(currentLevel);
         Debug.Log($"Player Granted {levels} Level(s)! Now Level {currentLevel}. XP to next: {xpToNextLevel}.");
        OnPlayerLevelUp.Invoke();
         if (effectsAudioSource != null && levelUpSound != null)
        {
            effectsAudioSource.PlayOneShot(levelUpSound);
        }
    }


    float CalculateXPForNextLevel(int level) 
    {
        return 75 + (level * 25); 
    }

    void OnTriggerEnter(Collider other)
    {
        PowerUpItem powerUpItem = other.GetComponent<PowerUpItem>();
        if (powerUpItem != null)
        {
            CollectPowerUp(powerUpItem.powerUpType, powerUpItem.duration);
            Destroy(other.gameObject); 
        }
    }

    public void ResetPlayerStateForNewGame()
    {
        currentHealth = maxHealth;
        currentLevel = 1;
        currentXP = 0;
        xpToNextLevel = CalculateXPForNextLevel(currentLevel);
        isShieldActive = false;
        if(shieldVisual) shieldVisual.SetActive(false);
        if(shieldCoroutine != null) StopCoroutine(shieldCoroutine);
        _isSpeedBoostActive = false;
        if(speedBoostCoroutine != null) StopCoroutine(speedBoostCoroutine);
        hasSpeedBoostPowerUp = false;
        RevertToDefaultBullet();
        if(bulletUpgradeCoroutine != null) StopCoroutine(bulletUpgradeCoroutine);
        isInvulnerable = false;
        if(invulnerabilityCoroutine != null) StopCoroutine(invulnerabilityCoroutine);
        if(shipModelRenderer) shipModelRenderer.enabled = true;
        magnetBombCharges = 0;
        
        StopLowHealthSound(); 
        gameObject.SetActive(true); 
    }
}