// HexCell.cs
using UnityEngine;
using System.Collections.Generic; // Required for List

/// <summary>
/// Helper struct to define a side of a hexagon by its two corner vertices.
/// </summary>
public struct HexSide
{
    public Vector3 V1; // Start vertex of the side
    public Vector3 V2; // End vertex of the side

    // A calculated property to get the midpoint of the side
    public Vector3 Center => (V1 + V2) / 2f;

    // A calculated property to get the outward-facing normal direction of the side
    public Vector3 Normal
    {
        get
        {
            Vector3 direction = V2 - V1;
            // Assuming the hex is flat, its "up" is world Y. We rotate the side's direction
            // vector 90 degrees on the Y axis to get the outward normal.
            return new Vector3(-direction.z, 0, direction.x).normalized;
        }
    }
}

public class HexCell : MonoBehaviour
{
    public Vector3 WorldCenter { get; private set; }
    public bool IsOccupied { get; private set; } = false;
    public GameObject SpawnedObject { get; private set; } = null;

    // --- NEW: To store side and neighbor information ---
    public HexSide[] Sides { get; private set; } = new HexSide[6];
    public bool[] IsSideWalled { get; private set; } = new bool[6];
    public List<HexCell> Neighbors { get; private set; } = new List<HexCell>(6);

    private MeshRenderer meshRenderer;

    [Header("State Materials")]
    public Material emptyDefaultMaterial;
    public Material emptyHoverMaterial;
    public Material occupiedDefaultMaterial;
    public Material occupiedHoverMaterial;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogWarning($"HexCell on {gameObject.name} is missing a MeshRenderer.", this);
        }
        if (emptyDefaultMaterial == null)
        {
            Debug.LogError($"HexCell on {gameObject.name}: 'Empty Default Material' is not assigned!", this);
        }
    }

    // --- UPDATED: Initialize now accepts the 'sides' array ---
    public void Initialize(Vector3 worldCenter, HexSide[] sides)
    {
        WorldCenter = worldCenter;
        Sides = sides; // Store the side data
        // All sides start as not walled
        for (int i = 0; i < IsSideWalled.Length; i++)
        {
            IsSideWalled[i] = false;
        }
        UpdateVisualState();
    }

    public void OnPointerEnter()
    {
        if (meshRenderer == null) return;

        if (IsOccupied)
        {
            if (occupiedHoverMaterial != null) meshRenderer.material = occupiedHoverMaterial;
        }
        else
        {
            if (emptyHoverMaterial != null) meshRenderer.material = emptyHoverMaterial;
        }
    }

    public void OnPointerExit()
    {
        if (meshRenderer == null) return;
        UpdateVisualState();
    }

    public bool TryOccupy(GameObject spawnedInstance)
    {
        if (IsOccupied) return false;
        IsOccupied = true;
        SpawnedObject = spawnedInstance;
        UpdateVisualState();
        return true;
    }

    public void Vacate()
    {
        IsOccupied = false;
        SpawnedObject = null;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (meshRenderer == null) return;
        if (IsOccupied)
        {
            if (occupiedDefaultMaterial != null) meshRenderer.material = occupiedDefaultMaterial;
        }
        else
        {
            if (emptyDefaultMaterial != null) meshRenderer.material = emptyDefaultMaterial;
        }
    }

    public void OnHexCellActivated()
    {
        PlayerHexSpawner spawner = FindObjectOfType<PlayerHexSpawner>();
        if (spawner != null)
        {
            spawner.HandleHexSelection(this.gameObject);
        }
        else
        {
            Debug.LogError("PlayerHexSpawner instance not found in the scene!", this);
        }
    }

    public void AddNeighbor(HexCell neighbor)
    {
        if (neighbor != null && !Neighbors.Contains(neighbor))
        {
            Neighbors.Add(neighbor);
        }
    }

    // --- NEW: Method to mark a side as having a wall ---
    public void SetWallOnSide(int sideIndex, bool hasWall)
    {
        if (sideIndex >= 0 && sideIndex < 6)
        {
            IsSideWalled[sideIndex] = hasWall;
        }
    }
}
