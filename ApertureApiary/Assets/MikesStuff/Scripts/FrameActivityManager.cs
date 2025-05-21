// File: FrameActivityManager.cs
// Purpose: Manages the bee activity (particle emission rates and noise agitation on grab)
// on individual frames within a beehive (front and back sides), based on overall hive health and frame position.
// Instructions:
// 1. Create an empty GameObject in your scene (e.g., "FrameActivityController").
// 2. Attach this script to it.
// 3. For each frame in your hive:
//    a. Ensure the frame GameObject itself is interactable (e.g., has an XR Grab Interactable and Collider).
//    b. Create two Particle System GameObjects (one for front activity, one for back).
//       Position these particle systems appropriately on or near each side of the frame.
//       Optionally, make them children of the frame GameObject.
// 4. In the Inspector for "FrameActivityController":
//    a. Populate the 'Frame Particle Pairs' list. For each element (representing one frame):
//       - Assign the 'Frame Game Object' (the actual grabbable frame GameObject).
//       - Assign the 'Front Side Particles' (Particle System for the front of that frame).
//       - Assign the 'Back Side Particles' (Particle System for the back of that frame).
//       - Ensure the order of these pairs in the list matches the physical order of frames in the hive.
//    b. Adjust 'Hive Health', 'Max Emission Per Healthy Frame Side', 'Activity Spread Factor',
//       'Frame Agitated Noise Strength', and 'Frame Agitation Duration'.
// 5. For each grabbable frame GameObject:
//    a. From its XR Interactable component's "On Select Entered" (or equivalent "On Grabbed") event,
//       call the 'OnFrameGrabbed(GameObject)' method of this FrameActivityManager script,
//       passing the frame GameObject itself as the argument. (You might need an intermediate event relay if the
//       UnityEvent on the interactable doesn't directly support passing a GameObject argument from itself,
//       or use a simple script on the frame to call the manager).

using UnityEngine;
using System.Collections.Generic; // Required for using Lists
using System.Collections; // Required for Coroutines

// Helper class to store front and back particle systems and related data for a single frame
[System.Serializable] // Makes this class show up in the Inspector
public class FrameParticlePair
{
    [Tooltip("The actual grabbable Frame GameObject in the scene.")]
    public GameObject frameGameObject; // Reference to the parent frame

    [Tooltip("Particle System for the front side of this frame.")]
    public ParticleSystem frontSideParticles;
    [Tooltip("Particle System for the back side of this frame.")]
    public ParticleSystem backSideParticles;

    // Internal storage for original noise and agitation state
    [HideInInspector] public float originalFrontNoiseStrengthConstant; // Changed name for clarity
    [HideInInspector] public float originalBackNoiseStrengthConstant;  // Changed name for clarity
    [HideInInspector] public Coroutine agitationCoroutineReference;
}

public class FrameActivityManager : MonoBehaviour
{
    [Header("Frame Particle Systems")]
    [Tooltip("Assign Particle Systems for the front and back of each frame, in their physical order in the hive.")]
    public List<FrameParticlePair> frameParticlePairs;

    [Header("Hive Health & Emission Control")]
    [Tooltip("Overall health of the hive (0.0 = very weak, 1.0 = very strong).")]
    [Range(0f, 1f)]
    public float hiveHealth = 0.8f;

    [Tooltip("Maximum emission rate for the most active side of the most active frame if the hive is at 100% health.")]
    public float maxEmissionPerHealthyFrameSide = 50f;

    [Tooltip("Controls how spread out the activity is. " +
             "0 = very focused on center; 1 = activity spreads further to outer frames (especially in healthy hives).")]
    [Range(0.01f, 1f)]
    public float activitySpreadFactor = 0.6f;

    [Header("Frame Agitation On Grab")]
    [Tooltip("Noise strength (constant value) for a frame's particles when it's first grabbed.")]
    public float frameAgitatedNoiseStrength = 2.0f;
    [Tooltip("Duration in seconds for the frame's agitated noise state after being grabbed.")]
    public float frameAgitationDuration = 3.0f;


    void Start()
    {
        if (frameParticlePairs == null || frameParticlePairs.Count == 0)
        {
            Debug.LogError("FrameActivityManager: No frame particle pairs assigned!", this);
            enabled = false;
            return;
        }

        InitializeParticleSystems();
        UpdateFrameEmissions();
    }

    void InitializeParticleSystems()
    {
        foreach (FrameParticlePair pair in frameParticlePairs)
        {
            if (pair == null)
            {
                Debug.LogWarning("FrameActivityManager: A null FrameParticlePair was found in the list.", this);
                continue;
            }
            if (pair.frameGameObject == null)
            {
                Debug.LogError("FrameActivityManager: A FrameParticlePair is missing its 'Frame Game Object' assignment!", this);
            }

            if (pair.frontSideParticles != null)
            {
                var noiseModule = pair.frontSideParticles.noise;
                pair.originalFrontNoiseStrengthConstant = noiseModule.strength.constant; // FIX: Read .constant
            }
            else
            {
                Debug.LogWarning($"FrameActivityManager: Frame '{pair.frameGameObject?.name ?? "UNASSIGNED"}' is missing its Front Side Particle System.", this);
            }

            if (pair.backSideParticles != null)
            {
                var noiseModule = pair.backSideParticles.noise;
                pair.originalBackNoiseStrengthConstant = noiseModule.strength.constant; // FIX: Read .constant
            }
            else
            {
                 Debug.LogWarning($"FrameActivityManager: Frame '{pair.frameGameObject?.name ?? "UNASSIGNED"}' is missing its Back Side Particle System.", this);
            }
        }
    }

    public void UpdateFrameEmissions()
    {
        if (frameParticlePairs == null || frameParticlePairs.Count == 0) return;

        int numberOfFrames = frameParticlePairs.Count;
        float centerFrameTrueIndex = (numberOfFrames - 1) / 2.0f;
        float peakEmissionForThisHiveSide = maxEmissionPerHealthyFrameSide * hiveHealth;

        for (int i = 0; i < numberOfFrames; i++)
        {
            FrameParticlePair currentPair = frameParticlePairs[i];
            if (currentPair == null) continue;

            float distanceFromCenter = Mathf.Abs(i - centerFrameTrueIndex);
            float normalizedDistance = (numberOfFrames <= 1) ? 0 : distanceFromCenter / (numberOfFrames / 2.0f);
            float falloffExponent = Mathf.Max(1.0f, 2.0f - (activitySpreadFactor * hiveHealth));
            float emissionMultiplier = Mathf.Clamp01(1.0f - Mathf.Pow(normalizedDistance, falloffExponent));
            float currentFrameSideEmissionRate = peakEmissionForThisHiveSide * emissionMultiplier;

            ApplyEmissionRateToParticleSystem(currentPair.frontSideParticles, currentFrameSideEmissionRate);
            ApplyEmissionRateToParticleSystem(currentPair.backSideParticles, currentFrameSideEmissionRate);
        }
    }

    private void ApplyEmissionRateToParticleSystem(ParticleSystem ps, float emissionRate)
    {
        if (ps == null) return;
        var emissionModule = ps.emission;
        emissionModule.rateOverTime = new ParticleSystem.MinMaxCurve(emissionRate);
        if (emissionRate > 0 && !ps.isPlaying) ps.Play();
        else if (emissionRate <= 0 && ps.isPlaying) ps.Stop();
    }

    public void OnFrameGrabbed(GameObject grabbedFrameObject)
    {
        if (grabbedFrameObject == null) return;

        foreach (FrameParticlePair pair in frameParticlePairs)
        {
            if (pair != null && pair.frameGameObject == grabbedFrameObject)
            {
                if (pair.agitationCoroutineReference != null)
                {
                    StopCoroutine(pair.agitationCoroutineReference);
                }
                pair.agitationCoroutineReference = StartCoroutine(AgitateFrameParticlesCoroutine(pair));
                return;
            }
        }
    }

    private IEnumerator AgitateFrameParticlesCoroutine(FrameParticlePair pair)
    {
        if (pair.frontSideParticles != null)
        {
            var noiseModule = pair.frontSideParticles.noise;
            noiseModule.strength = new ParticleSystem.MinMaxCurve(frameAgitatedNoiseStrength); // FIX: Assign new MinMaxCurve
        }

        if (pair.backSideParticles != null)
        {
            var noiseModule = pair.backSideParticles.noise;
            noiseModule.strength = new ParticleSystem.MinMaxCurve(frameAgitatedNoiseStrength); // FIX: Assign new MinMaxCurve
        }

        yield return new WaitForSeconds(frameAgitationDuration);

        if (pair.frontSideParticles != null)
        {
            var noiseModule = pair.frontSideParticles.noise;
            noiseModule.strength = new ParticleSystem.MinMaxCurve(pair.originalFrontNoiseStrengthConstant); // FIX: Assign new MinMaxCurve
        }

        if (pair.backSideParticles != null)
        {
            var noiseModule = pair.backSideParticles.noise;
            noiseModule.strength = new ParticleSystem.MinMaxCurve(pair.originalBackNoiseStrengthConstant); // FIX: Assign new MinMaxCurve
        }
        pair.agitationCoroutineReference = null;
    }


    void OnValidate()
    {
        if (maxEmissionPerHealthyFrameSide < 0) maxEmissionPerHealthyFrameSide = 0;
        if (frameAgitatedNoiseStrength < 0) frameAgitatedNoiseStrength = 0;
        if (frameAgitationDuration < 0) frameAgitationDuration = 0;


        if (Application.isPlaying && frameParticlePairs != null && frameParticlePairs.Count > 0)
        {
            // InitializeParticleSystems(); // Consider if this is truly needed on validate during play
            UpdateFrameEmissions();
        }
    }

    public void SetHiveHealth(float newHealth)
    {
        hiveHealth = Mathf.Clamp01(newHealth);
        UpdateFrameEmissions();
    }

    public void SetActivitySpread(float newSpread)
    {
        activitySpreadFactor = Mathf.Clamp(newSpread, 0.01f, 1f);
        UpdateFrameEmissions();
    }
}
