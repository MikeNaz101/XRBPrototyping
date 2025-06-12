// Script 1: LaserEmitterSpawner.cs
// This script finds valid surfaces (walls, ceilings) and spawns laser prefabs on them.
// DEBUG VERSION: Added extensive logging to diagnose spawning issues.
// FIX: Moved MRUK subscription from OnEnable to Start to fix execution order issues.

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;

public class LaserEmitterSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("The laser prefab to spawn. This prefab should have the LaserLine and LaserBeamController scripts attached.")]
    public GameObject laserEmitterPrefab;

    [Tooltip("The total number of lasers to spawn in the room.")]
    public int numberOfLasers = 5;

    // An enum to let you choose which surfaces lasers can spawn on.
    [System.Flags]
    public enum SpawnSurface
    {
        Walls = 1 << 0,
        Ceiling = 1 << 1,
    }

    [Tooltip("Select the surface types where lasers are allowed to spawn.")]
    public SpawnSurface spawnOn = SpawnSurface.Walls | SpawnSurface.Ceiling;

    // --- Private Variables ---
    private bool hasSpawned = false;

    void Start()
    {
        Debug.Log($"[{nameof(LaserEmitterSpawner)}] Start called. Checking for prefab and MRUK instance.");

        // FIX: The logic from OnEnable is moved here. The Start() method is guaranteed to run after all Awake() methods
        // have completed, which ensures that MRUK.Instance will not be null if an MRUK object exists in the scene.
        if (MRUK.Instance == null)
        {
            Debug.LogError($"[{nameof(LaserEmitterSpawner)}] MRUK.Instance is null in Start(). Make sure an MRUK object is active in your scene. Disabling script.", this);
            enabled = false;
            return;
        }

        // Subscribe to the event that fires when the scene is loaded.
        MRUK.Instance.SceneLoadedEvent.AddListener(SpawnLasers);
        Debug.Log($"[{nameof(LaserEmitterSpawner)}] Successfully subscribed to SceneLoadedEvent.");


        // If the scene is already loaded when this script starts, spawn the lasers immediately.
        if (MRUK.Instance.IsInitialized)
        {
            Debug.Log($"[{nameof(LaserEmitterSpawner)}] Scene already initialized. Calling SpawnLasers() from Start.");
            SpawnLasers();
        }
    }

    private void OnDisable()
    {
        Debug.Log($"[{nameof(LaserEmitterSpawner)}] OnDisable called. Unsubscribing from SceneLoadedEvent.");
        // Always unsubscribe from events to prevent errors. A null check is important here
        // in case the MRUK instance is destroyed before this object.
        if (MRUK.Instance)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(SpawnLasers);
        }
    }

    /// <summary>
    /// Finds all valid surfaces and spawns the configured number of lasers.
    /// </summary>
    void SpawnLasers()
    {
        Debug.Log($"[{nameof(LaserEmitterSpawner)}] SpawnLasers() method entered.");

        if (hasSpawned)
        {
            Debug.Log($"[{nameof(LaserEmitterSpawner)}] Aborting spawn: Lasers have already been spawned.");
            return;
        }
        if (!laserEmitterPrefab)
        {
            Debug.LogError($"[{nameof(LaserEmitterSpawner)}] Aborting spawn: Laser Emitter Prefab is not assigned. Cannot spawn lasers.", this);
            return;
        }

        var mrukRoom = MRUK.Instance.GetCurrentRoom();
        if (mrukRoom == null)
        {
            Debug.LogError($"[{nameof(LaserEmitterSpawner)}] Aborting spawn: MRUK Room object not found. Cannot determine spawn locations.", this);
            return;
        }
        Debug.Log($"[{nameof(LaserEmitterSpawner)}] Found MRUK Room: {mrukRoom.name}");

        // Build a list of valid labels based on the Inspector selection.
        List<string> validLabels = new List<string>();
        if (spawnOn.HasFlag(SpawnSurface.Walls))
        {
            validLabels.Add(MRUKAnchor.SceneLabels.WALL_FACE.ToString());
        }
        if (spawnOn.HasFlag(SpawnSurface.Ceiling))
        {
            validLabels.Add(MRUKAnchor.SceneLabels.CEILING.ToString());
        }
        
        if (validLabels.Count == 0)
        {
            Debug.LogWarning($"[{nameof(LaserEmitterSpawner)}] Aborting spawn: No spawn surfaces (Walls/Ceiling) selected in the inspector.", this);
            return;
        }
        Debug.Log($"[{nameof(LaserEmitterSpawner)}] Searching for surfaces with labels: {string.Join(", ", validLabels)}");

        // Filter all room anchors to get only the ones with the labels we want.
        List<MRUKAnchor> validSurfaces = mrukRoom.Anchors
            .Where(anchor => validLabels.Any(label => anchor.HasLabel(label)))
            .ToList();

        Debug.Log($"[{nameof(LaserEmitterSpawner)}] Found {validSurfaces.Count} valid surfaces to spawn on.");
        if (validSurfaces.Count == 0)
        {
            Debug.LogWarning($"[{nameof(LaserEmitterSpawner)}] Aborting spawn: No valid surfaces with the specified labels found to spawn lasers on. Check if your room scan has walls/ceilings.", this);
            return;
        }

        // Loop to spawn the desired number of lasers
        Debug.Log($"[{nameof(LaserEmitterSpawner)}] Attempting to spawn {numberOfLasers} lasers.");
        for (int i = 0; i < numberOfLasers; i++)
        {
            MRUKAnchor chosenSurface = validSurfaces[Random.Range(0, validSurfaces.Count)];
            Debug.Log($"[{nameof(LaserEmitterSpawner)}] Loop {i+1}: Chose surface '{chosenSurface.name}'.");

            if (!chosenSurface.PlaneRect.HasValue) 
            {
                Debug.LogWarning($"[{nameof(LaserEmitterSpawner)}] Loop {i+1}: Chosen surface '{chosenSurface.name}' has no PlaneRect. Skipping.");
                continue;
            }

            Rect planeRect = chosenSurface.PlaneRect.Value;
            if (planeRect.width == 0 || planeRect.height == 0) 
            {
                Debug.LogWarning($"[{nameof(LaserEmitterSpawner)}] Loop {i+1}: Chosen surface '{chosenSurface.name}' has a zero-sized PlaneRect. Skipping.");
                continue;
            }
            
            float randomX = Random.Range(-planeRect.width / 2, planeRect.width / 2);
            float randomY = Random.Range(-planeRect.height / 2, planeRect.height / 2);

            Vector3 spawnPositionOnPlane = chosenSurface.transform.position +
                                           (chosenSurface.transform.right * randomX) +
                                           (chosenSurface.transform.up * randomY);
            
            // The laser's forward direction should be the same as the surface normal, which points into the room.
            Quaternion spawnRotation = Quaternion.LookRotation(chosenSurface.transform.forward);
            
            Vector3 finalSpawnPosition = spawnPositionOnPlane + chosenSurface.transform.forward * 0.01f;

            Instantiate(laserEmitterPrefab, finalSpawnPosition, spawnRotation);
            Debug.Log($"[{nameof(LaserEmitterSpawner)}] Loop {i+1}: Spawned laser at {finalSpawnPosition} on surface '{chosenSurface.name}'.");
        }

        Debug.Log($"[{nameof(LaserEmitterSpawner)}] Finished spawning loop.");
        hasSpawned = true;
    }
}