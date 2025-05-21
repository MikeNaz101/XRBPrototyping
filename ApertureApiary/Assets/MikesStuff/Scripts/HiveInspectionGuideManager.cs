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
    [Tooltip("The TextMeshPro UI element used to display main instructions to the user.")]
    public TextMeshProUGUI instructionTextUI;
    [Tooltip("The TextMeshPro UI element used to display detailed informational text.")] // New UI Element
    public TextMeshProUGUI detailedInformationTextUI; // New UI Element

    [Header("Inspection Steps")]
    [Tooltip("Define all the steps for the hive inspection sequence here.")]
    public InspectionStep[] inspectionSteps;

    private int _currentStepIndex = -1;
    private Coroutine _autoAdvanceCoroutine;

    void Start()
    {
        if (instructionTextUI == null)
        {
            Debug.LogError("HiveInspectionGuideManager: Instruction Text UI is not assigned!", this);
            enabled = false;
            return;
        }
        // Detailed information UI is optional, so we only warn if it's meant to be used but not assigned.
        if (detailedInformationTextUI == null)
        {
            Debug.LogWarning("HiveInspectionGuideManager: Detailed Information Text UI is not assigned. No detailed info will be shown.", this);
        }

        if (inspectionSteps == null || inspectionSteps.Length == 0)
        {
            Debug.LogError("HiveInspectionGuideManager: No inspection steps defined!", this);
            enabled = false;
            return;
        }
        AdvanceToNextStep();
    }

    private void ShowCurrentStep()
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= inspectionSteps.Length)
        {
            instructionTextUI.text = "Inspection sequence finished or invalid step.";
            if(detailedInformationTextUI != null) detailedInformationTextUI.text = ""; // Clear detailed info
            Debug.Log("HiveInspectionGuideManager: End of steps or invalid index.");
            // Updated to use FindObjectsByType as FindObjectsOfType is obsolete
            foreach (var stepObj in FindObjectsByType<HighlightableObject>(FindObjectsSortMode.None))
            {
                stepObj.Unhighlight();
            }
            return;
        }

        InspectionStep currentStep = inspectionSteps[_currentStepIndex];

        // Update Main Instruction UI Text
        instructionTextUI.text = currentStep.instructionText;
        Debug.Log($"HiveInspectionGuideManager: Displaying Step {_currentStepIndex + 1}: {currentStep.instructionText}");

        // Update Detailed Information UI Text (New)
        if (detailedInformationTextUI != null)
        {
            if (!string.IsNullOrEmpty(currentStep.detailedInformationText))
            {
                detailedInformationTextUI.text = currentStep.detailedInformationText;
                detailedInformationTextUI.gameObject.SetActive(true); // Ensure it's visible
            }
            else
            {
                detailedInformationTextUI.text = ""; // Clear it if no detailed info for this step
                detailedInformationTextUI.gameObject.SetActive(false); // Optionally hide if empty
            }
        }


        // Handle Highlighting
        // Unhighlight all highlightable objects first (more robust than tracking previous)
        // Updated to use FindObjectsByType as FindObjectsOfType is obsolete
        HighlightableObject[] allHighlightableObjects = FindObjectsByType<HighlightableObject>(FindObjectsSortMode.None);
        foreach (var objToUnhighlight in allHighlightableObjects) {
            // Check if this object is the one we are about to highlight for the current step.
            // If so, don't unhighlight it just to re-highlight it immediately.
            if (currentStep.objectToHighlight != objToUnhighlight)
            {
                objToUnhighlight.Unhighlight();
            }
        }

        // Highlight the current step's object, if any
        if (currentStep.objectToHighlight != null)
        {
            currentStep.objectToHighlight.Highlight();
        }


        currentStep.onStepStart?.Invoke();

        if (!currentStep.waitForInteraction && currentStep.autoAdvanceDelay > 0)
        {
            if (_autoAdvanceCoroutine != null) StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = StartCoroutine(AutoAdvanceAfterDelay(currentStep.autoAdvanceDelay));
        }
    }

    private IEnumerator AutoAdvanceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"HiveInspectionGuideManager: Auto-advancing step {_currentStepIndex + 1} after {delay}s.");
        AdvanceToNextStep();
    }

    public void ProcessInteraction(GameObject interactedObject)
    {
        if (_currentStepIndex < 0 || _currentStepIndex >= inspectionSteps.Length) return;
        InspectionStep currentStep = inspectionSteps[_currentStepIndex];
        if (currentStep.waitForInteraction && currentStep.requiredInteractableObject == interactedObject)
        {
            Debug.Log($"HiveInspectionGuideManager: Correct interaction received for step {_currentStepIndex + 1} with object {interactedObject.name}. Advancing.");
            AdvanceToNextStep();
        }
        else if (currentStep.waitForInteraction)
        {
            Debug.Log($"HiveInspectionGuideManager: Interaction with {interactedObject.name} occurred, but current step requires interaction with {currentStep.requiredInteractableObject?.name ?? "null"}.");
        }
    }

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

    private void AdvanceToNextStep()
    {
        if (_autoAdvanceCoroutine != null)
        {
            StopCoroutine(_autoAdvanceCoroutine);
            _autoAdvanceCoroutine = null;
        }

        if (_currentStepIndex >= 0 && _currentStepIndex < inspectionSteps.Length)
        {
            //inspectionSteps[_currentStepIndex].onStepComplete?.Invoke();
            if(inspectionSteps[_currentStepIndex].objectToHighlight != null)
            {
                // Only unhighlight if it's not the same object being highlighted in the next step
                bool unhighlightThisObject = true;
                if (_currentStepIndex + 1 < inspectionSteps.Length) // Check if there is a next step
                {
                    if (inspectionSteps[_currentStepIndex].objectToHighlight == inspectionSteps[_currentStepIndex + 1].objectToHighlight)
                    {
                        unhighlightThisObject = false; // Don't unhighlight if it's the same for the next step
                    }
                }

                if (unhighlightThisObject)
                {
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
            instructionTextUI.text = "Hive inspection complete!";
            if(detailedInformationTextUI != null) detailedInformationTextUI.text = "You've learned the basics!";
            Debug.Log("HiveInspectionGuideManager: All inspection steps completed.");
            // Optionally, unhighlight all objects one last time
            // Updated to use FindObjectsByType as FindObjectsOfType is obsolete
            foreach (var stepObj in FindObjectsByType<HighlightableObject>(FindObjectsSortMode.None))
            {
                stepObj.Unhighlight();
            }
        }
    }

    void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("Debug: Right arrow pressed, attempting manual advance.");
            AdvanceGuideManually();
        }*/
    }
}