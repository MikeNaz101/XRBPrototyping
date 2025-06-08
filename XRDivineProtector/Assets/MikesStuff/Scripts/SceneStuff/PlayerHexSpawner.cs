// PlayerHexSpawner.cs
using UnityEngine;
using System.Collections.Generic;
using Oculus.Interaction; // Still required for RayInteractor, assuming it's part of the setup
using UnityEngine.EventSystems; // Required for Unity's PhysicsRaycaster

[RequireComponent(typeof(RayInteractor), typeof(PhysicsRaycaster))] // Ensure both components are present
public class PlayerHexSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    public List<SpawnableItemData> availableItemsToSpawn;

    [Header("Interaction Layers")]
    public LayerMask worldInteractionMask; // Assign your 'HexGridInteractive' layer here
    public LayerMask uiInteractionMask;    // Assign your 'UI' layer here

    // --- UPDATED ---
    private PhysicsRaycaster _physicsRaycaster; // Reference to Unity's standard PhysicsRaycaster

    void Awake()
    {
        // Get the standard PhysicsRaycaster component on this same GameObject
        _physicsRaycaster = GetComponent<PhysicsRaycaster>();
        if (_physicsRaycaster == null)
        {
            Debug.LogError("PlayerHexSpawner requires a UnityEngine.EventSystems.PhysicsRaycaster component on the same GameObject.", this);
        }
    }

    void Start()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("UIManager not found in the scene. PlayerHexSpawner needs it to function.");
            return;
        }
        // Pass necessary references to UIManager
        UIManager.Instance.availableUnitsToSpawn = this.availableItemsToSpawn;
        UIManager.Instance.playerHexSpawnerReference = this;

        if (availableItemsToSpawn == null || availableItemsToSpawn.Count == 0)
        {
            Debug.LogWarning("PlayerHexSpawner: No 'Available Items To Spawn' configured.");
        }

        // Start by allowing interaction with the world (hex grid)
        EnableWorldInteraction();
    }

    // This public method is called by your Ray Interactor system when a hex cell is selected.
    public void HandleHexSelection(GameObject selectedHexObject)
    {
        // --- UPDATED ---
        // Only handle selection if the raycaster is currently set to interact with the world
        if (_physicsRaycaster != null && _physicsRaycaster.eventMask != worldInteractionMask)
        {
            return;
        }

        if (UIManager.Instance == null) return;

        HexCell selectedCell = selectedHexObject.GetComponent<HexCell>();
        if (selectedCell == null)
        {
            Debug.LogError("Selected object is not a HexCell!", selectedHexObject);
            return;
        }

        if (selectedCell.IsOccupied)
        {
            if (selectedCell.SpawnedObject != null)
            {
                UIManager.Instance.DisplayObjectActionsMenu(selectedCell.SpawnedObject, selectedCell.transform);
            }
        }
        else
        {
            UIManager.Instance.DisplaySpawnSelectionMenu(selectedCell, selectedCell.transform);
        }
    }

    // This method is called by a UI button from the SpawnSelectionCanvas
    public void RequestSpawnOnHex(HexCell targetHex, GameObject itemPrefabToSpawn)
    {
        if (itemPrefabToSpawn == null)
        {
            Debug.LogError("PlayerHexSpawner: Item Prefab to Spawn is null in request!");
            return;
        }
        if (targetHex == null)
        {
            Debug.LogError("PlayerHexSpawner: Target Hex is null in request!");
            return;
        }

        GameObject spawnedInstance = Instantiate(itemPrefabToSpawn, targetHex.transform);
        spawnedInstance.transform.localPosition = new Vector3(0f, 0.00444f, 0f);
        spawnedInstance.transform.localScale = Vector3.one * 0.000508f;

        if (targetHex.TryOccupy(spawnedInstance))
        {
            Debug.Log($"Successfully spawned '{itemPrefabToSpawn.name}' on hex {targetHex.name}.");
        }
        else
        {
            Debug.LogWarning($"Failed to occupy hex {targetHex.name} after spawning {itemPrefabToSpawn.name}. Destroying instance.");
            Destroy(spawnedInstance);
        }
        UIManager.Instance.CloseAllMenus();
    }

    // Public methods for UIManager to call
    public void EnableUIInteraction()
    {
        // --- UPDATED ---
        if (_physicsRaycaster != null)
        {
            Debug.Log("Switching Raycaster to UI interaction.");
            _physicsRaycaster.eventMask = uiInteractionMask;
        }
    }

    public void EnableWorldInteraction()
    {
        // --- UPDATED ---
        if (_physicsRaycaster != null)
        {
            Debug.Log("Switching Raycaster to World interaction.");
            _physicsRaycaster.eventMask = worldInteractionMask;
        }
    }
}
