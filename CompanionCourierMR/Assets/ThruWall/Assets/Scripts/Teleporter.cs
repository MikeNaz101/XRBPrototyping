using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{

    public Transform destination;

    [Header("Trigger Settings")]
    [Tooltip("The layer(s) of objects that will be teleported when they enter this trigger.")]
    public LayerMask teleportLayer;

    private Transform carrier;

    // Use this for initialization
    void Start()
    {
        carrier = new GameObject(transform.parent.name + "-Carrier").transform;

        // --- NEW: Ensure there is a trigger collider on this object ---
        BoxCollider trigger = GetComponent<BoxCollider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<BoxCollider>();
        }
        trigger.isTrigger = true;
        // Make the trigger volume extend in front of the portal surface.
        // You can adjust these values if your portal is a different size.
        trigger.size = new Vector3(1.5f, 2.5f, 1f); 
        trigger.center = new Vector3(0, 0, 0.6f);
    }

    /// <summary>
    /// This method is now called automatically when a valid object enters the trigger.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is on a layer we should teleport.
        // This uses a bitwise operation to see if the object's layer is in our mask.
        if ((teleportLayer.value & (1 << other.gameObject.layer)) > 0)
        {
            Debug.Log($"'{other.name}' entered the teleport trigger. Attempting to teleport.");
            // If it is, call the existing teleport logic for that object.
            TeleportCollider(other);
        }
    }

    public void TeleportCollider(Collider col)
    {
        Rigidbody rb = col.GetComponent<Rigidbody>();
        if (rb == null)
        {
            return;
        }
        Vector3 velocity = rb.linearVelocity;

        float angleToEntrance = Vector3.Angle(-velocity, transform.forward);
        if (angleToEntrance > 90.0f)
        {
            return;
        }

        // The following lines seem to reference a custom 'GameConstants' class.
        // Since I don't have that class, I've commented them out but left the structure.
        // You may need to uncomment or adjust these based on your project's constants.
        // float destinationSpeed = velocity.magnitude * Mathf.Cos(angleToEntrance * Mathf.Deg2Rad);
        // float destinationAngle = Vector3.Angle(destination.forward, Vector3.up);
        // float minDestinationSPeed = GameConstants.OUTGOING_PUSH_SPEED +
        //                             (GameConstants.OUTGOING_PUSH_SPEED_ANG_MULTIPLIER *
        //                             Mathf.Cos(destinationAngle * Mathf.Deg2Rad));
        // destinationSpeed = Mathf.Max(minDestinationSPeed, destinationSpeed);
        // velocity = destination.forward * destinationSpeed;
        
        rb.linearVelocity = velocity;
        Transform toCarry = col.transform;

        // The following lines seem to reference a custom 'FirstPersonController' script.
        // I have commented them out but left the structure.
        // FirstPersonController controller = col.GetComponentInChildren<FirstPersonController>();
        // Vector3 originalCamPos = Vector3.zero;
        // if (controller != null)
        // {
        //     toCarry = controller.GetComponentInChildren<Camera>().transform;
        //     originalCamPos = toCarry.localPosition;
        // }

        carrier.position = transform.position + transform.forward;
        carrier.LookAt(transform.position, transform.up);
        toCarry.SetParent(carrier);
        carrier.position = destination.position;
        carrier.LookAt(destination.position + destination.forward);
        toCarry.SetParent(null);

        // if (controller != null)
        // {
        //     controller.transform.position = toCarry.position;
        //     Vector3 lookPos = toCarry.position + toCarry.forward;
        //     lookPos.y = controller.transform.position.y;
        //     controller.transform.LookAt(lookPos);
        //     controller.transform.Translate(-originalCamPos);
        //     toCarry.parent = controller.transform;
        //     toCarry.localPosition = originalCamPos;
        //     controller.transform.Translate(destination.forward, Space.World);
        // }
    }

    // --- SIMPLIFIED METHOD FOR LASER TELEPORTATION ---
    /// <summary>
    /// Calculates a simplified exit point and direction for a laser entering this portal.
    /// </summary>
    public void TeleportRay(Vector3 inPosition, Vector3 inDirection, out Vector3 outPosition, out Vector3 outDirection)
    {
        Transform entryPortal = transform;
        Transform exitPortal = destination;

        // --- Step 1: Calculate the Exit Position ---
        // To find the "same point" on the other portal, we convert the world hit position
        // into a local coordinate relative to the entrance portal's surface.
        Vector3 localPosition = entryPortal.InverseTransformPoint(inPosition);
        
        // We then apply that same local coordinate to the exit portal to find the
        // corresponding world position for the new laser.
        // To make it look like a mirror, we invert the local X axis.
        localPosition.x = -localPosition.x;

        outPosition = exitPortal.TransformPoint(localPosition);

        // --- Step 2: Calculate the Exit Direction ---
        // For simplicity, the new laser will always fire straight out from the exit portal,
        // in the direction of its "forward" vector.
        outDirection = exitPortal.forward;
    }
}