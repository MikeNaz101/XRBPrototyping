/*using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like FirstOrDefault
using Meta.XR.MRUtilityKit;

#if UNITY_EDITOR
using UnityEditor; // For a potential editor button
#endif

/// <summary>
/// Modifies specified mountain meshes by flattening any parts of them that are
/// directly above MRUK-detected floor surfaces in the real world.
/// This effectively "carves out" floor areas within the mountain meshes.
/// </summary>
public class MountainFloorFlattener : MonoBehaviour
{
    [Header("Mountain Meshes")]
    [Tooltip("Assign the MeshFilters of the premade mountain GameObjects you want to modify.")]
    public List<MeshFilter> mountainMeshFilters = new List<MeshFilter>();
    [Tooltip("Alternatively, provide a tag for GameObjects whose MeshFilters should be processed.")]
    public string mountainTag = "";

    [Header("Flattening Settings")]
    [Tooltip("The target Y-coordinate for flattened vertices, relative to the detected MRUK floor height. " +
             "A small positive value can prevent Z-fighting if you place other objects on the floor.")]
    public float floorLevelOffset = 0.01f;

    [Tooltip("Maximum vertical distance above a floor for a vertex to be considered for flattening. " +
             "Prevents accidentally flattening very high parts of mountains if a small floor piece is below.")]
    public float maxFlattenHeightAboveFloor = 10.0f; // Adjust as needed

    private List<MRUKAnchor> _mrukFloorAnchors = new List<MRUKAnchor>();
    private bool _mrukLoadedAndReady = false;
    private bool _subscribedToMRUKEvents = false;

    void Start()
    {
        // Optionally, find mountains by tag if the list is empty
        if (mountainMeshFilters.Count == 0 && !string.IsNullOrEmpty(mountainTag))
        {
            GameObject[] taggedMountains = GameObject.FindGameObjectsWithTag(mountainTag);
            foreach (var mountainObj in taggedMountains)
            {
                MeshFilter mf = mountainObj.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null) // Ensure it has a mesh
                {
                    mountainMeshFilters.Add(mf);
                }
            }
        }

        if (mountainMeshFilters.Count == 0)
        {
            Debug.LogWarning("MountainFloorFlattener: No mountain meshes assigned or found by tag. Flattener will not run.", this);
            return;
        }

        // Subscribe to MRUK scene loaded event/callback
        if (MRUK.Instance != null)
        {
            // Using RegisterSceneLoadedCallback as it's a common pattern in MRUK
            // Ensure this matches your MRUK version's API.
            // If this specific method name is incorrect, you'll need to find the equivalent
            // in your MRUK documentation (e.g., RegisterGeneratedSceneCallback, etc.)
            MRUK.Instance.RegisterSceneLoadedCallback(OnMRUKSceneLoaded);
            _subscribedToMRUKEvents = true;

            if (MRUK.Instance.IsSceneLoaded)
            {
                // If scene is already loaded when this Start runs, call it immediately
                Debug.Log("MountainFloorFlattener: MRUK Scene already loaded in Start. Initializing.", this);
                OnMRUKSceneLoaded();
            }
            else
            {
                Debug.Log("MountainFloorFlattener: Waiting for MRUK Scene to load (callback registered)...", this);
            }
        }
        else
        {
            Debug.LogError("MountainFloorFlattener: MRUK.Instance is null in Start(). Ensure MRUK is initialized in your scene and this script runs after MRUK.", this);
        }
    }

    private void OnMRUKSceneLoaded()
    {
        Debug.Log("OnMRUKSceneLoaded callback received. Initializing MountainFloorFlattener.", this);
        _mrukFloorAnchors.Clear();

        if (MRUK.Instance == null)
        {
            Debug.LogError("MountainFloorFlattener: MRUK.Instance became null by the time OnMRUKSceneLoaded was called.", this);
            _mrukLoadedAndReady = false;
            return;
        }
        // It's good practice to ensure this callback isn't processed multiple times if already ready
        // though the clear and re-population of _mrukFloorAnchors handles data refresh.
        // if (_mrukLoadedAndReady) {
        //     Debug.Log("MountainFloorFlattener: OnMRUKSceneLoaded called, but already initialized. Re-processing floors.", this);
        // }


        var rooms = MRUK.Instance.GetRooms();
        if (rooms == null || rooms.Count == 0)
        {
            Debug.LogWarning("MountainFloorFlattener: MRUK Scene loaded, but no rooms found or GetRooms() returned null.", this);
            _mrukLoadedAndReady = false;
            return;
        }

        foreach (var room in rooms)
        {
            if (room == null) continue; 

            var anchorsInRoom = room.Anchors; 
            if (anchorsInRoom == null) continue;

            foreach (var anchor in anchorsInRoom)
            {
                if (anchor == null) continue;

                if (anchor.SemanticLabels.Contains(MRUKAnchor.SemanticLabel.FLOOR))
                {
                    if (anchor.PlaneBoundary2D != null && anchor.PlaneBoundary2D.Count > 0)
                    {
                        _mrukFloorAnchors.Add(anchor);
                        Debug.Log($"Found MRUK Floor Anchor: {anchor.name} at Y: {anchor.transform.position.y}", anchor.gameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"MRUK Anchor {anchor.name} is labeled FLOOR but has no PlaneBoundary2D. Skipping.", anchor.gameObject);
                    }
                }
            }
        }

        if (_mrukFloorAnchors.Count == 0)
        {
            Debug.LogWarning("MountainFloorFlattener: No MRUK Floor anchors with valid boundaries found after processing all rooms.", this);
            _mrukLoadedAndReady = false;
            return;
        }

        _mrukLoadedAndReady = true;
        Debug.Log($"MountainFloorFlattener initialized with {_mrukFloorAnchors.Count} floor anchors. Ready to flatten.", this);

        // Example: Automatically flatten on load.
        // Consider if you want this to happen automatically or be triggered by the button/another script.
        // FlattenAllMountainMeshes(); 
    }

    public void FlattenAllMountainMeshes()
    {
        if (!_mrukLoadedAndReady)
        {
            Debug.LogError("MountainFloorFlattener: MRUK data is not loaded or no floors found. Cannot flatten.", this);
            if (MRUK.Instance != null && MRUK.Instance.IsSceneLoaded && _mrukFloorAnchors.Count == 0) {
                Debug.Log("Attempting to re-initialize floor anchors before flattening...", this);
                OnMRUKSceneLoaded(); 
                if (!_mrukLoadedAndReady)
                {
                    Debug.LogError("Re-initialization failed. Still not ready to flatten.", this);
                    return;
                }
            } else if (MRUK.Instance == null || !MRUK.Instance.IsSceneLoaded) {
                 Debug.LogWarning("MRUK Scene not loaded. Please ensure room setup is complete and MRUK has loaded the scene.", this);
                 return;
            }
        }

        if (mountainMeshFilters.Count == 0)
        {
            Debug.LogWarning("MountainFloorFlattener: No mountain meshes assigned to flatten.", this);
            return;
        }

        Debug.Log($"Starting to flatten {mountainMeshFilters.Count} mountain meshes...", this);

        foreach (MeshFilter mf in mountainMeshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning("A MeshFilter in the list is null or has no mesh. Skipping.", this);
                continue;
            }

            Mesh originalMesh = mf.sharedMesh;
            Mesh clonedMesh = Instantiate(originalMesh); 
            mf.mesh = clonedMesh; 

            Vector3[] vertices = clonedMesh.vertices;
            bool meshModified = false;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 localVertexPos = vertices[i];
                Vector3 worldVertexPos = mf.transform.TransformPoint(localVertexPos);

                MRUKAnchor relevantFloor = null;
                float highestFloorYBelowVertex = float.MinValue; 

                foreach (MRUKAnchor floorAnchor in _mrukFloorAnchors)
                {
                    if (floorAnchor == null || floorAnchor.PlaneBoundary2D == null || floorAnchor.PlaneBoundary2D.Count == 0) continue;

                    Vector3 vertexInFloorAnchorSpace = floorAnchor.transform.InverseTransformPoint(worldVertexPos);

                    if (MRUK.Helpers.IsPositionInPolygon(
                            new Vector2(vertexInFloorAnchorSpace.x, vertexInFloorAnchorSpace.z),
                            floorAnchor.PlaneBoundary2D 
                        ))
                    {
                        float currentFloorWorldY = floorAnchor.transform.position.y;
                        if (worldVertexPos.y >= currentFloorWorldY && 
                            worldVertexPos.y <= currentFloorWorldY + maxFlattenHeightAboveFloor) 
                        {
                            if (currentFloorWorldY > highestFloorYBelowVertex) 
                            {
                                highestFloorYBelowVertex = currentFloorWorldY;
                                relevantFloor = floorAnchor;
                            }
                        }
                    }
                }

                if (relevantFloor != null)
                {
                    worldVertexPos.y = highestFloorYBelowVertex + floorLevelOffset;
                    vertices[i] = mf.transform.InverseTransformPoint(worldVertexPos);
                    meshModified = true;
                }
            }

            if (meshModified)
            {
                clonedMesh.vertices = vertices;
                clonedMesh.RecalculateNormals();
                clonedMesh.RecalculateBounds();

                MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
                if (mc != null)
                {
                    mc.sharedMesh = null; 
                    mc.sharedMesh = clonedMesh;
                    Debug.Log($"Flattened and updated mesh & collider for: {mf.gameObject.name}", mf.gameObject);
                }
                else
                {
                    Debug.Log($"Flattened and updated mesh for: {mf.gameObject.name} (No MeshCollider found to update)", mf.gameObject);
                }
            }
            else
            {
                 Debug.Log($"No vertices were flattened for: {mf.gameObject.name}", mf.gameObject);
            }
        }
        Debug.Log("Mountain flattening process complete.", this);
    }

    void OnDestroy()
    {
        if (MRUK.Instance != null && _subscribedToMRUKEvents)
        {
            // Ensure this matches the registration method used in Start()
            MRUK.Instance.DeregisterSceneLoadedCallback(OnMRUKSceneLoaded);
            _subscribedToMRUKEvents = false;
            // Debug.Log("MountainFloorFlattener: Unregistered MRUK scene loaded callback.", this);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MountainFloorFlattener))]
public class MountainFloorFlattenerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); 

        MountainFloorFlattener flattener = (MountainFloorFlattener)target;
        if (GUILayout.Button("Flatten Mountain Meshes Now (Play Mode Only)"))
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Error", "Flattening can only be done in Play Mode when MRUK data is available.", "OK");
                return;
            }
            if (MRUK.Instance == null || !MRUK.Instance.IsSceneLoaded)
            {
                EditorUtility.DisplayDialog("Error", "MRUK Scene is not loaded. Please ensure Room Setup is complete and MRUK has loaded the scene in Play Mode.", "OK");
                return;
            }
            // Ensure OnMRUKSceneLoaded has run at least once if called via button
            // This is now handled better within FlattenAllMountainMeshes itself.
            flattener.FlattenAllMountainMeshes();
        }
    }
}
#endif*/