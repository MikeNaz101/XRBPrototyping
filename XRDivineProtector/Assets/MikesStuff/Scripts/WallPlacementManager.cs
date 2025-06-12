// WallPlacementManager.cs
/*using UnityEngine;

public class WallPlacementManager : MonoBehaviour
{
    public static WallPlacementManager Instance { get; private set; }

    [Header("Settings")]
    public float rotationSpeed = 120f; // Degrees per second for manual rotation
    public Material ghostMaterial; // A semi-transparent material for the preview object
    public GameObject previewUIPrefab; // Assign your new Preview UI Canvas prefab here

    private GameObject _ghostWallInstance;
    private GameObject _previewUIInstance;
    private HexCell _currentHex;
    private bool _isPreviewActive = false;
    private PlayerHexSpawner _playerHexSpawner;
    private SpawnableItemData _wallDataToSpawn;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartWallPreview(SpawnableItemData wallData, PlayerHexSpawner spawner)
    {
        if (_isPreviewActive) return; // Don't start a new preview if one is active

        Debug.Log("Starting wall preview mode.");
        _isPreviewActive = true;
        _playerHexSpawner = spawner;
        _wallDataToSpawn = wallData;

        // Create the ghost instance
        _ghostWallInstance = Instantiate(wallData.itemPrefab);
        _ghostWallInstance.transform.localScale = Vector3.one * 0.001f; // Use the larger wall scale
        foreach (Renderer renderer in _ghostWallInstance.GetComponentsInChildren<Renderer>())
        {
            renderer.material = ghostMaterial;
        }
        if (_ghostWallInstance.GetComponent<Collider>() != null)
        {
            _ghostWallInstance.GetComponent<Collider>().enabled = false;
        }

        // --- NEW: Instantiate and position the preview UI ---
        if (previewUIPrefab != null)
        {
            _previewUIInstance = Instantiate(previewUIPrefab);
            // You may want to parent this to the camera rig or keep it in world space
        }
    }

    void Update()
    {
        if (!_isPreviewActive) return;

        HandlePreviewMovement();
        HandleInput();
    }

    void HandlePreviewMovement()
    {
        HexCell hoveredHex = _playerHexSpawner.CurrentlyHoveredHex;

        if (hoveredHex != null && hoveredHex != _currentHex)
        {
            _currentHex = hoveredHex;
            _ghostWallInstance.transform.position = _currentHex.WorldCenter + new Vector3(0, 0.05f, 0);
            OrientWall();
        }
        else if (hoveredHex == null && _currentHex != null)
        {
            _currentHex = null;
            _ghostWallInstance.transform.position = Vector3.one * 10000;
        }
    }

    void OrientWall()
    {
        if (_currentHex == null) return;

        HexCell neighborWithWall = null;
        foreach (HexCell neighbor in _currentHex.Neighbors)
        {
            if (neighbor.IsOccupied && neighbor.SpawnedObject.GetComponent<WallIdentifier>() != null)
            {
                neighborWithWall = neighbor;
                break;
            }
        }

        if (neighborWithWall != null)
        {
            Vector3 directionToNeighbor = (neighborWithWall.WorldCenter - _currentHex.WorldCenter).normalized;
            _ghostWallInstance.transform.rotation = Quaternion.LookRotation(directionToNeighbor);
        }
    }

    void HandleInput()
    {
        if (_currentHex == null) return;

        // --- UPDATED: Only manual rotation logic remains here ---
        bool hasAdjacentWall = false;
        foreach (HexCell neighbor in _currentHex.Neighbors)
        {
            if (neighbor.IsOccupied && neighbor.SpawnedObject.GetComponent<WallIdentifier>() != null)
            {
                hasAdjacentWall = true;
                break;
            }
        }

        if (!hasAdjacentWall)
        {
            // Note: For hand tracking, you might want to use a different gesture or UI element for rotation.
            // Thumbstick input is kept here for controller compatibility.
            Vector2 thumbstickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.Active);
            if (Mathf.Abs(thumbstickInput.x) > 0.1f)
            {
                _ghostWallInstance.transform.Rotate(Vector3.up, thumbstickInput.x * rotationSpeed * Time.deltaTime);
            }
        }
    }

    public void ConfirmPlacement()
    {
        if (_currentHex != null && !_currentHex.IsOccupied)
        {
            _playerHexSpawner.SpawnFinalObject(_currentHex, _wallDataToSpawn, _ghostWallInstance.transform.position, _ghostWallInstance.transform.rotation);
        }
        CancelPlacement(); // End preview mode regardless of successful placement
    }

    public void CancelPlacement()
    {
        Debug.Log("Canceling wall preview mode.");
        if (_ghostWallInstance != null)
        {
            Destroy(_ghostWallInstance);
        }
        // --- NEW: Destroy the preview UI instance ---
        if (_previewUIInstance != null)
        {
            Destroy(_previewUIInstance);
        }
        
        _ghostWallInstance = null;
        _previewUIInstance = null;
        _currentHex = null;
        _isPreviewActive = false;
        
        _playerHexSpawner.EnableWorldInteraction();
    }
}*/