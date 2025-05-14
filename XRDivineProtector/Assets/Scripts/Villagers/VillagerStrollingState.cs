using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Villager state for aimless strolling or wandering.
/// The villager will pick random points on the NavMesh and walk to them.
/// </summary>
public class VillagerStrollingState : VillagerBaseState
{
    private float waitTimer;  // Timer for how long to wait at a destination
    private bool isWaiting;   // Flag indicating if the villager is currently waiting

    public override void EnterState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " entering Strolling state.", villager.gameObject);
        isWaiting = false;
        villager.agent.speed = villager.moveSpeed * 0.75f; // Stroll at a slightly reduced speed
        villager.currentTaskTarget = null; // No specific target when strolling
        SetNewRandomDestination(villager);
    }

    public override void UpdateState(VillagerAI villager)
    {
        // Ensure agent is valid and on NavMesh before proceeding
        if (!villager.agent.isOnNavMesh || !villager.agent.enabled) return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWaiting = false;
                SetNewRandomDestination(villager);
            }
        }
        else
        {
            // Check if the agent has reached its destination
            // PathPending means agent is still calculating the path
            // RemainingDistance is distance to target along the path
            // StoppingDistance is how close agent gets before considering it "arrived"
            if (!villager.agent.pathPending && villager.agent.remainingDistance <= villager.agent.stoppingDistance)
            {
                // Additional check to ensure it's truly stopped and not just near a corner
                if (!villager.agent.hasPath || villager.agent.velocity.sqrMagnitude == 0f)
                {
                    isWaiting = true;
                    waitTimer = Random.Range(villager.minStrollWaitTime, villager.maxStrollWaitTime);
                    // Debug.Log(villager.gameObject.name + " reached stroll destination, waiting for " + waitTimer + "s.", villager.gameObject);
                }
            }
        }
        // Note: Critical needs (hunger, energy) are checked in VillagerAI's Update and can override this state.
    }

    public override void ExitState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " exiting Strolling state.", villager.gameObject);
        // Reset path if agent is still moving and on navmesh
        if (villager.agent.isOnNavMesh && villager.agent.enabled && villager.agent.hasPath)
        {
            villager.agent.ResetPath();
        }
        villager.agent.speed = villager.moveSpeed; // Restore default speed
    }

    /// <summary>
    /// Finds and sets a new random destination for the villager to stroll to.
    /// </summary>
    private void SetNewRandomDestination(VillagerAI villager)
    {
        if (!villager.agent.isOnNavMesh || !villager.agent.enabled) return;

        Vector3 randomDestination;
        if (villager.FindRandomNavMeshPoint(villager.transform.position, villager.strollRadius, out randomDestination))
        {
            villager.agent.SetDestination(randomDestination);
            // Debug.Log(villager.gameObject.name + " strolling to: " + randomDestination, villager.gameObject);
        }
        else
        {
            // If a point couldn't be found (e.g., NavMesh is too small or villager is trapped),
            // just wait for a bit before trying again to avoid spamming.
            isWaiting = true;
            waitTimer = villager.minStrollWaitTime;
            // Debug.LogWarning(villager.gameObject.name + " couldn't find a stroll point, waiting.", villager.gameObject);
        }
    }
}
