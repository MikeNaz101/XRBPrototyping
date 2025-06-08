using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleporter : MonoBehaviour
{

    public Transform destination;

    private Transform carrier;

    // Use this for initialization
    void Start()
    {
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
        float minDestinationSPeed = GameConstants.OUTGOING_PUSH_SPEED +
                                    (GameConstants.OUTGOING_PUSH_SPEED_ANG_MULTIPLIER *
                                    Mathf.Cos(destinationAngle * Mathf.Deg2Rad));

        destinationSpeed = Mathf.Max(minDestinationSPeed, destinationSpeed);
        
        velocity = destination.forward * destinationSpeed;
        
        rb.linearVelocity = velocity;
        Transform toCarry = col.transform;

        FirstPersonController controller = col.GetComponentInChildren<FirstPersonController>();
        Vector3 originalCamPos = Vector3.zero;
        if (controller != null)
        {
            toCarry = controller.GetComponentInChildren<Camera>().transform;
            originalCamPos = toCarry.localPosition;
        }

        carrier.position = transform.position + transform.forward;
        carrier.LookAt(transform.position, transform.up);
        toCarry.parent = carrier;
        carrier.position = destination.position;
        carrier.LookAt(destination.position + destination.forward);
        toCarry.parent = null;

        if (controller != null)
        {
            controller.transform.position = toCarry.position;
            Vector3 lookPos = toCarry.position + toCarry.forward;
            lookPos.y = controller.transform.position.y;
            controller.transform.LookAt(lookPos);
            controller.transform.Translate(-originalCamPos);
            toCarry.parent = controller.transform;
            toCarry.localPosition = originalCamPos;
            controller.transform.Translate(destination.forward, Space.World);
        }
    }
}
