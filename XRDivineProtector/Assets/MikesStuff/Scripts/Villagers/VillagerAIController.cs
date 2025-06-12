// VillagerAIController.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class VillagerAIController : MonoBehaviour
{
    [Header("Wander Settings")]
    [Tooltip("Minimum time the villager will wait at a destination.")]
    public float minWaitTime = 2.0f;
    [Tooltip("Maximum time the villager will wait at a destination.")]
    public float maxWaitTime = 5.0f;

    private NavMeshAgent _agent;
    private FloorHexGridGenerator_V2 _gridGenerator;
    private List<HexCell> _walkableHexes;
    private float _waitTimer;
    private bool _isWaiting;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    void Start()
    {
        // Find the grid generator to get a list of places to walk to.
        _gridGenerator = FindObjectOfType<FloorHexGridGenerator_V2>();
        if (_gridGenerator != null)
        {
            _walkableHexes = _gridGenerator.GetAllHexCells();
        }
        else
        {
            Debug.LogError("VillagerAIController could not find the FloorHexGridGenerator_V2!", this);
            enabled = false; // Disable this script if there's no grid to walk on.
            return;
        }

        // Start wandering immediately.
        GoToNextDestination();
    }

    void Update()
    {
        // If the villager is waiting at a destination...
        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0)
            {
                // Time's up, find a new place to go.
                GoToNextDestination();
            }
        }
        // If the villager is not waiting and has reached its destination...
        else if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            // Start waiting.
            _isWaiting = true;
            _waitTimer = Random.Range(minWaitTime, maxWaitTime);
        }
    }

    /// <summary>
    /// Finds a new random hex cell and sets it as the agent's destination.
    /// </summary>
    void GoToNextDestination()
    {
        _isWaiting = false;
        if (_walkableHexes == null || _walkableHexes.Count == 0)
        {
            Debug.LogWarning("No walkable hexes found for villager to wander to.", this);
            return;
        }

        // Pick a random hex from the list.
        HexCell randomHex = _walkableHexes[Random.Range(0, _walkableHexes.Count)];

        // Set the destination on the NavMesh.
        _agent.SetDestination(randomHex.WorldCenter);
    }
}
