using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles UI notifications and messages in VR/AR experiences
// Provides animated text displays for navigation, arrivals, and system messages
// Uses the same frosted glass design as TransitionCues for visual consistency
public class UINotificationSystem : MonoBehaviour
{
    private static UINotificationSystem instance;

    // === MODE TOGGLE ===
    // Set to true for canvas overlay, false for old 3D head-locked panel
    private bool useCanvasMode = true;

    // === CUSTOMIZABLE DESIGN PARAMETERS ===

    // Colors (matching expandedPanel from TransitionCueConfig)
    private static readonly Color navigationPanelColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);

    // Alpha values
    private const float frostedGlassAlpha = 0.3f;

    // Font settings (matching TransitionCueConfig)
    private const float descriptionFontSize = 0.04f;
    private const float generalFontSizeFactor = 12f;

    // Panel positioning
    private const float navigationPanelVerticalOffset = 0.3f; // How far below eye level (meters)
    private const float navigationPanelDistance = 2f; // Distance from camera (meters)

    public static UINotificationSystem Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("UINotificationSystem");
                instance = go.AddComponent<UINotificationSystem>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    // Routes to canvas or 3D panel based on useCanvasMode flag
    public IEnumerator ShowNavigationContinued(string destination, float swipeSpeed = 1.0f, float displayDuration = 3.0f, float yOffset = -50f)
    {
        if (useCanvasMode)
            yield return StartCoroutine(ShowNavigationContinued_Canvas(destination, displayDuration));
        else
            yield return StartCoroutine(ShowNavigationContinued_3DPanel(destination, swipeSpeed, displayDuration, yOffset));
    }

    // === CANVAS-BASED OVERLAY (new) ===
    // Displays centered text on a screen-space canvas that fades in, holds, then fades out
    private IEnumerator ShowNavigationContinued_Canvas(string destination, float displayDuration)
    {
        // Create overlay canvas (same pattern as TransitionEffects.FadeToVRWithTitle)
        GameObject canvasObj = new GameObject("NotificationCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Semi-transparent background bar behind the text (not fullscreen)
        GameObject bgPanel = new GameObject("BackgroundBar");
        bgPanel.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0f); // starts transparent, will fade in
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(800, 70);
        bgRect.anchoredPosition = Vector2.zero;

        // Notification text centered in viewport
        GameObject textObj = new GameObject("NotificationText");
        textObj.transform.SetParent(canvasObj.transform, false);
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"Navigation to {destination} continued";
        text.fontSize = 36;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 1f, 1f, 0f); // starts transparent

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(800, 70);
        textRect.anchoredPosition = Vector2.zero;

        // Fade in
        float fadeInDuration = 0.4f;
        float elapsed = 0f;
        float bgTargetAlpha = 0.35f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeInDuration);
            text.color = new Color(1f, 1f, 1f, t);
            bgImage.color = new Color(0f, 0f, 0f, t * bgTargetAlpha);
            yield return null;
        }

        text.color = Color.white;
        bgImage.color = new Color(0f, 0f, 0f, bgTargetAlpha);

        // Hold
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        float fadeOutDuration = 0.5f;
        elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeOutDuration);
            text.color = new Color(1f, 1f, 1f, 1f - t);
            bgImage.color = new Color(0f, 0f, 0f, bgTargetAlpha * (1f - t));
            yield return null;
        }

        Destroy(canvasObj);
    }

    // === 3D HEAD-LOCKED PANEL (old) ===
    // Swipes panel from top of viewport; Uses head-locked 3D panel with rounded cube
    private IEnumerator ShowNavigationContinued_3DPanel(string destination, float swipeSpeed = 1.0f, float displayDuration = 3.0f, float yOffset = -50f)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        // Create 3D rounded panel that follows the head
        GameObject navPanel = CreateFrostedPanel(
            $"Navigation to {destination} continued",
            navigationPanelColor,
            new Vector2(0.8f, 0.1f), // Panel size in meters
            0.02f
        );

        // Add head-following component
        HeadLockedPanel headLock = navPanel.AddComponent<HeadLockedPanel>();

        // Configure head-lock parameters
        float forwardDistance = 1.5f; // Distance in front of camera
        float verticalOffset = 0.4f; // Vertical offset (positive = up)

        // Animation: swipe down from above
        float startOffsetY = 0.8f; // Start higher
        float targetOffsetY = verticalOffset;

        headLock.Initialize(mainCam.transform, forwardDistance, startOffsetY);

        // Swipe down animation
        float swipeDuration = 1f / swipeSpeed;
        float elapsed = 0f;

        while (elapsed < swipeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / swipeDuration);
            float currentOffsetY = Mathf.Lerp(startOffsetY, targetOffsetY, t);
            headLock.SetVerticalOffset(currentOffsetY);
            yield return null;
        }

        headLock.SetVerticalOffset(targetOffsetY);

        // Display duration
        yield return new WaitForSeconds(displayDuration);

        // Swipe back up
        elapsed = 0f;
        while (elapsed < swipeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / swipeDuration);
            float currentOffsetY = Mathf.Lerp(targetOffsetY, startOffsetY, t);
            headLock.SetVerticalOffset(currentOffsetY);
            yield return null;
        }

        Destroy(navPanel);
    }

    // Component that makes a panel follow the camera/head with smooth tracking
    private class HeadLockedPanel : MonoBehaviour
    {
        private Transform cameraTransform;
        private float distance;
        private float verticalOffset;
        private float smoothSpeed = 1f; // How quickly the panel follows head movement

        public void Initialize(Transform camTransform, float dist, float vertOffset)
        {
            cameraTransform = camTransform;
            distance = dist;
            verticalOffset = vertOffset;
        }

        public void SetVerticalOffset(float offset)
        {
            verticalOffset = offset;
        }

        void LateUpdate()
        {
            if (cameraTransform == null) return;

            // Calculate target position in front of camera with vertical offset
            Vector3 targetPosition = cameraTransform.position
                + cameraTransform.forward * distance
                + Vector3.up * verticalOffset;

            // Smooth follow
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

            // Always face the camera
            transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position);
        }
    }

    // (!) DEPRECATED
    // Shows "ARRIVED", followed by "CHECK YOUR COMPANION APP" with phone icon.
    public IEnumerator ShowArrivalAndCompanionPrompt(float delayBetweenMessages = 1.0f, float displayDuration = 3.0f)
    {
        // MESSAGE 1: ARRIVED with checkmark icon
        yield return StartCoroutine(ShowMessageWithIcon(
            "ARRIVED",
            "✓", 
            new Color(0.2f, 0.8f, 0.3f), // Green
            0.3f, // Vertical offset (higher on screen)
            displayDuration
        ));

        yield return new WaitForSeconds(delayBetweenMessages);

        // MESSAGE 2: CHECK YOUR COMPANION APP with phone icon
        yield return StartCoroutine(ShowMessageWithIcon(
            "CHECK YOUR COMPANION APP",
            "📱",
            new Color(0.2f, 0.6f, 0.8f), // Cyan
            0f, // Center
            displayDuration
        ));
    }

    // Helper function: Displays a message with an icon in a frosted glass panel
    private IEnumerator ShowMessageWithIcon(string message, string icon, Color color, float verticalOffset, float duration)
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) yield break;

        // Create frosted panel
        GameObject panel = CreateFrostedPanel(
            "", // Text will be added separately with icon
            color,
            new Vector2(1.2f, 0.2f),
            0.03f
        );

        // Position panel in front of camera
        Vector3 panelPosition = mainCam.transform.position + mainCam.transform.forward * 2f + Vector3.up * verticalOffset;
        panel.transform.position = panelPosition;
        panel.transform.rotation = Quaternion.LookRotation(panel.transform.position - mainCam.transform.position);

        // Add icon text (left side)
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(panel.transform, false);
        iconObj.transform.localPosition = new Vector3(-0.4f, 0, 0.02f);

        TextMeshPro iconText = iconObj.AddComponent<TextMeshPro>();
        iconText.text = icon;
        iconText.fontSize = 0.15f;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = Color.white;

        // Add message text (right side)
        GameObject textObj = new GameObject("MessageText");
        textObj.transform.SetParent(panel.transform, false);
        textObj.transform.localPosition = new Vector3(0.1f, 0, 0.02f);

        TextMeshPro messageText = textObj.AddComponent<TextMeshPro>();
        messageText.text = message;
        messageText.fontSize = 0.08f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.alignment = TextAlignmentOptions.Left;
        messageText.color = Color.white;
        messageText.rectTransform.sizeDelta = new Vector2(0.9f, 0.2f);

        // Scale animation (pop in)
        Vector3 targetScale = panel.transform.localScale;
        panel.transform.localScale = Vector3.zero;

        float fadeDuration = 0.3f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / fadeDuration);
            panel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            yield return null;
        }

        panel.transform.localScale = targetScale;

        // Display duration
        yield return new WaitForSeconds(duration);

        // Scale animation (pop out)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / fadeDuration);
            panel.transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(panel);
    }

    // Creates a frosted glass panel with text (matches TransitionCue design) 
    private GameObject CreateFrostedPanel(string text, Color color, Vector2 size, float depth)
    {
        // Use rounded cube model for aesthetic rounded edges (same as TransitionCues)
        GameObject panel = CreateRoundedCube();
        panel.name = "NotificationPanel";
        panel.transform.localScale = new Vector3(size.x, size.y, depth);

        // Apply frosted glass material
        Renderer renderer = panel.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = panel.GetComponentInChildren<Renderer>();
        }
        Material frostedMat = CreateFrostedGlassMaterial(color, frostedGlassAlpha);
        renderer.material = frostedMat;

        // Remove collider
        Collider collider = panel.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        // Add text if provided
        if (!string.IsNullOrEmpty(text))
        {
            GameObject textObj = new GameObject("PanelText");
            textObj.transform.SetParent(panel.transform, false);

            // Adjust z-ordering
            TransitionCueConfig demoConfig = new TransitionCueConfig();
            textObj.transform.localPosition = new Vector3(0, 0, demoConfig.smallPanelDepth / 2 + demoConfig.textZOffset);

            TextMeshPro textMesh = textObj.AddComponent<TextMeshPro>();
            textMesh.text = text;
            textMesh.fontSize = descriptionFontSize * generalFontSizeFactor; // Use same font size as TransitionCue description
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Center;
            textMesh.color = Color.black;
            textMesh.rectTransform.sizeDelta = new Vector2(size.x * 0.9f, size.y * 0.8f);
        }

        // Add glowing border effect (same as TransitionCues)
        GlowingBorderEffect glowEffect = panel.AddComponent<GlowingBorderEffect>();
        glowEffect.Initialize(color, 0.6f, 1.5f, false);

        return panel;
    }

    // Creates a rounded cube from FBX model (same as TransitionCues)
    private GameObject CreateRoundedCube()
    {
        // Try to load the custom rounded cube model from Resources
        GameObject roundedCubePrefab = Resources.Load<GameObject>("RoundedCubeModel");

        if (roundedCubePrefab != null)
        {
            GameObject roundedCube = Instantiate(roundedCubePrefab);
            return roundedCube;
        }
        else
        {
            // Fallback to standard cube if model not found
            Debug.LogWarning("[UINotificationSystem] RoundedCubeModel not found in Resources. Using standard cube as fallback.");
            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }

    // Creates a frosted glass material with optional blur effect
    private Material CreateFrostedGlassMaterial(Color color, float alpha)
    {
        // Try to use custom blur shader first
        Shader blurShader = Shader.Find("Custom/FrostedGlassBlur");

        if (blurShader != null)
        {
            Material mat = new Material(blurShader);
            Color transparentColor = new Color(color.r, color.g, color.b, alpha);
            mat.SetColor("_BaseColor", transparentColor);
            mat.SetFloat("_Smoothness", 0.85f);
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_BlurAmount", 3.0f);
            mat.renderQueue = 3000;
            return mat;
        }
        else
        {
            // Fallback to standard URP Lit shader
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            Color transparentColor = new Color(color.r, color.g, color.b, alpha);
            mat.SetColor("_BaseColor", transparentColor);

            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Smoothness", 0.85f);
            mat.SetFloat("_Metallic", 0.1f);

            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);

            mat.renderQueue = 3000;
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return mat;
        }
    }
}
