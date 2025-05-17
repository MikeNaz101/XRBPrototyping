using UnityEngine;
using UnityEngine.Events; // Required for UnityEvent

[System.Serializable] // Makes this class show up in the Inspector when used in an array
public class InspectionStep
{
    [Header("Step Configuration")]
    [Tooltip("The instruction text to display to the user for this step.")]
    [TextArea(3, 10)] // Makes the string field larger in the Inspector
    public string instructionText = "Default instruction for this step.";

    [Tooltip("The HighlightableObject that should be visually emphasized for this step. Can be null.")]
    public HighlightableObject objectToHighlight;

    [Tooltip("The GameObject that the user needs to interact with to complete this step. Can be null if progression is manual or timed.")]
    public GameObject requiredInteractableObject;

    [Tooltip("If true, this step will only advance when the 'requiredInteractableObject' is interacted with (via InteractionEventRelay). If false, use 'Advance Guide Manually' button or custom events.")]
    public bool waitForInteraction = true;

    [Tooltip("Delay in seconds before this step automatically completes and advances. Only active if waitForInteraction is false. Set to 0 for no auto-advance.")]
    public float autoAdvanceDelay = 0f;

    [Header("Step Events")]
    [Tooltip("UnityEvent triggered when this step becomes active.")]
    public UnityEvent onStepStart;

    [Tooltip("UnityEvent triggered when this step is completed (either by interaction or manually).")]
    public UnityEvent onStepComplete;
}