// File: InteractionEventRelay.cs
// Purpose: Acts as a bridge between an XR interactable's events and the HiveInspectionGuideManager.
// Instructions: Attach this script to the SAME GameObject that has your XR interactable component
// (e.g., XR Grab Interactable from XRI, or a Meta Building Block interactable).
// Then, in the Inspector for that XR interactable component, hook up its relevant event
// (e.g., "Select Entered", "OnGrabbed", "OnActivated") to call the "NotifyInteraction" method on THIS script.

using UnityEngine;

public class InteractionEventRelay : MonoBehaviour
{
    [Tooltip("Assign the HiveInspectionGuideManager from your scene here.")]
    public HiveInspectionGuideManager guideManager;

    void Start()
    {
        if (guideManager == null)
        {
            // Try to find it if not assigned, good for dynamic setups but assignment is safer.
            guideManager = FindObjectOfType<HiveInspectionGuideManager>();
            if (guideManager == null)
            {
                Debug.LogError("InteractionEventRelay: HiveInspectionGuideManager not found in scene and not assigned!", this);
                enabled = false;
            }
        }
    }

    /// <summary>
    /// This public method should be called by the UnityEvent of your XR interactable component
    /// (e.g., OnSelectEntered, OnActivated, Meta's OnGrab, etc.).
    /// </summary>
    public void NotifyInteraction()
    {
        if (guideManager != null)
        {
            // Pass 'this.gameObject' to let the manager know WHICH object was interacted with.
            guideManager.ProcessInteraction(this.gameObject);
        }
        else
        {
            Debug.LogWarning("InteractionEventRelay: guideManager is null. Cannot process interaction.", this);
        }
    }
}