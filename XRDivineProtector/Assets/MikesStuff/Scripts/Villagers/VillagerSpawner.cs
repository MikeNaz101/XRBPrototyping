// VillagerSpawner.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;

public class VillagerSpawner : MonoBehaviour
{
    [Header("Initial House Setup")]
    [Tooltip("The House prefab to spawn at the start. It must have a HouseController script.")]
    public GameObject initialHousePrefab;

    private FloorHexGridGenerator_V2 _hexGridGenerator;
    private bool _hasSpawned = false;

    void Start()
    {
        _hexGridGenerator = FindObjectOfType<FloorHexGridGenerator_V2>();
        if (_hexGridGenerator == null)
        {
            Debug.LogError("VillagerSpawner could not find a FloorHexGridGenerator_V2 in the scene.");
            enabled = false;
        }
        Invoke(nameof(SpawnInitialHouse), 2.5f);
    }

    void SpawnInitialHouse()
    {
        if (_hasSpawned || _hexGridGenerator == null || initialHousePrefab == null) return;

        List<HexCell> allHexCells = _hexGridGenerator.GetAllHexCells();
        if (allHexCells == null || allHexCells.Count == 0)
        {
            Debug.LogWarning("VillagerSpawner: No hex cells found to place the initial house on.");
            return;
        }

        // Find the hex cell closest to the center of the room
        Vector3 roomCenter = MRUK.Instance.GetCurrentRoom().GetRoomBounds().center;
        HexCell centerHex = allHexCells
            .OrderBy(cell => Vector3.Distance(cell.WorldCenter, roomCenter))
            .FirstOrDefault();

        if (centerHex == null)
        {
            Debug.LogError("Could not find a central hex to spawn the house.");
            return;
        }

        Debug.Log($"Spawning initial house on hex: {centerHex.name}");

        // Use the PlayerHexSpawner's logic to spawn the house (or replicate it here)
        // For simplicity, we'll replicate the core logic.
        Vector3 spawnPosition = centerHex.WorldCenter + new Vector3(0, (0.04594f), 0); // Use non-wall height
        Quaternion spawnRotation = centerHex.transform.rotation;

        GameObject houseInstance = Instantiate(initialHousePrefab, spawnPosition, spawnRotation);
        houseInstance.transform.localScale = Vector3.one * 1f; // Use non-wall scale

        // Mark the hex as occupied
        centerHex.TryOccupy(houseInstance);

        _hasSpawned = true;
    }
}
