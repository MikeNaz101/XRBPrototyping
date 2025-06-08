using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public float feetPosition;

    public float movementSpeed;

    public float jumpSpeed;

    private Rigidbody rb;

    private MouseLook camMouseLook;


    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        camMouseLook = GetComponentInChildren<MouseLook>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal") * movementSpeed;
        float v = Input.GetAxis("Vertical") * movementSpeed;
        Vector3 oldVelocity = rb.linearVelocity;
        float y = oldVelocity.y;
        if (Input.GetButton("Jump") && OnGround())
        {
            y = jumpSpeed;
        }

        Vector3 newVelocity = new Vector3(h, y, v);
        newVelocity = transform.localToWorldMatrix * newVelocity;
        newVelocity.x = Mathf.Abs(newVelocity.x) > Mathf.Abs(oldVelocity.x) ? newVelocity.x : oldVelocity.x;
        newVelocity.y = Mathf.Abs(newVelocity.y) > Mathf.Abs(oldVelocity.y) ? newVelocity.y : oldVelocity.y;
        newVelocity.z = Mathf.Abs(newVelocity.z) > Mathf.Abs(oldVelocity.z) ? newVelocity.z : oldVelocity.z;
        if (newVelocity.magnitude > GameConstants.SPEED_LIMIT)
        {
            newVelocity = newVelocity.normalized * GameConstants.SPEED_LIMIT;
        }
        rb.linearVelocity = newVelocity;
    }

    private bool OnGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * feetPosition;
        RaycastHit hit;
        if (Physics.Raycast(new Ray(rayStart, -Vector3.up), out hit, 0.2f))
        {
            return true;
        }

        return false;
    }
}
