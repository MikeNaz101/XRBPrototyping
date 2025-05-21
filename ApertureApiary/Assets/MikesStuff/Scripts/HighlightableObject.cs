// File: HighlightableObject.cs
// Purpose: Manages the visual highlighting state of a GameObject.
// Instructions: Attach this script to any GameObject you want to be able to highlight.
// You'll need to implement the actual highlighting visual effect (e.g., outline, material change, child object).

using UnityEngine;

public class HighlightableObject : MonoBehaviour
{
    [Tooltip("Assign a child GameObject that represents the highlight visual (e.g., an outline mesh, a glowing sphere). This will be enabled/disabled.")]
    public GameObject highlightVisual;

    [Tooltip("Alternatively, assign a Renderer to change its material's emission for highlighting.")]
    public Renderer objectRenderer;
    public Color highlightEmissionColor = Color.yellow;
    private Color _originalEmissionColor;
    private bool _isEmissiveMaterial = false;

    private const string EmissionColorShaderProperty = "_EmissionColor";

    void Awake()
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(false);
        }
        if (objectRenderer != null && objectRenderer.material.HasProperty(EmissionColorShaderProperty))
        {
            objectRenderer.material.EnableKeyword("_EMISSION");
            _originalEmissionColor = objectRenderer.material.GetColor(EmissionColorShaderProperty);
            _isEmissiveMaterial = true;
        }
    }

    public void Highlight()
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(true);
        }
        if (_isEmissiveMaterial && objectRenderer != null)
        {
            objectRenderer.material.SetColor(EmissionColorShaderProperty, highlightEmissionColor);
        }
    }

    public void Unhighlight()
    {
        if (highlightVisual != null)
        {
            highlightVisual.SetActive(false);
        }
        if (_isEmissiveMaterial && objectRenderer != null)
        {
            objectRenderer.material.SetColor(EmissionColorShaderProperty, _originalEmissionColor);
        }
    }
}