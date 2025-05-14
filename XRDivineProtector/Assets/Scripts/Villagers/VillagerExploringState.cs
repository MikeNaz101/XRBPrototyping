using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Villager state for exploring a specific point designated by the player.
/// After reaching the point, the villager might wait or perform a brief "scan" animation.
/// </summary>
public class VillagerExploringState : VillagerBaseState
{
    public Vector3 targetExplorationPoint; // Set by PlayerInteractionController
    private float explorationWaitTime = 5f; // How long to "explore" or wait at the location
    private float currentWaitTimer;
    private bool hasReachedDestination;

    public override void EnterState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " entering Exploring state.", villager.gameObject);
        currentWaitTimer = 0f;
        hasReachedDestination = false;
        villager.agent.speed = villager.moveSpeed * 1.1f; // Move slightly faster when exploring

        // The targetExplorationPoint should ideally be set before transitioning to this state.
        // PlayerInteractionController sets this.
        // If currentTaskTarget is also set (e.g., to a marker), we can use that.
        // Otherwise, we rely on targetExplorationPoint being pre-set.

        Vector3 destination;
        if (villager.currentTaskTarget != null) {
            destination = villager.currentTaskTarget.position;
        } else if (targetExplorationPoint != Vector3.zero) { // Check if targetExplorationPoint was set
            destination = targetExplorationPoint;
        } else {
            Debug.LogWarning(villager.gameObject.name + " has no exploration point for ExploringState. Transitioning to Stroll.", villager.gameObject);
            villager.TransitionToState(villager.strollingState);
            return;
        }


        if (villager.agent.isOnNavMesh && villager.agent.enabled)
        {
            villager.agent.SetDestination(destination);
            // Debug.Log(villager.gameObject.name + " exploring towards: " + destination, villager.gameObject);
        }
        else
        {
            Debug.LogWarning(villager.gameObject.name + " cannot explore (NavMesh issue). Transitioning to Stroll.", villager.gameObject);
            villager.TransitionToState(villager.strollingState);
        }
    }

    public override void UpdateState(VillagerAI villager)
    {
        if (!villager.agent.isOnNavMesh || !villager.agent.enabled) return;

        if (!hasReachedDestination)
        {
            if (!villager.agent.pathPending && villager.agent.remainingDistance <= villager.agent.stoppingDistance)
            {
                if (!villager.agent.hasPath || villager.agent.velocity.sqrMagnitude == 0f)
                {
                    hasReachedDestination = true;
                    currentWaitTimer = explorationWaitTime;
                    // Debug.Log(villager.gameObject.name + " reached exploration point. 'Scanning' for " + currentWaitTimer + "s.", villager.gameObject);
                    // Optionally, play a "looking around" or "scanning" animation here.
                    if (villager.currentTaskTarget != null && villager.currentTaskTarget.name.Contains("Explore Point Marker"))
                    {
                        Object.Destroy(villager.currentTaskTarget.gameObject, explorationWaitTime + 1f); // Clean up explore marker
                    }
                }
            }
        }
        else // Has reached destination, now waiting/exploring
        {
            currentWaitTimer -= Time.deltaTime;
            // Villager might look around, etc.
            villager.transform.Rotate(Vector3.up, 15f * Time.deltaTime); // Simple "looking around"

            if (currentWaitTimer <= 0)
            {
                // Debug.Log(villager.gameObject.name + " finished exploring point.", villager.gameObject);
                villager.TransitionToState(villager.strollingState); // Exploration complete, go back to strolling
            }
        }
    }

    public override void ExitState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " exiting Exploring state.", villager.gameObject);
        if (villager.agent.isOnNavMesh && villager.agent.enabled && villager.agent.hasPath)
        {
            villager.agent.ResetPath();
        }
        villager.agent.speed = villager.moveSpeed; // Restore default speed
        villager.currentTaskTarget = null; // Clear task target
        targetExplorationPoint = Vector3.zero; // Reset for next use
        // Stop any exploring animations
    }
}
