using UnityEngine;
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for using Lists

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    [Tooltip("A list of different enemy prefabs that can be spawned.")]
    public List<GameObject> enemyPrefabs;

    [Header("Spawning Settings")]
    [Tooltip("Time in seconds between each individual enemy spawn within a wave.")]
    public float spawnInterval = 1.0f; // This is now for time between spawns in a wave
    [Tooltip("The Transform where enemies will be spawned. If null, spawns at this spawner's position.")]
    public Transform spawnPoint;
    [Tooltip("Radius around the spawn point. Enemies will spawn at a random offset within this radius.")]
    public float spawnRadius = 5.0f;

    // Wave control variables
    private int _enemiesToSpawnThisWave;
    private int _enemiesSpawnedThisWaveCount;
    private bool _isWaveActive = false;
    private int _currentWaveNumber; // Optional: for varying enemy types/difficulty per wave
    private Coroutine _spawnWaveCoroutine;

    void Start()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogError($"[{nameof(EnemySpawner)}] No enemy prefabs assigned! Spawner will not function.", this);
            enabled = false; // Disable the spawner if no prefabs
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] Spawn Point not assigned. Enemies will spawn at the spawner's position: {transform.name}", this);
            spawnPoint = this.transform; // Default to this spawner's transform
        }
        _isWaveActive = false; // Not active until told by GameplayLoopManager
    }

    // Public method to be called by GameplayLoopManager
    public void StartNewWave(int numberOfEnemies, int waveNumber)
    {
        if (enemyPrefabs.Count == 0)
        {
            Debug.LogError($"[{nameof(EnemySpawner)}] Cannot start wave, no enemy prefabs assigned.", this);
            return;
        }
        if (numberOfEnemies <= 0)
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] StartNewWave called with numberOfEnemies <= 0. No enemies will be spawned for wave {waveNumber}.", this);
            _isWaveActive = false; // Ensure it's not considered active
            return;
        }

        _enemiesToSpawnThisWave = numberOfEnemies;
        _currentWaveNumber = waveNumber;
        _enemiesSpawnedThisWaveCount = 0;
        _isWaveActive = true;

        Debug.Log($"[{nameof(EnemySpawner)}] Starting Wave {waveNumber}. Target enemies: {_enemiesToSpawnThisWave}. Spawning one by one.");

        // Stop any previous wave spawning coroutine before starting a new one
        if (_spawnWaveCoroutine != null)
        {
            StopCoroutine(_spawnWaveCoroutine);
        }
        _spawnWaveCoroutine = StartCoroutine(SpawnWaveEnemies());
    }

    IEnumerator SpawnWaveEnemies()
    {
        while (_enemiesSpawnedThisWaveCount < _enemiesToSpawnThisWave)
        {
            SpawnRandomEnemy();
            _enemiesSpawnedThisWaveCount++;

            if (_enemiesSpawnedThisWaveCount < _enemiesToSpawnThisWave) // Only wait if more enemies are to be spawned in this wave
            {
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                // All enemies for this wave have been requested for spawning
                break; 
            }
        }
        _isWaveActive = false; // Mark wave as complete from spawner's perspective
        Debug.Log($"[{nameof(EnemySpawner)}] Wave {_currentWaveNumber} spawning process complete. {_enemiesSpawnedThisWaveCount} enemies initiated for spawn.");
        _spawnWaveCoroutine = null;
    }


    void SpawnRandomEnemy()
    {
        // This method is now called internally by SpawnWaveEnemies coroutine

        if (enemyPrefabs.Count == 0) return; 

        int randomIndex = Random.Range(0, enemyPrefabs.Count);
        GameObject prefabToSpawn = enemyPrefabs[randomIndex];

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] Prefab at index {randomIndex} is null. Skipping this spawn.", this);
            return;
        }

        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        // Optional: Flatten y if spawning on a 2D plane relative to spawnPoint
        // randomOffset.y = 0; 
        Vector3 actualSpawnPosition = spawnPoint.position + randomOffset;

        GameObject newEnemy = Instantiate(prefabToSpawn, actualSpawnPosition, spawnPoint.rotation);
        // Debug.Log($"Spawned enemy: {newEnemy.name} at {actualSpawnPosition} for wave {_currentWaveNumber}");

        // The Enemy script itself should handle notifying GameplayLoopManager upon its death.
        // This spawner is now primarily concerned with initiating the spawns for the wave.
    }

    // The Update method is no longer needed for continuous timed spawning,
    // as spawning is now triggered by StartNewWave and handled by the coroutine.
    // void Update() { }

    // ReportEnemyDestroyed() is no longer needed here.
    // GameplayLoopManager tracks enemies remaining based on Enemy.Die() notifications.

    void OnDrawGizmosSelected()
    {
        Transform pointToDraw = spawnPoint != null ? spawnPoint : transform;
        Gizmos.color = Color.red; // Changed color to distinguish from other gizmos
        Gizmos.DrawWireSphere(pointToDraw.position, spawnRadius);
    }

    void OnDisable()
    {
        // Stop any active spawning if the spawner itself is disabled
        if (_spawnWaveCoroutine != null)
        {
            StopCoroutine(_spawnWaveCoroutine);
            _spawnWaveCoroutine = null;
        }
        _isWaveActive = false;
    }
}
