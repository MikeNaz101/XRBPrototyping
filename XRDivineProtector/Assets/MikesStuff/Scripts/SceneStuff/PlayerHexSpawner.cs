// PlayerHexSpawner.cs
using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RayInteractor), typeof(PhysicsRaycaster))]
public class PlayerHexSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    public List<SpawnableItemData> availableItemsToSpawn;

    [Header("Interaction Layers")]
    public LayerMask worldInteractionMask;
    public LayerMask uiInteractionMask;
    
    // Public property for the currently hovered hex
    public HexCell CurrentlyHoveredHex { get; private set; }

    private PhysicsRaycaster _physicsRaycaster;
    private HexCell _lastHoveredHex;

    void Awake()
    {
        _physicsRaycaster = GetComponent<PhysicsRaycaster>();
    }

    void Start()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.availableUnitsToSpawn = this.availableItemsToSpawn;
            UIManager.Instance.playerHexSpawnerReference = this;
        }
        EnableWorldInteraction();
    }
    
    void Update()
    {
        UpdateHoveredHex();
    }

    void UpdateHoveredHex()
    {
        // This raycast is just to determine the currently hovered hex for visual feedback
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        HexCell newlyHoveredHex = null;
        if (_physicsRaycaster.eventMask == worldInteractionMask && Physics.Raycast(ray, out hit, 100f, worldInteractionMask))
        {
            newlyHoveredHex = hit.collider.GetComponent<HexCell>();
        }
        
        CurrentlyHoveredHex = newlyHoveredHex;

        if (CurrentlyHoveredHex != _lastHoveredHex)
        {
            _lastHoveredHex?.OnPointerExit();
            CurrentlyHoveredHex?.OnPointerEnter();
            _lastHoveredHex = CurrentlyHoveredHex;
        }
    }

    // This public method is called by your Ray Interactor system when a hex cell is selected.
    public void HandleHexSelection(GameObject selectedHexObject)
    {
        if (_physicsRaycaster.eventMask != worldInteractionMask) return;

        HexCell selectedCell = selectedHexObject.GetComponent<HexCell>();
        if(selectedCell == null) return;

        if (selectedCell.IsOccupied) 
        { 
            UIManager.Instance.DisplayObjectActionsMenu(selectedCell.SpawnedObject, selectedCell.transform); 
        }
        else 
        { 
            UIManager.Instance.DisplaySpawnSelectionMenu(selectedCell, selectedCell.transform); 
        }
    }

    public void RequestSpawnOnHex(HexCell targetHex, SpawnableItemData itemData)
    {
        if (itemData == null || itemData.itemPrefab == null || targetHex == null)
        {
            Debug.LogError("Invalid data provided for spawning request.");
            return;
        }

        Vector3 spawnPosition;
        Quaternion spawnRotation = targetHex.transform.rotation; // Start with the hex's default flat rotation

        if (itemData.isWall)
        {
            // Use special position for walls
            spawnPosition = targetHex.WorldCenter + new Vector3(0, 0.05f, 0);

            // --- Auto-Orientation Logic for Walls ---
            HexCell neighborWithWall = null;
            foreach (HexCell neighbor in targetHex.Neighbors)
            {
                // We need a way to identify walls. Let's assume a "Wall" tag or a component.
                if (neighbor.IsOccupied && neighbor.SpawnedObject.CompareTag("Wall")) 
                {
                    neighborWithWall = neighbor;
                    break; // Connect to the first found wall
                }
            }

            if (neighborWithWall != null)
            {
                // Auto-orient to connect to the neighbor wall
                Vector3 directionToNeighbor = (neighborWithWall.WorldCenter - targetHex.WorldCenter).normalized;
                // Look towards the neighbor, keeping the 'up' vector aligned with the hex's 'up'
                spawnRotation = Quaternion.LookRotation(directionToNeighbor, targetHex.transform.up);
            }
            // If no neighbor wall is found, it will just use the default spawnRotation.
        }
        else // For all other non-wall objects
        {
            spawnPosition = targetHex.WorldCenter + new Vector3(0, 0.04594f, 0);
        }

        // --- Spawn the final object ---
        GameObject spawnedInstance = Instantiate(itemData.itemPrefab, spawnPosition, spawnRotation);
        
        // --- UPDATED ---
        // Apply scaling ONLY to objects that are NOT walls.
        if (!itemData.isWall)
        {
            spawnedInstance.transform.localScale = Vector3.one * 1f;
        }
        // If it is a wall, its scale will remain the same as the prefab's scale.
        
        if (!targetHex.TryOccupy(spawnedInstance))
        {
            Debug.LogWarning($"Failed to occupy hex {targetHex.name}. Destroying instance.");
            Destroy(spawnedInstance);
        }

        UIManager.Instance.CloseAllMenus(); // Close spawn menu after action is complete
    }
    
    // The following interaction state methods are still needed by the UIManager
    public void EnableUIInteraction()
    {
        if (_physicsRaycaster != null) _physicsRaycaster.eventMask = uiInteractionMask;
    }

    public void EnableWorldInteraction()
    {
        if (_physicsRaycaster != null) _physicsRaycaster.eventMask = worldInteractionMask;
    }
}
