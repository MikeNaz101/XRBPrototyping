// File: FrameDisturbanceSwarm.cs
// Purpose: Activates a particle swarm and accompanying audio when its parent frame is moved quickly.
// The swarm detaches, plays, then returns and re-parents.
// Instructions:
// 1. Create a Particle System GameObject as a child of each of your grabbable hive frame GameObjects.
//    Configure this particle system to look like a small, brief, agitated swarm.
//    Ensure its "Looping" and "Play On Awake" in the Particle System's main module are OFF.
// 2. Attach this script to that Particle System GameObject.
// 3. Add an AudioSource component to this same Particle System GameObject.
//    - Assign a bee buzzing AudioClip to the AudioSource's "AudioClip" slot.
//    - Ensure "Play On Awake" on the AudioSource is OFF.
//    - "Loop" can be ON if your audio clip is a short, loopable buzz.
// 4. Assign the 'Parent Frame Transform' in the Inspector (should be its direct parent frame,
//    though the script will try to find it if not assigned).
// 5. Adjust activation thresholds, duration, audio parameters, and return delay.

using UnityEngine;
using System.Collections;

public class FrameDisturbanceSwarm : MonoBehaviour
{
    [Header("Frame Link")]
    [Tooltip("The Transform of the parent hive frame this swarm belongs to.")]
    public Transform parentFrameTransform;

    [Header("Activation Triggers")]
    [Tooltip("Speed (units/sec) the parent frame must exceed to activate the swarm.")]
    public float activationSpeedThreshold = 0.8f;

    [Header("Swarm Behavior")]
    [Tooltip("How long the particle system emits after being activated (seconds).")]
    public float swarmEmissionDuration = 1.5f;
    [Tooltip("Additional delay after swarm emission stops before returning to parent (allows existing particles to clear).")]
    public float visualClearDelay = 2.0f;

    [Header("Audio Settings")]
    [Tooltip("AudioClip for the bee buzzing sound.")]
    public AudioClip buzzAudioClip; // Assign this in the Inspector
    [Tooltip("Volume of the buzzing sound during agitation.")]
    [Range(0f, 1f)]
    public float agitatedVolume = 0.8f;
    [Tooltip("Pitch of the buzzing sound during agitation (1 is normal).")]
    [Range(0.5f, 2f)]
    public float agitatedPitch = 1.2f;
    [Tooltip("Time in seconds to fade out audio before re-parenting.")]
    public float audioFadeOutDuration = 0.5f;


    private ParticleSystem _particleSystemToControl;
    private AudioSource _audioSource;
    private Vector3 _lastFramePosition = Vector3.zero;
    private Quaternion _originalLocalRotation;
    private Vector3 _originalLocalPosition;
    private bool _isSwarmActive = false;
    private Coroutine _swarmLifecycleCoroutine;

    void Start()
    {
        _particleSystemToControl = GetComponent<ParticleSystem>();
        if (_particleSystemToControl == null)
        {
            Debug.LogError("FrameDisturbanceSwarm: No ParticleSystem component found on this GameObject!", this);
            enabled = false;
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            Debug.LogWarning("FrameDisturbanceSwarm: No AudioSource component found. Adding one.", this);
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (buzzAudioClip != null)
        {
            _audioSource.clip = buzzAudioClip;
        }
        else
        {
            Debug.LogWarning("FrameDisturbanceSwarm: 'Buzz Audio Clip' not assigned. No sound will play.", this);
        }
        _audioSource.playOnAwake = false;
        _audioSource.loop = true; // Assuming a loopable buzz sound is best for continuous agitation

        if (parentFrameTransform == null)
        {
            if (transform.parent != null)
            {
                parentFrameTransform = transform.parent;
            }
            else
            {
                Debug.LogError("FrameDisturbanceSwarm: 'Parent Frame Transform' is not assigned and no parent found! This script requires a parent frame.", this);
                enabled = false;
                return;
            }
        }

        if (transform.parent != parentFrameTransform)
        {
            transform.SetParent(parentFrameTransform);
        }
        _originalLocalPosition = transform.localPosition;
        _originalLocalRotation = transform.localRotation;

        if (parentFrameTransform != null)
        {
            _lastFramePosition = parentFrameTransform.position;
        }

        _particleSystemToControl.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _audioSource.Stop();
        _isSwarmActive = false;
    }

    void Update()
    {
        if (_isSwarmActive || parentFrameTransform == null)
        {
            return;
        }

        if (Time.deltaTime > 0)
        {
            Vector3 currentFrameVelocity = (parentFrameTransform.position - _lastFramePosition) / Time.deltaTime;
            float currentFrameSpeed = currentFrameVelocity.magnitude;

            if (currentFrameSpeed > activationSpeedThreshold)
            {
                ActivateSwarm();
            }
        }
        _lastFramePosition = parentFrameTransform.position;
    }

    public void ActivateSwarm()
    {
        if (_isSwarmActive || _particleSystemToControl == null)
        {
            return;
        }
        _isSwarmActive = true;
        transform.SetParent(null, true);

        _particleSystemToControl.Play();

        if (_audioSource != null && _audioSource.clip != null)
        {
            _audioSource.volume = agitatedVolume;
            _audioSource.pitch = agitatedPitch;
            _audioSource.Play();
            // Debug.Log($"Swarm {gameObject.name} audio started at volume {agitatedVolume}, pitch {agitatedPitch}.");
        }
        // Debug.Log($"Swarm {gameObject.name} activated and detached.");

        if (_swarmLifecycleCoroutine != null)
        {
            StopCoroutine(_swarmLifecycleCoroutine);
        }
        _swarmLifecycleCoroutine = StartCoroutine(SwarmLifecycleCoroutine());
    }

    private IEnumerator SwarmLifecycleCoroutine()
    {
        yield return new WaitForSeconds(swarmEmissionDuration);

        if (_particleSystemToControl != null)
        {
            _particleSystemToControl.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            // Debug.Log($"Swarm {gameObject.name} particle emission stopped.");
        }

        // Start fading out audio if it's playing
        if (_audioSource != null && _audioSource.isPlaying && audioFadeOutDuration > 0)
        {
            float startVolume = _audioSource.volume;
            float fadeTimer = 0f;
            while (fadeTimer < audioFadeOutDuration)
            {
                fadeTimer += Time.deltaTime;
                _audioSource.volume = Mathf.Lerp(startVolume, 0f, fadeTimer / audioFadeOutDuration);
                yield return null;
            }
            _audioSource.Stop();
            _audioSource.volume = 0; // Ensure it's fully silent
            // Debug.Log($"Swarm {gameObject.name} audio faded out and stopped.");
        }
        else if (_audioSource != null)
        {
            _audioSource.Stop(); // Stop immediately if no fade duration
        }

        // Wait for particles to visually clear out. This delay now also covers audio fade.
        // If audioFadeOutDuration is longer than visualClearDelay, the longer one dictates.
        // For simplicity, we'll just use visualClearDelay after audio fade has started.
        // A more precise timing would choose the max of the two.
        // For now, ensure visualClearDelay is long enough for both.
        yield return new WaitForSeconds(Mathf.Max(0, visualClearDelay - audioFadeOutDuration)); // Wait remaining time if any
        // Debug.Log($"Swarm {gameObject.name} visual clear delay ended.");

        if (parentFrameTransform != null)
        {
            transform.SetParent(parentFrameTransform);
            transform.localPosition = _originalLocalPosition;
            transform.localRotation = _originalLocalRotation;
            // Debug.Log($"Swarm {gameObject.name} re-parented and reset to {parentFrameTransform.name}.");
        }
        else
        {
            Debug.LogWarning("FrameDisturbanceSwarm: Parent frame lost, cannot re-parent. Disabling swarm.", this);
            gameObject.SetActive(false);
        }

        _isSwarmActive = false;
        _swarmLifecycleCoroutine = null;
    }
}
