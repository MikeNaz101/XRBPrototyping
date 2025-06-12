using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static OVRInput; // Allows using GetDown() instead of OVRInput.GetDown()
using Meta.XR.MRUtilityKit; // Required for MRUKAnchor

public class PortalShooter1 : MonoBehaviour
{
    [Header("Portal & Camera Prefabs")]
    public GameObject orangePortalPrefab;
    public GameObject bluePortalPrefab;
    public GameObject blueCameraPrefab;
    public GameObject orangeCameraPrefab;

    [Header("Portal Materials")]
    public Material orangeOpen;
    public Material orangeClosed;
    public Material blueOpen;
    public Material blueClosed;

    [Header("Controller & Camera References")]
    [Tooltip("The main camera used for rendering the portal view. Usually the CenterEyeAnchor.")]
    public Camera playerCamera; // Used to set the playerCam on the PortalCamera script
    [Tooltip("The transform of the left controller anchor.")]
    public Transform leftControllerAnchor;
    [Tooltip("The transform of the right controller anchor.")]
    public Transform rightControllerAnchor;

    [Header("OVRInput Control")]
    [Tooltip("The button on the left controller to fire the blue portal.")]
    public Button fireBluePortalButton = Button.SecondaryIndexTrigger;
    [Tooltip("The button on the right controller to fire the orange portal.")]
    public Button fireOrangePortalButton = Button.PrimaryIndexTrigger;

    // --- Private Portal References ---
    private GameObject orangePortal;
    private GameObject bluePortal;
    private GameObject orangeCamObject;
    private GameObject blueCamObject;
    private PortalCamera orangeCamera;
    private PortalCamera blueCamera;

    void Start()
    {
        // Instantiate portals and their cameras
        orangePortal = Instantiate(orangePortalPrefab);
        bluePortal = Instantiate(bluePortalPrefab);
        orangeCamObject = Instantiate(orangeCameraPrefab);
        blueCamObject = Instantiate(blueCameraPrefab);

        // Get PortalCamera components
        orangeCamera = orangeCamObject.GetComponent<PortalCamera>();
        blueCamera = blueCamObject.GetComponent<PortalCamera>();

        // --- Setup Orange Portal ---
        orangePortal.GetComponentInChildren<Teleporter>().destination = bluePortal.transform.Find("Destination");
        orangeCamera.associatedPortal = orangePortal.transform.Find("Surface");
        orangeCamera.oppositePortal = bluePortal.transform.Find("Surface");
        orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = orangeClosed;
        orangeCamera.playerCam = playerCamera ? playerCamera.transform : transform; // Use assigned camera or fallback
        orangePortal.SetActive(false);

        // --- Setup Blue Portal ---
        bluePortal.GetComponentInChildren<Teleporter>().destination = orangePortal.transform.Find("Destination");
        blueCamera.associatedPortal = bluePortal.transform.Find("Surface");
        blueCamera.oppositePortal = orangePortal.transform.Find("Surface");
        blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = blueClosed;
        blueCamera.playerCam = playerCamera ? playerCamera.transform : transform; // Use assigned camera or fallback
        bluePortal.SetActive(false);
    }

    void Update()
    {
        // --- Left Controller Input (Blue Portal) ---
        if (GetDown(fireBluePortalButton))
        {
            if (leftControllerAnchor)
            {
                PlacePortal(bluePortal, leftControllerAnchor);
                UpdatePortalsMaterials();
            }
        }
        // --- Right Controller Input (Orange Portal) ---
        else if (GetDown(fireOrangePortalButton))
        {
            if (rightControllerAnchor)
            {
                PlacePortal(orangePortal, rightControllerAnchor);
                UpdatePortalsMaterials();
            }
        }
    }

    private void UpdatePortalsMaterials()
    {
        if (bluePortal.activeSelf && orangePortal.activeSelf)
        {
            orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = orangeOpen;
            blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = blueOpen;
        }
        else
        {
            orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = orangeClosed;
            blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = blueClosed;
        }
    }

    private void PlacePortal(GameObject portal, Transform controllerTransform)
    {
        RaycastHit hit;
        // The portal is already inactive from the last placement, but we do it again just in case.
        portal.SetActive(false);

        // Fire raycast from the controller
        if (Physics.Raycast(controllerTransform.position, controllerTransform.forward, out hit))
        {
            // --- MRUK Validation ---
            // Check if the hit object is a valid MRUK surface
            if (hit.collider.GetComponentInParent<MRUKAnchor>() == null)
            {
                Debug.LogWarning($"Portal shot hit '{hit.collider.name}', which is not a valid MRUK surface. Portal not placed.");
                return; // Not a valid surface, so we stop here.
            }
            
            Debug.LogWarning($"Portal shot hit '{hit.collider.name}', which IS a valid MRUK surface. Portal Should Be placed.");

            // --- Placement and Orientation ---
            portal.transform.position = hit.point;

            // Use the more robust method for orientation
            portal.transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);

            // Offset slightly to prevent Z-fighting
            portal.transform.position += portal.transform.forward * 0.01f;

            // Validate the portal position to ensure it's not hanging off an edge
            if (ValidatePortalPosition(portal, hit.collider))
            {
                var detector = portal.GetComponentInChildren<BodyDetector>();
                if (detector)
                {
                    detector.associatedSurface = hit.collider;
                    detector.InitializeCollidersList();
                }

                portal.SetActive(true);

                // Update cameras and the other portal's detector
                if (portal == orangePortal)
                {
                    orangeCamera.RecalculateHolePositions();
                    if (detector && bluePortal.activeSelf)
                    {
                        detector.otherDetector = bluePortal.GetComponentInChildren<BodyDetector>();
                    }
                }
                else if (portal == bluePortal)
                {
                    blueCamera.RecalculateHolePositions();
                    if (detector && orangePortal.activeSelf)
                    {
                        detector.otherDetector = orangePortal.GetComponentInChildren<BodyDetector>();
                    }
                }
            }
        }
    }

    private bool ValidatePortalPosition(GameObject portal, Collider associatedSurface)
    {
        return true;
        Transform up = portal.transform.Find("PortalTop");
        Transform down = portal.transform.Find("PortalBottom");
        Transform left = portal.transform.Find("PortalLeft");
        Transform right = portal.transform.Find("PortalRight");

        if (up == null || down == null || left == null || right == null)
        {
            Debug.LogError("Portal prefab is missing corner validation points (e.g., 'PortalTop').");
            return false;
        }

        return IsOnCollider(up, associatedSurface) &&
               IsOnCollider(down, associatedSurface) &&
               IsOnCollider(left, associatedSurface) &&
               IsOnCollider(right, associatedSurface);
    }

    private bool IsOnCollider(Transform point, Collider col)
    {
        // Start the check slightly in front of the point to avoid precision errors
        Vector3 rayStart = point.position + (point.forward * 0.1f);
        if (Physics.Raycast(rayStart, -point.forward, out RaycastHit hit, 0.2f))
        {
            return hit.collider == col;
        }
        return true;
    }
}
