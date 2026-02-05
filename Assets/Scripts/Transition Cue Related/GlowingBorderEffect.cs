using UnityEngine;

// Creates a glowing border effect around a panel with (optional) breathing animation; Emits light and pulses gently for visual interest
public class GlowingBorderEffect : MonoBehaviour
{
    // === Configuration ===
    private Color glowColor;
    private float baseIntensity;
    private float breathingSpeed;
    private bool enableBreathing = true;

    // === Components ===
    private GameObject borderRing;
    private Material borderMaterial;
    private Renderer borderRenderer;

    // === Animation State ===
    private float breathingTimer = 0f;

    // Initializes the glowing border effect
    public void Initialize(Color color, float intensity, float speed, bool withBreathing = true)
    {
        glowColor = color;
        baseIntensity = intensity;
        breathingSpeed = speed;
        enableBreathing = withBreathing;

        CreateGlowingBorder();
    }

    // Creates the border geometry and material
    private void CreateGlowingBorder()
    {
        // Create a slightly larger rounded cube to act as border
        borderRing = CreateRoundedCube();
        borderRing.name = "GlowingBorder";
        borderRing.transform.SetParent(transform, false);
        borderRing.transform.localPosition = Vector3.zero;
        borderRing.transform.localRotation = Quaternion.identity;

        // Scale slightly larger than parent to create border effect
        borderRing.transform.localScale = new Vector3(1.05f, 1.05f, 0.95f);

        // Remove collider
        Collider borderCollider = borderRing.GetComponent<Collider>();
        if (borderCollider == null)
        {
            borderCollider = borderRing.GetComponentInChildren<Collider>();
        }
        if (borderCollider != null)
        {
            Destroy(borderCollider);
        }

        // Create emissive material for glow effect
        borderRenderer = borderRing.GetComponent<Renderer>();
        if (borderRenderer == null)
        {
            borderRenderer = borderRing.GetComponentInChildren<Renderer>();
        }
        borderMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        // Set material properties for emission
        borderMaterial.EnableKeyword("_EMISSION");
        borderMaterial.SetColor("_BaseColor", glowColor);
        borderMaterial.SetColor("_EmissionColor", glowColor * baseIntensity * 2f);
        borderMaterial.SetFloat("_Surface", 1); // Transparent
        borderMaterial.SetFloat("_Smoothness", 0.9f);

        borderRenderer.material = borderMaterial;
    }

    void Update()
    {
        if (!enableBreathing || borderMaterial == null)
            return;

        // Breathing animation using sine wave
        breathingTimer += Time.deltaTime * breathingSpeed;
        float breathe = Mathf.Sin(breathingTimer) * 0.5f + 0.5f; // 0 to 1

        // Vary intensity between 50% and 100% of base
        float currentIntensity = Mathf.Lerp(baseIntensity * 0.5f, baseIntensity, breathe);

        // Update emission color
        borderMaterial.SetColor("_EmissionColor", glowColor * currentIntensity * 2f);
    }

    // Updates the glow color dynamically
    public void SetGlowColor(Color color)
    {
        glowColor = color;
        if (borderMaterial != null)
        {
            borderMaterial.SetColor("_BaseColor", color);
            borderMaterial.SetColor("_EmissionColor", color * baseIntensity * 2f);
        }
    }

    // Updates the glow intensity dynamically
    public void SetIntensity(float intensity)
    {
        baseIntensity = intensity;
        if (borderMaterial != null)
        {
            borderMaterial.SetColor("_EmissionColor", glowColor * baseIntensity * 2f);
        }
    }

    // Enables or disables the breathing animation
    public void SetBreathing(bool enabled)
    {
        enableBreathing = enabled;
    }

    void OnDestroy()
    {
        // Clean up material
        if (borderMaterial != null)
        {
            Destroy(borderMaterial);
        }
    }

    // Loads and instantiates the RoundedCubeModel from Assets folder
    // Falls back to primitive cube if model not found
    private static GameObject CreateRoundedCube()
    {
        GameObject modelPrefab = UnityEngine.Resources.Load<GameObject>("RoundedCubeModel");

        if (modelPrefab != null)
        {
            return Instantiate(modelPrefab);
        }
        else
        {
            Debug.LogWarning("[GlowingBorderEffect] RoundedCubeModel.fbx not found! Falling back to Cube primitive.");
            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }
}
