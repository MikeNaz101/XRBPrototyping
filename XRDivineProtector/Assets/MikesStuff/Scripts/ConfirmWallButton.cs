// ConfirmWallButton.cs
/*using UnityEngine;
using UnityEngine.UI;

// Add this script to the 'Confirm' button inside your wall preview UI prefab.
[RequireComponent(typeof(Button))]
public class ConfirmWallButton : MonoBehaviour
{
    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnConfirm);
    }

    public void OnConfirm()
    {
        // Find the WallPlacementManager instance and call its ConfirmPlacement method
        if (WallPlacementManager.Instance != null)
        {
            WallPlacementManager.Instance.ConfirmPlacement();
        }
        else
        {
            Debug.LogError("ConfirmWallButton could not find an instance of WallPlacementManager in the scene.");
        }
    }

    void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(OnConfirm);
        }
    }
}*/