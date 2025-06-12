using UnityEngine;

public class GoalManager : MonoBehaviour
{
    [Header("Object References")]
    [Tooltip("The prefab for the visual goal area. It must have a trigger collider and the GoalTrigger script.")]
    public GameObject goalAreaPrefab;
    [Tooltip("The UI panel or effect to show when the player wins.")]
    public GameObject winScreenPrefab;
    [Tooltip("The tag assigned to your Resonance Core/Companion Sphere prefab.")]
    public string resonanceCoreTag = "ResonanceCore";

    // --- Private Variables ---
    private ResonanceCoreSpawner _coreSpawner;
    private Transform _resonanceCore;
    private bool _isGoalSetup = false;
    private GameObject _winScreenInstance;

    void Start()
    {
        // Find the spawner so we can know when the core has been created.
        _coreSpawner = FindFirstObjectByType<ResonanceCoreSpawner>();
        if (_coreSpawner == null)
        {
            Debug.LogError($"[{nameof(GoalManager)}] Cannot find the ResonanceCoreSpawner in the scene. The goal cannot be placed.", this);
            enabled = false;
        }
    }

    void Update()
    {
        // This loop runs only until the goal has been successfully placed.
        if (!_isGoalSetup)
        {
            // First, find the Resonance Core once it has been spawned.
            if (_resonanceCore == null)
            {
                GameObject coreObject = GameObject.FindGameObjectWithTag(resonanceCoreTag);
                if (coreObject != null)
                {
                    _resonanceCore = coreObject.transform;
                    Debug.Log($"[{nameof(GoalManager)}] Found Resonance Core. Proceeding to set up goal area.");
                    SetupGoalArea();
                }
            }
        }
    }

    /// <summary>
    /// Calculates the goal position and instantiates the goal area prefab.
    /// </summary>
    void SetupGoalArea()
    {
        var mrukRoom = Meta.XR.MRUtilityKit.MRUK.Instance.GetCurrentRoom();
        if (mrukRoom == null)
        {
            Debug.LogError($"[{nameof(GoalManager)}] MRUK Room not found. Cannot calculate goal position.", this);
            return;
        }

        // Get the overall bounds of the room to find its center.
        Bounds roomBounds = mrukRoom.GetRoomBounds();
        Vector3 roomCenter = roomBounds.center;

        // Get the starting position of the Resonance Core.
        Vector3 coreStartPosition = _resonanceCore.position;

        // Calculate the direction from the spawn point, through the center, to the opposite side.
        Vector3 directionToOppositeSide = (roomCenter - coreStartPosition).normalized;

        // Find a point far on the opposite side.
        Vector3 farOppositePoint = coreStartPosition + directionToOppositeSide * (roomBounds.size.magnitude);

        // Raycast from high above this point down to the floor to get an accurate ground position.
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(farOppositePoint.x, roomBounds.max.y + 1, farOppositePoint.z), Vector3.down, out hit))
        {
            Vector3 goalPosition = hit.point;

            if (goalAreaPrefab != null)
            {
                // Instantiate the goal area at the calculated position.
                GameObject goalInstance = Instantiate(goalAreaPrefab, goalPosition, Quaternion.identity);

                // Find the GoalTrigger script on the new instance and pass a reference to this manager.
                GoalTrigger goalTrigger = goalInstance.GetComponent<GoalTrigger>();
                if (goalTrigger != null)
                {
                    goalTrigger.SetGoalManager(this);
                }
                else
                {
                    Debug.LogError($"[{nameof(GoalManager)}] The 'goalAreaPrefab' is missing the GoalTrigger script!", goalAreaPrefab);
                }
                
                _isGoalSetup = true; // Mark setup as complete.
                Debug.Log($"[{nameof(GoalManager)}] Goal area has been placed at {goalPosition}", goalInstance);
            }
        }
        else
        {
            Debug.LogWarning($"[{nameof(GoalManager)}] Could not find a valid floor position on the opposite side of the room. Goal not placed.");
        }
    }

    /// <summary>
    /// This public method is called by the GoalTrigger when the player wins.
    /// </summary>
    public void PlayerWon()
    {
        Debug.Log("PLAYER HAS WON!");
        
        // Prevent the win screen from spawning multiple times.
        if (winScreenPrefab != null && _winScreenInstance == null)
        {
            _winScreenInstance = Instantiate(winScreenPrefab);
            
            // Optional: Freeze time, disable bots, etc.
            // Time.timeScale = 0; 
        }
    }
}