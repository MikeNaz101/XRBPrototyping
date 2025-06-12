using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter1 : MonoBehaviour
{
    public Transform destination;
    private Transform carrier;

    // Use this for initialization
    void Start()
    {
        // A helper object to smoothly carry the player or objects through the portal
        carrier = new GameObject(transform.parent.name + "-Carrier").transform;
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
        float destinationSpeed = velocity.magnitude * Mathf.Cos(angleToEntrance * Mathf.Deg2Rad);
        float destinationAngle = Vector3.Angle(destination.forward, Vector3.up);
        // This seems to be a custom game constant. Make sure you have this class defined.
        // float minDestinationSPeed = GameConstants.OUTGOING_PUSH_SPEED +
        //                             (GameConstants.OUTGOING_PUSH_SPEED_ANG_MULTIPLIER *
        //                             Mathf.Cos(destinationAngle * Mathf.Deg2Rad));

        // Using a fallback value since GameConstants is not defined here.
        float minDestinationSPeed = 5.0f;

        destinationSpeed = Mathf.Max(minDestinationSPeed, destinationSpeed);
        
        velocity = destination.forward * destinationSpeed;
        
        rb.linearVelocity = velocity;
        Transform toCarry = col.transform;

        // This seems to be a custom FirstPersonController script.
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
    
    // --- NEW METHOD FOR LASER TELEPORTATION ---
    /// <summary>
    /// Calculates the exit point and direction for a ray entering this portal.
    /// </summary>
    /// <param name="inPosition">The world-space point where the laser hits this portal.</param>
    /// <param name="inDirection">The world-space direction of the incoming laser.</param>
    /// <param name="outPosition">The calculated world-space exit point from the destination portal.</param>
    /// <param name="outDirection">The calculated world-space exit direction from the destination portal.</param>
    public void TeleportRay(Vector3 inPosition, Vector3 inDirection, out Vector3 outPosition, out Vector3 outDirection)
    {
        // Get the surface of the entrance portal (this one) and the exit portal
        Transform entrySurface = transform; // The Teleporter is usually on the surface
        Transform exitSurface = destination;

        // --- 1. Calculate the Exit Position ---
        // Convert the world hit position to the local space of the entrance portal's surface.
        Vector3 localPos = entrySurface.InverseTransformPoint(inPosition);

        // Invert the X and Z coordinates to mirror the position on the other side.
        // Y is typically the "up" axis for a portal on a wall, so it stays the same.
        localPos = new Vector3(-localPos.x, localPos.y, -localPos.z);
        
        // Convert the new local position back to world space, relative to the exit portal's surface.
        outPosition = exitSurface.TransformPoint(localPos);

        // --- 2. Calculate the Exit Direction ---
        // Convert the world direction to the local space of the entrance portal.
        Vector3 localDir = entrySurface.InverseTransformDirection(inDirection);

        // Invert the X and Z components of the direction.
        localDir = new Vector3(-localDir.x, localDir.y, -localDir.z);

        // Convert the new local direction back to world space, relative to the exit portal.
        outDirection = exitSurface.TransformDirection(localDir);
    }
}