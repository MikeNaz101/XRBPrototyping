using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

/// <summary>
/// Main controller for Villager AI. Manages NavMeshAgent, needs (energy, hunger),
/// and the state machine for behaviors.
/// Attach this script to your Villager prefab.
/// </summary>
public class VillagerAI : MonoBehaviour
{
    // --- Public Properties & References ---
    [Header("AI Components")]
    [Tooltip("The NavMeshAgent component for villager movement.")]
    public NavMeshAgent agent;

    [Header("Villager Stats/Needs (Example)")]
    [Tooltip("Maximum energy level.")]
    public float maxEnergy = 100f;
    [Tooltip("Current energy level. Decreases over time when active.")]
    public float currentEnergy;
    [Tooltip("Rate at which energy depletes per second when active.")]
    public float energyDepletionRate = 0.5f; // Slower depletion

    [Tooltip("Maximum hunger level (0 = not hungry, maxHunger = very hungry).")]
    public float maxHunger = 100f;
    [Tooltip("Current hunger level. Increases over time.")]
    public float currentHunger;
    [Tooltip("Rate at which hunger increases per second.")]
    public float hungerIncreaseRate = 0.3f; // Slower increase

    [Header("Movement & Strolling")]
    [Tooltip("Default movement speed for the villager.")]
    public float moveSpeed = 2.5f;
    [Tooltip("Radius within which the villager will search for a random strolling point.")]
    public float strollRadius = 20f;
    [Tooltip("Minimum time to wait after reaching a stroll destination.")]
    public float minStrollWaitTime = 4f;
    [Tooltip("Maximum time to wait after reaching a stroll destination.")]
    public float maxStrollWaitTime = 8f;

    [Header("Task Related")]
    [Tooltip("How close the villager needs to be to a target to interact with it (e.g., build, farm).")]
    public float interactionDistance = 1.8f;

    // --- State Machine ---
    [Header("States (Read Only - For Debugging)")]
    [SerializeField] // Show in inspector for debugging, but not meant to be set manually here
    private string _currentStateName; // For debugging in inspector
    public VillagerBaseState currentState;

    // Instances of each state
    public VillagerStrollingState strollingState;
    public VillagerBuildingState buildingState;
    public VillagerFarmingState farmingState;
    public VillagerSleepingState sleepingState;
    public VillagerEatingState eatingState;
    public VillagerExploringState exploringState;
    // Add other states here as needed (e.g., GatheringState, FleeingState)

    // --- Task Targets ---
    [Header("Task Information (Managed by States/Game Logic)")]
    [Tooltip("The current Transform the villager is targeting for a task (e.g., build site, farm plot).")]
    public Transform currentTaskTarget;
    [Tooltip("Tag used to find specific types of resources or locations (e.g., 'FoodSource', 'Bed').")]
    public string requiredResourceTag;

    void Awake()
    {
        // Ensure NavMeshAgent is assigned
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component not found on " + gameObject.name + "! Villager AI will not function.", this);
            enabled = false; // Disable script if no agent
            return;
        }

        // Initialize state instances
        strollingState = new VillagerStrollingState();
        buildingState = new VillagerBuildingState();
        farmingState = new VillagerFarmingState();
        sleepingState = new VillagerSleepingState();
        eatingState = new VillagerEatingState();
        // Initialize other states here if you add more
    }

    void Start()
    {
        currentEnergy = maxEnergy;
        currentHunger = 0f; // Start not hungry
        agent.speed = moveSpeed;
        // Set stopping distance: how close agent gets to destination.
        // Useful for tasks where villager doesn't need to be *exactly* on top of the target.
        agent.stoppingDistance = interactionDistance * 0.8f;

        // Initialize to a default state (e.g., Strolling)
        if (strollingState != null)
        {
            TransitionToState(strollingState);
        }
        else
        {
            Debug.LogError("StrollingState is not initialized. Cannot set initial state for " + gameObject.name, this);
        }
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState.UpdateState(this);
            _currentStateName = currentState.GetType().Name; // Update for inspector debugging
        }

        // --- Needs Management ---
        // Determine if the current state is one where needs should be paused (e.g., sleeping)
        bool needsPaused = (currentState == sleepingState || (currentState == eatingState && currentTaskTarget != null));

        if (!needsPaused)
        {
            currentEnergy -= energyDepletionRate * Time.deltaTime;
            currentHunger += hungerIncreaseRate * Time.deltaTime;
        }
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);

        // --- Automatic State Transitions Based on Needs ---
        // These are high-priority transitions that override current tasks if needs are critical.
        if (currentEnergy <= 0 && currentState != sleepingState && sleepingState != null)
        {
            Debug.Log(gameObject.name + " is exhausted and going to sleep.", this);
            TransitionToState(sleepingState);
        }
        else if (currentHunger >= maxHunger && currentState != eatingState && eatingState != null)
        {
            Debug.Log(gameObject.name + " is starving and looking for food.", this);
            TransitionToState(eatingState);
        }
    }

    /// <summary>
    /// Transitions the villager to a new state.
    /// Calls ExitState on the current state and EnterState on the new state.
    /// </summary>
    /// <param name="newState">The state to transition to.</param>
    public void TransitionToState(VillagerBaseState newState)
    {
        if (currentState != null)
        {
            currentState.ExitState(this);
        }
        currentState = newState;
        if (currentState != null)
        {
            currentState.EnterState(this);
            // Debug.Log(gameObject.name + " transitioned to " + newState.GetType().Name, this); // Can be spammy
        }
        else
        {
            Debug.LogError(gameObject.name + " attempted to transition to a null state! Ensure all states are initialized in Awake().", this);
        }
    }

    /// <summary>
    /// Finds a random point on the NavMesh within a specified range from a center point.
    /// </summary>
    /// <param name="center">The center of the search area.</param>
    /// <param name="range">The radius of the search area.</param>
    /// <param name="result">The found NavMesh point.</param>
    /// <returns>True if a point was found, false otherwise.</returns>
    public bool FindRandomNavMeshPoint(Vector3 center, float range, out Vector3 result)
    {
        for (int i = 0; i < 30; i++) // Try multiple times to find a suitable point
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * range;
            NavMeshHit hit;
            // NavMesh.SamplePosition samples the NavMesh for the closest point to randomPoint within '1.0f' distance.
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        // Debug.LogWarning($"Could not find a random NavMesh point near {center} within {range} for {gameObject.name}.", this);
        return false;
    }

    /// <summary>
    /// Finds the closest GameObject with a specific tag within a search radius.
    /// </summary>
    /// <param name="tag">The tag to search for.</param>
    /// <param name="searchRadius">The maximum distance to search.</param>
    /// <returns>The Transform of the closest target, or null if none found.</returns>
    public Transform FindClosestTargetWithTag(string tag, float searchRadius = 30f)
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
        Transform closestTarget = null;
        float shortestDistanceSqr = Mathf.Infinity; // Use squared distance for efficiency
        Vector3 position = transform.position;

        foreach (GameObject taggedObj in taggedObjects)
        {
            Vector3 directionToTarget = taggedObj.transform.position - position;
            float dSqrToTarget = directionToTarget.sqrMagnitude; // Squared distance

            if (dSqrToTarget < shortestDistanceSqr && dSqrToTarget <= searchRadius * searchRadius)
            {
                shortestDistanceSqr = dSqrToTarget;
                closestTarget = taggedObj.transform;
            }
        }
        return closestTarget;
    }

    /// <summary>
    /// Assigns a new task to the villager, causing a state transition.
    /// </summary>
    /// <param name="taskState">The state representing the task.</param>
    /// <param name="target">Optional target Transform for the task.</param>
    /// <param name="resourceTagForTask">Optional tag if the task involves finding a resource.</param>
    public void AssignTask(VillagerBaseState taskState, Transform target = null, string resourceTagForTask = null)
    {
        // Debug.Log($"AssignTask called for {gameObject.name}: State={taskState.GetType().Name}, Target={(target ? target.name : "null")}, ResourceTag={(string.IsNullOrEmpty(resourceTagForTask) ? "null" : resourceTagForTask)}", this);
        currentTaskTarget = target;
        requiredResourceTag = resourceTagForTask;
        TransitionToState(taskState);
    }
}
