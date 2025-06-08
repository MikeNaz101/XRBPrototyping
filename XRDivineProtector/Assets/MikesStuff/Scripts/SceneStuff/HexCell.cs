// HexCell.cs
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public Vector3 WorldCenter { get; private set; }
    public bool IsOccupied { get; private set; } = false;
    public GameObject SpawnedObject { get; private set; } = null;

    private MeshRenderer meshRenderer;
    private LineRenderer lineRenderer;

    [Header("State Materials")]
    public Material emptyDefaultMaterial;
    public Material emptyHoverMaterial;
    public Material occupiedDefaultMaterial;
    public Material occupiedHoverMaterial;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        lineRenderer = GetComponent<LineRenderer>();

        if (meshRenderer == null)
        {
            Debug.LogWarning($"HexCell on {gameObject.name} is missing a MeshRenderer. Material swapping will not work.", this);
        }
        if (emptyDefaultMaterial == null)
        {
            Debug.LogError($"HexCell on {gameObject.name}: 'Empty Default Material' is not assigned in the Inspector!", this);
        }
    }

    public void Initialize(Vector3 worldCenter)
    {
        WorldCenter = worldCenter;
        UpdateVisualState();
    }

    public void OnPointerEnter()
    {
        if (meshRenderer == null) return;

        if (IsOccupied)
        {
            if (occupiedHoverMaterial != null)
                meshRenderer.material = occupiedHoverMaterial;
            else
                Debug.LogWarning("Occupied Hover Material not set.", this);
        }
        else
        {
            if (emptyHoverMaterial != null)
                meshRenderer.material = emptyHoverMaterial;
            else
                Debug.LogWarning("Empty Hover Material not set.", this);
        }
    }

    public void OnPointerExit()
    {
        if (meshRenderer == null) return;
        UpdateVisualState();
    }

    public bool TryOccupy(GameObject spawnedInstance)
    {
        if (IsOccupied) return false;

        IsOccupied = true;
        SpawnedObject = spawnedInstance;
        UpdateVisualState();
        return true;
    }

    public void Vacate()
    {
        IsOccupied = false;
        SpawnedObject = null;
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (meshRenderer == null) return;

        if (IsOccupied)
        {
            if (occupiedDefaultMaterial != null)
                meshRenderer.material = occupiedDefaultMaterial;
            else
                Debug.LogWarning("Occupied Default Material not set.", this);
        }
        else
        {
            if (emptyDefaultMaterial != null)
                meshRenderer.material = emptyDefaultMaterial;
        }
    }

    // This method should be called by the 'selectEntered' event of the
    // Interactable component on this HexCellPrefab.
    public void OnHexCellActivated()
    {
        Debug.Log(this.gameObject.name + " was selected by the Ray Interactor! (HexCell.OnHexCellActivated called)");

        // Find the PlayerHexSpawner instance in the scene.
        // This is suitable if you only have one PlayerHexSpawner.
        PlayerHexSpawner spawner = FindFirstObjectByType<PlayerHexSpawner>();

        if (spawner != null)
        {
            spawner.HandleHexSelection(this.gameObject); // 'this.gameObject' is the HexCell instance itself
        }
        else
        {
            Debug.LogError("PlayerHexSpawner instance not found in the scene! Cannot handle hex selection.", this);
        }
    }
}
