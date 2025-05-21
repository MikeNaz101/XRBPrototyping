// File: HiveActivityManager.cs
// Purpose: Manages the main hive swarm particle effect (noise, emission) and its accompanying audio,
// including an agitation response when a specific object (like the hive top) is interacted with.
// Instructions:
// 1. Create an empty GameObject in your scene (e.g., "HiveManager").
// 2. Attach this script to the "HiveManager" GameObject.
// 3. Add an AudioSource component to this same "HiveManager" GameObject.
//    - Assign a continuous, loopable bee buzzing AudioClip to the AudioSource's "AudioClip" slot.
//    - Ensure "Play On Awake" and "Loop" on the AudioSource are CHECKED.
// 4. Assign your main hive swarm Particle System to the 'Main Hive Swarm Particle System' slot.
// 5. Configure the original and agitated noise/emission values, and audio parameters.
// 6. Assign the GameObject representing your hive top to the 'Hive Top Interactable Object' slot.
// 7. From that interactable component's "On Select Entered" (or equivalent "On Grabbed") event,
//    call the 'OnHiveTopGrabbed()' method of this HiveActivityManager script.

using UnityEngine;
using System.Collections;

public class HiveActivityManager : MonoBehaviour
{
    [Header("Main Hive Swarm Configuration")]
    [Tooltip("The primary Particle System representing the bees around the hive.")]
    public ParticleSystem mainHiveSwarmParticleSystem;

    [Tooltip("The GameObject that, when grabbed, will agitate the main hive swarm.")]
    public GameObject hiveTopInteractableObject; // For clarity in Inspector

    [Header("Normal Swarm State - Particles")]
    [Tooltip("Normal noise strength for the main hive swarm.")]
    public float normalNoiseStrength = 0.5f;
    [Tooltip("Normal emission rate over time for the main hive swarm.")]
    public float normalEmissionRate = 20f;

    [Header("Agitated Swarm State - Particles")]
    [Tooltip("Noise strength when the swarm is agitated.")]
    public float agitatedNoiseStrength = 2.0f;
    [Tooltip("Emission rate over time when the swarm is agitated.")]
    public float agitatedEmissionRate = 100f;
    [Tooltip("How long the swarm stays agitated in seconds.")]
    public float agitationDuration = 10.0f;

    [Header("Audio Configuration")]
    [Tooltip("AudioSource component for the hive swarm's buzzing sound.")]
    public AudioSource hiveSwarmAudioSource; // Assign this in Inspector
    [Tooltip("AudioClip for the continuous hive buzzing. Should be loopable.")]
    public AudioClip hiveBuzzLoopClip; // Assign this in Inspector

    [Header("Normal Swarm State - Audio")]
    [Tooltip("Volume of the buzzing sound during normal state.")]
    [Range(0f, 1f)]
    public float normalVolume = 0.5f;
    [Tooltip("Pitch of the buzzing sound during normal state (1 is normal).")]
    [Range(0.5f, 2f)]
    public float normalPitch = 1.0f;

    [Header("Agitated Swarm State - Audio")]
    [Tooltip("Volume of the buzzing sound during agitation.")]
    [Range(0f, 1f)]
    public float agitatedVolume = 0.9f;
    [Tooltip("Pitch of the buzzing sound during agitation.")]
    [Range(0.5f, 2f)]
    public float agitatedPitch = 1.3f;


    private Coroutine _agitationCoroutine;
    private bool _isCurrentlyAgitated = false;

    // Store initial values from the Particle System if not overridden by public vars
    private float _initialMainSwarmNoiseStrength;
    private float _initialMainSwarmEmissionRate;


    void Start()
    {
        // --- Particle System Setup ---
        if (mainHiveSwarmParticleSystem == null)
        {
            Debug.LogError("HiveActivityManager: Main Hive Swarm Particle System is not assigned!", this);
            enabled = false;
            return;
        }

        var noiseModulePS = mainHiveSwarmParticleSystem.noise;
        _initialMainSwarmNoiseStrength = noiseModulePS.strength.constant; // Assuming constant for simplicity

        var emissionModulePS = mainHiveSwarmParticleSystem.emission;
        _initialMainSwarmEmissionRate = emissionModulePS.rateOverTime.constant; // Assuming constant rate

        SetMainSwarmParticleState(normalNoiseStrength, normalEmissionRate);
        if (!mainHiveSwarmParticleSystem.isPlaying && (normalNoiseStrength > 0 || normalEmissionRate > 0))
        {
            mainHiveSwarmParticleSystem.Play();
        }

        // --- AudioSource Setup ---
        if (hiveSwarmAudioSource == null)
        {
            hiveSwarmAudioSource = GetComponent<AudioSource>();
            if (hiveSwarmAudioSource == null)
            {
                Debug.LogWarning("HiveActivityManager: Hive Swarm AudioSource not assigned and not found on this GameObject. Adding one.", this);
                hiveSwarmAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (hiveBuzzLoopClip != null)
        {
            hiveSwarmAudioSource.clip = hiveBuzzLoopClip;
            hiveSwarmAudioSource.loop = true;
            hiveSwarmAudioSource.playOnAwake = true; // Start playing the ambient buzz
            SetMainSwarmAudioState(normalVolume, normalPitch);
            if (!hiveSwarmAudioSource.isPlaying) // Ensure it plays if set to playOnAwake but somehow isn't
            {
                hiveSwarmAudioSource.Play();
            }
        }
        else
        {
            Debug.LogWarning("HiveActivityManager: 'Hive Buzz Loop Clip' not assigned. Main swarm audio will not play.", this);
        }
    }

    public void OnHiveTopGrabbed()
    {
        if (mainHiveSwarmParticleSystem == null && hiveSwarmAudioSource == null) return;

        Debug.Log("Hive top grabbed! Agitating main hive swarm.");
        _isCurrentlyAgitated = true;

        SetMainSwarmParticleState(agitatedNoiseStrength, agitatedEmissionRate);
        SetMainSwarmAudioState(agitatedVolume, agitatedPitch);

        if (_agitationCoroutine != null)
        {
            StopCoroutine(_agitationCoroutine);
        }
        _agitationCoroutine = StartCoroutine(RevertSwarmAgitationCoroutine());
    }

    private void SetMainSwarmParticleState(float noiseStrength, float emissionRate)
    {
        if (mainHiveSwarmParticleSystem == null) return;

        var noiseModule = mainHiveSwarmParticleSystem.noise;
        noiseModule.strength = new ParticleSystem.MinMaxCurve(noiseStrength);

        var emissionModule = mainHiveSwarmParticleSystem.emission;
        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(emissionRate);
    }

    private void SetMainSwarmAudioState(float volume, float pitch)
    {
        if (hiveSwarmAudioSource != null && hiveSwarmAudioSource.clip != null)
        {
            hiveSwarmAudioSource.volume = volume;
            hiveSwarmAudioSource.pitch = pitch;
        }
    }

    private IEnumerator RevertSwarmAgitationCoroutine()
    {
        yield return new WaitForSeconds(agitationDuration);

        Debug.Log("Main hive swarm agitation duration ended. Reverting to normal state.");
        _isCurrentlyAgitated = false;
        SetMainSwarmParticleState(normalNoiseStrength, normalEmissionRate);
        SetMainSwarmAudioState(normalVolume, normalPitch);
        _agitationCoroutine = null;
    }
}
