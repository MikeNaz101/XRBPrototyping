using UnityEngine;

/// <summary>
/// Abstract base class for all villager behavior states.
/// Defines the common interface for states within the state machine.
/// </summary>
public abstract class VillagerBaseState
{
    /// <summary>
    /// Called once when the villager transitions into this state.
    /// Use for initialization specific to this state.
    /// </summary>
    /// <param name="villager">Reference to the VillagerAI controller.</param>
    public abstract void EnterState(VillagerAI villager);

    /// <summary>
    /// Called every frame while the villager is in this state.
    /// Contains the primary logic for the state's behavior.
    /// </summary>
    /// <param name="villager">Reference to the VillagerAI controller.</param>
    public abstract void UpdateState(VillagerAI villager);

    /// <summary>
    /// Called once when the villager transitions out of this state.
    /// Use for cleanup specific to this state.
    /// </summary>
    /// <param name="villager">Reference to the VillagerAI controller.</param>
    public abstract void ExitState(VillagerAI villager);
}
