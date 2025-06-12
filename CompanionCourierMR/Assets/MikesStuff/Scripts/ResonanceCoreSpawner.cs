// Script 3: ResonanceCoreSpawner.cs
// This script finds a valid horizontal surface and spawns a "Resonance Core" on it.
// DEBUG VERSION: Added extensive logging to diagnose spawning issues.
// FIX: Moved MRUK subscription from OnEnable to Start to fix execution order issues.

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;

public class ResonanceCoreSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("The prefab for the Resonance Core object to be spawned.")]
    public GameObject resonanceCorePrefab;

    [Tooltip("A small vertical offset to prevent the spawned object from clipping into the floor or table surface.")]
    public float spawnHeightOffset = 0.05f;

    // --- Private Variables ---
    private bool hasSpawned = false;

    void Start()
    {
        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Start called. Checking for prefab and MRUK instance.");

        if (resonanceCorePrefab == null)
        {
            Debug.LogError($"[{nameof(ResonanceCoreSpawner)}] Resonance Core Prefab has not been assigned in the Inspector. Disabling script.", this);
            enabled = false;
            return;
        }

        // FIX: The logic from OnEnable is moved here.
        if (MRUK.Instance == null)
        {
            Debug.LogError($"[{nameof(ResonanceCoreSpawner)}] MRUK.Instance is null in Start(). Make sure an MRUK object is active in your scene. Disabling script.", this);
            enabled = false;
            return;
        }

        // Subscribe to the event.
        MRUK.Instance.SceneLoadedEvent.AddListener(SpawnResonanceCore);
        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Successfully subscribed to SceneLoadedEvent.");

        // If the scene is already loaded when this script starts, spawn immediately.
        if (MRUK.Instance.IsInitialized)
        {
            Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Scene already initialized. Calling SpawnResonanceCore() from Start.");
            SpawnResonanceCore();
        }
    }
    
    private void OnDisable()
    {
        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] OnDisable called. Unsubscribing from SceneLoadedEvent.");
        if (MRUK.Instance)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(SpawnResonanceCore);
        }
    }
    
    void SpawnResonanceCore()
    {
        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] SpawnResonanceCore() method entered.");
        if (hasSpawned)
        {
            Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Aborting spawn: Resonance Core has already been spawned.");
            return;
        }

        var mrukRoom = MRUK.Instance.GetCurrentRoom();
        if (mrukRoom == null)
        {
            Debug.LogError($"[{nameof(ResonanceCoreSpawner)}] Aborting spawn: MRUK Room object not found.", this);
            return;
        }
        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Found MRUK Room: {mrukRoom.name}");

        List<string> validLabels = new List<string> { MRUKAnchor.SceneLabels.TABLE.ToString(), MRUKAnchor.SceneLabels.FLOOR.ToString() };
        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Searching for surfaces with labels: {string.Join(", ", validLabels)}");

        List<MRUKAnchor> validSurfaces = mrukRoom.Anchors
            .Where(anchor => validLabels.Any(label => anchor.HasLabel(label)))
            .ToList();

        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Found {validSurfaces.Count} valid surfaces to spawn on.");
        if (validSurfaces.Count == 0)
        {
            Debug.LogWarning($"[{nameof(ResonanceCoreSpawner)}] Aborting spawn: No valid surfaces with TABLE or FLOOR labels found. Check your room scan.", this);
            return;
        }

        MRUKAnchor chosenSurface = validSurfaces[Random.Range(0, validSurfaces.Count)];
        Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Chose surface '{chosenSurface.name}'.");

        if (!chosenSurface.PlaneRect.HasValue)
        {
            Debug.LogError($"[{nameof(ResonanceCoreSpawner)}] Aborting spawn: Chosen surface '{chosenSurface.name}' has no PlaneRect value.", chosenSurface);
            return;
        }
        Rect planeRect = chosenSurface.PlaneRect.Value;
        if (planeRect.width == 0 || planeRect.height == 0)
        {
            Debug.LogError($"[{nameof(ResonanceCoreSpawner)}] Aborting spawn: Chosen surface '{chosenSurface.name}' has a zero-sized PlaneRect.", chosenSurface);
            return;
        }

        float randomX = Random.Range(-planeRect.width / 2, planeRect.width / 2);
        float randomY = Random.Range(-planeRect.height / 2, planeRect.height / 2);

        Vector3 spawnPositionOnPlane = chosenSurface.transform.position +
                                       (chosenSurface.transform.right * randomX) +
                                       (chosenSurface.transform.up * randomY);
        Vector3 finalSpawnPosition = spawnPositionOnPlane + (chosenSurface.transform.forward * spawnHeightOffset);

        if (resonanceCorePrefab)
        {
            Instantiate(resonanceCorePrefab, finalSpawnPosition, Quaternion.identity);
            Debug.Log($"[{nameof(ResonanceCoreSpawner)}] Successfully spawned Resonance Core at {finalSpawnPosition} on surface '{chosenSurface.name}'.");
            hasSpawned = true;
        }
    }
}
