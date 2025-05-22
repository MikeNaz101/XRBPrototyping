// File: TemporaryDisturbanceSwarm.cs
// Purpose: Plays a particle effect and optional audio, then destroys itself after a set lifetime.
// Instructions:
// 1. Attach this script to your "disturbance swarm" prefab.
// 2. Ensure the prefab has a ParticleSystem component set to "Play On Awake" = true and "Looping" = false.
// 3. Optionally, add an AudioSource with a sound effect set to "Play On Awake" = true and "Loop" = false.
// 4. The lifetime can be set in the Inspector on the prefab, or overridden by FrameAgitationSpawner.

using UnityEngine;
using System.Collections;

public class TemporaryDisturbanceSwarm : MonoBehaviour
{
    [Tooltip("Default lifetime in seconds for this effect before it's destroyed. Can be overridden by spawner.")]
    public float defaultLifetime = 2.5f;

    private ParticleSystem _particleSystem;
    private AudioSource _audioSource;
    private bool _isInitialized = false;

    void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        _audioSource = GetComponent<AudioSource>(); // Optional

        if (_particleSystem == null)
        {
            Debug.LogError("TemporaryDisturbanceSwarm: No ParticleSystem component found on this GameObject! Effect cannot play.", this);
            Destroy(gameObject); // Destroy immediately if no particle system
            return;
        }
    }

    void Start()
    {
        // If not initialized by a spawner, use default lifetime and play immediately.
        // ParticleSystem and AudioSource should have "Play On Awake" checked on the prefab.
        if (!_isInitialized)
        {
            Initialize(defaultLifetime);
        }
    }

    /// <summary>
    /// Initializes the swarm with a specific lifetime. Called by the spawner.
    /// </summary>
    public void Initialize(float lifetime)
    {
        if (_particleSystem != null && !_particleSystem.isPlaying)
        {
            _particleSystem.Play();
        }
        if (_audioSource != null && _audioSource.clip != null && !_audioSource.isPlaying)
        {
            _audioSource.Play();
        }

        Destroy(gameObject, lifetime);
        _isInitialized = true;
        // Debug.Log($"TemporaryDisturbanceSwarm {gameObject.name} initialized with lifetime {lifetime}s.");
    }
}
