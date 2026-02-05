/*using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System;

// Provides visual feedback for button hover states in VR/AR
public class ButtonHoverEffect : MonoBehaviour
{
    // === Configuration ===
    private Color baseColor;
    private Color hoverColor;
    private float transitionSpeed = 8f;

    // === Components ===
    private Material buttonMaterial;
    private Renderer buttonRenderer;
    private XRSimpleInteractable interactable;

    // === State ===
    private bool isHovered = false;
    private Color currentColor;

    // Initializes the hover effect with base color and hover brightness multiplier
    // e.g. hoverBrightness of 1.3 = 30% brighter
    public void Initialize(Color color, float hoverBrightness)
    {
        baseColor = color;

        // Calculate hover color by increasing brightness (RGB values)
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        v = Mathf.Clamp01(v * hoverBrightness);
        hoverColor = Color.HSVToRGB(h, s, v);
        hoverColor.a = color.a;

        currentColor = baseColor;

        // Setup material if renderer exists
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
        {
            buttonMaterial = buttonRenderer.material;
            buttonMaterial.color = baseColor;
        }

        // Setup XR interaction callbacks
        interactable = GetComponent<XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
    }

    void Update()
    {
        if (buttonMaterial == null)
            return;

        // Smoothly transition to target color
        Color targetColor = isHovered ? hoverColor : baseColor;
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * transitionSpeed);
        buttonMaterial.color = currentColor;
    }

    // Called when XR hover begins (gaze or hand proximity)
    private void OnHoverEnter(UnityEngine.XR.Interaction.Toolkit.HoverEnterEventArgs args)
    {
        isHovered = true;
    }

    // Called when XR hover ends
    private void OnHoverExit(UnityEngine.XR.Interaction.Toolkit.HoverExitEventArgs args)
    {
        isHovered = false;
    }

    // Manually sets the hover state (useful for custom interaction systems)
    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
    }

    // Updates the base color dynamically
    public void SetBaseColor(Color color, float hoverBrightness)
    {
        baseColor = color;

        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        v = Mathf.Clamp01(v * hoverBrightness);
        hoverColor = Color.HSVToRGB(h, s, v);
        hoverColor.a = color.a;
    }

    void OnDestroy()
    {
        // Clean up event listeners
        if (interactable != null)
        {
            interactable.hoverEntered.RemoveListener(OnHoverEnter);
            interactable.hoverExited.RemoveListener(OnHoverExit);
        }

        // Clean up material
        if (buttonMaterial != null)
        {
            Destroy(buttonMaterial);
        }
    }
}
*/