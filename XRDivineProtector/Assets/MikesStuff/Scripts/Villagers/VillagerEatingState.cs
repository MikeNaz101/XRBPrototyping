using UnityEngine;

/// <summary>
/// Villager state for eating to reduce hunger.
/// Villager will try to find an object tagged "FoodSource".
/// </summary>
public class VillagerEatingState : VillagerBaseState
{
    private float eatTimeToComplete = 5f; // Example: Time in seconds to complete eating
    private float timeSpentEating = 0f;
    private Transform foodTarget; // Store the specific food source being targeted

    public override void EnterState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " entering Eating state.", villager.gameObject);
        timeSpentEating = 0f;
        villager.agent.speed = villager.moveSpeed;

        villager.requiredResourceTag = "FoodSource"; // Standard tag for food objects
        foodTarget = villager.FindClosestTargetWithTag(villager.requiredResourceTag);
        villager.currentTaskTarget = foodTarget; // Use currentTaskTarget for consistency

        if (foodTarget != null)
        {
            if (villager.agent.isOnNavMesh && villager.agent.enabled)
            {
                villager.agent.SetDestination(foodTarget.position);
                // Debug.Log(villager.gameObject.name + " pathing to food source: " + foodTarget.name, villager.gameObject);
            }
            else
            {
                // Debug.LogWarning(villager.gameObject.name + " cannot path to food (NavMesh issue). Transitioning to Stroll.", villager.gameObject);
                villager.TransitionToState(villager.strollingState); // Fallback
            }
        }
        else
        {
            // Debug.LogWarning(villager.gameObject.name + " no food source found. Transitioning to Stroll (will likely become hungry again soon).", villager.gameObject);
            villager.TransitionToState(villager.strollingState); // Fallback
        }
    }

    public override void UpdateState(VillagerAI villager)
    {
        if (foodTarget == null) // Food source might have been consumed by another or disappeared
        {
            // Debug.Log(villager.gameObject.name + " food source became null. Transitioning to Stroll.", villager.gameObject);
            villager.TransitionToState(villager.strollingState);
            return;
        }

        if (villager.agent.isOnNavMesh && villager.agent.enabled &&
            !villager.agent.pathPending && villager.agent.remainingDistance <= villager.agent.stoppingDistance)
        {
            // Reached the food source
            villager.transform.LookAt(new Vector3(foodTarget.position.x, villager.transform.position.y, foodTarget.position.z));

            // --- Placeholder for Eating Action ---
            // Play eating animation
            // Debug.Log(villager.gameObject.name + " is 'eating' at " + foodTarget.name, villager.gameObject);
            timeSpentEating += Time.deltaTime;

            if (timeSpentEating >= eatTimeToComplete)
            {
                Debug.Log(villager.gameObject.name + " 'finished eating' placeholder at " + foodTarget.name, villager.gameObject);
                villager.currentHunger = 0; // Reset hunger to not hungry

                // --- !!! ACTUAL FOOD CONSUMPTION LOGIC GOES HERE !!! ---
                // For example:
                // 1. Reduce the quantity of food in the FoodSource object.
                // 2. If the FoodSource is a single item (e.g., an apple), destroy it.
                // FoodSource foodSourceScript = foodTarget.GetComponent<FoodSource>();
                // if (foodSourceScript != null) { foodSourceScript.ConsumeFood(1); } // Consume 1 unit
                // if (foodTarget.CompareTag("SingleUseFood")) { GameObject.Destroy(foodTarget.gameObject); }
                // --- End of Placeholder ---

                villager.currentTaskTarget = null; // Task is complete for this food source
                foodTarget = null;
                villager.TransitionToState(villager.strollingState); // Or check for other needs/tasks
            }
        }
        // If still pathing to food, VillagerAI's Update for needs (hunger increase) is paused by the `needsPaused` flag.
    }

    public override void ExitState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " exiting Eating state. Hunger: " + villager.currentHunger.ToString("F1"), villager.gameObject);
        timeSpentEating = 0f;
        // Stop eating animation
        villager.currentTaskTarget = null;
        foodTarget = null;
    }
}
