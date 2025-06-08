using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;

public class FloorHexGridGenerator : MonoBehaviour
{
    [Header("Hex Grid Settings")]
    public float hexOuterRadius = 0.25f; // Distance from center to any vertex
    public Material lineMaterial;
    public Color lineColor = Color.magenta;
    public float lineWidth = 0.01f;

    [Header("Runtime Objects")]
    [SerializeField]
    private GameObject hexGridParent;
    private MRUKRoom currentRoom;
    private bool sceneHasLoaded = false;

    // Calculated geometric constants for pointy-top hexagons
    private float hexWidth;      // Full width of the hexagon (sqrt(3) * R)
    private float hexHeight;     // Full height of the hexagon (2 * R)
    private float horizSpacing;  // Horizontal distance between centers (same as hexWidth)
    private float vertSpacing;   // Vertical distance between centers of rows (1.5 * R)

    void Start()
    {
        CalculateHexMetrics();
        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneReady);
    }

    void CalculateHexMetrics()
    {
        hexWidth = Mathf.Sqrt(3) * hexOuterRadius;
        hexHeight = 2 * hexOuterRadius;
        horizSpacing = hexWidth;
        vertSpacing = 1.5f * hexOuterRadius;
    }

    void OnDestroy()
    {
        // If you were using SceneLoadedEvent.AddListener, you'd remove it here.
        // For RegisterSceneLoadedCallback, MRUK handles its lifecycle.
        ClearGrid();
    }

    void OnSceneReady()
    {
        if (sceneHasLoaded) return;
        sceneHasLoaded = true;

        Debug.Log("MRUK Scene is ready. Attempting to generate hexagonal floor grid.");
        currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom != null)
        {
            GenerateHexGrid(currentRoom);
        }
        else
        {
            Debug.LogError("Failed to get current room from MRUK after scene loaded.");
        }
    }

    public void GenerateHexGrid(MRUKRoom room)
    {
        if (!sceneHasLoaded)
        {
            Debug.LogWarning("GenerateHexGrid called, but scene hasn't loaded yet. Aborting.");
            return;
        }
        if (room == null)
        {
            Debug.LogError("Room is null. Cannot generate hex grid.");
            return;
        }

        MRUKAnchor floorAnchor = room.GetFloorAnchor();
        if (floorAnchor == null)
        {
            Debug.LogWarning("Floor anchor not found. Cannot generate hex grid.");
            return;
        }
        if (!floorAnchor.PlaneRect.HasValue)
        {
            Debug.LogWarning("Floor anchor PlaneRect is null. Cannot determine grid dimensions.");
            return;
        }

        ClearGrid();
        hexGridParent = new GameObject("FloorHexGrid");
        // Optional: hexGridParent.transform.SetParent(floorAnchor.transform, false);

        Rect localFloorRect = floorAnchor.PlaneRect.Value;
        Transform floorTransform = floorAnchor.transform;

        // Get the boundaries in the anchor's local XZ plane
        float minLocalX = localFloorRect.xMin;
        float maxLocalX = localFloorRect.xMax;
        float minLocalZ = localFloorRect.yMin; // PlaneRect.y maps to local Z
        float maxLocalZ = localFloorRect.yMax; // PlaneRect.y maps to local Z

        bool oddRow = false;
        int hexCounter = 0;

        // Iterate vertically (along local Z)
        // Start well before minLocalZ to ensure coverage if centers are outside but hexes overlap
        for (float z = minLocalZ - hexHeight; z < maxLocalZ + hexHeight; z += vertSpacing)
        {
            float xOffset = oddRow ? horizSpacing / 2f : 0f;
            // Iterate horizontally (along local X)
            // Start well before minLocalX for coverage
            for (float x = minLocalX - hexWidth + xOffset; x < maxLocalX + hexWidth; x += horizSpacing)
            {
                Vector2 localHexCenter = new Vector2(x, z);

                // Basic check: is the center within the broad rect?
                // A more accurate check would be if any part of the hex overlaps.
                // For simplicity, we'll draw if the center is roughly within the extended bounds.
                // More robust: check if hexagon's bounding box overlaps localFloorRect.
                Rect hexBoundingBox = new Rect(
                    localHexCenter.x - hexWidth / 2f,
                    localHexCenter.y - hexOuterRadius, // For pointy-top, height is 2*R
                    hexWidth,
                    hexOuterRadius * 2f
                );

                if (hexBoundingBox.Overlaps(localFloorRect))
                {
                    CreateHexagon(localHexCenter, floorTransform, $"Hex_{hexCounter++}");
                }
            }
            oddRow = !oddRow;
        }
        Debug.Log($"Generated {hexGridParent.transform.childCount} hexagons.");
    }

    void CreateHexagon(Vector2 localCenter, Transform anchorTransform, string hexName)
    {
        GameObject hexObj = new GameObject(hexName);
        hexObj.transform.SetParent(hexGridParent.transform);

        LineRenderer lr = hexObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        if (lr.material == null)
        {
            Debug.LogError($"Line material is null for {hexName}. Grid lines will not be visible. Please assign a material in the Inspector.");
            lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
        }
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 6;
        lr.loop = true; // Connects the last point to the first

        Vector3[] worldVertices = new Vector3[6];
        for (int i = 0; i < 6; i++)
        {
            // Calculate 2D vertex in local XY plane of the hexagon
            float angle_deg = 60 * i - 30; // -30 to make it pointy-top (one flat side on top/bottom if 0)
                                           // For true pointy-top, first point is at top, so angle should be 90 (or 90 + 60*i)
                                           // Let's use angles for pointy top: 90, 90-60, 90-120 ...
            angle_deg = 90 - (60 * i); // Correct for pointy top
            float angle_rad = Mathf.Deg2Rad * angle_deg;

            // Local coordinates relative to hexagon center
            float x_local_hex = hexOuterRadius * Mathf.Cos(angle_rad);
            float z_local_hex = hexOuterRadius * Mathf.Sin(angle_rad);

            // Position in anchor's local space (localCenter.x, localCenter.y is on anchor's XZ plane)
            Vector3 localPointOnAnchor = new Vector3(localCenter.x + x_local_hex, 0, localCenter.y + z_local_hex);
            worldVertices[i] = anchorTransform.TransformPoint(localPointOnAnchor);
        }
        lr.SetPositions(worldVertices);
    }

    public void ClearGrid()
    {
        if (hexGridParent != null)
        {
            Destroy(hexGridParent);
            hexGridParent = null;
        }
        // sceneHasLoaded = false; // Don't reset this here unless you intend to reload scene data
    }

    // Call this if hexOuterRadius is changed at runtime from the inspector
    public void OnValidate()
    {
        CalculateHexMetrics();
    }
}