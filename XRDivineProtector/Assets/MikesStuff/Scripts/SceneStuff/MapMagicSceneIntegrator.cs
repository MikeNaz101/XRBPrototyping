using UnityEngine;
//using UnityEngine.AI.Navigation; // Required for NavMeshSurface
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation; // For LINQ operations like FirstOrDefault

// You'll need to ensure you have the correct namespace for MapMagic.
// This might be MapMagic.Core, Den.Tools.Matrices etc. depending on what you need to access.
// For basic control, MapMagic.Core.MapMagicObject is usually the main one.
#if MAPMAGIC2 // Conditional compilation to prevent errors if MapMagic is not present
using MapMagic.Core;
using Den.Tools; // For Vector2D if MapMagic uses it for area size
#endif

/// <summary>
/// Integrates MapMagic 2 with Meta Quest Scene API data.
/// 1. Finds a specified real-world surface (e.g., a TABLE).
/// 2. Configures and positions a MapMagic graph to generate terrain on that surface.
/// 3. Triggers MapMagic generation.
/// 4. After MapMagic completes, triggers runtime NavMesh baking on the generated terrain.
///
/// Attach this script to a GameObject in your scene, e.g., a "WorldGenerationManager".
/// </summary>
public class MapMagicSceneIntegrator : MonoBehaviour
{
    [Header("Scene Query Settings")]
    [Tooltip("The semantic label of the surface to find (e.g., TABLE, FLOOR). Case-insensitive.")]
    public string targetSurfaceLabel = "TABLE";
    [Tooltip("Delay in seconds before attempting to query scene data and start generation.")]
    public float initialDelay = 2.0f;

    [Header("MapMagic 2 Integration")]
#if MAPMAGIC2
    [Tooltip("Assign your MapMagic Object (the GameObject with the MapMagic Graph component) here.")]
    public MapMagicObject mapMagicInstance;
#else
    [Tooltip("MapMagic 2 is not detected in this project. Please import MapMagic 2.")]
    public GameObject mapMagicInstancePlaceholder; // To avoid compile errors if MM isn't there
#endif
    [Tooltip("The NavMeshSurface component that will be used to bake the NavMesh on the MapMagic terrain.")]
    public NavMeshSurface navMeshSurfaceToBake;
    [Tooltip("Offset to apply to the MapMagic terrain relative to the found surface's center. Useful for Y-axis adjustments.")]
    public Vector3 mapMagicOffset = new Vector3(0, 0.01f, 0); // Slight Y offset to avoid Z-fighting
    [Tooltip("Default dimensions (Width, Length) for MapMagic terrain if the surface dimensions cannot be determined or are unsuitable. MapMagic typically uses X and Z for terrain plane.")]
    public Vector2 defaultMapMagicSize = new Vector2(2f, 2f); // e.g., 2m x 2m
    [Tooltip("Height of the MapMagic terrain volume. This is MapMagic's own height parameter for its generation space.")]
    public float mapMagicTerrainHeight = 50f; // Adjust based on your graph's needs


    private OVRSceneAnchor _foundSurfaceAnchor;
    private bool _isProcessing = false;

    void Start()
    {
#if MAPMAGIC2
        if (mapMagicInstance == null)
        {
            Debug.LogError("MapMagicSceneIntegrator: MapMagicObject instance is not assigned!", this);
            enabled = false;
            return;
        }
        if (navMeshSurfaceToBake == null)
        {
            Debug.LogError("MapMagicSceneIntegrator: NavMeshSurface to Bake is not assigned!", this);
            enabled = false;
            return;
        }
        StartCoroutine(InitializeGenerationSequence());
#else
        Debug.LogError("MapMagicSceneIntegrator: MapMagic 2 is required for this script to function. Please import MapMagic 2 from the Asset Store.", this);
        enabled = false;
#endif
    }

    private IEnumerator InitializeGenerationSequence()
    {
        if (_isProcessing) yield break;
        _isProcessing = true;

        Debug.Log("MapMagicSceneIntegrator: Starting generation sequence...", this);
        yield return new WaitForSeconds(initialDelay);

        Debug.Log($"Attempting to find surface with label: {targetSurfaceLabel}", this);
        _foundSurfaceAnchor = FindTargetSurface(targetSurfaceLabel.ToUpperInvariant());

        if (_foundSurfaceAnchor == null)
        {
            Debug.LogWarning($"No suitable surface found with label '{targetSurfaceLabel}'. MapMagic generation will not proceed on a specific surface.", this);
            _isProcessing = false;
            yield break;
        }

        Debug.Log($"Found surface: {_foundSurfaceAnchor.gameObject.name} with UUID: {_foundSurfaceAnchor.Uuid}", _foundSurfaceAnchor.gameObject);

        PositionAndConfigureMapMagic(_foundSurfaceAnchor);

        Debug.Log("Triggering MapMagic generation...", this);
#if MAPMAGIC2
        // --- !!! IMPORTANT: Triggering MapMagic Generation !!! ---
        // The line `mapMagicInstance.Generate(null);` might be incorrect for your MapMagic version.
        // Consult your MapMagic 2 documentation for the correct scripting API to trigger generation.
        // Common alternatives include:
        // mapMagicInstance.Generate();
        // mapMagicInstance.ForceGenerate();
        // mapMagicInstance.graph.Generate(); // If the graph object handles it
        // Or it might be a method that starts a coroutine.

        // For now, we'll keep the original attempt, but you MUST verify this.
        if (mapMagicInstance != null)
        {
            mapMagicInstance.graph.Generate(null); // Replace this line if it's incorrect for your MM version.
        }
        else
        {
            Debug.LogError("MapMagic instance is null before trying to generate!", this);
            _isProcessing = false;
            yield break;
        }


        // Wait for MapMagic to finish generating
        // This also depends on MapMagic's API. `IsGenerating()` is a common pattern.
        // Some versions might use `isGenerating` (lowercase 'i'), or an event/callback.
        float timeout = Time.time + 60f; // 60 second timeout for generation
        bool stillGenerating = true;

        if (mapMagicInstance.graph == null)
        {
            Debug.LogError("MapMagic graph is null, cannot reliably check if generation is complete via standard IsGenerating(). Assuming it might take a fixed time or complete quickly.", this);
            // As a fallback, you might just wait a fixed short duration if graph is null,
            // but this is not ideal.
            yield return new WaitForSeconds(5f); // Fallback wait
            stillGenerating = false; // Assume done after fallback
        }


        // Check IsGenerating() only if graph is not null and mapMagicInstance is valid
        while (mapMagicInstance != null && mapMagicInstance.graph != null && stillGenerating && mapMagicInstance.IsGenerating())
        {
            if (Time.time > timeout)
            {
                Debug.LogError("MapMagic generation timed out!", this);
                _isProcessing = false;
                yield break;
            }
            // Debug.Log("MapMagic is generating...", this); // Can be spammy
            yield return null; // Wait for the next frame
        }
        // If the loop exited because IsGenerating() returned false or graph was null and we used fallback.
        Debug.Log("MapMagic generation assumed complete.", this);
#endif

        Debug.Log("Starting NavMesh bake...", this);
        if (navMeshSurfaceToBake != null)
        {
            if (navMeshSurfaceToBake.transform != mapMagicInstance.transform && navMeshSurfaceToBake.collectObjects == CollectObjects.Children)
            {
                 Debug.LogWarning("NavMeshSurface is set to collect children but is not on the MapMagic instance. Consider parenting it correctly or using a Volume.", this);
            }
            navMeshSurfaceToBake.BuildNavMesh();
            Debug.Log("NavMesh bake complete.", this);
        }
        else
        {
            Debug.LogError("NavMeshSurface reference is null, cannot bake NavMesh.", this);
        }

        _isProcessing = false;
    }

    private OVRSceneAnchor FindTargetSurface(string labelToUpper)
    {
        OVRSceneAnchor[] anchors = FindObjectsOfType<OVRSceneAnchor>();
        List<OVRSceneAnchor> candidates = new List<OVRSceneAnchor>();

        foreach (OVRSceneAnchor anchor in anchors)
        {
            OVRSemanticClassification classification = anchor.GetComponent<OVRSemanticClassification>();
            if (classification != null)
            {
                foreach (string semanticLabel in classification.Labels)
                {
                    if (semanticLabel.ToUpperInvariant() == labelToUpper)
                    {
                        candidates.Add(anchor);
                        break;
                    }
                }
            }
        }

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        return candidates
            .Where(a => {
                OVRScenePlane plane = a.GetComponent<OVRScenePlane>();
                return plane != null && Mathf.Abs(Vector3.Dot(a.transform.up, Vector3.up)) > 0.85f; // Check if it's reasonably horizontal
            })
            .OrderByDescending(a => a.GetComponent<OVRScenePlane>().Dimensions.x * a.GetComponent<OVRScenePlane>().Dimensions.y)
            .FirstOrDefault();
    }

    private void PositionAndConfigureMapMagic(OVRSceneAnchor surfaceAnchor)
    {
#if MAPMAGIC2
        if (mapMagicInstance == null || surfaceAnchor == null) return;

        mapMagicInstance.transform.position = surfaceAnchor.transform.position + mapMagicOffset;
        mapMagicInstance.transform.rotation = surfaceAnchor.transform.rotation;

        OVRScenePlane plane = surfaceAnchor.GetComponent<OVRScenePlane>();
        Vector2 mapSize = defaultMapMagicSize;

        if (plane != null)
        {
            mapSize = new Vector2(plane.Dimensions.x, plane.Dimensions.y);
            Debug.Log($"Using surface plane dimensions for MapMagic: {mapSize.x}m x {mapSize.y}m", this);
        }
        else
        {
            Debug.LogWarning($"No OVRScenePlane found on anchor '{surfaceAnchor.gameObject.name}'. Using default MapMagic size: {defaultMapMagicSize.x}m x {defaultMapMagicSize.y}m", this);
        }

        mapSize.x = Mathf.Max(mapSize.x, 0.5f);
        mapSize.y = Mathf.Max(mapSize.y, 0.5f);

        if (mapMagicInstance.graph != null)
        {
            Debug.LogWarning("MapMagicSceneIntegrator: Setting MapMagic area size via script is highly version-dependent. " +
                             "The following is a placeholder. Please verify with your MapMagic version's API. " +
                             "You might need to directly access graph properties or use a specific API function. " +
                             "Commonly, you'd find an 'Area' or 'Size' property on the graph or a main generator node.", this);
            // --- !!! USER ACTION REQUIRED: Correctly set MapMagic Size & Height via API !!! ---
            // Example (PSEUDO-CODE - LIKELY NEEDS ADJUSTMENT):
            // if (mapMagicInstance.graph.HasVar("terrainSize")) // Check if a variable exists
            // {
            //    mapMagicInstance.graph.SetVar("terrainSize", new Vector2D(mapSize.x, mapSize.y));
            // }
            // else if (mapMagicInstance.graph.area != null) // Check for an 'area' property
            // {
            //    mapMagicInstance.graph.area.pixelSize = new Vector2D(mapSize.x, mapSize.y); // Or .worldSize depending on API
            // }
            // else
            // {
            //    // Fallback: Try to find a "Size" property on the MapMagicObject component itself if it exists
            //    // This is less common for graph-based systems but worth checking if desperate.
            //    // var sizeProperty = mapMagicInstance.GetType().GetProperty("size");
            //    // if (sizeProperty != null) sizeProperty.SetValue(mapMagicInstance, new Vector2D(mapSize.x, mapSize.y));
            // }
            //
            // mapMagicInstance.graph.SetValue("terrainHeight", mapMagicTerrainHeight); // Example for height
            // --- !!! END USER ACTION REQUIRED !!! ---
            Debug.Log($"Attempting to configure MapMagic (target size: {mapSize.x}x{mapSize.y}, height: {mapMagicTerrainHeight}). This part needs to be correct for your MM version.", this);
        }
        else
        {
            Debug.LogError("MapMagic instance does not have a graph assigned! Cannot configure area settings.", this);
        }
#endif
    }

    public void RegenerateWorld()
    {
        if (!_isProcessing)
        {
            Debug.Log("RegenerateWorld called.", this);
            StartCoroutine(InitializeGenerationSequence());
        }
        else
        {
            Debug.LogWarning("Cannot regenerate world, a process is already ongoing.", this);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            RegenerateWorld();
        }
    }
}
