using UnityEngine;
using System.Collections.Generic; // For lists
using System.Linq; // For LINQ operations like OrderBy

/// <summary>
/// Handles player interaction for designating tasks on the map.
/// Allows player to point at a location, select a task, and assign it to a nearby available villager.
/// </summary>
public class PlayerInteractionController : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("The camera used for raycasting (e.g., main camera or XR controller's pointing ray).")]
    public Camera playerCamera;
    [Tooltip("The layer mask for the terrain/ground the player can point at.")]
    public LayerMask terrainLayerMask;
    [Tooltip("Maximum distance for the raycast.")]
    public float maxRaycastDistance = 100f;

    [Header("Task Assignment")]
    [Tooltip("Radius to search for available villagers around the designated point.")]
    public float villagerSearchRadius = 20f;
    [Tooltip("Prefab to instantiate as a visual marker for build/farm sites. Should be simple (e.g., a transparent cylinder or flag).")]
    public GameObject taskSiteMarkerPrefab;
    [Tooltip("How many villagers to assign to a task if multiple are available (for future expansion). For now, typically 1.")]
    public int maxVillagersPerTask = 1;


    [Header("UI & Feedback (Simplified - Use Keyboard for now)")]
    [Tooltip("Visual marker for the point selected by the player on the terrain.")]
    public GameObject selectionMarkerPrefab; // Optional: A temporary visual cue for the selected point
    private GameObject currentSelectionMarker;
    private Vector3 selectedPoint;
    private bool pointSelected = false;

    // References to VillagerAI states (assuming VillagerAI script is in the same project)
    // These will be new instances passed to the VillagerAI's AssignTask method.
    private VillagerBuildingState _buildingStateInstance;
    private VillagerFarmingState _farmingStateInstance;
    private VillagerExploringState _exploringStateInstance; // We'll create this state script

    void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("PlayerInteractionController: Player Camera is not assigned and Main Camera not found!", this);
                enabled = false;
                return;
            }
        }

        if (selectionMarkerPrefab != null)
        {
            currentSelectionMarker = Instantiate(selectionMarkerPrefab, Vector3.zero, Quaternion.identity);
            currentSelectionMarker.SetActive(false);
        }

        // Initialize state instances once
        _buildingStateInstance = new VillagerBuildingState();
        _farmingStateInstance = new VillagerFarmingState();
        _exploringStateInstance = new VillagerExploringState();
    }

    void Update()
    {
        HandleMouseInput(); // Primary interaction for selecting point
        HandleTaskAssignmentInput(); // Keyboard input for assigning task to selected point
    }

    void HandleMouseInput()
    {
        // Use mouse click to select a point on the terrain
        if (Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxRaycastDistance, terrainLayerMask))
            {
                selectedPoint = hit.point;
                pointSelected = true;
                Debug.Log("Point selected at: " + selectedPoint, this);

                if (currentSelectionMarker != null)
                {
                    currentSelectionMarker.transform.position = selectedPoint + Vector3.up * 0.1f; // Slightly above ground
                    currentSelectionMarker.SetActive(true);
                }
            }
            else
            {
                pointSelected = false;
                if (currentSelectionMarker != null)
                {
                    currentSelectionMarker.SetActive(false);
                }
            }
        }
    }

    void HandleTaskAssignmentInput()
    {
        if (!pointSelected) return; // Need a point selected first

        VillagerBaseState taskToAssign = null;
        string taskName = "";
        bool requiresSiteMarker = false;

        if (Input.GetKeyDown(KeyCode.Alpha1)) // Press '1' for Build
        {
            taskToAssign = _buildingStateInstance;
            taskName = "Build";
            requiresSiteMarker = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2)) // Press '2' for Farm
        {
            taskToAssign = _farmingStateInstance;
            taskName = "Farm";
            requiresSiteMarker = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3)) // Press '3' for Explore
        {
            taskToAssign = _exploringStateInstance;
            taskName = "Explore";
            requiresSiteMarker = false; // Explore just needs a point
        }

        if (taskToAssign != null)
        {
            Debug.Log($"Player chose to {taskName} at {selectedPoint}", this);

            Transform taskTargetTransform = null;
            if (requiresSiteMarker && taskSiteMarkerPrefab != null)
            {
                GameObject markerInstance = Instantiate(taskSiteMarkerPrefab, selectedPoint, Quaternion.identity);
                taskTargetTransform = markerInstance.transform;
                // Optionally, name the marker for clarity in hierarchy
                markerInstance.name = taskName + " Site Marker";
            }
            else if (!requiresSiteMarker)
            {
                // For tasks like explore, we might still want a temporary marker or pass the point directly.
                // If the state handles a Vector3, we might not need a transform.
                // For simplicity with current VillagerAI.AssignTask, we can use a temporary marker for explore too.
                // Or, modify VillagerExploringState to take a Vector3. For now, let's assume it can use a target transform.
                if (taskSiteMarkerPrefab != null) // Create a generic marker for explore if prefab exists
                {
                     GameObject markerInstance = Instantiate(taskSiteMarkerPrefab, selectedPoint, Quaternion.identity);
                     taskTargetTransform = markerInstance.transform;
                     markerInstance.name = taskName + " Point Marker";
                }
                else // If no marker prefab, explore state will need to handle Vector3 or currentTaskTarget being null
                {
                    // This means VillagerExploringState needs to be robust enough to handle a null currentTaskTarget
                    // and perhaps use a stored Vector3. For now, we'll assume it wants a transform.
                    // A better approach for explore would be to modify VillagerAI.AssignTask or ExploringState
                    // to directly accept a Vector3 position.
                    // For now, we'll log a warning if no marker can be made for explore.
                    Debug.LogWarning("No taskSiteMarkerPrefab assigned for Explore task. Villager might not have a specific Transform target.", this);
                }
            }


            AssignTaskToNearbyVillagers(taskToAssign, taskTargetTransform != null ? taskTargetTransform : null, selectedPoint);

            pointSelected = false; // Reset point selection
            if (currentSelectionMarker != null)
            {
                currentSelectionMarker.SetActive(false);
            }
        }
    }

    void AssignTaskToNearbyVillagers(VillagerBaseState task, Transform taskSiteTransform, Vector3 taskLocation)
    {
        Collider[] hitColliders = Physics.OverlapSphere(taskLocation, villagerSearchRadius);
        List<VillagerAI> availableVillagers = new List<VillagerAI>();

        foreach (var hitCollider in hitColliders)
        {
            VillagerAI villager = hitCollider.GetComponent<VillagerAI>();
            if (villager != null && villager.enabled &&
                (villager.currentState == villager.strollingState /* Add other interruptible states here, e.g., idle */ ))
            {
                availableVillagers.Add(villager);
            }
        }

        if (availableVillagers.Count == 0)
        {
            Debug.Log("No available villagers found near the task location.", this);
            // If a site marker was created but no villager found, you might want to destroy it
            // or leave it for a villager to find later (more complex system).
            if (taskSiteTransform != null && taskSiteTransform.gameObject.name.Contains("Marker"))
            {
                // Destroy(taskSiteTransform.gameObject, 5f); // Clean up unassigned marker after a delay
            }
            return;
        }

        // Sort villagers by distance to the task location
        availableVillagers = availableVillagers.OrderBy(v => Vector3.Distance(v.transform.position, taskLocation)).ToList();

        int villagersAssigned = 0;
        for (int i = 0; i < availableVillagers.Count && villagersAssigned < maxVillagersPerTask; i++)
        {
            VillagerAI villagerToAssign = availableVillagers[i];
            Debug.Log($"Assigning {task.GetType().Name} to {villagerToAssign.gameObject.name}", this);

            // For explore, the VillagerExploringState will use the taskSiteTransform (which is just a marker at selectedPoint)
            // or could be modified to take a Vector3 directly.
            if (task is VillagerExploringState && taskSiteTransform != null)
            {
                 ((VillagerExploringState)task).targetExplorationPoint = taskSiteTransform.position;
            }


            // If the task is 'Build' or 'Farm', the taskSiteTransform is the actual site marker.
            // If it's 'Explore', taskSiteTransform is a temporary marker at the explore point.
            villagerToAssign.AssignTask(task, taskSiteTransform);
            villagersAssigned++;
        }
    }

    // Optional: Draw gizmos in the editor to visualize search radius
    void OnDrawGizmosSelected()
    {
        if (pointSelected)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(selectedPoint, 0.5f); // Small sphere at selected point
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(selectedPoint, villagerSearchRadius); // Villager search radius
        }
    }
}
