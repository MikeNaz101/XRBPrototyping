using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalShooter : MonoBehaviour
{

    public GameObject orangePortalPrefab;
    public GameObject bluePortalPrefab;

    public GameObject blueCameraPrefab;
    public GameObject orangeCameraPrefab;

    public Material orangeOpen;
    public Material orangeClosed;
    public Material blueOpen;
    public Material blueClosed;

    private GameObject orangePortal;
    private GameObject bluePortal;
    private GameObject orangeCamObject;
    private GameObject blueCamObject;

    private PortalCamera orangeCamera;
    private PortalCamera blueCamera;

    // Use this for initialization
    void Start()
    {
        orangePortal = Instantiate<GameObject>(orangePortalPrefab);
        bluePortal = Instantiate<GameObject>(bluePortalPrefab);
        orangePortal.GetComponentInChildren<Teleporter>().destination = bluePortal.transform.Find("Destination");
        orangePortal.SetActive(false);
        bluePortal.GetComponentInChildren<Teleporter>().destination = orangePortal.transform.Find("Destination");
        bluePortal.SetActive(false);
        orangeCamObject = Instantiate<GameObject>(orangeCameraPrefab);
        orangeCamera = orangeCamObject.GetComponent<PortalCamera>();
        orangeCamera.associatedPortal = orangePortal.transform.Find("Surface");
        orangeCamera.oppositePortal = bluePortal.transform.Find("Surface");
        orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = orangeClosed;
        orangeCamera.playerCam = transform;
        blueCamObject = Instantiate<GameObject>(blueCameraPrefab);
        blueCamera = blueCamObject.GetComponent<PortalCamera>();
        blueCamera.associatedPortal = bluePortal.transform.Find("Surface");
        blueCamera.oppositePortal = orangePortal.transform.Find("Surface");
        blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = blueClosed;
        blueCamera.playerCam = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            PlacePortal(orangePortal);
            UpdatePortalsMaterials();
        }
        else if (Input.GetButtonDown("Fire2"))
        {
            PlacePortal(bluePortal);
            UpdatePortalsMaterials();
        }
    }

    private void UpdatePortalsMaterials()
    {
        if (bluePortal.activeSelf)
        {
            orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = orangeOpen;
        }
        else
        {
            orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = orangeClosed;
        }
        if (orangePortal.activeSelf)
        {
            blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = blueOpen;
        }
        else
        {
            blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = blueClosed;
        }
    }

    private void PlacePortal(GameObject portal)
    {
        RaycastHit hit;
        portal.SetActive(false);
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            if (hit.transform.tag != "OpenableSurface")
            {
                return;
            }
            portal.transform.position = hit.point;
            Vector3 lookPos = transform.position;
            lookPos.y = portal.transform.position.y;
            portal.transform.LookAt(lookPos);

            float surfaceAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (surfaceAngle < 35.0f || surfaceAngle > 145.0f)
            {
                portal.transform.LookAt(hit.point + hit.normal, transform.up);
            }
            else
            {
                portal.transform.LookAt(hit.point + hit.normal);
            }

            portal.transform.Translate(0.0f, 0.0f, 0.01f);
            bool validPosition = ValidatePortalPosition(portal, hit.collider);
            if (validPosition)
            {
                BodyDetector detector = portal.GetComponentInChildren<BodyDetector>();
                detector.associatedSurface = hit.collider;
                detector.InitializeCollidersList();
                portal.SetActive(true);

                if (portal == orangePortal)
                {
                    orangeCamera.RecalculateHolePositions();
                    detector.otherDetector = bluePortal.GetComponentInChildren<BodyDetector>();
                }
                else if (portal == bluePortal)
                {
                    blueCamera.RecalculateHolePositions();
                    detector.otherDetector = orangePortal.GetComponentInChildren<BodyDetector>();
                }
            }
        }
    }

    private bool ValidatePortalPosition(GameObject portal, Collider associatedSurface)
    {
        Transform up, down, left, right;
        up = portal.transform.Find("PortalTop");
        down = portal.transform.Find("PortalBottom");
        left = portal.transform.Find("PortalLeft");
        right = portal.transform.Find("PortalRight");

        return IsOnCollider(up, associatedSurface) &&
                IsOnCollider(down, associatedSurface) &&
                IsOnCollider(left, associatedSurface) &&
                IsOnCollider(right, associatedSurface);
    }

    private bool IsOnCollider(Transform point, Collider col)
    {
        RaycastHit hit;
        if (Physics.Raycast(point.position, -point.forward, out hit, 0.1f))
        {
            return hit.collider == col;
        }
        return false;
    }


}
