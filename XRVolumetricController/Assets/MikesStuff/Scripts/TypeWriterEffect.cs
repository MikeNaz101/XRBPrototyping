using System.Collections;
using TMPro;
using UnityEngine;

public class TypeWriterEffect : MonoBehaviour
{
    [SerializeField] private float typingSpeed = 0.05f; // Speed of text appearing
    private Coroutine typingCoroutine;

    // Modified to return the Coroutine so other scripts can wait for it.
    public Coroutine StartTyping(TMP_Text textComponent, string text, float visibleDuration)
    {
        if (textComponent == null)
        {
            Debug.LogError("Text Component is null!", this.gameObject);
            return null; // Return null if setup fails
        }

        // Stop any previous typing/hiding sequence for this component
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Make sure the text component is active and clear before starting
        textComponent.gameObject.SetActive(true);
        textComponent.text = "";

        // Start the new typing and hiding coroutine
        typingCoroutine = StartCoroutine(TypeTextAndHide(textComponent, text, visibleDuration));
        return typingCoroutine; // Return the started coroutine
    }

    // If you need to stop typing and clear immediately (e.g., closing a menu)
    public void StopAndClear(TMP_Text textComponent)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        if (textComponent != null)
        {
            textComponent.text = "";
            textComponent.gameObject.SetActive(false);
        }
    }

    private IEnumerator TypeTextAndHide(TMP_Text textComponent, string text, float visibleDuration)
    {
        // Type out the message, character by character
        foreach (char letter in text.ToCharArray())
        {
            // Ensure textComponent hasn't been destroyed (e.g., if the parent canvas is destroyed prematurely)
            if (textComponent == null) yield break;
            textComponent.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        // Keep the full message visible for the specified duration
        yield return new WaitForSeconds(visibleDuration);

        // Now, hide the text component
        if (textComponent != null) // Check if textComponent still exists
        {
            textComponent.text = "";
            textComponent.gameObject.SetActive(false);
        }
        
        typingCoroutine = null; // Reset coroutine tracker
    }

    // Optional: Public getter for typingSpeed if other scripts might need it for calculations.
    // For the current request, it's not strictly necessary if we return the coroutine.
    public float GetTypingSpeed()
    {
        return typingSpeed;
    }
}