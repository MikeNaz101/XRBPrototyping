// File: HiveInspectionGuideManager.cs
// Purpose: Orchestrates the step-by-step hive inspection guide.
// Instructions: Attach this script to an empty GameObject in your scene (e.g., "GuideManager").
// Assign the UI Text element and define your inspection steps in the Inspector.

using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements
using System.Collections; // Required for Coroutines (like auto-advance delay)

public class HiveInspectionGuideManager : MonoBehaviour
{
    [Header("UI Assignments")]
    [Tooltip("The TextMeshPro UI element used to display instructions to the user.")]
    public TextMeshProUGUI instructionTextUI;

    [Header("Inspection Steps")]
    [Tooltip("Define all the steps for the hive inspection sequence here.")]
    public InspectionStep[] inspectionSteps;

    private int _currentStepIndex = -1; // Start at -1 so the first call to AdvanceStep sets it to 0
    private Coroutine _autoAdvanceCoroutine;

    void Start()
    {
        // Validate critical assignments
        if (instructionTextUI == null)
        {
            Debug.LogError("HiveInspectionGuideManager: Instruction Text UI is not assigned!", this);
            enabled = false; // Disable the script if setup is incomplete
            return;
        }
        if (inspectionSteps == null || inspectionSteps.Length == 0)
        {
            Debug.LogError("HiveInspectionGuideManager: No inspection steps defined!", this);
            enabled = false;
            return;
        }

        // Initialize the first step
        AdvanceToNextStep();
    }

    /// <summary>
    /// Displays the current step's instructions and handles highlighting.
    /// </summary>
    private void ShowCurrentStep()
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= inspectionSteps.Length)
        {
            instructionTextUI.text = "Inspection sequence finished or invalid step.";
            Debug.Log("HiveInspectionGuideManager: End of steps or invalid index.");
            // Optionally, disable all highlights one last time
            foreach (var stepObj in FindObjectsOfType<HighlightableObject>()) // Simple way to get all
            {
                stepObj.Unhighlight();
            }
            return;
        }

        InspectionStep currentStep = inspectionSteps[_currentStepIndex];

        // Update UI Text
        instructionTextUI.text = currentStep.instructionText;
        Debug.Log($"HiveInspectionGuideManager: Displaying Step {_currentStepIndex + 1}: {currentStep.instructionText}");

        // Handle Highlighting
        // First, unhighlight all objects (or just the previous step's object if you track it)
        // For simplicity, let's unhighlight all known highlightable objects managed by steps.
        // A more optimized way would be to store and unhighlight only the previously highlighted object.
        foreach (var stepDefinition in inspectionSteps)
        {
            if (stepDefinition.objectToHighlight != null)
            {
                stepDefinition.objectToHighlight.Unhighlight();
            }
        }

        // Then, highlight the current step's object, if any
        if (currentStep.objectToHighlight != null)
        {
            currentStep.objectToHighlight.Highlight();
        }

        // Trigger OnStepStart UnityEvent
        currentStep.onStepStart?.Invoke();

        // Handle auto-advance if not waiting for interaction and delay is set
        if (!currentStep.waitForInteraction && currentStep.autoAdvanceDelay > 0)
        {
            if (_autoAdvanceCoroutine != null)
            {
                StopCoroutine(_autoAdvanceCoroutine);
            }
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay(currentStep.autoAdvanceDelay));
        }
    }

    private IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"HiveInspectionGuideManager: Auto-advancing step {_currentStepIndex + 1} after {delay}s.");
        AdvanceToNextStep();
    }

    /// <summary>
    /// Called by InteractionEventRelay when an object it's attached to is interacted with.
    /// </summary>
    /// <param name="interactedObject">The GameObject that was interacted with.</param>
    public void ProcessInteraction(GameObject interactedObject)
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= inspectionSteps.Length) return; // No active step

        InspectionStep currentStep = inspectionSteps[_currentStepIndex];

        // Check if this interaction is the one we're waiting for
        if (currentStep.waitForInteraction && currentStep.requiredInteractableObject == interactedObject)
        {
            Debug.Log($"HiveInspectionGuideManager: Correct interaction received for step {_currentStepIndex + 1} with object {interactedObject.name}. Advancing.");
            AdvanceToNextStep();
        }
        else if (currentStep.waitForInteraction)
        {
            Debug.Log($"HiveInspectionGuideManager: Interaction with {interactedObject.name} occurred, but current step requires interaction with {currentStep.requiredInteractableObject?.name ?? "null"}.");
        }
        // If waitForInteraction is false, this method call might be from a generic event,
        // but typically progression would be manual or timed in that case.
    }

    /// <summary>
    /// Public method to manually advance to the next step. Can be hooked up to a UI button for testing or for observational steps.
    /// </summary>
    public void AdvanceGuideManually()
    {
         if (_currentStepIndex < 0 || _currentStepIndex >= inspectionSteps.Length) return;

        InspectionStep currentStep = inspectionSteps[_currentStepIndex];
        if (currentStep.waitForInteraction)
        {
            Debug.LogWarning($"HiveInspectionGuideManager: Manual advance called on step {_currentStepIndex + 1}, but it was waiting for an interaction. Ensure this is intended.");
        }
         Debug.Log($"HiveInspectionGuideManager: Manually advancing from step {_currentStepIndex + 1}.");
        AdvanceToNextStep();
    }


    /// <summary>
    /// Completes the current step and moves to the next one.
    /// </summary>
    public void AdvanceToNextStep()
    {
        // Stop any ongoing auto-advance coroutine from the previous step
        if (_autoAdvanceCoroutine != null)
        {
            StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = null;
        }

        // Trigger OnStepComplete for the step we are leaving (if it was a valid step)
        if (_currentStepIndex >= 0 && _currentStepIndex < inspectionSteps.Length)
        {
            //inspectionSteps[_currentStepIndex].onStepComplete?.Invoke();
            // Unhighlight the object from the completed step if it's not the same as the next step's highlight
            if(inspectionSteps[_currentStepIndex].objectToHighlight != null)
            {
                bool highlightIsDifferentInNextStep = true;
                if(_currentStepIndex + 1 < inspectionSteps.Length) {
                    if(inspectionSteps[_currentStepIndex].objectToHighlight == inspectionSteps[_currentStepIndex+1].objectToHighlight) {
                        highlightIsDifferentInNextStep = false;
                    }
                }
                if(highlightIsDifferentInNextStep) {
                     inspectionSteps[_currentStepIndex].objectToHighlight.Unhighlight();
                }
            }
        }

        _currentStepIndex++;

        if (_currentStepIndex < inspectionSteps.Length)
        {
            ShowCurrentStep();
        }
        else
        {
            // All steps completed
            instructionTextUI.text = "Hive inspection complete!";
            Debug.Log("HiveInspectionGuideManager: All inspection steps completed.");
            // Optionally, trigger a final "all complete" event
        }
    }

    // Example: Hook this up to a debug button or a controller input for testing.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) // Simple debug key
        {
            Debug.Log("Debug: Right arrow pressed, attempting manual advance.");
            AdvanceGuideManually();
        }
    }
}