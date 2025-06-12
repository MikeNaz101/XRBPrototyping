using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;

/// <summary>
/// Procedurally decorates the MRUK scene to resemble a science lab by applying new materials
/// to existing surfaces, effectively "re-skinning" the room.
/// </summary>
public class LabDecorator : MonoBehaviour
{
    [Header("Reskinning Materials")]
    [Tooltip("The material to apply to all furniture objects (tables, couches, etc.).")]
    public Material furnitureReskinMaterial;
    [Tooltip("The material to apply to all wall surfaces.")]
    public Material wallReskinMaterial;
    [Tooltip("The material to apply to the ceiling.")]
    public Material ceilingReskinMaterial;
    [Tooltip("The material to apply to the floor.")]
    public Material floorReskinMaterial;

    [Header("Floor Decoration (Optional)")]
    [Tooltip("(Optional) A prefab for floor panels. If assigned, this will be placed on top of the reskinned floor.")]
    public GameObject floorPanelPrefab;
    [Tooltip("The spacing for the floor panel grid. Only used if a Floor Panel Prefab is assigned.")]
    public float floorGridSize = 1.0f;

    // --- Private Variables ---
    private bool hasDecorated = false;

    void Start()
    {
        if (MRUK.Instance == null)
        {
            Debug.LogError($"[{nameof(LabDecorator)}] MRUK.Instance is null. Ensure an MRUK object is in the scene.", this);
            enabled = false;
            return;
        }

        // Subscribe to the scene loaded event.
        MRUK.Instance.SceneLoadedEvent.AddListener(DecorateScene);

        // If the scene is already loaded, decorate immediately.
        if (MRUK.Instance.IsInitialized)
        {
            DecorateScene();
        }
    }

    private void OnDisable()
    {
        if (MRUK.Instance)
        {
            MRUK.Instance.SceneLoadedEvent.RemoveListener(DecorateScene);
        }
    }

    /// <summary>
    /// The main method that triggers the decoration process for the entire room.
    /// </summary>
    void DecorateScene()
    {
        if (hasDecorated) return;
        Debug.Log($"[{nameof(LabDecorator)}] DecorateScene called. Beginning reskinning process.");

        var mrukRoom = MRUK.Instance.GetCurrentRoom();
        if (mrukRoom == null)
        {
            Debug.LogError($"[{nameof(LabDecorator)}] Could not find the current room.", this);
            return;
        }
        
        // Create a parent GameObject to keep the hierarchy clean for any spawned objects.
        Transform decorationParent = new GameObject("FloorDecorations").transform;
        decorationParent.SetParent(this.transform);

        // --- FIX: Convert the furniture labels to a List<string> to match the HasAnyLabel method signature ---
        List<string> furnitureLabelStrings = new List<string>
        {
            MRUKAnchor.SceneLabels.TABLE.ToString(),
            MRUKAnchor.SceneLabels.COUCH.ToString(),
            MRUKAnchor.SceneLabels.STORAGE.ToString(),
            MRUKAnchor.SceneLabels.BED.ToString(),
            MRUKAnchor.SceneLabels.SCREEN.ToString()
        };

        // Iterate through all anchors and reskin them based on their labels.
        foreach (var anchor in mrukRoom.Anchors)
        {
            if (anchor.HasAnyLabel(furnitureLabelStrings))
            {
                ReskinAnchor(anchor, furnitureReskinMaterial, "Furniture");
            }
            else if (anchor.HasLabel(MRUKAnchor.SceneLabels.FLOOR.ToString()))
            {
                ReskinAnchor(anchor, floorReskinMaterial, "Floor");
                // Optionally, add a grid of panels on top of the reskinned floor.
                if (floorPanelPrefab)
                {
                    DecorateFloorWithPanels(anchor, decorationParent);
                }
            }
            else if (anchor.HasLabel(MRUKAnchor.SceneLabels.WALL_FACE.ToString()))
            {
                ReskinAnchor(anchor, wallReskinMaterial, "Wall");
            }
            else if (anchor.HasLabel(MRUKAnchor.SceneLabels.CEILING.ToString()))
            {
                ReskinAnchor(anchor, ceilingReskinMaterial, "Ceiling");
            }
        }

        hasDecorated = true;
        Debug.Log($"[{nameof(LabDecorator)}] Decoration process complete.");
    }
    
    /// <summary>
    /// Finds the MeshRenderer on an anchor's visualization and applies a new material to it.
    /// </summary>
    /// <param name="anchor">The MRUKAnchor to reskin.</param>
    /// <param name="newMaterial">The Material to apply.</param>
    /// <param name="surfaceType">A string for debugging purposes.</param>
    void ReskinAnchor(MRUKAnchor anchor, Material newMaterial, string surfaceType)
    {
        // If no material is assigned for this type, do nothing.
        if (newMaterial == null) return;

        // Directly search for the MeshRenderer component on the anchor object and its children.
        var renderer = anchor.GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
        {
            // Simply assign the new material to the existing mesh renderer.
            renderer.material = newMaterial;
            Debug.Log($"Reskinned '{anchor.name}' ({surfaceType}) with '{newMaterial.name}'.", anchor);
        }
        else
        {
            Debug.LogWarning($"Could not find a MeshRenderer on anchor '{anchor.name}' to reskin.", anchor);
        }
    }

    /// <summary>
    /// Spawns floor panels in a grid pattern on a floor anchor.
    /// </summary>
    void DecorateFloorWithPanels(MRUKAnchor floorAnchor, Transform parent)
    {
        if (!floorPanelPrefab || !floorAnchor.PlaneRect.HasValue) return;

        Rect planeRect = floorAnchor.PlaneRect.Value;
        int gridX = Mathf.FloorToInt(planeRect.width / floorGridSize);
        int gridY = Mathf.FloorToInt(planeRect.height / floorGridSize);

        for (int i = 0; i < gridX; i++)
        {
            for (int j = 0; j < gridY; j++)
            {
                float xPos = -planeRect.width / 2 + (i + 0.5f) * floorGridSize;
                float yPos = -planeRect.height / 2 + (j + 0.5f) * floorGridSize;

                Vector3 localPos = new Vector3(xPos, yPos, 0);
                Vector3 worldPos = floorAnchor.transform.TransformPoint(localPos);
                
                // Offset slightly to prevent Z-fighting with the original floor mesh.
                worldPos += floorAnchor.transform.forward * 0.005f;

                Quaternion rotation = Quaternion.LookRotation(floorAnchor.transform.forward, floorAnchor.transform.up);
                
                Instantiate(floorPanelPrefab, worldPos, rotation, parent);
            }
        }
    }
}
