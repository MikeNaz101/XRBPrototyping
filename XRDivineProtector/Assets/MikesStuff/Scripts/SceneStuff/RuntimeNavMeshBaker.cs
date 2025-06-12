// RuntimeNavMeshBaker.cs
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class RuntimeNavMeshBaker : MonoBehaviour
{
    public static RuntimeNavMeshBaker Instance { get; private set; }

    // These are private as they will be found and identified automatically by the script.
    private NavMeshSurface _villagerNavMeshSurface;
    private NavMeshSurface _enemyNavMeshSurface;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Find the grid generator and subscribe to its completion event
        FloorHexGridGenerator_V2 gridGenerator = FindObjectOfType<FloorHexGridGenerator_V2>();
        if (gridGenerator != null)
        {
            gridGenerator.OnGenerationComplete.AddListener(HandleGenerationComplete);
        }
        else
        {
            Debug.LogError("RuntimeNavMeshBaker could not find FloorHexGridGenerator_V2 to subscribe to its event! NavMeshes will not be baked.", this);
        }
    }

    /// <summary>
    /// This method is called by the event from FloorHexGridGenerator_V2 once it's finished.
    /// </summary>
    private void HandleGenerationComplete()
    {
        Debug.Log("RuntimeNavMeshBaker received OnGenerationComplete event. Starting NavMesh baking process.");
        
        // Find the NavMesh Surfaces now that the scene is ready.
        if (!FindAndIdentifyNavMeshSurfaces())
        {
            Debug.LogError("Baking process aborted because one or more NavMeshSurfaces were not found or identified correctly.");
            return;
        }

        // Bake both NavMeshes in order.
        BakeVillagerNavMesh();
        BakeEnemyNavMesh();
    }
    
    /// <summary>
    /// Finds all NavMeshSurface components and identifies them by their configured Agent Type ID.
    /// </summary>
    /// <returns>True if both required surfaces were found, false otherwise.</returns>
    private bool FindAndIdentifyNavMeshSurfaces()
    {
        // --- UPDATED: More robust method to find and identify surfaces ---

        // Find all NavMeshSurface components currently active in the scene.
        NavMeshSurface[] allSurfaces = FindObjectsOfType<NavMeshSurface>();

        if (allSurfaces.Length < 2)
        {
            Debug.LogError("Found fewer than 2 NavMeshSurfaces in the scene. Make sure both 'NavMesh_Villagers' and 'NavMesh_Enemies' exist and have a NavMeshSurface component.");
            return false;
        }
        
        foreach (NavMeshSurface surface in allSurfaces)
        {
            // For each surface, get the name of the agent type it is configured for.
            string agentTypeName = NavMesh.GetSettingsNameFromID(surface.agentTypeID);
            
            if (agentTypeName == "Villager")
            {
                _villagerNavMeshSurface = surface;
                Debug.Log("Found and assigned Villager NavMesh Surface.", surface.gameObject);
            }
            else if (agentTypeName == "Enemy")
            {
                _enemyNavMeshSurface = surface;
                Debug.Log("Found and assigned Enemy NavMesh Surface.", surface.gameObject);
            }
        }
        
        // Final check to ensure both were found and assigned.
        if (_villagerNavMeshSurface == null)
        {
            Debug.LogError("Failed to find a NavMeshSurface configured with the 'Villager' Agent Type. Check the component settings in the Inspector.");
            return false;
        }
        if (_enemyNavMeshSurface == null)
        {
            Debug.LogError("Failed to find a NavMeshSurface configured with the 'Enemy' Agent Type. Check the component settings in the Inspector.");
            return false;
        }
        
        return true;
    }

    private void BakeVillagerNavMesh()
    {
        if (_villagerNavMeshSurface != null)
        {
            Debug.Log("Baking Villager NavMesh...");
            _villagerNavMeshSurface.BuildNavMesh();
        }
    }
    
    private void BakeEnemyNavMesh()
    {
        // First, we need to find the spawned zones and parent them to the enemy NavMesh surface.
        GameObject spawnZoneParent = GameObject.Find("EnemySpawnZones");
        if (spawnZoneParent != null && _enemyNavMeshSurface != null)
        {
            // Parent the zones to the NavMeshSurface's transform so it knows to bake them
            spawnZoneParent.transform.SetParent(_enemyNavMeshSurface.transform);
            
            Debug.Log("Baking Enemy NavMesh...");
            _enemyNavMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogError("Enemy NavMesh Surface reference is missing OR 'EnemySpawnZones' parent not found! Cannot bake enemy NavMesh.");
        }
    }

    // Clean up listener when this object is destroyed
    void OnDestroy()
    {
        FloorHexGridGenerator_V2 gridGenerator = FindObjectOfType<FloorHexGridGenerator_V2>();
        if (gridGenerator != null)
        {
            gridGenerator.OnGenerationComplete.RemoveListener(HandleGenerationComplete);
        }
    }
}
