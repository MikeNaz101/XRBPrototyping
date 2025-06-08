using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.VR; // Or using Meta.XR, depending on SDK version
using static OVRInput; // Allows using GetDown() instead of OVRInput.GetDown()

// It's good practice to ensure the required components are attached to the GameObject.
// This script requires an OVRCameraRig to be present in the scene to find controller anchors.
public class PortalShooter : MonoBehaviour
{
    // Portal GameObjects to be instantiated
    public GameObject orangePortalPrefab;
    public GameObject bluePortalPrefab;

    // Camera prefabs for rendering the portal view
    public GameObject blueCameraPrefab;
    public GameObject orangeCameraPrefab;

    // Materials to indicate if portals are linked and active
    public Material orangeOpen;
    public Material orangeClosed;
    public Material blueOpen;
    public Material blueClosed;

    // --- Controller References ---
    // Assign these in the Unity Inspector by dragging the LeftHandAnchor and RightHandAnchor
    // from your OVRCameraRig -> TrackingSpace.
    public Transform leftControllerAnchor;
    public Transform rightControllerAnchor;

    // --- OVRInput Control ---
    [Header("OVRInput Control")]
    [Tooltip("The OVRInput Button on the left controller for firing the blue portal.")]
    public Button fireBluePortalButton = Button.PrimaryIndexTrigger;
    [Tooltip("The OVRInput Button on the right controller for firing the orange portal.")]
    public Button fireOrangePortalButton = Button.PrimaryIndexTrigger;

    // Private references to the instantiated portal objects
    private GameObject orangePortal;
    private GameObject bluePortal;
    private GameObject orangeCamObject;
    private GameObject blueCamObject;

    // Private references to the portal camera scripts
    private PortalCamera orangeCamera;
    private PortalCamera blueCamera;


    // Use this for initialization
    void Start()
    {
        // Instantiate the portals and their cameras from prefabs
        orangePortal = Instantiate<GameObject>(orangePortalPrefab);
        bluePortal = Instantiate<GameObject>(bluePortalPrefab);

        // Find the destination points within the portal prefabs
        // These are used to tell one portal where the other one is.
        Transform orangeDest = orangePortal.transform.Find("Destination");
        Transform blueDest = bluePortal.transform.Find("Destination");

        // Error checking to ensure prefabs are set up correctly
        if (orangeDest == null) Debug.LogError("Orange Portal Prefab is missing a 'Destination' child object!");
        if (blueDest == null) Debug.LogError("Blue Portal Prefab is missing a 'Destination' child object!");

        // Set up the teleporter components
        orangePortal.GetComponentInChildren<Teleporter>().destination = blueDest;
        orangePortal.SetActive(false); // Start with portals inactive
        bluePortal.GetComponentInChildren<Teleporter>().destination = orangeDest;
        bluePortal.SetActive(false);

        // --- Camera and Surface Setup ---
        orangeCamObject = Instantiate<GameObject>(orangeCameraPrefab);
        orangeCamera = orangeCamObject.GetComponent<PortalCamera>();

        // Find the surfaces of the portals that will display the camera's output
        Transform orangeSurface = orangePortal.transform.Find("Surface");
        Transform blueSurface = bluePortal.transform.Find("Surface");

        // Error checking for surfaces
        if (orangeSurface == null) Debug.LogError("Orange Portal Prefab is missing a 'Surface' child object!");
        if (blueSurface == null) Debug.LogError("Blue Portal Prefab is missing a 'Surface' child object!");

        // Configure the orange portal's camera
        orangeCamera.associatedPortal = orangeSurface;
        orangeCamera.oppositePortal = blueSurface;
        orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = orangeClosed;
        // Assign the main VR camera (CenterEyeAnchor) to the portal camera
        orangeCamera.playerCam = Camera.main.transform;

        // Configure the blue portal's camera
        blueCamObject = Instantiate<GameObject>(blueCameraPrefab);
        blueCamera = blueCamObject.GetComponent<PortalCamera>();
        blueCamera.associatedPortal = blueSurface;
        blueCamera.oppositePortal = orangeSurface;
        blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = blueClosed;
        blueCamera.playerCam = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        // --- Input for Left Controller (Blue Portal) ---
        // Check for the left index trigger press using the configurable button
        if (GetDown(fireBluePortalButton, Controller.LTouch))
        {
            if (leftControllerAnchor != null)
            {
                PlacePortal(bluePortal, leftControllerAnchor);
                UpdatePortalsMaterials();
            }
            else
            {
                Debug.LogWarning("Left Controller Anchor not assigned. Cannot place blue portal.");
            }
        }

        // --- Input for Right Controller (Orange Portal) ---
        // Check for the right index trigger press using the configurable button
        if (GetDown(fireOrangePortalButton, Controller.RTouch))
        {
            if (rightControllerAnchor != null)
            {
                PlacePortal(orangePortal, rightControllerAnchor);
                UpdatePortalsMaterials();
            }
            else
            {
                Debug.LogWarning("Right Controller Anchor not assigned. Cannot place orange portal.");
            }
        }
    }

    // This method updates the portal materials to 'Open' or 'Closed' based on whether
    // the other portal is active.
    private void UpdatePortalsMaterials()
    {
        // If both portals are active, their materials should be set to 'Open'
        bool bothPortalsActive = bluePortal.activeSelf && orangePortal.activeSelf;
        orangeCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = bothPortalsActive ? orangeOpen : orangeClosed;
        blueCamera.associatedPortal.GetComponent<Renderer>().sharedMaterial = bothPortalsActive ? blueOpen : blueClosed;
    }

    // This method handles the logic for placing a portal in the world.
    private void PlacePortal(GameObject portal, Transform rayOriginTransform)
    {
        RaycastHit hit;
        
        // Raycast forward from the controller's position and orientation
        if (Physics.Raycast(rayOriginTransform.position, rayOriginTransform.forward, out hit))
        {
            // --- Scene Understanding Integration ---
            // Check if the surface hit is part of the scanned room geometry.
            bool isScannedSurface = hit.collider.GetComponent<OVRScenePlane>() != null || hit.collider.GetComponent<OVRSceneVolume>() != null;

            if (!isScannedSurface)
            {
                // We hit something that isn't a valid surface for a portal, so do nothing.
                return;
            }

            // --- Portal Placement and Orientation ---
            portal.transform.position = hit.point;
            
            // The portal's "forward" should face away from the wall.
            portal.transform.forward = -hit.normal;

            // Offset slightly to prevent Z-fighting (rendering flicker with the wall).
            portal.transform.position += portal.transform.forward * 0.01f;

            // Validate that the portal's corners are all on the same surface.
            bool validPosition = ValidatePortalPosition(portal, hit.collider);
            if (validPosition)
            {
                // Link the portal's body detector to the surface it's on.
                BodyDetector detector = portal.GetComponentInChildren<BodyDetector>();
                detector.associatedSurface = hit.collider;
                detector.InitializeCollidersList();

                // --- Spatial Anchoring ---
                // Parent the portal to the OVRSceneAnchor of the surface it hit.
                // This ensures the portal stays locked to the real-world object.
                OVRSceneAnchor hitAnchor = hit.collider.GetComponentInParent<OVRSceneAnchor>();
                if (hitAnchor != null)
                {
                    portal.transform.SetParent(hitAnchor.transform, worldPositionStays: true);
                }
                else
                {
                    // Fallback: If no anchor is found, parent to the scene root.
                    // The portal might drift if the playspace is reset.
                    portal.transform.SetParent(null, worldPositionStays: true);
                    Debug.LogWarning("Hit surface did not have an OVRSceneAnchor. Portal may not persist correctly.");
                }

                // Activate the portal GameObject.
                portal.SetActive(true);

                // Update references and recalculate camera positions.
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
            else
            {
                // If the position is not valid (e.g., hanging off an edge), deactivate the portal.
                portal.SetActive(false);
            }
        }
    }
    
    // Checks if the portal's four corners are flush against a valid collider.
    private bool ValidatePortalPosition(GameObject portal, Collider associatedSurface)
    {
        // These should be empty GameObjects at the corners of your portal prefab.
        Transform up = portal.transform.Find("PortalTop");
        Transform down = portal.transform.Find("PortalBottom");
        Transform left = portal.transform.Find("PortalLeft");
        Transform right = portal.transform.Find("PortalRight");

        if (up == null || down == null || left == null || right == null)
        {
            Debug.LogError("Portal prefab missing corner validation points. Cannot validate position.");
            return false;
        }

        // All four corners must be on the target surface.
        return IsOnCollider(up, associatedSurface) &&
               IsOnCollider(down, associatedSurface) &&
               IsOnCollider(left, associatedSurface) &&
               IsOnCollider(right, associatedSurface);
    }

    // Utility function to check if a single point is on a specific collider.
    private bool IsOnCollider(Transform point, Collider col)
    {
        RaycastHit hit;
        // Raycast from the point back towards the surface to confirm it hits the correct collider.
        if (Physics.Raycast(point.position, -point.transform.forward, out hit, 0.1f))
        {
            return hit.collider == col;
        }
        return false;
    }
}
