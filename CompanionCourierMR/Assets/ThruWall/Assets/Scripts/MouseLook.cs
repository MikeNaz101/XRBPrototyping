using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{

    public float speed;

    public bool lockCursor;

    private float xAng;

    // Use this for initialization
    void Start()
    {
        xAng = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 rot = transform.localEulerAngles;
        if (Mathf.Abs(rot.y) != 0.0f ||
        Mathf.Abs(rot.z) != 0.0f)
        {
            UpdateInterpolation();
        }

        UpdateMovement();
        UpdateCursorLock();
    }

    private void UpdateMovement()
    {
        float x = Input.GetAxis("Mouse X");
        float y = Input.GetAxis("Mouse Y");
        transform.parent.Rotate(0.0f, x * Time.deltaTime * speed, 0.0f);

        float xRotation = -y * Time.deltaTime * speed;
        transform.Rotate(xRotation, 0.0f, 0.0f);
        xAng += xRotation;
        if (xAng > 90.0f)
        {
            transform.Rotate(90.0f - xAng, 0.0f, 0.0f);
            xAng = 90.0f;
        }
        else if (xAng < -90.0f)
        {
            transform.Rotate(-xAng - 90.0f, 0.0f, 0.0f);
            xAng = -90.0f;
        }
    }

    private void UpdateCursorLock()
    {
        Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void UpdateInterpolation()
    {
        Vector3 rot = transform.localEulerAngles;
        rot.y = 0.0f;
        rot.z = Mathf.LerpAngle(rot.z, 0.0f, Time.deltaTime * 10.0f);
        if (Mathf.Abs(rot.z) <= 0.25f)
        {
            rot.z = 0.0f;
        }

        transform.localEulerAngles = rot;

        float xRot = Vector3.Angle(transform.forward, transform.parent.forward) *
            Mathf.Sign(transform.parent.forward.y - transform.forward.y);
        xAng = xRot;
    }
}
