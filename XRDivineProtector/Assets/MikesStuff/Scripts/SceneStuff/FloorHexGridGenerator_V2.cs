using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;

public class FloorHexGridGenerator_V2 : MonoBehaviour
{
    [Header("Hex Grid Settings")]
    public GameObject hexCellPrefab; // Assign your HexCell prefab here in the Inspector
    public float hexOuterRadius = 0.25f; // Distance from center to any vertex
    public float hexPrefabScaleFactor = 0.25f; // Scale factor for the instantiated prefab

    public Material lineMaterialOverride;
    public Color lineColorOverride = Color.clear;
    public float lineWidthOverride = -1f;

    [Header("Debug")]
    public bool logAnchorOrientation = true;

    [Header("Runtime Objects")]
    [SerializeField]
    private GameObject hexGridParent;
    private MRUKRoom currentRoom;
    private bool sceneHasLoaded = false;

    // Geometric constants for "flat-top" hexagons
    private float hexWidth_flat;  // Width across pointy ends (2 * R)
    private float hexHeight_flat; // Height across flat sides (sqrt(3) * R)
    private float horizSpacing_flat; // Horizontal distance between centers of columns (1.5 * R)
    private float vertSpacing_flat;  // Vertical distance between centers of rows (sqrt(3) * R, same as hexHeight_flat)

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
        // Metrics for "flat-top" hexagons
        hexWidth_flat = 2f * hexOuterRadius;
        hexHeight_flat = Mathf.Sqrt(3) * hexOuterRadius;

        horizSpacing_flat = 1.5f * hexOuterRadius; // Horizontal distance between column centers
        vertSpacing_flat = hexHeight_flat;         // Vertical distance between row centers
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

        Debug.Log("MRUK Scene is ready. Attempting to generate FLAT-TOP hexagonal floor grid (V2).");
        currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom != null)
        {
            GenerateHexGrid(currentRoom);
        }
        else
        {
            Debug.LogError("V2 (Flat-Top): Failed to get current room from MRUK.");
        }
    }

    public void GenerateHexGrid(MRUKRoom room)
    {
        if (hexCellPrefab == null) return;
        if (!sceneHasLoaded)
        {
            Debug.LogWarning("V2 (Flat-Top): GenerateHexGrid called, but scene hasn't loaded yet.");
            return;
        }
        if (room == null)
        {
            Debug.LogError("V2 (Flat-Top): Room is null.");
            return;
        }

        MRUKAnchor floorAnchor = room.GetFloorAnchor();
        if (floorAnchor == null)
        {
            Debug.LogWarning("V2 (Flat-Top): Floor anchor not found.");
            return;
        }
        if (!floorAnchor.PlaneRect.HasValue)
        {
            Debug.LogWarning("V2 (Flat-Top): Floor anchor PlaneRect is null.");
            return;
        }

        Transform anchorTransform = floorAnchor.transform;

        if (logAnchorOrientation)
        {
            Debug.Log($"--- Floor Anchor Orientation (V2 Flat-Top) ---");
            Debug.Log($"Anchor World Position: {anchorTransform.position}");
            Debug.Log($"Anchor Local X (World Right): {anchorTransform.right}");
            Debug.Log($"Anchor Local Y (World Up): {anchorTransform.up}");
            Debug.Log($"Anchor Local Z (World Forward/Normal): {anchorTransform.forward}");
            Debug.Log($"--- End Floor Anchor Orientation ---");
        }

        ClearGrid();
        hexGridParent = new GameObject("FloorHexGrid_FlatTop_V2");
        // hexGridParent.transform.SetParent(anchorTransform, false); // Optional

        Rect localFloorPlaneRect = floorAnchor.PlaneRect.Value;

        float minLocalX_Plane = localFloorPlaneRect.xMin;
        float maxLocalX_Plane = localFloorPlaneRect.xMax;
        float minLocalY_Plane = localFloorPlaneRect.yMin; // This is the second dimension on the anchor's local XY plane
        float maxLocalY_Plane = localFloorPlaneRect.yMax;

        bool oddColumn = false;
        int hexCounter = 0;

        // Iterate by columns (along anchor's local X-axis on the plane)
        for (float plane_x = minLocalX_Plane - hexWidth_flat; plane_x < maxLocalX_Plane + hexWidth_flat; plane_x += horizSpacing_flat)
        {
            float y_offset = oddColumn ? vertSpacing_flat / 2f : 0f;
            // Iterate by rows within columns (along anchor's local Y-axis on the plane)
            for (float plane_y = minLocalY_Plane - hexHeight_flat + y_offset; plane_y < maxLocalY_Plane + hexHeight_flat; plane_y += vertSpacing_flat)
            {
                Vector2 localHexCenterOnPlane = new Vector2(plane_x, plane_y); // Center on anchor's local XY plane

                // Bounding box for culling (using flat-top dimensions)
                Rect hexBoundingBoxOnPlane = new Rect(
                    localHexCenterOnPlane.x - hexOuterRadius, // For flat-top, width is 2*R, so half-width is R
                    localHexCenterOnPlane.y - hexHeight_flat / 2f,
                    hexOuterRadius * 2f, // Full width of flat-top hex
                    hexHeight_flat       // Full height of flat-top hex
                );

                if (hexBoundingBoxOnPlane.Overlaps(localFloorPlaneRect))
                {
                    InstantiateHexCellPrefab(localHexCenterOnPlane, anchorTransform, $"Hex_{hexCounter++}");
                }
            }
            oddColumn = !oddColumn;
        }
        Debug.Log($"V2 (Flat-Top): Generated {hexGridParent.transform.childCount} hex cells from prefab.");
    }

    void InstantiateHexCellPrefab(Vector2 localCenterOnPlane, Transform anchorTransform, string hexName)
    {
        if (hexCellPrefab == null) return;

        Vector3 anchorLocalHexCenter3D = new Vector3(localCenterOnPlane.x, localCenterOnPlane.y, 0);
        Vector3 hexWorldCenter = anchorTransform.TransformPoint(anchorLocalHexCenter3D);

        // --- Rotation for Flat-Top ---
        // Prefab's local Y (up) should align with anchor's local Z (floor normal).
        // Prefab's local Z (forward) should align with anchor's local X (along the floor plane).
        // This is the same base rotation as pointy-top to lay it flat.
        Quaternion baseFlatRotation = Quaternion.LookRotation(anchorTransform.right, anchorTransform.forward);

        // For a "flat-top" hexagon, its flat sides are typically horizontal.
        // If your prefab is designed with a point facing its local +Z (or +X),
        // you might need an additional rotation around its new local Y axis (which is anchorTransform.forward).
        // A common orientation for flat-top hex art has a flat side along its local X or Z.
        // If the LineRenderer in your prefab is already drawing a flat-top hex relative to its local axes,
        // this baseFlatRotation might be enough.
        // If not, you might need something like:
        // Quaternion correctiveRotation = Quaternion.Euler(0, 30, 0); // Rotate around its new Y axis
        // Quaternion hexWorldRotation = baseFlatRotation * correctiveRotation;
        // For now, let's assume the prefab is designed to be flat-top with baseFlatRotation.
        Quaternion hexWorldRotation = baseFlatRotation;


        GameObject hexInstance = Instantiate(hexCellPrefab, hexWorldCenter, hexWorldRotation, hexGridParent.transform);
        hexInstance.name = hexName;
        hexInstance.transform.localScale = Vector3.one * hexPrefabScaleFactor;

        int layerInt = LayerMask.NameToLayer(INTERACTIVE_HEX_LAYER_NAME);
        if (layerInt == -1)
        {
            Debug.LogError($"Layer '{INTERACTIVE_HEX_LAYER_NAME}' does not exist.");
        } else {
            SetLayerRecursively(hexInstance, layerInt);
        }

        HexCell hexCellComponent = hexInstance.GetComponent<HexCell>();
        if (hexCellComponent != null)
        {
            hexCellComponent.Initialize(hexWorldCenter);
            LineRenderer lr = hexInstance.GetComponentInChildren<LineRenderer>();
            if (lr != null)
            {
                if (lineMaterialOverride != null) lr.material = lineMaterialOverride;
                if (lineColorOverride.a > 0)
                {
                    lr.startColor = lineColorOverride;
                    lr.endColor = lineColorOverride;
                }
                if (lineWidthOverride >= 0)
                {
                    lr.startWidth = lineWidthOverride;
                    lr.endWidth = lineWidthOverride;
                }
                // Ensure your prefab's LineRenderer points define a flat-top hexagon
                // relative to its local origin, scaled appropriately.
            }
        }
        else
        {
            Debug.LogWarning($"Instantiated hex cell '{hexName}' does not have a HexCell component.");
        }
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void ClearGrid()
    {
        if (hexGridParent != null)
        {
            Destroy(hexGridParent);
            hexGridParent = null;
        }
    }

    public void OnValidate()
    {
        if (!Application.isPlaying) {
            CalculateHexMetrics();
        }
    }
}
