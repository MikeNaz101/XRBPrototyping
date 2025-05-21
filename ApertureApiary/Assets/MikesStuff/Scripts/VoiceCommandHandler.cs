// File: VoiceCommandHandler.cs
// Purpose: Handles recognized voice intents from Meta Voice SDK (Wit.ai) and triggers actions.
// Instructions:
// 1. Create an empty GameObject in your scene (e.g., "VoiceManager" or add this to your existing "GuideManager").
// 2. Attach this script to that GameObject.
// 3. Assign your "GuideManager" GameObject (the one with HiveInspectionGuideManager.cs)
//    to the 'Guide Manager' slot in the Inspector of this script.
// 4. In your Meta Voice SDK's core Wit service component (e.g., VoiceService, Wit),
//    find the event that fires when an intent is recognized. Hook this event up to call
//    the 'HandleWitIntent(string intentName)' method on this VoiceCommandHandler script.
//    (The exact signature of the event callback from Wit might vary, e.g., it might provide more data.
//     Adjust the HandleWitIntent parameters if needed based on the Wit SDK's event signature.)

using UnityEngine;
// You might need to add a using directive for the Meta Wit SDK if you need to access specific Wit response types,
// e.g., using Meta.WitAi.Interfaces; or using Meta.WitAi.Data;

public class VoiceCommandHandler : MonoBehaviour
{
    [Header("Target Systems")]
    [Tooltip("Reference to the HiveInspectionGuideManager that controls the UI guide.")]
    public HiveInspectionGuideManager guideManager;

    [Header("Voice Command Configuration")]
    [Tooltip("The exact name of the Wit.ai intent that should trigger the 'next step' action.")]
    public string advanceGuideIntentName = "advance_guide"; // Make sure this matches your Wit.ai intent name

    void Start()
    {
        if (guideManager == null)
        {
            Debug.LogError("VoiceCommandHandler: HiveInspectionGuideManager is not assigned! Voice commands for the guide will not work.", this);
        }
        if (string.IsNullOrEmpty(advanceGuideIntentName))
        {
            Debug.LogWarning("VoiceCommandHandler: 'Advance Guide Intent Name' is not set. Please configure it with your Wit.ai intent name.", this);
        }
    }

    /// <summary>
    /// This public method should be called by the Meta Voice SDK (Wit.ai integration)
    /// when a voice command/intent has been successfully recognized.
    /// The signature might need adjustment based on the exact event provided by the Wit SDK.
    /// For example, it might be: public void HandleWitIntent(WitResponseNode responseNode)
    /// And then you would extract the intent name from responseNode.
    /// For simplicity, this example assumes the event can directly pass the intent name string.
    /// </summary>
    /// <param name="recognizedIntentName">The name of the intent recognized by Wit.ai.</param>
    public void HandleWitIntent(string recognizedIntentName)
    {
        if (guideManager == null)
        {
            Debug.LogError("VoiceCommandHandler: Cannot handle intent, GuideManager is not assigned.", this);
            return;
        }

        Debug.Log($"VoiceCommandHandler: Received intent: '{recognizedIntentName}'");

        if (!string.IsNullOrEmpty(recognizedIntentName) && recognizedIntentName.Equals(advanceGuideIntentName, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"VoiceCommandHandler: Matched intent '{advanceGuideIntentName}'. Advancing guide.");
            guideManager.AdvanceGuideManually();
        }
        else
        {
            Debug.Log($"VoiceCommandHandler: Intent '{recognizedIntentName}' did not match '{advanceGuideIntentName}'. No action taken for guide.");
            // You could add more if/else if blocks here to handle other voice intents
        }
    }

    // --- Example of a more complex handler if Wit SDK provides a richer response object ---
    // You would need to include the correct 'using' directive for IWitResponseNode, e.g.,
    // using Meta.WitAi.Interfaces;
    /*
    public void HandleWitResponse(Meta.WitAi.Interfaces.IWitResponseNode responseNode)
    {
        if (guideManager == null || responseNode == null) return;

        // Wit.ai typically returns multiple intents with confidence scores.
        // You usually take the one with the highest confidence.
        string bestIntentName = "";
        float highestConfidence = 0f;

        var intentsNode = responseNode.GetChildNode("intents");
        if (intentsNode != null && intentsNode.AsArray != null)
        {
            foreach (var intentNode in intentsNode.AsArray)
            {
                string currentIntentName = intentNode.GetChildNode("name")?.Value;
                float currentConfidence = intentNode.GetChildNode("confidence")?.AsFloat ?? 0f;

                if (!string.IsNullOrEmpty(currentIntentName) && currentConfidence > highestConfidence)
                {
                    highestConfidence = currentConfidence;
                    bestIntentName = currentIntentName;
                }
            }
        }

        Debug.Log($"VoiceCommandHandler: Best recognized intent: '{bestIntentName}' with confidence: {highestConfidence}");

        if (!string.IsNullOrEmpty(bestIntentName) && bestIntentName.Equals(advanceGuideIntentName, System.StringComparison.OrdinalIgnoreCase))
        {
            // Optional: Add a confidence threshold check
            // if (highestConfidence > 0.7f) // Example threshold
            // {
                Debug.Log($"VoiceCommandHandler: Matched intent '{advanceGuideIntentName}'. Advancing guide.");
                guideManager.AdvanceGuideManually();
            // }
            // else
            // {
            //     Debug.Log($"VoiceCommandHandler: Intent '{bestIntentName}' matched but confidence {highestConfidence} was below threshold.");
            // }
        }
        else
        {
            Debug.Log($"VoiceCommandHandler: Intent '{bestIntentName}' did not match '{advanceGuideIntentName}'. No action taken for guide.");
        }
    }
    */
}
