using UnityEngine;
using UnityEngine.InputSystem;

public class VolumeToggleController : MonoBehaviour
{
    [Header("Target Controller")]
    [Tooltip("The HandMirrorVolume component instance you want to control.")]
    public HandMirrorVolume targetVolumeController;

    [Header("Player Reference")]
    [Tooltip("Assign the player's Camera or Head Transform here.")]
    public Transform playerCameraTransform;

    [Header("Positioning")]
    [Tooltip("How far in front of the player the volume should appear.")]
    public float spawnDistance = 1.0f;
    // --- NEW FIELD FOR OFFSET ---
    [Tooltip("How far to the player's right the volume should be offset (use negative for left).")]
    public float spawnOffsetRight = 0.0f; // Default to 0 (no offset)

    [Header("Input Action")]
    [Tooltip("Reference to the Input Action used to toggle the controller (Button type).")]
    public InputActionReference toggleActionReference;

    private InputAction toggleAction;
    private GameObject targetVolumeObject;

    void Awake()
    {
        // Validate Component Assignment
        if (targetVolumeController == null)
        {
            Debug.LogError("Target Volume Controller is not assigned!", this);
            enabled = false;
            return;
        }
        targetVolumeObject = targetVolumeController.gameObject;

        // Validate Camera Assignment
        if (playerCameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                playerCameraTransform = mainCam.transform;
                Debug.LogWarning("Player Camera Transform not assigned. Found and using Camera.main.", this);
            }
            else
            {
                Debug.LogError("Player Camera Transform is not assigned, and Camera.main could not be found!", this);
                enabled = false;
                return;
            }
        }

        // Validate and Setup Input Action
        if (toggleActionReference == null || toggleActionReference.action == null)
        {
            Debug.LogError("Toggle Action Reference is not set or invalid!", this);
            enabled = false;
            return;
        }
        toggleAction = toggleActionReference.action;

        if(targetVolumeObject != null)
        {
             targetVolumeObject.SetActive(targetVolumeController.enabled);
           //  Debug.Log($"Initial state for {targetVolumeObject.name}: {(targetVolumeObject.activeSelf ? "Active" : "Inactive")}");
        }
    }

    void OnEnable()
    {
        if (toggleAction != null)
        {
            toggleAction.performed += OnToggleControlPerformed;
            toggleAction.Enable();
          //  Debug.Log($"Toggle action '{toggleAction.name}' enabled and listener attached.");
        }
    }

    void OnDisable()
    {
        if (toggleAction != null)
        {
            toggleAction.performed -= OnToggleControlPerformed;
            toggleAction.Disable();
           // Debug.Log($"Toggle action '{toggleAction.name}' disabled and listener removed.");
        }
    }

    private void OnToggleControlPerformed(InputAction.CallbackContext context)
    {
        if (targetVolumeController == null || targetVolumeObject == null || playerCameraTransform == null)
        {
            Debug.LogError("Cannot perform toggle due to missing references.");
            return;
        }

        bool shouldBeEnabled = !targetVolumeController.enabled;

        if (shouldBeEnabled) // Turning ON
        {
           // Debug.Log($"Toggling ON: Positioning {targetVolumeObject.name}");

            // --- Calculate Horizontal Forward Direction ---
            Vector3 forwardHorizontal = playerCameraTransform.forward;
            forwardHorizontal.y = 0;
            if (forwardHorizontal.sqrMagnitude < 0.001f)
            {
                Vector3 rightHorizontalFallback = playerCameraTransform.right;
                rightHorizontalFallback.y = 0;
                if (rightHorizontalFallback.sqrMagnitude < 0.001f)
                {
                     forwardHorizontal = Vector3.forward;
                } else {
                     forwardHorizontal = Vector3.forward;
                }
            }
            forwardHorizontal.Normalize();

            // --- Calculate Horizontal Right Direction for Offset ---
            Vector3 rightHorizontal = playerCameraTransform.right;
            rightHorizontal.y = 0; // Ensure the right offset is purely horizontal
            rightHorizontal.Normalize(); // Normalize after zeroing y, in case of camera roll

            // --- Calculate Position ---
            // Point in front of the player
            Vector3 positionInFront = playerCameraTransform.position + (forwardHorizontal * spawnDistance);
            // Apply the rightward offset
            Vector3 finalSpawnPosition = positionInFront + (rightHorizontal * spawnOffsetRight);

            // Rotation: Look in the horizontal forward direction, oriented upright to the world floor
            Quaternion spawnRotation = Quaternion.LookRotation(forwardHorizontal, Vector3.up);

            // --- Apply Transform ---
            targetVolumeObject.transform.position = finalSpawnPosition;
            targetVolumeObject.transform.rotation = spawnRotation;

            targetVolumeObject.SetActive(true);
            targetVolumeController.enabled = true;
        }
        else // Turning OFF
        {
           // Debug.Log($"Toggling OFF: Deactivating {targetVolumeObject.name}");
            targetVolumeController.enabled = false;
            targetVolumeObject.SetActive(false);
        }

       // Debug.Log($"New state for {targetVolumeObject.name}: {(targetVolumeObject.activeSelf ? "Active" : "Inactive")}, Controller Script Enabled: {targetVolumeController.enabled}");
    }
}