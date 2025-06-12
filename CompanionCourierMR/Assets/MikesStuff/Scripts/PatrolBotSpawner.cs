using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using Unity.AI.Navigation; // Required for NavMeshSurface
using UnityEngine.AI;

/// <summary>
/// Spawns patrol bots onto a NavMesh that is generated and managed
/// by the official SceneNavigation component.
/// </summary>
public class PatrolBotSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("The Patrol Bot prefab to spawn. It must have a NavMeshAgent component.")]
    public GameObject patrolBotPrefab;
    [Tooltip("The number of bots to spawn in the room.")]
    public int numberOfBots = 3;

    [Header("Patrol Route Generation")]
    [Tooltip("The minimum number of points in a patrol route.")]
    [Range(2, 10)]
    public int minPatrolPoints = 2;
    [Tooltip("The maximum number of points in a patrol route.")]
    [Range(2, 10)]
    public int maxPatrolPoints = 4;

    private bool hasSpawned = false;
    private SceneNavigation _sceneNavigation;

    void Awake()
    {
        // Find the SceneNavigation component in the scene.
        _sceneNavigation = FindFirstObjectByType<SceneNavigation>();
        if (_sceneNavigation == null)
        {
            Debug.LogError($"[{nameof(PatrolBotSpawner)}] Could not find a 'SceneNavigation' component in the scene. This script requires it to function. Please add one.", this);
            enabled = false;
            return;
        }

        // Initialize the Agents list in Awake().
        if (_sceneNavigation.Agents == null)
        {
            _sceneNavigation.Agents = new List<NavMeshAgent>();
        }
    }

    void Start()
    {
        // Subscribe to the event from the SceneNavigation script.
        _sceneNavigation.OnNavMeshInitialized.AddListener(OnNavMeshReady);
    }

    private void OnDisable()
    {
        // Always unsubscribe from events.
        if (_sceneNavigation)
        {
            _sceneNavigation.OnNavMeshInitialized.RemoveListener(OnNavMeshReady);
        }
    }

    /// <summary>
    /// This method is called by the SceneNavigation's event once the NavMesh has been successfully built.
    /// </summary>
    void OnNavMeshReady()
    {
        if (hasSpawned) return;

        Debug.Log($"[{nameof(PatrolBotSpawner)}] OnNavMeshReady event received. Spawning bots.");

        var mrukRoom = MRUK.Instance.GetCurrentRoom();
        if (mrukRoom == null)
        {
            Debug.LogError($"[{nameof(PatrolBotSpawner)}] Could not find current room.", this);
            return;
        }

        // Find all floor anchors to determine valid spawning areas.
        List<MRUKAnchor> floorAnchors = mrukRoom.Anchors
            .Where(anchor => anchor.HasLabel(MRUKAnchor.SceneLabels.FLOOR.ToString()))
            .ToList();

        if (floorAnchors.Count == 0)
        {
            Debug.LogWarning($"[{nameof(PatrolBotSpawner)}] No floor anchors found. Cannot spawn patrol bots.", this);
            return;
        }

        // Spawn the bots.
        for (int i = 0; i < numberOfBots; i++)
        {
            SpawnBot(floorAnchors);
        }

        hasSpawned = true;
    }

    /// <summary>
    /// Spawns a single bot with a dynamically generated patrol route on the pre-existing NavMesh.
    /// </summary>
    void SpawnBot(List<MRUKAnchor> floorAnchors)
    {
        if (patrolBotPrefab == null) return;

        MRUKAnchor chosenFloor = floorAnchors[Random.Range(0, floorAnchors.Count)];

        Vector3 startPos;
        if (!TryGetValidPointOnNavMesh(chosenFloor, out startPos))
        {
            Debug.LogWarning($"[{nameof(PatrolBotSpawner)}] Failed to find a valid starting point on the NavMesh for a bot spawn attempt. Skipping bot.", this);
            return;
        }
        
        List<Vector3> route = GeneratePatrolRoute(startPos, chosenFloor);
        if (route.Count < minPatrolPoints)
        {
            Debug.LogWarning($"[{nameof(PatrolBotSpawner)}] Could not generate a valid patrol route with enough points. Skipping bot spawn.", this);
            return;
        }

        GameObject botObject = Instantiate(patrolBotPrefab, startPos, Quaternion.identity);
        
        // --- FIX: Ensure the spawned bot uses the correct NavMesh Agent Type ---
        NavMeshAgent spawnedAgent = botObject.GetComponent<NavMeshAgent>();
        NavMeshSurface navMeshSurface = _sceneNavigation.GetComponent<NavMeshSurface>();

        if (spawnedAgent != null && navMeshSurface != null)
        {
            // Set the agent's type ID to match the one used to bake the NavMesh.
            spawnedAgent.agentTypeID = navMeshSurface.agentTypeID;
            Debug.Log($"Assigned Agent Type ID {navMeshSurface.agentTypeID} to spawned bot.", botObject);
        }
        else
        {
            Debug.LogError("Failed to assign agentTypeID. Spawned bot is missing a NavMeshAgent or SceneNavigation is missing a NavMeshSurface.", botObject);
        }
        // --- END FIX ---

        var botController = botObject.GetComponent<PatrolBotController>();
        if (botController != null)
        {
            botController.patrolPoints = route;
        }
    }
    
    /// <summary>
    /// Generates a list of valid, reachable points on the NavMesh for a patrol route.
    /// </summary>
    List<Vector3> GeneratePatrolRoute(Vector3 startPoint, MRUKAnchor floorAnchor)
    {
        List<Vector3> route = new List<Vector3> { startPoint };
        int pointsToGenerate = Random.Range(minPatrolPoints, maxPatrolPoints + 1);

        Vector3 lastPoint = startPoint;
        for (int i = 1; i < pointsToGenerate; i++)
        {
            Vector3 nextPoint;
            if (TryGetValidPointOnNavMesh(floorAnchor, out nextPoint))
            {
                // Verify the new point is actually reachable from the last point.
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(lastPoint, nextPoint, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    route.Add(nextPoint);
                    lastPoint = nextPoint;
                }
            }
        }
        return route;
    }

    /// <summary>
    /// A robust helper method to find a random, valid point on the NavMesh.
    /// </summary>
    bool TryGetValidPointOnNavMesh(MRUKAnchor anchor, out Vector3 validPoint)
    {
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            if (!anchor.PlaneRect.HasValue) continue;
            Rect planeRect = anchor.PlaneRect.Value;
            float randomX = Random.Range(-planeRect.width / 2, planeRect.width / 2);
            float randomY = Random.Range(-planeRect.height / 2, planeRect.height / 2);
            Vector3 randomPoint = anchor.transform.TransformPoint(new Vector3(randomX, randomY, 0));

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                validPoint = hit.position;
                return true;
            }
        }
        
        validPoint = Vector3.zero;
        return false;
    }
}
