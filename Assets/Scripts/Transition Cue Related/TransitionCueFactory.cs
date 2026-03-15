using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using TMPro;
using UnityEngine;
using System;
using UnityEngine.Video;

// Factory class for creating transition cues
public static class TransitionCueFactory
{

    // Creates a cue with expandable panel design or a minimal cue
    //
    // Design includes:
    // - Small panel with glowing border and breathing animation
    // - Expands to show full panel with text/image when approached and gazed at
    // - Interactive button with hover effect
    // - All panels rotate toward user within constraints
    //
    // config: Configuration object with all customization parameters
    // Returns: Root GameObject of the cue
    public static GameObject CreateCue(TransitionCueConfig config)
    {
        if (!config.isBland)
        {
            // Normal, enhanced cue design

            // === Root Container ===
            GameObject root = new GameObject($"TransitionCue_{config.label}");
            root.transform.SetParent(config.parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * config.globalScale;


            // === Small Panel ===
            GameObject smallPanel = CreateSmallPanel(config);
            smallPanel.transform.SetParent(root.transform, false);

            // === Expanded Panel ===
            float textBottomY;
            GameObject expandedPanel = CreateExpandedPanel(config, out textBottomY);
            expandedPanel.transform.SetParent(root.transform, false);
            expandedPanel.transform.localPosition = Vector3.zero;
            AddIsdkSelectToInvoke(expandedPanel, config);
            // === Button Container ===
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(root.transform, false);

            if (config.leadsToAR)
            {
                // Keep constant spacing relative to text
                buttonContainer.transform.localPosition =
                    new Vector3(0, textBottomY - config.buttonOffset, 0);
            }

            else
            {
                // Original behaviour (relative to panel)
                float actualPanelHeight = expandedPanel.transform.localScale.y;

                buttonContainer.transform.localPosition =
                    new Vector3(0, -(actualPanelHeight / 2 + config.buttonOffset), 0);
            }
            //buttonContainer.transform.localPosition = new Vector3(0, -(config.expandedPanelHeight / 2 + config.buttonOffset), 0);

            // === Action Button ===
            // === Close Button (only for collapsible cues) ===
            GameObject closeButton = null;
            GameObject actionButton = null;
            if (!config.isLeaveCue)
            {
                actionButton = CreateButton(config);
                actionButton.transform.SetParent(buttonContainer.transform, false);
                AddIsdkSelectToInvoke(actionButton, config);

                if (!config.alwaysExpanded)
                {
                    float actionButtonX = (config.buttonSpacing + config.closeButtonSize) / 2f;
                    actionButton.transform.localPosition = new Vector3(actionButtonX, 0, 0);

                    closeButton = CreateCloseButton(config);
                    float closeButtonX = -(config.buttonWidth + config.buttonSpacing) / 2f;
                    closeButton.transform.SetParent(buttonContainer.transform, false);
                    closeButton.transform.localPosition = new Vector3(closeButtonX, 0, 0);
                }
            }

            // === Expansion Controller ===
            TransitionCueExpander expander = root.AddComponent<TransitionCueExpander>();
            expander.Initialize(config, smallPanel, expandedPanel, buttonContainer);

            // Wire close button to dismiss the expanded panel
            if (closeButton != null)
            {
                AddIsdkSelectToInvoke(closeButton, () => expander.DismissToSmall());
            }

            // add collision interaction
            AddCollisionSupport(root, smallPanel, expandedPanel, actionButton, config);

            // === Rotation Effect ===
            if (config.enableTurnTowardsUser && !config.leadsToAR)
            {
                TurnTowardsUser rotateToUser = root.AddComponent<TurnTowardsUser>();
                rotateToUser.Initialize(config.turnMaxAngle, config.turnRotationSpeed, config.turnTriggerDistance);
            }

            // === Arrival Welcome Animation ===
            if (config.isArrival)
            {
                WelcomeAnimation welcome = root.AddComponent<WelcomeAnimation>();
                welcome.Initialize(config.turnTriggerDistance);
            }

            if (!config.isArrival)
            {
                // === Ambient Audio ===
                AddAmbientAudio(root, config);

            }

            return root;
        }
        else
        {
            // Minimal cue design

            GameObject root = new GameObject($"MinimalCue_{config.label}");
            root.transform.SetParent(config.parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one * config.globalScale;

            // === Small Panel ===
            GameObject smallPanel = CreateSmallPanel(config);
            smallPanel.transform.SetParent(root.transform, false);
            AddIsdkSelectToInvoke(smallPanel, config);

            // ADD THIS
            AddCollisionSupport(root, smallPanel, null, null, config);

            return root;
        }
    }

    private static void AddIsdkSelectToInvoke(GameObject button, TransitionCueConfig config)
    {
        AddIsdkSelectToInvoke(button, () => config?.onInteract?.Invoke());
    }

    private static void AddIsdkSelectToInvoke(GameObject button, Action action)
    {
        Collider col = button.GetComponent<Collider>();
        if (col == null) col = button.AddComponent<BoxCollider>();

        var surface = button.GetComponent<ColliderSurface>();
        if (surface == null) surface = button.AddComponent<ColliderSurface>();
        surface.InjectAllColliderSurface(col);

        var ray = button.GetComponent<RayInteractable>();
        if (ray == null) ray = button.AddComponent<RayInteractable>();
        ray.InjectAllRayInteractable(surface);

        ray.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select)
            {
                action?.Invoke();
            }
        };
    }

    private static System.Collections.IEnumerator BindNextFrame(InteractableUnityEventWrapper events, TransitionCueConfig config)
    {
        yield return new WaitForSeconds(5f);

        events.WhenSelect.AddListener(() => config?.onInteract?.Invoke());
    }

    // Creates the small panel with label text and glowing border
    private static GameObject CreateSmallPanel(TransitionCueConfig config)
    {
        // Use rounded cube model for rounded edges
        GameObject smallPanel = CreateRoundedCube();
        smallPanel.name = "SmallPanel";
        smallPanel.transform.localRotation = Quaternion.identity; // No rotation needed for cube
        float widthMultiplier = 3.5f; // increase width (was effectively 1 before)

        smallPanel.transform.localScale = new Vector3(
            config.smallPanelSize * widthMultiplier,
            config.smallPanelSize,
            config.smallPanelDepth
        );

        // Achieve a frosted glass effect/look
        Renderer renderer = smallPanel.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = smallPanel.GetComponentInChildren<Renderer>();
        }

        // Set the right material / optic, based on type of cue (Enhanced vs. Minimal)
        if (!config.isBland)
        {
            Material frostedMat = CreateFrostedGlassMaterial(config.primaryColor, config.frostedGlassAlpha + 0.2f);
            renderer.material = frostedMat;
        }
        else
        {
            Material frostedMat = CreateFrostedGlassMaterial(config.primaryColor, 1);
            renderer.material = frostedMat;
        }

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


        // --- IMPORTANT: compensate parent scaling so text size stays constant ---
        Vector3 panelScale = smallPanel.transform.localScale;

        labelObj.transform.localScale = new Vector3(
    1f / widthMultiplier, // keeps original visual width
    1f,
    1f
);

        // Apply custom font (bold variant)
        ApplyCustomFont(labelText, config, true);

        // === Glowing Border Effect ===
        GlowingBorderEffect glowEffect = smallPanel.AddComponent<GlowingBorderEffect>();
        glowEffect.Initialize(config.primaryColor, config.glowIntensity, config.glowBreathingSpeed, true);

        return smallPanel;
    }

    // Creates the expanded panel with description and optional content
    private static GameObject CreateExpandedPanel(TransitionCueConfig config, out float textBottomY)
    {
        // Use rounded cube model for aesthetic rounded edges
        GameObject expandedPanel = CreateRoundedCube();
        expandedPanel.name = "ExpandedPanel";
        if (config.leadsToAR)
        {
            expandedPanel.transform.localScale = new Vector3(config.expandedPanelWidth * 2, config.expandedPanelHeight * 4, config.expandedPanelDepth);
        }
        else if (config.leadsOutOfLecture)
        {
            expandedPanel.transform.localScale = new Vector3(config.expandedPanelWidth * 4, config.expandedPanelHeight * 2, config.expandedPanelDepth);
        }
        else
        {
            expandedPanel.transform.localScale = new Vector3(config.expandedPanelWidth, config.expandedPanelHeight, config.expandedPanelDepth);
        }

        // Frosted glass look (use expandedPanelColor instead of primaryColor)
        Renderer renderer = expandedPanel.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = expandedPanel.GetComponentInChildren<Renderer>();
        }

        // Content (Screenshot or 3D Object)
        float contentBottomY = 0f; // Y-position of the bottom of the content

        bool noContentLayout = false;
        if (!config.isBland)
        {
            if (config.isTransparent && !config.isLeaveCue)
            {
                Material frostedMat;

                if (config.leadsToAR)
                {
                    frostedMat = CreateFrostedGlassMaterial(config.expandedPanelColor, 0f);
                }
                else if (config.isVoiceCue)
                {
                    frostedMat = CreateBlueMaterial();
                }
                else
                {
                    frostedMat = CreateFrostedGlassMaterial(config.expandedPanelColor, config.frostedGlassAlpha);
                }
                if (frostedMat != null)
                {
                    renderer.material = frostedMat;
                }
            }
            else
            {
                Material frostedMat;
                if (config.isLeaveCue)
                {
                    frostedMat = CreateWhiteMaterial();
                }
                else if (config.isVoiceCue)
                {
                    frostedMat = CreateBlueMaterial();
                }
                else
                {
                    frostedMat = CreateFrostedGlassMaterial(config.expandedPanelColor, 1f);
                }
                if (frostedMat != null)
                {
                    renderer.material = frostedMat;
                }
            }
            if (config.videoClip != null)
            {
                contentBottomY = CreateVideoDisplay(expandedPanel.transform, config);
            }
            else if (config.screenshotTexture != null)
            {
                contentBottomY = CreateScreenshotDisplay(expandedPanel.transform, config);
            }
            else if (config.contentObject != null)
            {
                contentBottomY = Create3DObjectDisplay(expandedPanel.transform, config);
            }
            else
            {
                // No content
                //Make expanded panel smaller and center the description text
                //contentBottomY = config.expandedPanelHeight * 0.1f;


                noContentLayout = true;
            }
        }

        // Description Text
        GameObject descObj = new GameObject("DescriptionText");
        descObj.transform.SetParent(expandedPanel.transform, false);

        float descTextOffset = (config.expandedPanelDepth / 2) + config.textZOffset;

        float descYPosition = noContentLayout
        ? contentBottomY
        : contentBottomY - config.contentDescriptionSpacing;

        //float descYPosition = contentBottomY - config.contentDescriptionSpacing;
        descObj.transform.localPosition = new Vector3(0, descYPosition, descTextOffset);
        descObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

        TextMeshPro descText = descObj.AddComponent<TextMeshPro>();
        descText.text = config.expandedDescription;
        descText.fontSize = config.descriptionFontSize * config.generalFontSizeFactor;
        descText.alignment = TextAlignmentOptions.Center;
        if (config.isLeaveCue || config.leadsToAR)
        {
            descText.color = Color.black;
        }
        else
        {
            descText.color = Color.white;
        }

        ApplyCustomFont(descText, config, false);

        float textWidth = config.screenshotTexture != null ? config.screenshotWidth : config.expandedPanelWidth * (1.0f - config.contentMarginLeft - config.contentMarginRight);
        descText.rectTransform.sizeDelta = new Vector2(textWidth, config.descriptionFontSize * 3f);
        descText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Force higher render queue to always render on top of transparent panel
        if (descText.fontMaterial != null)
        {
            descText.fontMaterial.renderQueue = 3100; // Higher than panel's 3000
        }

        Vector2 preferredSize = descText.GetPreferredValues(descText.text, textWidth, Mathf.Infinity);
        float textHeight = preferredSize.y;
        textBottomY = descYPosition - textHeight / 2f;

        if (noContentLayout && !config.leadsToAR)
        {
            MakeExpandedPanelSmallerAndCenterDescription(
                expandedPanel.transform,
                config,
                textHeight
            );
        }
        // Compensate for parent scale squashing
        descObj.transform.localScale = new Vector3(1f / expandedPanel.transform.localScale.x, 1f / expandedPanel.transform.localScale.y, 1f);

        return expandedPanel;
    }


    // Makes the expanded panel smaller when there is no content
    // and returns a centered Y position for the description text
    private static float MakeExpandedPanelSmallerAndCenterDescription(
    Transform expandedPanel,
    TransitionCueConfig config,
    float textHeight)
    {
        float verticalPadding = config.descriptionFontSize * 1.5f;

        float newHeight = textHeight + verticalPadding;

        float newWidth = config.expandedPanelWidth;

        expandedPanel.localScale = new Vector3(
            newWidth,
            newHeight,
            config.expandedPanelDepth
        );

        return 0f; // keep text centered
    }

    private static float CreateVideoDisplay(Transform parent, TransitionCueConfig config)
    {
        GameObject videoQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        videoQuad.name = "VideoDisplay";
        videoQuad.transform.SetParent(parent, false);

        float videoWidth = config.videoWidth;
        float videoHeight = config.videoHeight;
        videoQuad.transform.localScale = new Vector3(videoWidth, videoHeight, 1f);

        float topOfPanel = config.expandedPanelHeight / 2;
        float marginTopOffset = config.expandedPanelHeight * config.contentMarginTop;
        float videoCenterY = topOfPanel - marginTopOffset - (videoHeight / 2);

        // Same Z-offset logic as screenshot
        videoQuad.transform.localPosition = new Vector3(
            0,
            videoCenterY,
            config.expandedPanelDepth / 2 + config.textZOffset
        );

        videoQuad.transform.localRotation = Quaternion.Euler(0, 180, 0);

        // Prevent squashing from parent scale
        videoQuad.transform.localScale = new Vector3(
            videoWidth / parent.localScale.x,
            videoHeight / parent.localScale.y,
            1f
        );

        Collider collider = videoQuad.GetComponent<Collider>();
        if (collider != null)
        {
            UnityEngine.Object.Destroy(collider);
        }

        Renderer renderer = videoQuad.GetComponent<Renderer>();

        // Create render texture for video output
        RenderTexture renderTexture = new RenderTexture(1024, 1024, 0);

        // Setup video player
        VideoPlayer videoPlayer = videoQuad.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = true;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.source = VideoSource.VideoClip; // or VideoSource.VideoClip
        if (config.isVoiceCue)
        {
            videoPlayer.loopPointReached += (vp) =>
            {
                GameObject.Destroy(config.parent.gameObject);
            };
        }

        if (config.videoClip != null)
        {
            videoPlayer.clip = config.videoClip;
        }

        Material videoMat = new Material(Shader.Find("Unlit/Texture"));
        videoMat.mainTexture = renderTexture;
        videoMat.renderQueue = 3100;

        renderer.material = videoMat;

        videoPlayer.Prepare();
        videoPlayer.Play();

        return videoCenterY - (videoHeight / 2);
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
        if (config.isTransparent)
        {
            Material buttonMat = CreateFrostedGlassMaterial(config.primaryColor, config.frostedGlassAlpha + 0.2f);
            renderer.material = buttonMat;
        }
        else
        {
            Material buttonMat = CreateFrostedGlassMaterial(config.primaryColor, 1f);
            renderer.material = buttonMat;
        }

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

        return button;
    }

    // Creates the close button with an X icon
    private static GameObject CreateCloseButton(TransitionCueConfig config)
    {
        GameObject closeButton = CreateRoundedCube();
        closeButton.name = "CloseButton";
        closeButton.transform.localScale = new Vector3(config.closeButtonSize, config.buttonHeight, config.buttonDepth);

        Renderer renderer = closeButton.GetComponent<Renderer>();
        if (renderer == null)
            renderer = closeButton.GetComponentInChildren<Renderer>();
        //Material buttonMat = CreateFrostedGlassMaterial(config.primaryColor, config.frostedGlassAlpha + 0.2f);
        Material buttonMat = CreateFrostedGlassMaterial(Color.grey, config.frostedGlassAlpha + 0.2f);
        if (buttonMat != null)
        {
            renderer.material = buttonMat;
        }

        CreateXIcon(closeButton.transform, config);// === Button Text ===
        /*GameObject textObj = new GameObject("CloseButtonText");
        textObj.transform.SetParent(closeButton.transform, false);
        // Position text clearly in front of the button (negative Z for cube forward face)
        float closeButtonTextOffset = (config.buttonDepth / 2) + config.textZOffset;
        textObj.transform.localPosition = new Vector3(0, 0, closeButtonTextOffset);
        textObj.transform.localRotation = Quaternion.Euler(0, 180, 0); // Rotate 180° around Y-axis (green axis)

        TextMeshPro closeButtonText = textObj.AddComponent<TextMeshPro>();
        closeButtonText.text = "X";
        closeButtonText.fontSize = config.buttonFontSize * config.generalFontSizeFactor;
        closeButtonText.fontStyle = FontStyles.Bold;
        closeButtonText.alignment = TextAlignmentOptions.Center;
        closeButtonText.color = Color.white;
        closeButtonText.enableAutoSizing = false; // Prevent vertical squashing

        // Apply custom font (bold variant)
        ApplyCustomFont(closeButtonText, config, true);

        // Fix vertical compression caused by parent rotation: Scale Y by factor ~5 to compensate
        textObj.transform.localScale = new Vector3(1f, 5f, 1f);
        UnityEngine.Debug.Log("created X text");*/

        return closeButton;
    }

    // Creates two crossed lines forming an X icon on the front face of the parent
    private static void CreateXIcon(Transform parent, TransitionCueConfig config)
    {
        float zOffset = (config.buttonDepth / 2) + config.textZOffset;
        float lineLength = 0.6f;
        float lineThickness = 0.06f;
        Material lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (lineMat != null)
        {
            lineMat.SetColor("_BaseColor", Color.white);
            lineMat.renderQueue = 3100;

            for (int i = 0; i < 2; i++)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Quad);
                line.name = i == 0 ? "XLine1" : "XLine2";
                line.transform.SetParent(parent, false);
                line.transform.localPosition = new Vector3(0, 0, zOffset);
                float angle = i == 0 ? 45f : -45f;
                line.transform.localRotation = Quaternion.Euler(0, 180, angle);
                line.transform.localScale = new Vector3(lineLength, lineThickness, 1f);

                Collider col = line.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.Destroy(col);

                line.GetComponent<Renderer>().material = lineMat;
            }
        }
    }

    // Creates a standalone interactive button in the same visual style as cue action buttons (for the mensa)
    public static GameObject CreateStandaloneButton(TransitionCueConfig config)
    {
        GameObject root = new GameObject($"StandaloneButton_{config.buttonText}");
        root.transform.SetParent(config.parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one * config.globalScale;

        GameObject button = CreateButton(config);
        button.transform.SetParent(root.transform, false);

        AddIsdkSelectToInvoke(button, config);

        if (config.onCollide != null)
        {
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            TransitionCueTriggerReceiver receiver =
                root.AddComponent<TransitionCueTriggerReceiver>();

            receiver.Initialize(config.onCollide);

            SetupPanelTrigger(button, receiver);
        }

        return root;
    }


    private static Material CreateWhiteMaterial()
    {
        Debug.Log("generating white color for expanded panel");
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

        // Set base color to completely white
        Color whiteColor = new Color(1f, 1f, 1f, 1f);
        mat.SetColor("_BaseColor", whiteColor);

        // Set Surface Type to opaque
        mat.SetFloat("_Surface", 0); // 0 = opaque

        // Moderate smoothness for a clean white surface
        mat.SetFloat("_Smoothness", 0.5f);
        mat.SetFloat("_Metallic", 0f);

        // Configure blending for opaque rendering
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1); // Depth write enabled for opaque objects

        // Set render queue for geometry
        mat.renderQueue = 2000;

        // Set render type
        mat.SetOverrideTag("RenderType", "Opaque");

        return mat;
    }

    private static Material CreateBlueMaterial()
    {
        Debug.Log("generating blue color for expanded panel");
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

        // Convert hex 4C66CC to RGB (0-1 range)
        Color hexColor = new Color(0x4C / 255f, 0x66 / 255f, 0xCC / 255f, 1f);
        mat.SetColor("_BaseColor", hexColor);

        // Set Surface Type to opaque
        mat.SetFloat("_Surface", 0); // 0 = opaque

        // Moderate smoothness for a clean surface
        mat.SetFloat("_Smoothness", 0.5f);
        mat.SetFloat("_Metallic", 0f);

        // Configure blending for opaque rendering
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1); // Depth write enabled for opaque objects

        // Set render queue for geometry
        mat.renderQueue = 2000;

        // Set render type
        mat.SetOverrideTag("RenderType", "Opaque");

        return mat;
    }

    // Creates a frosted glass material with transparency
    // Uses URP/Lit shader with transparency and smoothness for a polished glass effect
    private static Material CreateFrostedGlassMaterial(Color color, float alpha)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

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
                return; // Keep default font
            }
        }
        textComponent.font = fontToUse;
    }

    private static void AddCollisionSupport(
    GameObject root,
    GameObject smallPanel,
    GameObject expandedPanel,
    GameObject button,
    TransitionCueConfig config)
    {
        if (config == null)
        {
            Debug.LogError("AddCollisionSupport: config is NULL");
            return;
        }

        if (config.onCollide == null)
        {
            Debug.Log("AddCollisionSupport: config.onCollide is null -> collision disabled");
            return;
        }

        if (root == null)
        {
            Debug.LogError("AddCollisionSupport: root is NULL");
            return;
        }

        Rigidbody rb = root.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.Log("AddCollisionSupport: adding Rigidbody to root");
            rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        TransitionCueTriggerReceiver receiver = root.AddComponent<TransitionCueTriggerReceiver>();

        if (receiver == null)
        {
            Debug.LogError("AddCollisionSupport: failed to add TransitionCueTriggerReceiver");
            return;
        }

        receiver.Initialize(config.onCollide);

        if (smallPanel != null)
        {
            SetupPanelTrigger(smallPanel, receiver);
        }
        else
        {
            Debug.LogWarning("AddCollisionSupport: smallPanel is NULL");
        }

        if (expandedPanel != null)
        {
            SetupPanelTrigger(expandedPanel, receiver);
        }

        if (button != null)
        {
            SetupPanelTrigger(button, receiver);
        }
        else
        {
            Debug.Log("AddCollisionSupport: button is NULL (expected for minimal cue)");
        }
    }

    private static void SetupPanelTrigger(
    GameObject panel,
    TransitionCueTriggerReceiver receiver)
    {
        if (panel == null)
        {
            Debug.LogError("SetupPanelTrigger: panel is NULL");
            return;
        }

        if (receiver == null)
        {
            Debug.LogError("SetupPanelTrigger: receiver is NULL");
            return;
        }


        BoxCollider col = panel.GetComponent<BoxCollider>();

        if (col == null)
        {
            Debug.Log($"SetupPanelTrigger: adding BoxCollider to {panel.name}");
            col = panel.AddComponent<BoxCollider>();
        }

        col.isTrigger = true;

        TransitionCueTriggerForwarder forwarder =
            panel.AddComponent<TransitionCueTriggerForwarder>();

        if (forwarder == null)
        {
            Debug.LogError($"SetupPanelTrigger: failed to add TransitionCueTriggerForwarder to {panel.name}");
            return;
        }

        forwarder.Initialize(receiver);
    }


}