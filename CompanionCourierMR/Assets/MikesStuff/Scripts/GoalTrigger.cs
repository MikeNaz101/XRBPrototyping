using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    private GoalManager _goalManager;
    private string _resonanceCoreTag;
    private bool _isGoalReached = false;

    void Awake()
    {
        // Ensure the collider is set to be a trigger.
        GetComponent<Collider>().isTrigger = true;
    }

    // This method allows the GoalManager to give this script a reference to itself.
    public void SetGoalManager(GoalManager manager)
    {
        _goalManager = manager;
        _resonanceCoreTag = manager.resonanceCoreTag;
    }

    private void OnTriggerEnter(Collider other)
    {
        // If the goal has already been reached, do nothing.
        if (_isGoalReached || _goalManager == null)
        {
            return;
        }

        // Check if the object that entered the trigger has the correct tag.
        if (other.CompareTag(_resonanceCoreTag))
        {
            // Tell the GoalManager that the player has won.
            _goalManager.PlayerWon();
            _isGoalReached = true;

            // Optional: Play a victory sound or particle effect from this goal area.
        }
    }
}