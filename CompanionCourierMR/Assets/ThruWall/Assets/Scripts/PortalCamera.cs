using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCamera : MonoBehaviour
{

    public Transform associatedPortal;
    public Transform oppositePortal;
    public Transform playerCam;

    private Camera myCam;
    private Vector3 portalLeft;
    private Vector3 portalRight;
    private Vector3 portalTop;
    private Vector3 portalBottom;
    private float nearPane;

    // Use this for initialization
    void Start()
    {
        myCam = GetComponent<Camera>();
        RecalculateHolePositions();
    }

    public void RecalculateHolePositions()
    {
        portalLeft = associatedPortal.parent.Find("PortalLeft").position;
        portalRight = associatedPortal.parent.Find("PortalRight").position;
        portalTop = associatedPortal.parent.Find("PortalTop").position;
        portalBottom = associatedPortal.parent.Find("PortalBottom").position;
    }

    // Update is called once per frame
    void Update()
    {
        if (associatedPortal != null && oppositePortal != null)
        {
            if (!myCam.enabled)
            {
                myCam.enabled = true;
            }
            UpdateHorizontalPosition();
            UpdateVerticalPosition();
            UpdateViewFrustum();
        }
        else
        {
            myCam.enabled = false;
        }

    }
    
    void UpdateHorizontalPosition()
    {
        Vector3 playerPosition = playerCam.transform.position;
        Vector3 playerDirection = playerCam.forward;

        transform.position = associatedPortal.position;
        transform.rotation = associatedPortal.parent.rotation;

        Vector3 portalToPlayer = playerCam.transform.position - oppositePortal.transform.position;
        //portalToPlayer.y = 0;
        float portalPlayerAngle = Vector3.Angle(-oppositePortal.parent.right, portalToPlayer);
        float portaPlayerAngleF = portalPlayerAngle < 90.0f ? 90.0f - portalPlayerAngle : portalPlayerAngle - 90.0f;
        Vector3 p2pZProjection = portalToPlayer * Mathf.Cos(portaPlayerAngleF * Mathf.Deg2Rad);
        Vector3 p2pXProjection = portalToPlayer * Mathf.Sin(portaPlayerAngleF * Mathf.Deg2Rad);
        nearPane = p2pZProjection.magnitude;
        if (portalPlayerAngle > 90.0f)
        {
            transform.Translate(-p2pXProjection.magnitude, 0, -p2pZProjection.magnitude);
        }
        else
        {
            transform.Translate(p2pXProjection.magnitude, 0, -p2pZProjection.magnitude);
        }
    }

    void UpdateVerticalPosition()
    {
        Vector3 portalToPlayer = playerCam.transform.position - oppositePortal.transform.position;
        portalToPlayer.x = 0;
        float portalPlayerAngle = Vector3.Angle(-oppositePortal.parent.up, portalToPlayer);
        float portaPlayerAngleF = portalPlayerAngle < 90.0f ? 90.0f - portalPlayerAngle : portalPlayerAngle - 90.0f;
        Vector3 p2pYProjection = portalToPlayer * Mathf.Sin(portaPlayerAngleF * Mathf.Deg2Rad);

        if (portalPlayerAngle > 90.0f)
        {
            transform.Translate(0, p2pYProjection.magnitude, 0);
        }
        else
        {
            transform.Translate(0, -p2pYProjection.magnitude, 0);
        }
    }

    void UpdateViewFrustum()
    {
        Vector3 cameraToPortalLeft = transform.position - portalRight;
        float leftCamAngle = Vector3.Angle(associatedPortal.parent.right, cameraToPortalLeft);
        float leftCamAngleF = leftCamAngle < 90.0f ? 90.0f - leftCamAngle : leftCamAngle - 90.0f;
        Vector3 l2cZProjection = cameraToPortalLeft * Mathf.Cos(leftCamAngleF * Mathf.Deg2Rad);
        Vector3 l2cXProjection = cameraToPortalLeft * Mathf.Sin(leftCamAngleF * Mathf.Deg2Rad);

        Vector3 cameraToPortalRight = transform.position - portalLeft;
        float rightCamAngle = Vector3.Angle(associatedPortal.parent.right, cameraToPortalRight);
        float rightCamAngleF = rightCamAngle < 90.0f ? 90.0f - rightCamAngle : rightCamAngle - 90.0f;
        Vector3 r2cZProjection = cameraToPortalRight * Mathf.Cos(rightCamAngleF * Mathf.Deg2Rad);
        Vector3 r2cXProjection = cameraToPortalRight * Mathf.Sin(rightCamAngleF * Mathf.Deg2Rad);

        myCam.nearClipPlane = nearPane;
        float left = -l2cXProjection.magnitude;
        float right = r2cXProjection.magnitude;
        if (leftCamAngle > 90.0f)
        {
            left *= -1.0f;
        }
        if (rightCamAngle < 90.0f)
        {
            right *= -1.0f;
        }

        Vector3 cameraToPortalTop = transform.position - portalTop;
        float topCamAngle = Vector3.Angle(associatedPortal.parent.up, cameraToPortalTop);
        float topCamAngleF = topCamAngle < 90.0f ? 90.0f - topCamAngle : topCamAngle - 90.0f;
        Vector3 t2cZProjection = cameraToPortalTop * Mathf.Cos(topCamAngleF * Mathf.Deg2Rad);
        Vector3 t2cYProjection = cameraToPortalTop * Mathf.Sin(topCamAngleF * Mathf.Deg2Rad);

        Vector3 cameraToPortalBottom = transform.position - portalBottom;
        float bottomCamAngle = Vector3.Angle(associatedPortal.parent.up, cameraToPortalBottom);
        float bottomCamAngleF = bottomCamAngle < 90.0f ? 90.0f - bottomCamAngle : bottomCamAngle - 90.0f;
        Vector3 b2cZProjection = cameraToPortalBottom * Mathf.Cos(bottomCamAngleF * Mathf.Deg2Rad);
        Vector3 b2cYProjection = cameraToPortalBottom * Mathf.Sin(bottomCamAngleF * Mathf.Deg2Rad);

        float bottom = -b2cYProjection.magnitude;
        float top = t2cYProjection.magnitude;
        
        if (bottomCamAngle > 90.0f)
        {
            bottom *= -1.0f;
        }
        if (topCamAngle < 90.0f)
        {
            top *= -1.0f;
        }

        try{
            myCam.projectionMatrix = PerspectiveOffCenter(left, right, bottom, top, myCam.nearClipPlane, myCam.farClipPlane);
        } catch{
            print("Invalid matrix");
        }
    }

    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }
}
