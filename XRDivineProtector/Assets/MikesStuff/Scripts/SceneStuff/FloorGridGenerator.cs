using UnityEngine;
using Meta.XR.MRUtilityKit;
using System.Collections.Generic;

public class FloorGridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridSize = 0.5f;
    public Material lineMaterial;
    public Color lineColor = Color.cyan;
    public float lineWidth = 0.01f;

    [Header("Runtime Objects")]
    [SerializeField]
    private GameObject gridLinesParent;
    private MRUKRoom currentRoom;
    private bool sceneHasLoaded = false; // Our own flag

    void Start()
    {
        // Register the callback. This will execute OnSceneReady immediately if already loaded,
        // or when the scene finishes loading.
        MRUK.Instance.RegisterSceneLoadedCallback(OnSceneReady);

        // Alternative: Subscribe to the event.
        // MRUK.Instance.SceneLoadedEvent.AddListener(OnSceneReadyWithEvent);
    }

    void OnDestroy()
    {
        // It's good practice to unregister callbacks if the object is destroyed,
        // though MRUK might handle it internally.
        // For SceneLoadedEvent, you would use RemoveListener.
        // There isn't a direct UnregisterSceneLoadedCallback for a specific function,
        // so managing listeners for SceneLoadedEvent is cleaner if you need to remove them.
        // For simplicity with RegisterSceneLoadedCallback, we'll rely on MRUK's lifecycle.

        if (MRUK.Instance != null) {
            // If you used SceneLoadedEvent.AddListener(OnSceneReadyWithEvent):
            // MRUK.Instance.SceneLoadedEvent.RemoveListener(OnSceneReadyWithEvent);
        }
        ClearGrid();
    }

    // This method will be called by RegisterSceneLoadedCallback
    void OnSceneReady()
    {
        if (sceneHasLoaded) return; // Prevent running multiple times if called again

        sceneHasLoaded = true;
        Debug.Log("MRUK Scene is ready (via RegisterSceneLoadedCallback). Attempting to generate floor grid.");
        currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom != null)
        {
            GenerateGrid(currentRoom);
        }
        else
        {
            Debug.LogError("Failed to get current room from MRUK after scene loaded.");
        }
    }

    // Alternative method if you used SceneLoadedEvent.AddListener
    // void OnSceneReadyWithEvent()
    // {
    //     if (sceneHasLoaded) return;

    //     sceneHasLoaded = true;
    //     Debug.Log("MRUK SceneLoadedEvent fired. Attempting to generate floor grid.");
    //     currentRoom = MRUK.Instance.GetCurrentRoom();
    //     if (currentRoom != null)
    //     {
    //         GenerateGrid(currentRoom);
    //     }
    //     else
    //     {
    //         Debug.LogError("Failed to get current room from MRUK after SceneLoadedEvent.");
    //     }
    // }


    public void GenerateGrid(MRUKRoom room)
    {
        if (!sceneHasLoaded)
        {
            Debug.LogWarning("GenerateGrid called, but scene hasn't loaded yet. Aborting.");
            return;
        }

        if (room == null)
        {
            Debug.LogError("Room is null. Cannot generate grid.");
            return;
        }

        MRUKAnchor floorAnchor = room.GetFloorAnchor();

        if (floorAnchor == null)
        {
            Debug.LogWarning("Floor anchor not found in the room. Cannot generate grid.");
            return;
        }

        if (!floorAnchor.PlaneRect.HasValue)
        {
            Debug.LogWarning("Floor anchor does not have PlaneRect data. Cannot determine grid dimensions.");
            return;
        }

        ClearGrid();

        gridLinesParent = new GameObject("FloorGridLines");
        // Optional: Parent to this transform or the floor anchor
        // gridLinesParent.transform.SetParent(floorAnchor.transform, false);


        Rect localFloorRect = floorAnchor.PlaneRect.Value;
        Transform floorTransform = floorAnchor.transform;

        Vector2 localCenter = localFloorRect.center;
        Vector2 localSize = localFloorRect.size;

        // Assuming Y is up in the floor anchor's local space, PlaneRect.y maps to local Z
        float localMinX = localCenter.x - localSize.x / 2f;
        float localMaxX = localCenter.x + localSize.x / 2f;
        float localMinZ = localCenter.y - localSize.y / 2f;
        float localMaxZ = localCenter.y + localSize.y / 2f;

        // Create lines along the local X-axis of the floor anchor
        for (float z = localMinZ; z <= localMaxZ; z += gridSize)
        {
            // Ensure we don't create lines slightly outside due to floating point precision if z is very close to localMaxZ
            float currentZ = Mathf.Min(z, localMaxZ);
            Vector3 localStart = new Vector3(localMinX, 0, currentZ);
            Vector3 localEnd = new Vector3(localMaxX, 0, currentZ);

            Vector3 worldStart = floorTransform.TransformPoint(localStart);
            Vector3 worldEnd = floorTransform.TransformPoint(localEnd);

            CreateLine(worldStart, worldEnd, "GridLine_Z_Axis"); // Lines run along X, spaced along Z
        }

        // Create lines along the local Z-axis of the floor anchor
        for (float x = localMinX; x <= localMaxX; x += gridSize)
        {
            float currentX = Mathf.Min(x, localMaxX);
            Vector3 localStart = new Vector3(currentX, 0, localMinZ);
            Vector3 localEnd = new Vector3(currentX, 0, localMaxZ);

            Vector3 worldStart = floorTransform.TransformPoint(localStart);
            Vector3 worldEnd = floorTransform.TransformPoint(localEnd);

            CreateLine(worldStart, worldEnd, "GridLine_X_Axis"); // Lines run along Z, spaced along X
        }

        Debug.Log($"Generated grid with {gridLinesParent.transform.childCount} lines.");
    }

    void CreateLine(Vector3 startPoint, Vector3 endPoint, string lineName = "GridLine")
    {
        GameObject lineObj = new GameObject(lineName);
        lineObj.transform.SetParent(gridLinesParent.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial; // Make sure this is assigned in the Inspector
        if (lr.material == null)
        {
            Debug.LogError($"Line material is null for {lineName}. Grid lines will not be visible. Please assign a material in the Inspector.");
            // Optionally create a default material here if none is assigned
            lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply")); // Or Unlit/Color
        }
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;

        lr.SetPosition(0, startPoint);
        lr.SetPosition(1, endPoint);
    }

    public void ClearGrid()
    {
        if (gridLinesParent != null)
        {
            Destroy(gridLinesParent);
            gridLinesParent = null;
        }
        sceneHasLoaded = false; // Reset flag if grid is cleared, allowing regeneration
    }
}