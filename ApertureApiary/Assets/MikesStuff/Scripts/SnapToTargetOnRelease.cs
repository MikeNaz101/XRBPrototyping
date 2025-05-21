// File: SnapToTargetOnRelease.cs
// Purpose: Allows a grabbable object to smoothly snap to a target position and rotation
// when released nearby, and optionally prevents it from affecting other objects while held/snapping.
// Instructions:
// 1. Attach this script to each GameObject that you want to be snappable (e.g., HiveLid, Frame1, Frame2).
// 2. Ensure these GameObjects also have an XRGrabInteractable component (from Unity's XRI or an equivalent
//    from Meta's SDK if that's what you're using for grabbing) and a Rigidbody.
// 3. Create an empty GameObject in your scene to act as the "snap target" for each snappable object.
//    Position and rotate this empty GameObject exactly where you want the snappable object to end up.
// 4. In the Inspector for your snappable object (e.g., HiveLid):
//    a. Drag the corresponding "snap target" empty GameObject into the 'Target Snap Transform' slot.
//    b. Adjust 'Snap Distance Threshold', 'Snap Rotation Dot Threshold', 'Snap Move Speed', and 'Snap Rotate Speed'.
//    c. Set 'Disable Colliders While Manipulated' if you want the object to pass through others.
//    d. If not already assigned, drag the XRGrabInteractable component from this GameObject
//       into the 'Grab Interactable' slot (the script will try to find it if not assigned).

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // For XRGrabInteractable and SelectExitEventArgs
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
    private bool _collidersWereDisabled = false;


    [Header("XR Interaction")]
    [Tooltip("Reference to the XRGrabInteractable component on this object.")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    private Rigidbody _rigidbody;
    private Coroutine _snappingCoroutine;
    private bool _isSnapped = false;
    private bool _isGrabbed = false;
    private bool _originalRigidbodyKinematicState;

    void Awake()
    {
        if (grabInteractable == null)
        {
            grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }

        if (grabInteractable == null)
        {
            Debug.LogError($"SnapToTargetOnRelease on {gameObject.name}: XRGrabInteractable component not found or assigned. Snapping will not work.", this);
            enabled = false;
            return;
        }

        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
        {
            Debug.LogError($"SnapToTargetOnRelease on {gameObject.name}: Rigidbody not found. This script requires a Rigidbody.", this);
            enabled = false;
            return;
        }
        _originalRigidbodyKinematicState = _rigidbody.isKinematic;

        _colliders = new List<Collider>();
        GetComponentsInChildren<Collider>(true, _colliders); // Get all colliders, including inactive ones if any
    }

    void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
        if (_snappingCoroutine != null)
        {
            StopCoroutine(_snappingCoroutine);
            _snappingCoroutine = null;
        }
        // Restore collider state if disabled and script is disabled/destroyed
        SetCollidersEnabled(true);
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (!disableCollidersWhileManipulated && enabled) return; // Only manage if we are supposed to
        if (disableCollidersWhileManipulated)
        {
            foreach (Collider col in _colliders)
            {
                if (col != null) col.enabled = enabled;
            }
            _collidersWereDisabled = !enabled;
        }
    }


    private void OnGrabbed(SelectEnterEventArgs args)
    {
        _isGrabbed = true;
        _isSnapped = false;
        if (_snappingCoroutine != null)
        {
            StopCoroutine(_snappingCoroutine);
            _snappingCoroutine = null;
        }

        if (_rigidbody != null)
        {
            // Store the original kinematic state before making it kinematic for grab
            // Note: XRGrabInteractable might also change this based on its MovementType.
            // This script will ensure it's kinematic for manipulation if desired.
            _originalRigidbodyKinematicState = _rigidbody.isKinematic;
            _rigidbody.isKinematic = true; // Make kinematic to prevent pushing other objects while held
        }
        SetCollidersEnabled(false); // Disable colliders if configured
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        if (targetSnapTransform == null)
        {
            SetCollidersEnabled(true); // Re-enable colliders if not snapping
            // Restore original kinematic state if not snapping and not kinematic by default by XRGrabInteractable
            if (_rigidbody != null && grabInteractable.movementType != UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Kinematic && grabInteractable.movementType != UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.Instantaneous)
            {
                 _rigidbody.isKinematic = _originalRigidbodyKinematicState;
            }
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, targetSnapTransform.position);
        float rotationAlignment = Vector3.Dot(transform.forward, targetSnapTransform.forward);

        if (distanceToTarget <= snapDistanceThreshold && rotationAlignment >= snapRotationDotThreshold)
        {
            if (_snappingCoroutine != null)
            {
                StopCoroutine(_snappingCoroutine);
            }
            // Colliders will be re-enabled at the end of the coroutine if they were disabled
            _snappingCoroutine = StartCoroutine(SnapToTargetCoroutine());
        }
        else
        {
            SetCollidersEnabled(true); // Re-enable colliders if not snapping
            if (_rigidbody != null)
            {
                // If not snapping, revert to original kinematic state or let XRGrabInteractable handle it.
                // If XRGrabInteractable's movement type is VelocityTracking, it expects non-kinematic.
                if (grabInteractable.movementType == UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking)
                {
                    _rigidbody.isKinematic = false;
                }
                else
                {
                    _rigidbody.isKinematic = _originalRigidbodyKinematicState; // Or true if it should remain kinematic
                }
            }
        }
    }

    private IEnumerator SnapToTargetCoroutine()
    {
        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = true; // Ensure kinematic during snap
        }
        // Colliders are already disabled by OnGrabbed if disableCollidersWhileManipulated is true.
        // If they weren't (e.g. if grabbed by a different system not calling OnGrabbed),
        // you might want to call SetCollidersEnabled(false) here too.

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

        // Rigidbody remains kinematic after snapping to stay in place.
        // If you want it to be affected by physics after snapping, set _rigidbody.isKinematic = false; here.
        if (_rigidbody != null) _rigidbody.isKinematic = true;
    }

    public bool IsSnapped()
    {
        return _isSnapped;
    }
}