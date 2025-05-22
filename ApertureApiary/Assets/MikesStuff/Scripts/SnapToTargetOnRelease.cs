// File: SnapToTargetOnRelease.cs
// Purpose: Allows a grabbable object to smoothly snap to a target position and rotation
// when released nearby. Designed to be triggered by UnityEvents from an external interaction system
// (like Meta's Interactable Unity Event Wrapper).
// Instructions:
// 1. Attach this script to each GameObject that you want to be snappable (e.g., HiveLid, Frame1, Frame2).
// 2. Ensure these GameObjects have a Rigidbody.
// 3. Create an empty GameObject in your scene to act as the "snap target" for each snappable object.
//    Position and rotate this empty GameObject exactly where you want the snappable object to end up.
// 4. In the Inspector for your snappable object (e.g., HiveLid):
//    a. Drag the corresponding "snap target" empty GameObject into the 'Target Snap Transform' slot.
//    b. Adjust 'Snap Distance Threshold', 'Snap Rotation Dot Threshold', 'Snap Move Speed', and 'Snap Rotate Speed'.
//    c. Set 'Disable Colliders While Manipulated' if you want the object to pass through others.
// 5. On your "Interactable Unity Event Wrapper" (or equivalent Meta Building Block event source):
//    a. Hook up its "Selected" (or "Grabbed") event to call the 'HandleGrabbed()' method on this script.
//    b. Hook up its "Unselected" (or "Released") event to call the 'HandleReleased()' method on this script.

using UnityEngine;
using System.Collections;
using System.Collections.Generic; // For List

public class SnapToTargetOnRelease : MonoBehaviour
{
    [Header("Snap Configuration")]
    [Tooltip("The transform representing the target position and rotation to snap to.")]
    public Transform targetSnapTransform;

    [Tooltip("How close the object needs to be to the target (in world units) to initiate snapping upon release.")]
    public float snapDistanceThreshold = 0.2f;

    [Tooltip("How close the object's forward vector needs to align with the target's forward vector (dot product) to snap. 1 = perfectly aligned, -1 = opposite. Use a value like 0.7 or higher.")]
    [Range(-1f,1f)]
    public float snapRotationDotThreshold = 0.7f;


    [Tooltip("How quickly the object moves to the target position.")]
    public float snapMoveSpeed = 8f;

    [Tooltip("How quickly the object rotates to the target rotation.")]
    public float snapRotateSpeed = 8f;

    [Header("Physics Handling")]
    [Tooltip("If true, the object's colliders will be disabled while it's grabbed and during the snapping process.")]
    public bool disableCollidersWhileManipulated = true;
    private List<Collider> _colliders;
    private bool _collidersWereDisabled = false; // Tracks if *this script* disabled them


    private Rigidbody _rigidbody;
    private Coroutine _snappingCoroutine;
    private bool _isSnapped = false;
    // private bool _isGrabbed = false; // No longer strictly needed by this script's internal logic, managed by external events

    void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError($"SnapToTargetOnRelease on {gameObject.name}: Rigidbody not found. This script requires a Rigidbody.", this);
            enabled = false;
            return;
        }

        _colliders = new List<Collider>();
        GetComponentsInChildren<Collider>(true, _colliders);
    }

    void OnDisable() // Changed from OnEnable/OnDisable for event listeners
    {
        if (_snappingCoroutine != null)
        {
            StopCoroutine(_snappingCoroutine);
            _snappingCoroutine = null;
        }
        // Restore collider state if disabled and script is disabled/destroyed
        if (_collidersWereDisabled) // Only re-enable if this script was the one to disable them
        {
            SetCollidersEnabled(true);
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (!disableCollidersWhileManipulated && enabled && !_collidersWereDisabled) return; // Only manage if we are supposed to or restoring our change

        if (disableCollidersWhileManipulated)
        {
            foreach (Collider col in _colliders)
            {
                if (col != null) col.enabled = enabled;
            }
            _collidersWereDisabled = !enabled;
        }
    }

    /// <summary>
    /// Public method to be called by an external event when the object is grabbed/selected.
    /// </summary>
    public void HandleGrabbed()
    {
        // _isGrabbed = true;
        _isSnapped = false;
        if (_snappingCoroutine != null)
        {
            StopCoroutine(_snappingCoroutine);
            _snappingCoroutine = null;
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true; // Make kinematic to prevent pushing other objects while held
        }
        SetCollidersEnabled(false); // Disable colliders if configured
        // Debug.Log($"{gameObject.name} Grabbed - Snapping script notified.");
    }

    /// <summary>
    /// Public method to be called by an external event when the object is released/unselected.
    /// </summary>
    public void HandleReleased()
    {
        // _isGrabbed = false;
        if (targetSnapTransform == null)
        {
            // Debug.LogWarning($"SnapToTargetOnRelease on {gameObject.name}: Target Snap Transform not assigned. Cannot snap.", this);
            RestorePhysicsStateIfNotSnapping();
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetSnapTransform.position);
        float rotationAlignment = Vector3.Dot(transform.forward, targetSnapTransform.forward);

        // Debug.Log($"Released {gameObject.name}. Dist: {distanceToTarget}, RotAlign: {rotationAlignment}");

        if (distanceToTarget <= snapDistanceThreshold && rotationAlignment >= snapRotationDotThreshold)
        {
            if (_snappingCoroutine != null)
            {
                StopCoroutine(_snappingCoroutine);
            }
            _snappingCoroutine = StartCoroutine(SnapToTargetCoroutine());
        }
        else
        {
            RestorePhysicsStateIfNotSnapping();
        }
    }

    private void RestorePhysicsStateIfNotSnapping()
    {
        SetCollidersEnabled(true); // Re-enable colliders if not snapping
        if (_rigidbody != null)
        {
            // If not snapping, allow Rigidbody to be non-kinematic for physics
            _rigidbody.isKinematic = false; // Or set to its original state if you stored it
        }
    }

    private IEnumerator SnapToTargetCoroutine()
    {
        // Debug.Log($"Snapping {gameObject.name} to {targetSnapTransform.name}");
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true; // Ensure kinematic during snap
        }
        // Colliders should have been disabled by HandleGrabbed if configured.

        while (Vector3.Distance(transform.position, targetSnapTransform.position) > 0.001f ||
               Quaternion.Angle(transform.rotation, targetSnapTransform.rotation) > 0.1f)
        {
            transform.position = Vector3.Lerp(transform.position, targetSnapTransform.position, Time.deltaTime * snapMoveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetSnapTransform.rotation, Time.deltaTime * snapRotateSpeed);
            yield return null;
        }

        transform.position = targetSnapTransform.position;
        transform.rotation = targetSnapTransform.rotation;

        _isSnapped = true;
        SetCollidersEnabled(true); // Re-enable colliders after snapping is complete
        _snappingCoroutine = null;
        // Debug.Log($"{gameObject.name} snapped successfully.");

        if (_rigidbody != null) _rigidbody.isKinematic = true; // Keep kinematic after snapping to stay in place.
    }

    public bool IsSnapped()
    {
        return _isSnapped;
    }
}