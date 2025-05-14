using UnityEngine;

/// <summary>
/// Villager state for farming at a designated farm plot.
/// Assumes 'currentTaskTarget' on VillagerAI is set to the farm plot Transform,
/// or 'requiredResourceTag' is set to find a farm plot.
/// </summary>
public class VillagerFarmingState : VillagerBaseState
{
    private float farmProgress = 0f;
    private float farmTimeToComplete = 12f; // Example: Time in seconds to complete one farming cycle

    public override void EnterState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " entering Farming state.", villager.gameObject);
        farmProgress = 0f;
        villager.agent.speed = villager.moveSpeed;

        // If no specific target, try to find one using the resource tag
        if (villager.currentTaskTarget == null && !string.IsNullOrEmpty(villager.requiredResourceTag))
        {
            villager.currentTaskTarget = villager.FindClosestTargetWithTag(villager.requiredResourceTag);
            // Debug.Log(villager.gameObject.name + " found farm plot: " + (villager.currentTaskTarget ? villager.currentTaskTarget.name : "None"), villager.gameObject);
        }

        if (villager.currentTaskTarget != null) // This should be the farm plot
        {
            if (villager.agent.isOnNavMesh && villager.agent.enabled)
            {
                villager.agent.SetDestination(villager.currentTaskTarget.position);
            }
            else
            {
                Debug.LogWarning(villager.gameObject.name + " cannot move to farm plot (NavMesh issue). Transitioning to Stroll.", villager.gameObject);
                villager.TransitionToState(villager.strollingState);
            }
        }
        else
        {
            Debug.LogWarning(villager.gameObject.name + " has no farm plot assigned/found for FarmingState. Transitioning to Stroll.", villager.gameObject);
            villager.TransitionToState(villager.strollingState);
        }
    }

    public override void UpdateState(VillagerAI villager)
    {
        if (villager.currentTaskTarget == null)
        {
            // Debug.Log(villager.gameObject.name + " farm plot became null/finished. Transitioning to Stroll.", villager.gameObject);
            villager.TransitionToState(villager.strollingState);
            return;
        }

        if (villager.agent.isOnNavMesh && villager.agent.enabled &&
            !villager.agent.pathPending && villager.agent.remainingDistance <= villager.agent.stoppingDistance)
        {
            // Reached the farm plot
            villager.transform.LookAt(new Vector3(villager.currentTaskTarget.position.x, villager.transform.position.y, villager.currentTaskTarget.position.z));

            // --- Placeholder for Farming Action ---
            // Play farming animation, interact with soil/plants
            // Debug.Log(villager.gameObject.name + " is 'farming' at " + villager.currentTaskTarget.name, villager.gameObject);
            farmProgress += Time.deltaTime;

            if (farmProgress >= farmTimeToComplete)
            {
                Debug.Log(villager.gameObject.name + " 'finished farming' placeholder at " + villager.currentTaskTarget.name, villager.gameObject);

                // --- !!! ACTUAL FARMING COMPLETION LOGIC GOES HERE !!! ---
                // For example:
                // 1. "Harvest" resources from the farm plot.
                // 2. Add resources to villager's inventory or village storage.
                // 3. The farm plot might have its own state (e.g., needs replanting, regrowing).
                // FarmPlot plotScript = villager.currentTaskTarget.GetComponent<FarmPlot>();
                // if (plotScript != null) { plotScript.HarvestOperation(villager); }
                // --- End of Placeholder ---

                villager.currentTaskTarget = null; // Task for this specific plot might be done
                villager.TransitionToState(villager.strollingState); // Or find a new task/another farm plot
            }
        }
    }

    public override void ExitState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " exiting Farming state.", villager.gameObject);
        farmProgress = 0f;
        // Stop farming animation
    }
}
