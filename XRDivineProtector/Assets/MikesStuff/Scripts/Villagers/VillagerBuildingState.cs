using UnityEngine;

/// <summary>
/// Villager state for constructing a building at a designated site.
/// Assumes 'currentTaskTarget' on VillagerAI is set to the build site Transform.
/// </summary>
public class VillagerBuildingState : VillagerBaseState
{
    private float buildProgress = 0f;
    private float buildTimeToComplete = 10f; // Example: Time in seconds to complete building

    public override void EnterState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " entering Building state.", villager.gameObject);
        buildProgress = 0f;
        villager.agent.speed = villager.moveSpeed; // Move at normal speed to build site

        if (villager.currentTaskTarget != null)
        {
            if (villager.agent.isOnNavMesh && villager.agent.enabled)
            {
                villager.agent.SetDestination(villager.currentTaskTarget.position);
            }
            else
            {
                Debug.LogWarning(villager.gameObject.name + " cannot move to build site (NavMesh issue). Transitioning to Stroll.", villager.gameObject);
                villager.TransitionToState(villager.strollingState); // Fallback
            }
        }
        else
        {
            Debug.LogWarning(villager.gameObject.name + " has no build site (currentTaskTarget is null) for BuildingState. Transitioning to Stroll.", villager.gameObject);
            villager.TransitionToState(villager.strollingState); // Fallback
        }
    }

    public override void UpdateState(VillagerAI villager)
    {
        if (villager.currentTaskTarget == null) // Target might have been completed or destroyed
        {
            // Debug.Log(villager.gameObject.name + " build site became null. Transitioning to Stroll.", villager.gameObject);
            villager.TransitionToState(villager.strollingState);
            return;
        }

        if (villager.agent.isOnNavMesh && villager.agent.enabled &&
            !villager.agent.pathPending && villager.agent.remainingDistance <= villager.agent.stoppingDistance)
        {
            // Reached the build site
            villager.transform.LookAt(new Vector3(villager.currentTaskTarget.position.x, villager.transform.position.y, villager.currentTaskTarget.position.z)); // Look at target on Y-plane

            // --- Placeholder for Building Action ---
            // Play building animation, make sounds, etc.
            // Debug.Log(villager.gameObject.name + " is 'building' at " + villager.currentTaskTarget.name, villager.gameObject);
            buildProgress += Time.deltaTime;

            if (buildProgress >= buildTimeToComplete)
            {
                Debug.Log(villager.gameObject.name + " 'finished building' placeholder at " + villager.currentTaskTarget.name, villager.gameObject);

                // --- !!! ACTUAL BUILDING COMPLETION LOGIC GOES HERE !!! ---
                // For example:
                // 1. Instantiate the actual building model.
                // 2. Consume resources from village inventory.
                // 3. Remove the "build site" marker object.
                // if (villager.currentTaskTarget.CompareTag("BuildSiteMarker")) // Example
                // {
                //     GameObject.Destroy(villager.currentTaskTarget.gameObject);
                // }
                // --- End of Placeholder ---

                villager.currentTaskTarget = null; // Task is complete
                villager.TransitionToState(villager.strollingState); // Or find a new task
            }
        }
    }

    public override void ExitState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " exiting Building state.", villager.gameObject);
        buildProgress = 0f;
        // Stop building animation if any
        // Optionally, if agent was stopped, ensure it can move:
        // if (villager.agent.isOnNavMesh && villager.agent.enabled) villager.agent.isStopped = false;
    }
}
