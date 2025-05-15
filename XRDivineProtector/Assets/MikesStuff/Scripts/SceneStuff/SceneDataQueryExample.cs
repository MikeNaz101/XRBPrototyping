using UnityEngine;
using System.Collections.Generic; // Required for List
using System.Text; // Required for StringBuilder

/// <summary>
/// Example script demonstrating how to find and query OVRSceneAnchors
/// and their OVRSemanticClassification components.
///
/// This script assumes that OVRSceneManager (or a similar mechanism like a Scene Building Block)
/// has already loaded the scene data from the Meta Quest device.
///
/// You would typically attach this to a GameObject in your scene, perhaps a dedicated
/// "SceneQueryManager" or integrate parts of this logic into your procedural generation system.
/// </summary>
public class SceneDataQueryExample : MonoBehaviour
{
    [Header("Query Settings")]
    [Tooltip("If true, will automatically query scene data on Start.")]
    public bool queryOnStart = true;
    [Tooltip("If true, will log details of every anchor found. Can be verbose.")]
    public bool logAllAnchorDetails = false;

    [Header("Specific Semantic Labels to Find (Examples)")]
    public List<string> targetSemanticLabels = new List<string> { "TABLE", "FLOOR", "WALL_FACE" };

    private List<OVRSceneAnchor> foundSceneAnchors = new List<OVRSceneAnchor>();

    void Start()
    {
        if (queryOnStart)
        {
            // It's often good practice to wait a brief moment for OVRSceneManager
            // to fully initialize and load any existing scene data, especially on app start.
            // For a more robust solution, you might listen to an event from OVRSceneManager
            // indicating that scene loading is complete.
            Invoke(nameof(QueryAllSceneAnchors), 1.0f);
        }
    }

    /// <summary>
    /// Finds all active OVRSceneAnchor components in the scene.
    /// This is a simple way to get them; OVRSceneManager might offer more direct access.
    /// </summary>
    public void QueryAllSceneAnchors()
    {
        foundSceneAnchors.Clear();
        // FindObjectsOfType can be slow if there are many objects.
        // A more optimized approach might involve OVRSceneManager providing a list or events.
        OVRSceneAnchor[] anchors = FindObjectsOfType<OVRSceneAnchor>();

        if (anchors == null || anchors.Length == 0)
        {
            Debug.LogWarning("No OVRSceneAnchors found in the scene. Ensure Scene data has been loaded.", this);
            return;
        }

        Debug.Log($"Found {anchors.Length} OVRSceneAnchors in the scene.", this);

        foreach (OVRSceneAnchor anchor in anchors)
        {
            foundSceneAnchors.Add(anchor);
            ProcessSceneAnchor(anchor);
        }

        FindSpecificSemanticAnchors();
    }

    /// <summary>
    /// Processes an individual OVRSceneAnchor and logs its details.
    /// </summary>
    /// <param name="anchor">The OVRSceneAnchor to process.</param>
    private void ProcessSceneAnchor(OVRSceneAnchor anchor)
    {
        if (anchor == null) return;

        StringBuilder anchorDetails = new StringBuilder();
        anchorDetails.AppendLine($"--- Anchor Details ---");
        anchorDetails.AppendLine($"Anchor GameObject Name: {anchor.gameObject.name}");
        anchorDetails.AppendLine($"Anchor UUID: {anchor.Uuid}");

        // Get OVRSemanticClassification component
        OVRSemanticClassification classification = anchor.GetComponent<OVRSemanticClassification>();
        if (classification != null)
        {
            // OVRSemanticClassification stores labels as a read-only list of strings.
            // Typically, an anchor has one primary semantic label, but the API supports multiple.
            if (classification.Labels.Count > 0)
            {
                anchorDetails.Append("Semantic Labels: ");
                for (int i = 0; i < classification.Labels.Count; i++)
                {
                    anchorDetails.Append(classification.Labels[i]);
                    if (i < classification.Labels.Count - 1)
                    {
                        anchorDetails.Append(", ");
                    }
                }
                anchorDetails.AppendLine();
            }
            else
            {
                anchorDetails.AppendLine("Semantic Labels: None");
            }
        }
        else
        {
            anchorDetails.AppendLine("OVRSemanticClassification: Not found on this anchor.");
        }

        // You can also check for other components that define the anchor's geometry, like:
        // OVRScenePlane, OVRSceneVolume, OVRRoomLayout, OVRTriangleMesh etc.

        OVRScenePlane plane = anchor.GetComponent<OVRScenePlane>();
        if (plane != null)
        {
            anchorDetails.AppendLine($"Type: Scene Plane | Dimensions: {plane.Dimensions} | Offset: {plane.Offset}");
        }

        OVRSceneVolume volume = anchor.GetComponent<OVRSceneVolume>();
        if (volume != null)
        {
            anchorDetails.AppendLine($"Type: Scene Volume | Dimensions: {volume.Dimensions} | Offset: {volume.Offset}");
        }

        if (logAllAnchorDetails)
        {
            Debug.Log(anchorDetails.ToString(), anchor.gameObject);
        }
    }

    /// <summary>
    /// Finds and logs anchors that match the targetSemanticLabels list.
    /// </summary>
    public void FindSpecificSemanticAnchors()
    {
        if (foundSceneAnchors.Count == 0 && !queryOnStart)
        {
            Debug.LogWarning("No scene anchors loaded yet. Call QueryAllSceneAnchors first or enable queryOnStart.", this);
            // Optionally, call QueryAllSceneAnchors here if desired
            // QueryAllSceneAnchors();
            // if (foundSceneAnchors.Count == 0) return; // Still none, then exit
        }


        Debug.Log($"--- Searching for Specific Semantic Labels: {string.Join(", ", targetSemanticLabels)} ---", this);

        foreach (OVRSceneAnchor anchor in foundSceneAnchors)
        {
            OVRSemanticClassification classification = anchor.GetComponent<OVRSemanticClassification>();
            if (classification != null)
            {
                foreach (string label in classification.Labels)
                {
                    if (targetSemanticLabels.Contains(label.ToUpperInvariant())) // Compare case-insensitively
                    {
                        Debug.Log($"Found target anchor: '{anchor.gameObject.name}' with label: '{label}'. Position: {anchor.transform.position}", anchor.gameObject);
                        // --- YOUR GAME LOGIC HERE ---
                        // Example: If it's a "TABLE", you might instantiate your village base on it.
                        // if (label.ToUpperInvariant() == "TABLE")
                        // {
                        //    InstantiateYourVillage(anchor.transform, plane.Dimensions); // Assuming you get plane dimensions
                        // }
                        break; // Found a matching label for this anchor, move to the next anchor
                    }
                }
            }
        }
    }

    // Example method for game logic
    // private void InstantiateYourVillage(Transform anchorTransform, Vector2 planeDimensions)
    // {
    //     // Your logic to place the village based on the anchor's transform and plane size
    //     Debug.Log($"Placeholder: Would instantiate village on {anchorTransform.name} with size {planeDimensions}");
    // }

    // Update is called once per frame - useful for testing with a key press
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Press Space to re-query
        {
            Debug.Log("Manual query triggered by Spacebar.", this);
            QueryAllSceneAnchors();
        }
    }
}
