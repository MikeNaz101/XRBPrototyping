/*using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using Oculus.Interaction; // For Vector3 تقريبا operations if needed, or just Unity's Vector3 math

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Creates, visualizes, and manages a persistent grid system on an MRUK-detected floor.
/// The grid configuration (cell size, associated floor) is saved using an OVRSpatialAnchor.
/// </summary>
public class PersistentFloorGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("The desired size of each grid cell in meters.")]
    public float cellSize = 0.5f;
    [Tooltip("Color for visualizing the grid lines.")]
    public Color gridColor = new Color(0f, 1f, 1f, 0.5f); // Cyan, semi-transparent
    [Tooltip("Height offset for drawing the grid lines slightly above the actual floor to prevent Z-fighting.")]
    public float gridLineYOffset = 0.005f; // 5mm above floor

    [Header("Persistence")]
    [Tooltip("A unique label used to identify the spatial anchor that stores this grid's configuration. " +
             "Changing this will effectively create a new saved grid configuration.")]
    public string gridAnchorLabel = "MyFloorGridConfig_v1";
    [Tooltip("Optional: A prefab to instantiate at the grid data anchor's position for visualization or interaction.")]
    public GameObject gridDataAnchorDebugPrefab;


    private MRUKAnchor _currentFloorAnchor;
    private List<Vector3> _gridLinePoints = new List<Vector3>(); // For LineRenderer or Gizmos
    private LineRenderer _lineRenderer;

    private OVRSpatialAnchor _gridConfigAnchor; // The anchor that stores our grid's config
    private System.Guid _gridConfigAnchorUuid = System.Guid.Empty;
    private bool _isGridDefined = false;
    private bool _mrukReady = false;

    // Data to be saved/loaded for the grid config
    private struct GridSaveData
    {
        public float CellSize;
        public System.Guid AssociatedFloorAnchorUuid;
        // Add other parameters if needed, e.g., grid rotation offset, origin offset
    }
    private GridSaveData _currentGridData;


    void Start()
    {
        // Ensure LineRenderer component exists for drawing the grid
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }
        _lineRenderer.startWidth = 0.01f; // 1cm thick lines
        _lineRenderer.endWidth = 0.01f;
        _lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply")); // Simple unlit material
        _lineRenderer.startColor = gridColor;
        _lineRenderer.endColor = gridColor;
        _lineRenderer.positionCount = 0;
        _lineRenderer.useWorldSpace = true;

        if (MRUK.Instance != null)
        {
            MRUK.Instance.RegisterSceneLoadedCallback(OnMRUKSceneLoaded);
            if (MRUK.Instance.IsSceneLoaded)
            {
                OnMRUKSceneLoaded();
            }
            else
            {
                Debug.Log("PersistentFloorGrid: Waiting for MRUK Scene to load...", this);
            }
        }
        else
        {
            Debug.LogError("PersistentFloorGrid: MRUK.Instance is null. Ensure MRUK is set up in your scene.", this);
        }
    }

    private void OnMRUKSceneLoaded()
    {
        Debug.Log("PersistentFloorGrid: MRUK Scene Loaded callback received.", this);
        _mrukReady = true;
        FindAndDefineFloor();
        // After finding the floor, attempt to load any saved grid configuration
        LoadGridConfiguration();
    }

    private void FindAndDefineFloor()
    {
        if (!_mrukReady) return;

        MRUKRoom primaryRoom = MRUK.Instance.GetCurrentRoom(); // Or iterate MRUK.Instance.GetRooms()
        if (primaryRoom == null)
        {
            Debug.LogWarning("PersistentFloorGrid: No current MRUK room found.", this);
            return;
        }

        _currentFloorAnchor = primaryRoom.GetFloorAnchor();

        if (_currentFloorAnchor == null)
        {
            Debug.LogWarning("PersistentFloorGrid: No FLOOR anchor found in the current room.", this);
            _isGridDefined = false;
            ClearGridVisualization();
            return;
        }

        Debug.Log($"PersistentFloorGrid: Found floor anchor '{_currentFloorAnchor.name}' (UUID: {_currentFloorAnchor.AnchorUuid})", _currentFloorAnchor.gameObject);
        _isGridDefined = true; // We have a floor, so a grid *can* be defined.
                               // Actual grid parameters will be from loaded data or defaults.
    }


    private void GenerateGridVisualization(MRUKAnchor floorAnchor, float currentCellSize)
    {
        if (floorAnchor == null || floorAnchor.PlaneBoundary2D == null || floorAnchor.PlaneBoundary2D.Count == 0)
        {
            Debug.LogWarning("PersistentFloorGrid: Cannot generate grid, floor anchor or its boundary is invalid.", this);
            ClearGridVisualization();
            return;
        }

        _gridLinePoints.Clear();

        // Get floor transform (position and rotation define the plane)
        Transform floorTransform = floorAnchor.transform;
        Vector3 floorPosition = floorTransform.position;
        Quaternion floorRotation = floorTransform.rotation; // This aligns the grid with the floor plane

        // Get the 2D boundary points (these are in the anchor's local XZ space)
        List<Vector2> localBoundary = floorAnchor.PlaneBoundary2D;

        // Calculate world space bounding box of the local 2D boundary points
        // This helps determine the extents for grid line generation.
        if (localBoundary.Count == 0)
        {
            ClearGridVisualization();
            return;
        }

        Vector2 minBounds = localBoundary[0];
        Vector2 maxBounds = localBoundary[0];
        foreach (Vector2 pt in localBoundary)
        {
            minBounds.x = Mathf.Min(minBounds.x, pt.x);
            minBounds.y = Mathf.Min(minBounds.y, pt.y); // y in Vector2 here is local Z
            maxBounds.x = Mathf.Max(maxBounds.x, pt.x);
            maxBounds.y = Mathf.Max(maxBounds.y, pt.y); // y in Vector2 here is local Z
        }

        // Determine grid lines based on these local bounds and cell size
        // Vertical lines (along local Z / boundary Y)
        for (float x = minBounds.x - (minBounds.x % currentCellSize); x <= maxBounds.x; x += currentCellSize)
        {
            Vector3 localStart = new Vector3(x, 0, minBounds.y);
            Vector3 localEnd = new Vector3(x, 0, maxBounds.y);
            // Transform to world space, applying floor rotation and position, and Y offset
            _gridLinePoints.Add(floorTransform.TransformPoint(localStart) + Vector3.up * gridLineYOffset);
            _gridLinePoints.Add(floorTransform.TransformPoint(localEnd) + Vector3.up * gridLineYOffset);
        }

        // Horizontal lines (along local X / boundary X)
        for (float z = minBounds.y - (minBounds.y % currentCellSize); z <= maxBounds.y; z += currentCellSize)
        {
            Vector3 localStart = new Vector3(minBounds.x, 0, z);
            Vector3 localEnd = new Vector3(maxBounds.x, 0, z);
            _gridLinePoints.Add(floorTransform.TransformPoint(localStart) + Vector3.up * gridLineYOffset);
            _gridLinePoints.Add(floorTransform.TransformPoint(localEnd) + Vector3.up * gridLineYOffset);
        }

        // Update LineRenderer
        if (_lineRenderer)
        {
            _lineRenderer.positionCount = _gridLinePoints.Count;
            _lineRenderer.SetPositions(_gridLinePoints.ToArray());
            _lineRenderer.enabled = true;
        }
        Debug.Log($"PersistentFloorGrid: Generated grid visualization with {_gridLinePoints.Count / 2} lines.", this);
    }

    private void ClearGridVisualization()
    {
        if (_lineRenderer)
        {
            _lineRenderer.positionCount = 0;
            _lineRenderer.enabled = false;
        }
        _gridLinePoints.Clear();
    }

    /// <summary>
    /// Call this method to save the current grid configuration.
    /// </summary>
    public async void SaveGridConfiguration()
    {
        if (!_mrukReady || _currentFloorAnchor == null)
        {
            Debug.LogError("PersistentFloorGrid: MRUK not ready or no floor anchor found. Cannot save grid.", this);
            return;
        }

        if (_gridConfigAnchor == null) // Create a new anchor for our grid config if one doesn't exist
        {
            GameObject anchorGO = new GameObject($"GridConfigAnchor_{gridAnchorLabel}");
            // Position it at the center of the floor for reference, though its exact position
            // is less critical than its UUID and the data it stores.
            anchorGO.transform.position = _currentFloorAnchor.transform.position + Vector3.up * 0.1f; // Slightly above floor
            anchorGO.transform.rotation = _currentFloorAnchor.transform.rotation;

            _gridConfigAnchor = anchorGO.AddComponent<OVRSpatialAnchor>();
            // Add a component to store our custom data
            GridDataComponent dataComponent = anchorGO.AddComponent<GridDataComponent>();
            Debug.Log("PersistentFloorGrid: Created new OVRSpatialAnchor for grid configuration.", this);

            if (gridDataAnchorDebugPrefab) Instantiate(gridDataAnchorDebugPrefab, anchorGO.transform.position, anchorGO.transform.rotation);
        }

        // Populate the data to save
        _currentGridData.CellSize = this.cellSize;
        _currentGridData.AssociatedFloorAnchorUuid = _currentFloorAnchor.AnchorUuid;

        // Update the data component on the anchor
        GridDataComponent existingDataComponent = _gridConfigAnchor.GetComponent<GridDataComponent>();
        if (existingDataComponent)
        {
            existingDataComponent.Initialize(_currentGridData.CellSize, _currentGridData.AssociatedFloorAnchorUuid);
        } else {
            Debug.LogError("PersistentFloorGrid: GridDataComponent not found on config anchor for saving.", this);
            return;
        }


        // Save the OVRSpatialAnchor (which will also save its GridDataComponent if it implements IOVRSyncable)
        var success = await _gridConfigAnchor.SaveAsync();

        if (success)
        {
            _gridConfigAnchorUuid = _gridConfigAnchor.Uuid;
            PlayerPrefs.SetString(gridAnchorLabel + "_UUID", _gridConfigAnchorUuid.ToString()); // Save UUID to find it later
            PlayerPrefs.Save();
            Debug.Log($"PersistentFloorGrid: Grid configuration anchor SAVED successfully. UUID: {_gridConfigAnchorUuid}", _gridConfigAnchor.gameObject);
        }
        else
        {
            Debug.LogError("PersistentFloorGrid: Failed to save grid configuration anchor.", _gridConfigAnchor.gameObject);
        }
    }

    /// <summary>
    /// Attempts to load the grid configuration when the scene starts or MRUK is ready.
    /// </summary>
    public async void LoadGridConfiguration()
    {
        if (!_mrukReady)
        {
            Debug.LogWarning("PersistentFloorGrid: MRUK not ready, cannot load grid configuration yet.", this);
            return;
        }

        string savedUuidString = PlayerPrefs.GetString(gridAnchorLabel + "_UUID", "");
        if (string.IsNullOrEmpty(savedUuidString) || !System.Guid.TryParse(savedUuidString, out _gridConfigAnchorUuid))
        {
            Debug.Log("PersistentFloorGrid: No saved grid configuration anchor UUID found. Using default settings.", this);
            // If no saved config, define grid with defaults on current floor
            if (_currentFloorAnchor != null) {
                this.cellSize = 0.5f; // Default cell size
                _currentGridData.CellSize = this.cellSize;
                _currentGridData.AssociatedFloorAnchorUuid = _currentFloorAnchor.AnchorUuid;
                GenerateGridVisualization(_currentFloorAnchor, this.cellSize);
                // Optionally, save this default configuration now
                // SaveGridConfiguration();
            }
            return;
        }

        Debug.Log($"PersistentFloorGrid: Attempting to load grid configuration anchor with UUID: {_gridConfigAnchorUuid}", this);

        // Load the specific anchor by its UUID
        var loadOptions = new OVRSpatialAnchor.LoadOptions
        {
            Uuids = new List<System.Guid> { _gridConfigAnchorUuid },
            StorageLocation = OVRSpatialAnchor.StorageLocation.Local, // Or Cloud if you saved it there
            Timeout = 0 // No timeout, rely on completion
        };

        var anchors = await OVRSpatialAnchor.LoadAnchorsAsync(loadOptions);

        if (anchors != null && anchors.Count > 0)
        {
            _gridConfigAnchor = anchors[0];
            GridDataComponent dataComponent = _gridConfigAnchor.GetComponent<GridDataComponent>();
            if (dataComponent != null)
            {
                this.cellSize = dataComponent.CellSize; // Load cell size
                System.Guid associatedFloorUuid = dataComponent.AssociatedFloorAnchorUuid;

                Debug.Log($"PersistentFloorGrid: Grid configuration LOADED. CellSize: {this.cellSize}, AssocFloorUUID: {associatedFloorUuid}", _gridConfigAnchor.gameObject);

                // Now find the MRUK floor anchor that matches the saved UUID
                if (_currentFloorAnchor != null && _currentFloorAnchor.AnchorUuid == associatedFloorUuid)
                {
                    GenerateGridVisualization(_currentFloorAnchor, this.cellSize);
                }
                else
                {
                    // Try to find the specific floor anchor again if the current one doesn't match
                    // (e.g., if room layout changed slightly or multiple rooms)
                    MRUKAnchor savedFloor = FindMRUKAnchorByUuid(associatedFloorUuid);
                    if (savedFloor != null)
                    {
                        _currentFloorAnchor = savedFloor;
                        GenerateGridVisualization(_currentFloorAnchor, this.cellSize);
                         Debug.Log("PersistentFloorGrid: Matched saved grid to floor anchor: " + _currentFloorAnchor.name, _currentFloorAnchor.gameObject);
                    } else {
                        Debug.LogWarning($"PersistentFloorGrid: Could not find the MRUK floor anchor (UUID: {associatedFloorUuid}) that this grid was originally associated with. Grid not drawn.", this);
                        ClearGridVisualization();
                    }
                }
            }
            else
            {
                Debug.LogError("PersistentFloorGrid: Loaded grid config anchor, but GridDataComponent is missing!", _gridConfigAnchor.gameObject);
            }
        }
        else
        {
            Debug.LogWarning($"PersistentFloorGrid: Failed to load grid configuration anchor with UUID: {_gridConfigAnchorUuid}. It might have been erased or not saved to this location. Using defaults.", this);
            if (_currentFloorAnchor != null) { // Fallback to default on current floor
                this.cellSize = 0.5f;
                GenerateGridVisualization(_currentFloorAnchor, this.cellSize);
            }
        }
    }

    private MRUKAnchor FindMRUKAnchorByUuid(System.Guid uuid)
    {
        if (!_mrukReady) return null;
        foreach (var room in MRUK.Instance.GetRooms())
        {
            foreach (var anchor in room.Anchors)
            {
                if (anchor.AnchorUuid == uuid)
                {
                    return anchor;
                }
            }
        }
        return null;
    }


    void OnDestroy()
    {
        if (MRUK.Instance != null)
        {
            MRUK.Instance.UnregisterSceneLoadedCallback(OnMRUKSceneLoaded);
        }
    }

    // For Gizmo visualization in Editor when the object is selected
    void OnDrawGizmosSelected()
    {
        if (_isGridDefined && _gridLinePoints.Count > 0 && _lineRenderer && _lineRenderer.enabled)
        {
            Gizmos.color = gridColor;
            for (int i = 0; i < _gridLinePoints.Count; i += 2)
            {
                if (i + 1 < _gridLinePoints.Count)
                {
                    Gizmos.DrawLine(_gridLinePoints[i], _gridLinePoints[i + 1]);
                }
            }
        }
    }
}*/