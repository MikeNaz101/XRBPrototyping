// CancelWallButton.cs
/*using UnityEngine;
using UnityEngine.UI;

// Add this script to the 'Cancel' button inside your wall preview UI prefab.
[RequireComponent(typeof(Button))]
public class CancelWallButton : MonoBehaviour
{
    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnCancel);
    }

    public void OnCancel()
    {
        // Find the WallPlacementManager instance and call its CancelPlacement method
        if (WallPlacementManager.Instance != null)
        {
            WallPlacementManager.Instance.CancelPlacement();
        }
        else
        {
            Debug.LogError("CancelWallButton could not find an instance of WallPlacementManager in the scene.");
        }
    }

    void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnCancel);
        }
    }
}*/