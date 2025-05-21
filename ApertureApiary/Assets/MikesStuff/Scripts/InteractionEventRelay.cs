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
            guideManager = FindFirstObjectByType<HiveInspectionGuideManager>();
            if (guideManager == null)
            {
                Debug.LogError("InteractionEventRelay: HiveInspectionGuideManager not found in scene and not assigned!", this);
                enabled = false;
            }
        }
    }

    public void NotifyInteraction()
    {
        if (guideManager != null)
        {
            guideManager.ProcessInteraction(this.gameObject);
        }
        else
        {
            Debug.LogWarning("InteractionEventRelay: guideManager is null. Cannot process interaction.", this);
        }
    }
}