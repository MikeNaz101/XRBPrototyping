using Oculus.Interaction;
using Oculus.Interaction.Input.Visuals;
using UnityEngine;

/// <summary>
/// Replaces the default Meta controller or hand model with a custom prefab (e.g., a gun).
/// This script is designed for the Meta XR SDK and its Building Blocks.
/// It works by finding and disabling the OVRControllerVisual or OVRHandVisual components.
/// </summary>
public class HandModelReplacer_Meta : MonoBehaviour
{
    [Header("Prefab to Spawn")]
    [Tooltip("The gun, tool, or other object to display instead of the hand.")]
    public GameObject itemPrefab;

    [Header("Offset Adjustments")]
    [Tooltip("Fine-tune the position of the item relative to the controller.")]
    public Vector3 positionOffset = Vector3.zero;

    [Tooltip("Fine-tune the rotation of the item relative to the controller.")]
    public Vector3 rotationOffset = Vector3.zero;

    // The object that gets spawned.
    private GameObject spawnedItem;

    void Start()
    {
        // Ensure we have a prefab to work with
        if (itemPrefab == null)
        {
            Debug.LogError("Item Prefab is not set on " + gameObject.name, this);
            return;
        }

        // --- Hide the default Meta controller/hand model ---
        // We search in children because the visual components are often on a different GameObject.
        
        // Disable the controller model visual
        ControllerVisual controllerVisual = GetComponentInChildren<ControllerVisual>();
        if (controllerVisual != null)
        {
            controllerVisual.enabled = false;
            Debug.Log("Disabled OVRControllerVisual on: " + controllerVisual.gameObject.name);
        }

        // Also disable the hand tracking visual, if present
        HandVisual handVisual = GetComponentInChildren<HandVisual>();
        if (handVisual != null)
        {
            handVisual.enabled = false;
            Debug.Log("Disabled OVRHandVisual on: " + handVisual.gameObject.name);
        }
        
        // Log a warning if we couldn't find either component
        if (controllerVisual == null && handVisual == null)
        {
            Debug.LogWarning("Could not find OVRControllerVisual or OVRHandVisual in children. The default model may still be visible. You may need to hide it manually.", this);
        }

        // --- Spawn and position the custom item ---
        // Instantiate the gun prefab and parent it to this controller's transform.
        spawnedItem = Instantiate(itemPrefab, transform);

        // Apply the position and rotation offsets.
        // Use localPosition and localRotation to ensure the item moves with the controller.
        spawnedItem.transform.localPosition = positionOffset;
        spawnedItem.transform.localRotation = Quaternion.Euler(rotationOffset);

        Debug.Log("Successfully spawned " + itemPrefab.name + " on " + gameObject.name);
    }
}
