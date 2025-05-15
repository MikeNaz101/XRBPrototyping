using UnityEngine;

/// <summary>
/// Villager state for sleeping to regain energy.
/// Villager will try to find an object tagged "Bed".
/// </summary>
public class VillagerSleepingState : VillagerBaseState
{
    // Threshold to consider waking up if other needs become pressing,
    // even if not fully rested. Full rest is handled by VillagerAI's energy check.
    private float partialRestEnergyThreshold = 30f;
    private Transform bedTarget; // Store the specific bed being targeted

    public override void EnterState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " entering Sleeping state.", villager.gameObject);
        villager.agent.speed = villager.moveSpeed; // Normal speed to get to bed

        villager.requiredResourceTag = "Bed"; // Standard tag for bed objects
        bedTarget = villager.FindClosestTargetWithTag(villager.requiredResourceTag, 50f); // Search a bit wider for a bed
        villager.currentTaskTarget = bedTarget; // Use currentTaskTarget for consistency if needed by other systems

        if (bedTarget != null)
        {
            if (villager.agent.isOnNavMesh && villager.agent.enabled)
            {
                villager.agent.SetDestination(bedTarget.position);
                // Debug.Log(villager.gameObject.name + " pathing to bed at " + bedTarget.name, villager.gameObject);
            }
            else
            {
                // Debug.LogWarning(villager.gameObject.name + " cannot path to bed (NavMesh issue). Sleeping on current spot.", villager.gameObject);
                SleepOnCurrentSpot(villager);
            }
        }
        else
        {
            // Debug.Log(villager.gameObject.name + " no bed found. Sleeping on current spot.", villager.gameObject);
            SleepOnCurrentSpot(villager);
        }
    }

    /// <summary>
    /// Helper method to handle logic for sleeping on the current spot if no bed is found or reachable.
    /// </summary>
    private void SleepOnCurrentSpot(VillagerAI villager)
    {
        if (villager.agent.isOnNavMesh && villager.agent.enabled && villager.agent.hasPath)
        {
            villager.agent.ResetPath(); // Stop any current movement
        }
        // Play sleeping animation (e.g., lie down on the ground)
        // Debug.Log(villager.gameObject.name + " is now sleeping on the spot.", villager.gameObject);
    }

    public override void UpdateState(VillagerAI villager)
    {
        bool isAtBed = bedTarget != null && villager.agent.isOnNavMesh && villager.agent.enabled &&
                       !villager.agent.pathPending && villager.agent.remainingDistance <= villager.agent.stoppingDistance;
        bool isSleepingOnSpot = bedTarget == null; // If no bed was targeted, they are sleeping on the spot

        if (isAtBed || isSleepingOnSpot)
        {
            // If they were moving to a bed and just arrived, stop them.
            if (isAtBed && villager.agent.velocity.sqrMagnitude > 0.01f) {
                 villager.agent.velocity = Vector3.zero; // Force stop if needed
                 // Play "go to bed" animation if distinct from "sleeping" animation
            }


            // --- Actual Sleeping Logic: Regain Energy ---
            // Example: Regain full energy over ~15 seconds of sleep
            villager.currentEnergy += (villager.maxEnergy / 15f) * Time.deltaTime;
            villager.currentEnergy = Mathf.Clamp(villager.currentEnergy, 0, villager.maxEnergy);
            // Debug.Log(villager.gameObject.name + " sleeping... Energy: " + villager.currentEnergy.ToString("F1"), villager.gameObject);


            // VillagerAI's main Update loop will transition out if maxEnergy is reached.
            // Additional logic: Wake up if partially rested and very hungry (and food is available).
            if (villager.currentEnergy > partialRestEnergyThreshold && villager.currentHunger >= villager.maxHunger * 0.9f) // If 90% hungry
            {
                // Check if food is available before waking up just to be hungry
                Transform potentialFood = villager.FindClosestTargetWithTag("FoodSource");
                if (potentialFood != null)
                {
                    // Debug.Log(villager.gameObject.name + " woke up due to hunger (partially rested).", villager.gameObject);
                    villager.TransitionToState(villager.eatingState);
                    return; // Exit early to avoid other checks in this frame
                }
            }
        }
        // If still pathing to bed, VillagerAI's Update for needs (energy depletion) is paused by the `needsPaused` flag.
    }

    public override void ExitState(VillagerAI villager)
    {
        // Debug.Log(villager.gameObject.name + " exiting Sleeping state. Energy: " + villager.currentEnergy.ToString("F1"), villager.gameObject);
        // Stop sleeping animation, stand up
        villager.currentTaskTarget = null; // Clear generic task target
        bedTarget = null; // Clear specific bed target
    }
}
