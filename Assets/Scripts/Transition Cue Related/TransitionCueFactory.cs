using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;

// Factory class for creating transition cues
public static class TransitionCueFactory
{
    // Creates a transition cue with expandable panel design
    //
    // Design includes:
    // - Small panel with glowing border and breathing animation
    // - Expands to show full panel with text/image when approached and gazed at
    // - Interactive button with hover effect
    // - All panels rotate toward user within constraints
    //
    // config: Configuration object with all customization parameters
    // Returns: Root GameObject of the transition cue
    public static GameObject CreateFrostedTransitionCue(TransitionCueConfig config)
    {
        // === Root Container ===
        GameObject root = new GameObject($"TransitionCue_{config.label}");
        root.transform.SetParent(config.parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * config.globalScale;
        //root.AddComponent<Canvas>();
        //GameObject pokeInteractable = new GameObject("PokeInteractionHolder");
        //pokeInteractable.AddComponent<PointableCanvas>();
        //pokeInteractable.AddComponent<PokeInteractable>();


        //GameObject rayInteractable = new GameObject("RayInteractionHolder");
        //rayInteractable.AddComponent<PointableCanvas>();
        //rayInteractable.AddComponent<RayInteractable>();

        // === Small Panel ===
        GameObject smallPanel = CreateSmallPanel(config);
        smallPanel.transform.SetParent(root.transform, false);

        // === Expanded Panel ===
        GameObject expandedPanel = CreateExpandedPanel(config);
        expandedPanel.transform.SetParent(root.transform, false);
        expandedPanel.transform.localPosition = Vector3.zero;

        // === Button ===
        GameObject button = CreateButton(config);
        button.transform.SetParent(root.transform, false);
        button.transform.localPosition = new Vector3(0, -(config.expandedPanelHeight / 2 + config.buttonOffset), 0);

        AddIsdkSelectToInvoke(button, config);

        // === Expansion Controller ===
        TransitionCueExpander expander = root.AddComponent<TransitionCueExpander>();
        expander.Initialize(config, smallPanel, expandedPanel, button);

        // === Rotation Effect ===
        if (config.enableTurnTowardsUser)
        {
            TurnTowardsUser rotateToUser = root.AddComponent<TurnTowardsUser>();
            rotateToUser.Initialize(config.turnMaxAngle, config.turnRotationSpeed, config.turnTriggerDistance);
        }

        // === Ambient Audio ===
        AddAmbientAudio(root, config);

        return root;
    }

    private static void AddIsdkSelectToInvoke(GameObject button, TransitionCueConfig config)
    {
        // Collider (falls dein RoundedCubeModel keinen hat)
        Collider col = button.GetComponent<Collider>();
        if (col == null) col = button.AddComponent<BoxCollider>();

        // Surface (macht den Collider als ISurface nutzbar)
        var surface = button.GetComponent<ColliderSurface>();
        if (surface == null) surface = button.AddComponent<ColliderSurface>();
        surface.InjectAllColliderSurface(col);

        /*// Surface Patch (needed for ISurfacePatch)
        var surfacePatch = button.GetComponent<ColliderSurfacePatch>();
        if (surfacePatch == null)
            surfacePatch = button.AddComponent<ColliderSurfacePatch>();

        surfacePatch.InjectAllColliderSurfacePatch(surface);*/

        // RayInteractable (für Ray/Pointer-Select; funktioniert i.d.R. auch mit Controller-Ray)
        var ray = button.GetComponent<RayInteractable>();
        if (ray == null) ray = button.AddComponent<RayInteractable>();
        ray.InjectAllRayInteractable(surface);

        /*var poke = button.GetComponent<PokeInteractable>();
        if (poke == null) poke = button.AddComponent<PokeInteractable>();
        poke.InjectAllPokeInteractable(surface as ISurfacePatch);*/

        ray.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select)
            {
                Debug.Log("[TransitionCue] Button selected");
                config?.onInteract?.Invoke();
            }
        };
        // PokeInteractable (für Ray/Pointer-Select; funktioniert i.d.R. auch mit Controller-Ray)
        /*var poke = button.GetComponent<PokeInteractable>();
        if (poke == null) poke = button.AddComponent<PokeInteractable>();
        poke.InjectAllPokeInteractable((ISurfacePatch)surface);*/

        /*poke.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select)
            {
                Debug.Log("[TransitionCue] Button selected");
                config?.onInteract?.Invoke();
            }
        };*/

    }

    private static System.Collections.IEnumerator BindNextFrame(InteractableUnityEventWrapper events, TransitionCueConfig config)
    {
        //yield return null; // 1 Frame warten
        yield return new WaitForSeconds(5f);
        /*if (events == null || events.WhenSelect == null)
        {
            Debug.LogError("[ISDK] Wrapper/WhenSelect not initialized yet.");
            yield break;
        }*/

        events.WhenSelect.AddListener(() => config?.onInteract?.Invoke());
        Debug.Log("select state of transition cue: ");
        Debug.Log(events.WhenSelect != null);
    }

    // Creates the small panel with label text and glowing border
    private static GameObject CreateSmallPanel(TransitionCueConfig config)
    {
        // Use rounded cube model for rounded edges
        GameObject smallPanel = CreateRoundedCube();
        smallPanel.name = "SmallPanel";
        smallPanel.transform.localRotation = Quaternion.identity; // No rotation needed for cube
        smallPanel.transform.localScale = new Vector3(config.smallPanelSize, config.smallPanelSize, config.smallPanelDepth);

        // Achieve a frosted glass effect/look
        Renderer renderer = smallPanel.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = smallPanel.GetComponentInChildren<Renderer>();
        }
        Material frostedMat = CreateFrostedGlassMaterial(config.primaryColor, config.frostedGlassAlpha + 0.2f);
        renderer.material = frostedMat;

        // Remove default collider (we'll add XR interaction to button only)
        Collider collider = smallPanel.GetComponent<Collider>();
        if (collider == null)
        {
            collider = smallPanel.GetComponentInChildren<Collider>();
        }
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        // === Label Text ===
        GameObject labelObj = new GameObject($"{config.label}_Text");
        labelObj.transform.SetParent(smallPanel.transform, false);

        // Ensure label is positioned in front of panel (negative Z for cube forward face)
        float labelOffset = (config.smallPanelDepth / 2) + config.textZOffset;
        labelObj.transform.localPosition = new Vector3(0, 0, labelOffset);
        labelObj.transform.localRotation = Quaternion.Euler(0, 180, 0); // Rotate 180° around Y-axis (green axis)

        TextMeshPro labelText = labelObj.AddComponent<TextMeshPro>();
        labelText.text = config.label;
        labelText.fontSize = config.labelFontSize * config.generalFontSizeFactor;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        // Apply custom font (bold variant)
        ApplyCustomFont(labelText, config, true);

        // === Glowing Border Effect ===
        GlowingBorderEffect glowEffect = smallPanel.AddComponent<GlowingBorderEffect>();
        glowEffect.Initialize(config.primaryColor, config.glowIntensity, config.glowBreathingSpeed, true);

        return smallPanel;
    }

    // Creates the expanded panel with description and optional content
    private static GameObject CreateExpandedPanel(TransitionCueConfig config)
    {
        // Use rounded cube model for aesthetic rounded edges
        GameObject expandedPanel = CreateRoundedCube();
        expandedPanel.name = "ExpandedPanel";
        expandedPanel.transform.localScale = new Vector3(config.expandedPanelWidth, config.expandedPanelHeight, config.expandedPanelDepth);

        // Frosted glass look (use expandedPanelColor instead of primaryColor)
        Renderer renderer = expandedPanel.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = expandedPanel.GetComponentInChildren<Renderer>();
        }
        Material frostedMat = CreateFrostedGlassMaterial(config.expandedPanelColor, config.frostedGlassAlpha);
        renderer.material = frostedMat;

        // XR Interaction for Expanded Panel
        /*XRSimpleInteractable expandedInteractable = expandedPanel.AddComponent<XRSimpleInteractable>();
        if (config.onInteract != null)
        {
            expandedInteractable.selectEntered.AddListener((args) => config.onInteract());
        }*/

        // Content (Screenshot or 3D Object)
        float contentBottomY = 0f; // Y-position of the bottom of the content

        if (config.screenshotTexture != null)
        {
            contentBottomY = CreateScreenshotDisplay(expandedPanel.transform, config);
        }
        else if (config.contentObject != null)
        {
            contentBottomY = Create3DObjectDisplay(expandedPanel.transform, config);
        }
        else
        {
            // No content, center the description text
            contentBottomY = config.expandedPanelHeight * 0.1f;
        }

        // Description Text
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(expandedPanel.transform, false);

        float descTextOffset = (config.expandedPanelDepth / 2) + config.textZOffset;
        float descYPosition = contentBottomY - config.contentDescriptionSpacing;
        descObj.transform.localPosition = new Vector3(0, descYPosition, descTextOffset);
        descObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        TextMeshPro descText = descObj.AddComponent<TextMeshPro>();
        descText.text = config.expandedDescription;
        descText.fontSize = config.descriptionFontSize * config.generalFontSizeFactor;
        descText.alignment = TextAlignmentOptions.Center;
        descText.color = Color.white;

        ApplyCustomFont(descText, config, false);

        float textWidth = config.screenshotTexture != null ? config.screenshotWidth : config.expandedPanelWidth * (1.0f - config.contentMarginLeft - config.contentMarginRight);
        descText.rectTransform.sizeDelta = new Vector2(textWidth, config.descriptionFontSize * 3f);
        descText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Force higher render queue to always render on top of transparent panel
        if (descText.fontMaterial != null)
        {
            descText.fontMaterial.renderQueue = 3100; // Higher than panel's 3000
        }

        // Compensate for parent scale squashing
        descObj.transform.localScale = new Vector3(1f / expandedPanel.transform.localScale.x, 1f / expandedPanel.transform.localScale.y, 1f);

        return expandedPanel;
    }

    private static float CreateScreenshotDisplay(Transform parent, TransitionCueConfig config)
    {
        GameObject imageQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        imageQuad.name = "ScreenshotDisplay";
        imageQuad.transform.SetParent(parent, false);

        float imageWidth = config.screenshotWidth;
        float imageHeight = config.screenshotHeight;
        imageQuad.transform.localScale = new Vector3(imageWidth, imageHeight, 1f);

        float topOfPanel = config.expandedPanelHeight / 2;
        float marginTopOffset = config.expandedPanelHeight * config.contentMarginTop;
        float imageCenterY = topOfPanel - marginTopOffset - (imageHeight / 2);

        // Use same Z-offset as text to prevent transparency sorting issues
        imageQuad.transform.localPosition = new Vector3(0, imageCenterY, config.expandedPanelDepth / 2 + config.textZOffset);
        imageQuad.transform.localRotation = Quaternion.Euler(0, 180, 0);

        // Compensate for parent scale to prevent squashing
        imageQuad.transform.localScale = new Vector3(
            imageWidth / parent.localScale.x,
            imageHeight / parent.localScale.y,
            1f
        );

        Collider collider = imageQuad.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        Renderer renderer = imageQuad.GetComponent<Renderer>();
        Material imageMat;
        if (config.leadsToAR)
        {
            imageMat = CreateFrostedGlassMaterial(Color.clear, 0f);
        }
        else
        {
            imageMat = new Material(Shader.Find("Custom/HorizontalFadeImage"));
            imageMat.mainTexture = config.screenshotTexture;
            imageMat.SetFloat("_FadeEdgeStart", 0.15f);
            imageMat.SetColor("_Color", Color.white);
        }
        imageMat.renderQueue = 3100; // Higher than panel's 3000 to render on top
        renderer.material = imageMat;

        return imageCenterY - (imageHeight / 2);
    }

    // Creates a 3D object display in the expanded panel (Alternative to CreateScreenshotDisplay())
    // Returns the Y-position of the bottom edge of the 3D object bounds
    private static float Create3DObjectDisplay(Transform parent, TransitionCueConfig config)
    {
        GameObject contentObj = UnityEngine.Object.Instantiate(config.contentObject);
        contentObj.name = "3DObjectDisplay";
        contentObj.transform.SetParent(parent, false);

        // Calculate available space within margins
        float availableWidth = config.expandedPanelWidth * (1.0f - config.contentMarginLeft - config.contentMarginRight);
        float availableHeight = config.expandedPanelHeight * (1.0f - config.contentMarginTop - config.contentMarginBottom);

        // Scale to fit within available space
        float maxDimension = Mathf.Min(availableWidth, availableHeight) * 0.4f;
        contentObj.transform.localScale = Vector3.one * maxDimension;

        // Position from top with margin
        float topOfPanel = config.expandedPanelHeight / 2;
        float marginTopOffset = config.expandedPanelHeight * config.contentMarginTop;
        float objectCenterY = topOfPanel - marginTopOffset - (maxDimension / 2);

        // Position OUTSIDE panel surface
        contentObj.transform.localPosition = new Vector3(0, objectCenterY, config.expandedPanelDepth / 2 + 0.001f);

        // Return approximate bottom Y-position (assuming object height is roughly maxDimension)
        return objectCenterY - (maxDimension / 2f);
    }

    // Creates the interactive button below the expanded panel
    private static GameObject CreateButton(TransitionCueConfig config)
    {
        
        // Use rounded cube model for aesthetic rounded edges
        GameObject button = CreateRoundedCube();
        button.name = "InteractionButton";
        button.transform.localScale = new Vector3(config.buttonWidth, config.buttonHeight, config.buttonDepth);

        // Button look
        Renderer renderer = button.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = button.GetComponentInChildren<Renderer>();
        }
        Material buttonMat = CreateFrostedGlassMaterial(config.primaryColor, config.frostedGlassAlpha + 0.2f);
        renderer.material = buttonMat;

        // === Button Text ===
        GameObject textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(button.transform, false);
        // Position text clearly in front of the button (negative Z for cube forward face)
        float buttonTextOffset = (config.buttonDepth / 2) + config.textZOffset;
        textObj.transform.localPosition = new Vector3(0, 0, buttonTextOffset);
        textObj.transform.localRotation = Quaternion.Euler(0, 180, 0); // Rotate 180° around Y-axis (green axis)

        TextMeshPro buttonText = textObj.AddComponent<TextMeshPro>();
        buttonText.text = config.buttonText;
        buttonText.fontSize = config.buttonFontSize * config.generalFontSizeFactor;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.white;
        buttonText.enableAutoSizing = false; // Prevent vertical squashing

        // Apply custom font (bold variant)
        ApplyCustomFont(buttonText, config, true);

        // Fix vertical compression caused by parent rotation: Scale Y by factor ~5 to compensate
        textObj.transform.localScale = new Vector3(1f, 5f, 1f);

        // === XR Interaction ===
        /*XRSimpleInteractable interactable = button.AddComponent<XRSimpleInteractable>();
        if (config.onInteract != null)
        {
            interactable.selectEntered.AddListener((args) => config.onInteract());
        }*/

        // === Hover Effect ===
        /*ButtonHoverEffect hoverEffect = button.AddComponent<ButtonHoverEffect>();
        hoverEffect.Initialize(config.primaryColor, config.buttonHoverBrightness);*/

        return button;
    }

    // Creates a frosted glass material with transparency
    // Uses URP/Lit shader with transparency and smoothness for a polished glass effect
    private static Material CreateFrostedGlassMaterial(Color color, float alpha)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        // Set base color with specified alpha for semi-transparency
        Color transparentColor = new Color(color.r, color.g, color.b, alpha);
        mat.SetColor("_BaseColor", transparentColor);

        // Set Surface Type to transparent
        mat.SetFloat("_Surface", 1); // 1 = transparent

        // High smoothness for glass-like reflective appearance
        mat.SetFloat("_Smoothness", 0.85f);
        mat.SetFloat("_Metallic", 0.1f);

        // Configure alpha blending for proper transparency
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0); // No depth write for transparency

        // Set render queue for transparency
        mat.renderQueue = 3000;

        // Enable transparency keywords
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        return mat;
    }

    /*private static Material CreateFrostedGlassMaterialAlternative(Color color, float alpha)
    {
        var baseMat = Resources.Load<Material>("FrostedGlass_Base");

        Material mat = null;
        if (baseMat != null)
        {
            mat = UnityEngine.Object.Instantiate(baseMat);
        }
        else
        {
            // Fallback: direkt Shader benutzen (damit kein Hard-Fail)
            var shader = Shader.Find("Custom/FrostedGlassURP");
            if (shader == null)
            {
                Debug.LogError("[TransitionCueFactory] Custom/FrostedGlassURP shader not found (compile error or not created).");
                return new Material(Shader.Find("Universal Render Pipeline/Lit"));
            }
            mat = new Material(shader);
        }

        if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", color);
        if (mat.HasProperty("_Alpha")) mat.SetFloat("_Alpha", alpha);

        if (mat.HasProperty("_Distortion")) mat.SetFloat("_Distortion", 0.6f);
        if (mat.HasProperty("_BlurRadius")) mat.SetFloat("_BlurRadius", 1.0f);

        var bump = Resources.Load<Texture2D>("FrostedGlass_NoiseNormal");
        if (bump != null && mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", bump);

        return mat;
    }*/

    // === Helper Methods ===

    // Loads and instantiates the RoundedCubeModel from Assets folder
    private static GameObject CreateRoundedCube()
    {
        GameObject modelPrefab = Resources.Load<GameObject>("RoundedCubeModel");

        if (modelPrefab != null)
        {
            return UnityEngine.Object.Instantiate(modelPrefab);
        }
        else
        {
            Debug.LogWarning("[TransitionCueFactory] RoundedCubeModel.fbx not found! Falling back to Cube primitive.");
            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }

    // Recursively sets layer for GameObject and all its children
    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    // === Helper Methods ===

    // Adds ambient audio to the transition cue
    private static void AddAmbientAudio(GameObject root, TransitionCueConfig config)
    {
        // Load default sound
        AudioClip soundClip = config.ambientSound;

        if (soundClip == null)
        {
            return; // No sound to play
        }

        // Create AudioSource component
        AudioSource audioSource = root.AddComponent<AudioSource>();
        audioSource.clip = soundClip;
        audioSource.volume = config.ambientVolume;
        audioSource.loop = config.ambientLoop;
        audioSource.spatialBlend = config.ambientSpatialBlend;
        audioSource.minDistance = config.ambientMinDistance;
        audioSource.maxDistance = config.ambientMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.priority = config.ambientPriority;
        audioSource.dopplerLevel = config.ambientDopplerLevel;
        audioSource.playOnAwake = true;

        // Enable spread for more natural 3D sound
        audioSource.spread = 60f; // Degrees of spread for 3D sound

        // Optional: Add low-pass filter for distant, muffled effect
        AudioLowPassFilter lowPassFilter = root.AddComponent<AudioLowPassFilter>();
        lowPassFilter.cutoffFrequency = 5000f; // Cuts high frequencies for softer sound

        audioSource.Play();
    }

    // Applies custom font to TextMeshPro component
    private static void ApplyCustomFont(TMPro.TextMeshPro textComponent, TransitionCueConfig config, bool useBold)
    {
        TMPro.TMP_FontAsset fontToUse = null;

        // Try to use config font first
        if (useBold && config.customFontBold != null)
        {
            fontToUse = config.customFontBold;
        }
        else if (!useBold && config.customFont != null)
        {
            fontToUse = config.customFont;
        }
        else
        {
            // Load default SF Pro Display font from Resources
            string fontName = useBold ? "SF-Pro-Display-Bold-Font" : "SF-Pro-Display-Regular-Font";
            fontToUse = Resources.Load<TMPro.TMP_FontAsset>(fontName);

            if (fontToUse == null)
            {
                Debug.LogWarning($"[TransitionCueFactory] Font '{fontName}' not found in Resources. Using default TextMeshPro font.");
                return; // Keep default font
            }
        }
        textComponent.font = fontToUse;
    }
}
