// CloseMenuButton.cs
using UnityEngine;
using UnityEngine.UI;

// Add this script to the 'Close' button GameObject within your canvas prefabs.
[RequireComponent(typeof(Button))]
public class CloseMenuButton : MonoBehaviour
{
    private Button _closeButton;

    void Awake()
    {
        _closeButton = GetComponent<Button>();
        // Add a listener to this button's onClick event that calls our close method.
        _closeButton.onClick.AddListener(CloseTheMenu);
    }

    /// <summary>
    /// Finds the UIManager instance in the scene and tells it to close all open menus.
    /// </summary>
    public void CloseTheMenu()
    {
        if (UIManager.Instance != null)
        {
            Debug.Log("Close button clicked, telling UIManager to close menus.");
            UIManager.Instance.CloseAllMenus();
        }
        else
        {
            Debug.LogError("CloseMenuButton could not find an instance of UIManager in the scene.");
        }
    }

    // Clean up the listener when the button is destroyed.
    void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveListener(CloseTheMenu);
        }
    }
}