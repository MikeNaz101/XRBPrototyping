// File: QueenBeeBehavior.cs
// Purpose: Controls the behavior of the Queen Bee, making it spawn on a random frame
// and wander slowly on its surface, while maintaining its own world scale using an intermediate pivot.
// Instructions:
// 1. Attach this script to your QueenBee GameObject.
// 2. Ensure your QueenBee GameObject's prefab has a scale of (1,1,1).
// 3. Assign your FrameActivityManager to the 'Frame Manager' slot.
// 4. Adjust movement parameters as needed.

using UnityEngine;
using System.Collections.Generic; // Required for using Lists

public class QueenBeeBehavior : MonoBehaviour
{
    [Header("Frame Association")]
    [Tooltip("Reference to the FrameActivityManager that holds the list of frames.")]
    public FrameActivityManager frameManager;
    [Tooltip("Offset along the frame's local Z-axis to position the queen's pivot. " +
             "Positive for one side, negative for the other, 0 for center of thickness.")]
    public float surfaceOffsetZ = 0.01f; // Small offset to avoid Z-fighting

    [Header("Movement Parameters")]
    public float movementSpeed = 0.05f;
    [Tooltip("How quickly the queen rotates to face her movement direction.")]
    public float rotationSpeed = 5f;
    [Tooltip("Approximate size of the wander area on the frame's local XY plane (e.g., width, height of the comb area).")]
    public Vector2 wanderAreaExtents = new Vector2(0.2f, 0.3f);
    [Tooltip("Minimum time in seconds before picking a new wander target.")]
    public float minWanderInterval = 3.0f;
    [Tooltip("Maximum time in seconds before picking a new wander target.")]
    public float maxWanderInterval = 8.0f;
    [Tooltip("How close the queen needs to get to a target before considering it reached.")]
    public float targetReachedThreshold = 0.01f;

    private Transform _chosenFrameTransform;
    private Transform _queenPivotTransform; // Intermediate pivot
    private Vector3 _currentLocalWanderTargetOnPivot; // Target relative to the pivot's parent (the frame)
    private float _timeToNextWanderDecision;

    void Start()
    {
        if (frameManager == null || frameManager.frameParticlePairs == null || frameManager.frameParticlePairs.Count == 0)
        {
            Debug.LogError("QueenBeeBehavior: FrameActivityManager or its frame list is not assigned or empty! Queen cannot spawn.", this);
            enabled = false;
            return;
        }

        // 1. Pick a random frame
        int randomIndex = Random.Range(0, frameManager.frameParticlePairs.Count);
        FrameParticlePair chosenPair = frameManager.frameParticlePairs[randomIndex];

        if (chosenPair.frameGameObject == null)
        {
            Debug.LogError($"QueenBeeBehavior: Chosen frame at index {randomIndex} has no frameGameObject assigned! Queen cannot spawn.", this);
            enabled = false;
            return;
        }
        _chosenFrameTransform = chosenPair.frameGameObject.transform;

        // 2. Create and parent the intermediate pivot to the chosen frame.
        // The pivot will inherit the frame's non-uniform scale initially, but its own localScale will be (1,1,1).
        GameObject pivotGO = new GameObject(gameObject.name + "_Pivot");
        _queenPivotTransform = pivotGO.transform;
        _queenPivotTransform.SetParent(_chosenFrameTransform, false); // worldPositionStays = false, so it's positioned at frame's origin with scale 1,1,1 relative to frame's potentially non-uniform scale.

        // 3. Parent the Queen Bee to this new pivot.
        // The Queen Bee itself should have a scale of (1,1,1) in its prefab.
        // When parented to the pivot (which has localScale 1,1,1 relative to the frame),
        // the Queen will also have localScale (1,1,1) relative to the pivot.
        transform.SetParent(_queenPivotTransform, false); // worldPositionStays = false
        transform.localPosition = Vector3.zero;          // Queen is at the center of the pivot
        transform.localRotation = Quaternion.identity;   // Queen is aligned with the pivot initially

        // 4. Position the pivot (and thus the Queen) on the frame surface.
        PickNewLocalWanderTargetOnPivot(); // This determines _currentLocalWanderTargetOnPivot
        _queenPivotTransform.localPosition = _currentLocalWanderTargetOnPivot; // Set the pivot's local position on the frame.

        // 5. Set initial local orientation of the Queen relative to the pivot.
        // The MoveAndRotate() method will then orient the Queen based on its movement direction.
        // (Already done by transform.localRotation = Quaternion.identity above)

        _timeToNextWanderDecision = Random.Range(minWanderInterval, maxWanderInterval);

        Debug.Log($"Queen Bee spawned on frame: {_chosenFrameTransform.name} via pivot, maintaining own scale.", this);
    }

    void Update()
    {
        if (_queenPivotTransform == null) return;

        _timeToNextWanderDecision -= Time.deltaTime;

        // Check distance from pivot's current position to its target position
        if (_timeToNextWanderDecision <= 0 || Vector3.Distance(_queenPivotTransform.localPosition, _currentLocalWanderTargetOnPivot) < targetReachedThreshold)
        {
            PickNewLocalWanderTargetOnPivot();
            _timeToNextWanderDecision = Random.Range(minWanderInterval, maxWanderInterval);
        }

        MovePivotAndRotateQueen();
    }

    void PickNewLocalWanderTargetOnPivot()
    {
        float randomX = Random.Range(-wanderAreaExtents.x / 2f, wanderAreaExtents.x / 2f);
        float randomY = Random.Range(-wanderAreaExtents.y / 2f, wanderAreaExtents.y / 2f);

        // This target is the PIVOT's local position on the FRAME
        _currentLocalWanderTargetOnPivot = new Vector3(randomX, randomY, surfaceOffsetZ);
    }

    void MovePivotAndRotateQueen()
    {
        // Move the PIVOT towards its target local position on the FRAME
        _queenPivotTransform.localPosition = Vector3.MoveTowards(_queenPivotTransform.localPosition, _currentLocalWanderTargetOnPivot, movementSpeed * Time.deltaTime);

        // Calculate direction for the Queen's rotation.
        // This is the direction from the PIVOT's current position to its target.
        // Since the Queen is at (0,0,0) relative to the pivot, this direction is also
        // effectively the direction the Queen "wants" to face on the frame's surface.
        Vector3 directionToTargetOnFrame = (_currentLocalWanderTargetOnPivot - _queenPivotTransform.localPosition);
        directionToTargetOnFrame.z = 0; // We only care about movement in the XY plane of the frame

        if (directionToTargetOnFrame.sqrMagnitude > 0.001f)
        {
            // Rotate the QUEEN BEE relative to the PIVOT
            float angle = Mathf.Atan2(directionToTargetOnFrame.y, directionToTargetOnFrame.x) * Mathf.Rad2Deg;
            Quaternion targetQueenLocalRotation = Quaternion.Euler(0, 0, angle - 90f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetQueenLocalRotation, rotationSpeed * Time.deltaTime);
        }
    }
}