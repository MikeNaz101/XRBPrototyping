// UIManager.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Configuration")]
    public float menuHeightOffset = 1.25f;

    [Header("Canvas & Button Prefabs")]
    public GameObject spawnSelectionCanvasPrefab;
    public GameObject objectActionsCanvasPrefab;
    public GameObject uiButtonPrefab;

    [Header("Runtime Canvases")]
    private GameObject currentSpawnSelectionCanvas;
    private GameObject currentObjectActionsCanvas;

    public List<SpawnableItemData> availableUnitsToSpawn;
    public PlayerHexSpawner playerHexSpawnerReference;

    private HexCell activeHexForSpawning;
    private bool isMenuOpen = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void DisplaySpawnSelectionMenu(HexCell targetHex, Transform anchorTransform)
    {
        if (isMenuOpen) return;
        isMenuOpen = true;

        if (spawnSelectionCanvasPrefab == null || uiButtonPrefab == null)
        {
            Debug.LogError("A UI prefab is not assigned to the UIManager!");
            isMenuOpen = false;
            return;
        }

        // --- NEW: Switch to UI Interaction ---
        if (playerHexSpawnerReference != null)
        {
            playerHexSpawnerReference.EnableUIInteraction();
        }

        CloseAllMenus(false); // Close other menus without reverting layer mask yet
        activeHexForSpawning = targetHex;

        Vector3 canvasPosition = anchorTransform.position + anchorTransform.up * menuHeightOffset;
        Quaternion canvasRotation = Quaternion.LookRotation(canvasPosition - Camera.main.transform.position, Vector3.up);
        currentSpawnSelectionCanvas = Instantiate(spawnSelectionCanvasPrefab, canvasPosition, canvasRotation);

        Transform buttonParent = currentSpawnSelectionCanvas.transform.Find("ButtonLayout");
        if (buttonParent == null) {
            Debug.LogError("ButtonLayout object not found in SpawnSelectionCanvasPrefab!");
            // Revert interaction state if menu fails to open properly
            if (playerHexSpawnerReference != null) playerHexSpawnerReference.EnableWorldInteraction();
            isMenuOpen = false;
            return;
        }
        
        // ... (rest of the button population logic remains the same) ...
        foreach (Transform child in buttonParent) { Destroy(child.gameObject); }
        foreach (SpawnableItemData itemData in availableUnitsToSpawn)
        {
            if (itemData == null || itemData.itemPrefab == null) continue;
            GameObject buttonGO = Instantiate(uiButtonPrefab, buttonParent);
            // ... (button text, icon, and listener setup as before) ...
            TextMeshProUGUI buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null) buttonText.text = itemData.itemName;
            
            Button buttonComponent = buttonGO.GetComponent<Button>();
            if (buttonComponent != null)
            {
                SpawnableItemData currentItemData = itemData;
                buttonComponent.onClick.AddListener(() => {
                    if (playerHexSpawnerReference != null && activeHexForSpawning != null) {
                        // --- CHANGE THIS LINE ---
                        // Pass the whole itemData object, not just the prefab
                        playerHexSpawnerReference.RequestSpawnOnHex(activeHexForSpawning, currentItemData);
                    }
                    CloseAllMenus();
                });
            }
        }
    }

    public void DisplayObjectActionsMenu(GameObject targetObject, Transform anchorTransform)
    {
        if (isMenuOpen) return;
        isMenuOpen = true;

        if (objectActionsCanvasPrefab == null)
        {
            Debug.LogError("ObjectActionsCanvasPrefab not assigned to UIManager!");
            isMenuOpen = false;
            return;
        }

        // --- NEW: Switch to UI Interaction ---
        if (playerHexSpawnerReference != null)
        {
            playerHexSpawnerReference.EnableUIInteraction();
        }

        CloseAllMenus(false);
        Vector3 canvasPosition = anchorTransform.position + anchorTransform.up * menuHeightOffset;
        Quaternion canvasRotation = Quaternion.LookRotation(canvasPosition - Camera.main.transform.position, Vector3.up);
        currentObjectActionsCanvas = Instantiate(objectActionsCanvasPrefab, canvasPosition, canvasRotation);

        Debug.Log($"Displaying actions menu for: {targetObject.name}");
        // TODO: Populate this canvas with buttons.
    }

    public void CloseAllMenus(bool switchInteractionState = true)
    {
        if (currentSpawnSelectionCanvas != null) Destroy(currentSpawnSelectionCanvas);
        if (currentObjectActionsCanvas != null) Destroy(currentObjectActionsCanvas);
        activeHexForSpawning = null;

        if (switchInteractionState)
        {
            // --- NEW: Switch back to World Interaction ---
            if (playerHexSpawnerReference != null)
            {
                playerHexSpawnerReference.EnableWorldInteraction();
            }
            isMenuOpen = false;
        }
    }
}