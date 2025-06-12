using UnityEngine;

[RequireComponent(typeof(LaserLine))] // Ensures the LaserLine script is also on this object.
public class LaserBeamController : MonoBehaviour
{
    [Header("Laser Properties")]
    [Tooltip("The maximum distance the laser will travel.")]
    public float maxDistance = 100f;
    [Tooltip("The layer(s) that the laser will detect as a valid target (but not a portal).")]
    public LayerMask targetLayer;
    [Tooltip("The layer mask for all objects the laser should interact with (including portals and regular targets).")]
    public LayerMask collisionMask;

    [Header("Prefabs")]
    [Tooltip("Assign this laser's own prefab here to allow it to spawn mirrored copies.")]
    public GameObject laserPrefab; // Assign this in the Inspector

    // --- Private Variables ---
    private LaserLine _laserLine;
    private GameObject mirroredLaser; // A reference to the laser spawned from this one.

    void Awake()
    {
        _laserLine = GetComponent<LaserLine>();
    }

    void Update()
    {
        // Clean up any previously created laser segment before calculating the new one.
        if (mirroredLaser != null)
        {
            Destroy(mirroredLaser);
        }

        RaycastHit hit;
        Vector3 endPosition;

        // Cast a ray from this laser's starting position in its forward direction.
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxDistance, collisionMask))
        {
            // The laser will always stop at the point of impact.
            endPosition = hit.point;
            
            // --- UPDATED PORTAL DETECTION ---
            // First, check if the hit object has the correct tag.
            if (hit.collider.CompareTag("TeleportSurface"))
            {
                // If it has the tag, then try to find the Teleporter component on its parent's children.
                Transform hitParent = hit.collider.transform.parent;
                Teleporter teleporter = null;
                if (hitParent != null)
                {
                    teleporter = hitParent.GetComponentInChildren<Teleporter>();
                }
            
                if (teleporter != null && teleporter.destination != null)
                {
                    // Check the destination's status BEFORE deciding to teleport.
                    bool isDestinationActive = teleporter.destination.gameObject.activeInHierarchy;
                    if (isDestinationActive)
                    {
                        // --- Portal Logic ---
                        Debug.Log("[LaserBeam] Destination is active! Attempting to teleport laser.");
                        
                        // Calculate where the new laser should come from.
                        teleporter.TeleportRay(hit.point, transform.forward, out Vector3 exitPosition, out Vector3 exitDirection);
                        
                        // Create the new "mirrored" laser segment.
                        mirroredLaser = Instantiate(laserPrefab, exitPosition, Quaternion.LookRotation(exitDirection));
                        Debug.Log("[LaserBeam] Instantiated mirrored laser.", mirroredLaser);
                    }
                    else
                    {
                        Debug.Log("[LaserBeam] Destination portal is not active. Laser will stop here.");
                    }
                }
            }
            // --- Standard Target Logic ---
            // If it's not a portal surface, check if it's a different kind of target.
            else if ((targetLayer.value & (1 << hit.collider.gameObject.layer)) > 0)
            {
                Debug.Log($"[LaserBeam] Laser hit a standard target: {hit.collider.name}", hit.collider.gameObject);
            }
        }
        else
        {
            // If the laser didn't hit anything, it extends to its maximum distance.
            endPosition = transform.position + transform.forward * maxDistance;
        }

        // Update the visual representation of this laser segment.
        if (_laserLine != null && _laserLine.ControlLine != null)
        {
            _laserLine.ControlLine.SetPosition(0, transform.position);
            _laserLine.ControlLine.SetPosition(1, endPosition);

            // Synchronize the inner and outer parts of the laser visuals.
            _laserLine.Synchronize();
        }
    }

    // When this laser segment is destroyed, make sure to also destroy any subsequent segment it created.
    void OnDestroy()
    {
        if (mirroredLaser != null)
        {
            Destroy(mirroredLaser);
        }
    }
}