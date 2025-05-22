// File: FrameAgitationSpawner.cs
// Purpose: Monitors the speed of the frame it's attached to. If the speed exceeds a threshold,
// it instantiates a temporary disturbance swarm prefab at the frame's current position.
// Ensures only one such temporary swarm is active globally.
// Instructions:
// 1. Attach this script to each of your grabbable hive frame GameObjects.
// 2. Create a prefab for your "disturbance swarm" effect. This prefab should have:
//    a. A ParticleSystem component, configured to play automatically (Play On Awake = true, Looping = false).
//    b. Optionally, an AudioSource component with a sound effect (Play On Awake = true, Loop = false).
//    c. The TemporaryDisturbanceSwarm.cs script attached to it.
// 3. In the Inspector for each frame GameObject (with this FrameAgitationSpawner.cs script):
//    a. Drag your "disturbance swarm" prefab into the 'Disturbance Swarm Prefab' slot.
//    b. Adjust 'Activation Speed Threshold' and 'Swarm Lifetime'.

using UnityEngine;

public class FrameAgitationSpawner : MonoBehaviour
{
    [Header("Swarm Settings")]
    [Tooltip("The prefab of the particle system (with TemporaryDisturbanceSwarm script) to instantiate.")]
    public GameObject disturbanceSwarmPrefab;

    [Tooltip("Speed (units/sec) this frame must exceed to activate the swarm.")]
    public float activationSpeedThreshold = 0.8f;

    [Tooltip("How long the instantiated swarm effect should last before destroying itself (seconds).")]
    public float swarmLifetime = 2.5f;

    // --- Internal Variables ---
    private Vector3 _lastPosition;
    private bool _isGrabbed = false; // To only check speed while grabbed

    // --- Static variable to track the globally active temporary swarm ---
    private static GameObject _activeInstantiatedSwarm;
    private static FrameAgitationSpawner _spawnerOfActiveSwarm;


    void Start()
    {
        if (disturbanceSwarmPrefab == null)
        {
            Debug.LogError($"FrameAgitationSpawner on {gameObject.name}: Disturbance Swarm Prefab is not assigned!", this);
            enabled = false;
            return;
        }
        // Attempt to get an interactable component to track grab state (works with the SnapToTargetOnRelease script's public methods)
        // If you are using a different interaction system, you'll need to adapt how _isGrabbed is set.
        SnapToTargetOnRelease snappable = GetComponent<SnapToTargetOnRelease>();
        if (snappable == null)
        {
            Debug.LogWarning($"FrameAgitationSpawner on {gameObject.name}: SnapToTargetOnRelease script not found. Speed checking will always be active. Consider adding it or integrating grab state detection.", this);
            _isGrabbed = true; // Assume always active for speed check if no grab detection
        }


        _lastPosition = transform.position;
    }

    // These methods would be called by your "Interactable Unity Event Wrapper"
    // or equivalent from your Meta Grabbable Building Block.
    public void OnFrameGrabbed()
    {
        _isGrabbed = true;
        _lastPosition = transform.position; // Reset last position on grab
    }

    public void OnFrameReleased()
    {
        _isGrabbed = false;
    }


    void Update()
    {
        if (!_isGrabbed || disturbanceSwarmPrefab == null) // Only check speed if grabbed
        {
            // If not grabbed, ensure _lastPosition is updated so speed isn't miscalculated on next grab
            _lastPosition = transform.position;
            return;
        }

        if (Time.deltaTime > 0)
        {
            Vector3 currentVelocity = (transform.position - _lastPosition) / Time.deltaTime;
            float currentSpeed = currentVelocity.magnitude;

            if (currentSpeed > activationSpeedThreshold)
            {
                TrySpawnDisturbanceSwarm();
            }
        }
        _lastPosition = transform.position;
    }

    private void TrySpawnDisturbanceSwarm()
    {
        // If there's already an active swarm from another frame or this one, destroy it first.
        if (_activeInstantiatedSwarm != null)
        {
            // Optional: If the active swarm was spawned by *this* frame recently,
            // you might choose not to spawn a new one immediately.
            // For now, we'll just replace it.
            Destroy(_activeInstantiatedSwarm);
            _activeInstantiatedSwarm = null;
            if (_spawnerOfActiveSwarm != null) _spawnerOfActiveSwarm = null;
        }

        // Instantiate the new swarm at the current frame's position and rotation
        GameObject swarmInstance = Instantiate(disturbanceSwarmPrefab, transform.position, transform.rotation);
        _activeInstantiatedSwarm = swarmInstance;
        _spawnerOfActiveSwarm = this;


        // The TemporaryDisturbanceSwarm script on the prefab will handle its own lifetime.
        // If you need to pass the lifetime from here:
        TemporaryDisturbanceSwarm tempSwarmScript = swarmInstance.GetComponent<TemporaryDisturbanceSwarm>();
        if (tempSwarmScript != null)
        {
            tempSwarmScript.Initialize(swarmLifetime);
        }
        else
        {
            Debug.LogWarning($"FrameAgitationSpawner: Instantiated swarm prefab '{disturbanceSwarmPrefab.name}' is missing the TemporaryDisturbanceSwarm script. It will not self-destruct as intended by this spawner.", swarmInstance);
            // Fallback destroy if script is missing
            Destroy(swarmInstance, swarmLifetime);
        }
        // Debug.Log($"Spawned disturbance swarm from frame {gameObject.name}");
    }

    void OnDestroy()
    {
        // If this frame is destroyed and it was the one that spawned the currently active swarm,
        // clear the static reference so another frame can spawn one.
        // The swarm itself will self-destruct via TemporaryDisturbanceSwarm.
        if (_spawnerOfActiveSwarm == this && _activeInstantiatedSwarm != null)
        {
            // Don't destroy _activeInstantiatedSwarm here, let its own script handle it.
            // Just clear the static references.
            _activeInstantiatedSwarm = null;
            _spawnerOfActiveSwarm = null;
        }
    }
}