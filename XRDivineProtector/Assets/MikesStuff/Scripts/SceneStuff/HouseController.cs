// HouseController.cs
using UnityEngine;
using UnityEngine.AI; // Required for NavMeshAgent

public class HouseController : MonoBehaviour
{
    [Header("Villager Spawning")]
    [Tooltip("The Villager prefab this house will spawn.")]
    public GameObject villagerPrefab;
    [Tooltip("How often (in seconds) to spawn a new villager.")]
    public float spawnInterval = 10f;
    [Tooltip("The position relative to the house where villagers will appear.")]
    public Vector3 spawnPointOffset = new Vector3(0, 0, -1f); // e.g., in front of the door

    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= spawnInterval)
        {
            SpawnVillager();
            _timer = 0f; // Reset the timer
        }
    }

    void SpawnVillager()
    {
        if (villagerPrefab == null)
        {
            Debug.LogError("House is missing its Villager Prefab!", this);
            return;
        }

        // Calculate the world-space position for the spawn point
        Vector3 worldSpawnPoint = transform.TransformPoint(spawnPointOffset);

        // Find the closest valid point on the NavMesh to our spawn point
        if (NavMesh.SamplePosition(worldSpawnPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
        {
            GameObject villagerInstance = Instantiate(villagerPrefab, hit.position, Quaternion.identity);

            // Set Area Costs for this specific villager so they avoid walls
            NavMeshAgent agent = villagerInstance.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                int wallAreaIndex = NavMesh.GetAreaFromName("Walls");
                if (wallAreaIndex != -1)
                {
                    agent.SetAreaCost(wallAreaIndex, 1000f);
                }
            }
            // Note: This villager will not have a pre-defined patrol route.
            // You would need a different AI script for them (e.g., wander, find work, etc.)
            // or modify this to also generate a route.
        }
        else
        {
            Debug.LogWarning("Could not find a valid NavMesh position near the house's spawn point.", this);
        }
    }
}
