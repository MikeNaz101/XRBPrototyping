// EnemySpawner.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    public GameObject enemyPrefab;
    public float spawnInterval = 10f; // How often (in seconds) to spawn a new enemy
    public string spawnZoneTag = "EnemySpawnZone";

    private GameObject[] _spawnZones;
    private float _timer;
    private bool _isReadyToSpawn = false; // Flag to control the update loop

    void Start()
    {
        // Find the grid generator and subscribe to its completion event
        FloorHexGridGenerator_V2 gridGenerator = FindObjectOfType<FloorHexGridGenerator_V2>();
        if (gridGenerator != null)
        {
            gridGenerator.OnGenerationComplete.AddListener(OnGridReady);
        }
        else
        {
            Debug.LogError("EnemySpawner could not find the FloorHexGridGenerator_V2 to subscribe to its event.", this);
        }
    }

    /// <summary>
    /// This method is called by the event from FloorHexGridGenerator_V2 once it's finished.
    /// </summary>
    void OnGridReady()
    {
        Debug.Log("EnemySpawner received OnGridReady event. Finding spawn zones.");
        _spawnZones = GameObject.FindGameObjectsWithTag(spawnZoneTag);
        if (_spawnZones == null || _spawnZones.Length == 0)
        {
            Debug.LogError($"EnemySpawner could not find any GameObjects with the tag '{spawnZoneTag}' even after the grid was ready.", this);
            enabled = false;
            return;
        }
        _isReadyToSpawn = true; // Enable the spawner
    }

    void Update()
    {
        // Only run the timer if the spawner is ready
        if (!_isReadyToSpawn) return;

        _timer += Time.deltaTime;

        if (_timer >= spawnInterval)
        {
            SpawnSingleEnemy();
            _timer = 0f; // Reset timer
        }
    }

    void SpawnSingleEnemy()
    {
        if (enemyPrefab == null) return;

        // Pick a random spawn zone plane
        GameObject randomZone = _spawnZones[Random.Range(0, _spawnZones.Length)];
        Collider zoneCollider = randomZone.GetComponent<Collider>();
        if (zoneCollider == null) return;

        // Find a random point within the bounds of the spawn zone's collider
        Bounds bounds = zoneCollider.bounds;
        Vector3 randomPoint = new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.center.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );

        // Find the closest valid point on the NavMesh to our random point.
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            Vector3 spawnPosition = hit.position;
            GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            // Assign a patrol route (optional, but good to have)
            var botController = enemyInstance.GetComponent<PatrolBotController>();
            if (botController != null)
            {
                // Give it a simple two-point patrol from its spawn to the center of the zone
                botController.patrolPoints = new List<Vector3> { spawnPosition, randomZone.transform.position };
            }

            // Set Area Costs for this specific enemy
            NavMeshAgent agent = enemyInstance.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                int wallAreaIndex = NavMesh.GetAreaFromName("Walls");
                if (wallAreaIndex != -1)
                {
                    agent.SetAreaCost(wallAreaIndex, 1.0f);
                }
            }
        }
        else
        {
            Debug.LogWarning($"Could not find a valid NavMesh position in spawn zone '{randomZone.name}'. Skipping enemy spawn.");
        }
    }
}
