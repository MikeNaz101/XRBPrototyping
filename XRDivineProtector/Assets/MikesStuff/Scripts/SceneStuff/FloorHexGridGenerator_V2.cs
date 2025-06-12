// FloorHexGridGenerator_V2.cs
using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;
using UnityEngine.Events; // Required for UnityEvent

public class FloorHexGridGenerator_V2 : MonoBehaviour
{
    [Header("Hex Grid Settings")]
    public GameObject hexCellPrefab;
    public float hexOuterRadius = 0.25f;
    public float hexPrefabScaleFactor = 0.25f;

    [Header("Enemy Spawn Zone Settings")]
    public bool createSpawnZones = true;
    public float spawnZoneOffset = 2.0f; 
    public Vector3 spawnZoneScale = new Vector3(1.0f, 1.0f, 0.5f); 

    [Header("Optional Overrides")]
    public Material lineMaterialOverride;
    public Color lineColorOverride = Color.clear;
    public float lineWidthOverride = -1f;

    [Header("Debug")]
    public bool logAnchorOrientation = true;

    // --- NEW: Event to signal when grid and zones are ready ---
    [Header("Events")]
    public UnityEvent OnGenerationComplete;

    [Header("Runtime Objects")]
    [SerializeField]
    private GameObject hexGridParent;
    [SerializeField]
    private GameObject spawnZoneParent;
    private MRUKRoom currentRoom;
    private bool sceneHasLoaded = false;

    private Dictionary<Vector2Int, HexCell> grid = new Dictionary<Vector2Int, HexCell>();

    private float hexHeight_flat;
    private float horizSpacing_flat;
    private float vertSpacing_flat;

    private const string INTERACTIVE_HEX_LAYER_NAME = "HexGridInteractive";

    void Start()
    {
        if (hexCellPrefab == null)
        {
            Debug.LogError("FloorHexGridGenerator_V2: HexCellPrefab is not assigned! Grid cannot be generated.");
            return;
        }
        CalculateHexMetrics();
        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneReady);
    }

    void CalculateHexMetrics()
    {
        hexHeight_flat = Mathf.Sqrt(3) * hexOuterRadius;
        horizSpacing_flat = 1.5f * hexOuterRadius;
        vertSpacing_flat = hexHeight_flat;
    }

    void OnDestroy()
    {
        ClearGrid();
    }

    void OnSceneReady()
    {
        if (hexCellPrefab == null) return;
        if (sceneHasLoaded) return;
        sceneHasLoaded = true;

        currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom != null)
        {
            GenerateHexGrid(currentRoom);
            AssignNeighbors();
            if (createSpawnZones)
            {
                CreateEnemySpawnZones(currentRoom);
            }
            // --- Fire the event after all generation is complete ---
            OnGenerationComplete?.Invoke();
        }
        else
        {
            Debug.LogError("V2 (Flat-Top): Failed to get current room from MRUK.");
        }
    }
    
    // --- NEW: Public method for other scripts to get all hex cells ---
    public List<HexCell> GetAllHexCells()
    {
        return new List<HexCell>(grid.Values);
    }

    #region Grid Generation Logic
    public void GenerateHexGrid(MRUKRoom room)
    {
        MRUKAnchor floorAnchor = room.GetFloorAnchor();
        if (floorAnchor == null || !floorAnchor.PlaneRect.HasValue)
        {
            Debug.LogError("Floor anchor not found or has no PlaneRect.");
            return;
        }
        Transform anchorTransform = floorAnchor.transform;
        if (logAnchorOrientation) { /* ... logging ... */ }

        ClearGrid();
        grid = new Dictionary<Vector2Int, HexCell>();
        hexGridParent = new GameObject("FloorHexGrid_FlatTop_V2");
        Rect localFloorPlaneRect = floorAnchor.PlaneRect.Value;
        
        int q = 0;
        for (float plane_x = localFloorPlaneRect.xMin; plane_x < localFloorPlaneRect.xMax; plane_x += horizSpacing_flat)
        {
            int r = 0;
            float y_offset = (q % 2 != 0) ? vertSpacing_flat / 2f : 0f;
            for (float plane_y = localFloorPlaneRect.yMin + y_offset; plane_y < localFloorPlaneRect.yMax; plane_y += vertSpacing_flat)
            {
                Vector2 localHexCenterOnPlane = new Vector2(plane_x, plane_y);
                if (localFloorPlaneRect.Contains(localHexCenterOnPlane))
                {
                    Vector2Int gridCoords = new Vector2Int(q, r);
                    InstantiateHexCellPrefab(localHexCenterOnPlane, anchorTransform, gridCoords);
                }
                r++;
            }
            q++;
        }
    }

    void InstantiateHexCellPrefab(Vector2 localCenterOnPlane, Transform anchorTransform, Vector2Int gridCoords)
    {
        Vector3 anchorLocalHexCenter3D = new Vector3(localCenterOnPlane.x, localCenterOnPlane.y, 0);
        Vector3 hexWorldCenter = anchorTransform.TransformPoint(anchorLocalHexCenter3D);
        Quaternion hexWorldRotation = Quaternion.LookRotation(anchorTransform.right, anchorTransform.forward);
        
        Vector3[] worldVertices = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 60 * i;
            float angle_rad = Mathf.Deg2Rad * angle_deg;
            Vector3 pointOnPlane = new Vector3(hexOuterRadius * Mathf.Cos(angle_rad), hexOuterRadius * Mathf.Sin(angle_rad), 0);
            worldVertices[i] = hexWorldCenter + (hexWorldRotation * pointOnPlane * hexPrefabScaleFactor);
        }

        HexSide[] sides = new HexSide[6];
        for (int i = 0; i < 6; i++)
        {
            sides[i] = new HexSide { V1 = worldVertices[i], V2 = worldVertices[(i + 1) % 6] };
        }

        GameObject hexInstance = Instantiate(hexCellPrefab, hexWorldCenter, hexWorldRotation, hexGridParent.transform);
        hexInstance.name = $"Hex_{gridCoords.x}_{gridCoords.y}";
        hexInstance.transform.localScale = Vector3.one * hexPrefabScaleFactor;
        
        SetLayerRecursively(hexInstance, LayerMask.NameToLayer(INTERACTIVE_HEX_LAYER_NAME));
        
        HexCell hexCellComponent = hexInstance.GetComponent<HexCell>();
        if (hexCellComponent != null)
        {
            hexCellComponent.Initialize(hexWorldCenter, sides);
            grid.Add(gridCoords, hexCellComponent);
        }
    }

    void AssignNeighbors() {
        foreach(var pair in grid)
        {
            Vector2Int coords = pair.Key;
            HexCell cell = pair.Value;
            Vector2Int[] neighborOffsets;
            if (coords.x % 2 == 0) // Even columns
            {
                neighborOffsets = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0) };
            }
            else // Odd columns
            {
                neighborOffsets = new Vector2Int[] { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1) };
            }
            foreach(var offset in neighborOffsets)
            {
                if (grid.TryGetValue(coords + offset, out HexCell neighborCell))
                {
                    cell.AddNeighbor(neighborCell);
                }
            }
        }
     }
    #endregion
    
    void CreateEnemySpawnZones(MRUKRoom room)
    {
        List<MRUKAnchor> walls = room.GetWallAnchors();
        if (walls == null || walls.Count < 2)
        {
            Debug.LogWarning("Not enough walls found to create opposite spawn zones.");
            return;
        }

        float maxDistanceSq = 0;
        MRUKAnchor wallA = null, wallB = null;
        for (int i = 0; i < walls.Count; i++)
        {
            for (int j = i + 1; j < walls.Count; j++)
            {
                float distSq = (walls[i].transform.position - walls[j].transform.position).sqrMagnitude;
                if (distSq > maxDistanceSq)
                {
                    maxDistanceSq = distSq;
                    wallA = walls[i];
                    wallB = walls[j];
                }
            }
        }
        
        if (wallA == null || wallB == null) return;
        
        spawnZoneParent = new GameObject("EnemySpawnZones");
        MRUKAnchor floorAnchor = room.GetFloorAnchor();
        CreateSingleSpawnPlane(wallA, floorAnchor, "SpawnZone_A");
        CreateSingleSpawnPlane(wallB, floorAnchor, "SpawnZone_B");
    }

    void CreateSingleSpawnPlane(MRUKAnchor wallAnchor, MRUKAnchor floorAnchor, string zoneName)
    {
        Vector3 behindWallDirection = -wallAnchor.transform.forward;
        Vector3 planePosition = wallAnchor.transform.position + behindWallDirection * spawnZoneOffset;
        planePosition.y = floorAnchor.transform.position.y;

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = zoneName;
        plane.transform.SetParent(spawnZoneParent.transform);
        plane.transform.position = planePosition;
        plane.transform.rotation = Quaternion.FromToRotation(plane.transform.up, Vector3.up);
        plane.transform.localScale = spawnZoneScale;
        plane.GetComponent<MeshRenderer>().enabled = false;
        plane.tag = "EnemySpawnZone";
    }

    void SetLayerRecursively(GameObject obj, int newLayer) {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void ClearGrid() {
        if (hexGridParent != null) Destroy(hexGridParent);
        if (spawnZoneParent != null) Destroy(spawnZoneParent);
    }
    
    public void OnValidate() {
        if (!Application.isPlaying) CalculateHexMetrics();
    }
}
