using UnityEngine;
using System;

// Configuration data for creating a transition cue;
// Contains all customizable parameters with defaults
// Comes with 3 default configs: CreateVRConfig, CreateARConfig, CreateRConfig
[Serializable]
public class TransitionCueConfig
{
    // === Basic Settings ===

    // Label shown on the small icon panel (e.g. "VR", "AR", "R")
    public string label = "VR";

    // Primary color for small panel and button
    public Color primaryColor = new Color(0.3f, 0.4f, 0.8f); // Blue

    // Color for the expanded panel (separate from button and small panel)
    public Color expandedPanelColor = new Color(0.7f, 0.7f, 0.7f, 0.7f); // Light gray

    // Parent transform to attach the transition cue to
    public Transform parent;

    // Action to invoke when the button is interacted with
    public Action onInteract;

    // decides if transition leads to AR
    public bool leadsToAR = false;

    // === Expansion Behavior ===

    // If true, the panel starts and stays in expanded state. If false, expands on proximity + gaze
    public bool alwaysExpanded = false;

    // Distance from panel at which expansion can be triggered (in meters)
    public float expansionTriggerDistance = 1.5f;

    // Required dot product of camera forward vs panel direction to trigger expansion (0-1, higher = more precise aim required)
    public float gazeThreshold = 0.7f;

    // Speed of the expand/collapse animation
    public float expansionSpeed = 3f;


    // === Small Panel Design ===

    // Size of the small panel (in meters)
    public float smallPanelSize = 0.3f;

    // Depth/thickness of the small panel (in meters)
    public float smallPanelDepth = 0.02f;

    // Font size for the label text on small panel
    public float labelFontSize = 0.35f;

    // Intensity of the glowing border (0-1)
    public float glowIntensity = 0.8f;

    // Speed of the breathing/pulsing glow animation
    public float glowBreathingSpeed = 2f;


    // === Expanded Panel Design ===

    // Width of the expanded panel (in meters)
    public float expandedPanelWidth = 0.9f;

    // Height of the expanded panel (in meters)
    public float expandedPanelHeight = 0.7f;

    // Depth/thickness of the expanded panel (in meters)
    public float expandedPanelDepth = 0.02f;

    // Description text shown in the expanded panel
    public string expandedDescription = "You can enter VR to take a look inside the driving simulator laboratory.";

    // Font size for the description text
    public float descriptionFontSize = 0.04f;

    // Margins for content within expanded panel (as percentage, 0-1);
    // Controls spacing from panel edges for screenshot/3D object and description text
    public float contentMarginTop = 0f;
    public float contentMarginBottom = 0f;
    public float contentMarginLeft = 0f;
    public float contentMarginRight = 0f;  

    // Optional screenshot texture to display in the panel
    // Note: Ensure that the import is configured as "Texture2D"
    // Note: If you don't want to show a screenshot, set labScreenshot = null
    public Texture2D screenshotTexture = null;

    // Width of the screenshot display (in meters)
    public float screenshotWidth = 0.8f;

    // Height of the screenshot display (in meters)
    public float screenshotHeight = 0.4f;

    // Spacing between screenshot/3D content and description text (in meters)
    public float contentDescriptionSpacing = 0.25f;

    // Optional 3D object prefab to display in the panel
    // Note: As an alternative to screenshotTexture
    public GameObject contentObject = null;

    // If true, uses stencil buffer parallax effect (Magic card window) for 3D content 
    // Only works with contentObject, not with screenshotTexture
    public bool useParallaxEffect = false;

    // Alpha value for frosted glass effect (0-1)
    public float frostedGlassAlpha = 0.5f;

    // Border color for small panel (if null, uses primaryColor)
    public Color? smallPanelBorderColor = null;


    // === Button ===

    // Text shown on the interaction button
    public string buttonText = "Enter VR";

    // Width of the button (meters)
    public float buttonWidth = 0.4f;

    // Height of the button (meters)
    public float buttonHeight = 0.1f;

    // Depth/thickness of the button (meters)
    public float buttonDepth = 0.02f;

    // Vertical offset of button below the expanded panel (meters)
    public float buttonOffset = 0.075f;

    // Font size for button text
    public float buttonFontSize = 0.1f;

    // Duration of button slide-down animation (seconds)
    public float buttonAnimationDuration = 0.5f;

    // Color multiplier for button hover state (e.g., 1.2 for 20% brighter)
    public float buttonHoverBrightness = 1.3f;


    // === Following/Rotation Behavior ===

    // If true, panels rotate to face the user
    public bool enableTurnTowardsUser = true;

    // Maximum rotation angle toward user (degrees)
    public float turnMaxAngle = 12.5f;

    // Distance at which rotation starts (meters) (Set to 0 for always active)
    public float turnTriggerDistance = 6f;

    // Speed of rotation
    public float turnRotationSpeed = 2f;


    // === Global Scale ===

    // Global scale multiplier for the entire transition cue (root GameObject)
    public float globalScale = 1.0f;


    // === Z-Fighting Prevention ===
    // Additional Z-offset for text elements to prevent z-fighting and transparency issues
    public float textZOffset = 0.6f;


    // === Audio ===

    // Audio clip to play as continuous ambient sound from the transition cue (Resources/TransitionCueAmbient.wav)
    // Default sound will be loaded from Resources/TransitionCueAmbient.wav if this is null
    public AudioClip ambientSound = Resources.Load<AudioClip>("TransitionCueAmbient");

    // Volume of the ambient sound (0-1)
    public float ambientVolume = 0.005f;

    // Whether the ambient sound should loop continuously
    public bool ambientLoop = true;

    // Spatial blend (0 = 2D, 1 = 3D spatial audio)
    public float ambientSpatialBlend = 1.0f;

    // Minimum distance before volume attenuation starts (meters)
    public float ambientMinDistance = 3f;

    // Maximum distance at which the sound can be heard (meters)
    public float ambientMaxDistance = 10f;

    // Priority of the audio source (0-256, lower = higher priority)
    public int ambientPriority = 200;

    // Doppler scale (0-5, 0 = no doppler effect) (Set to 0 for stationary ambient sounds)
    public float ambientDopplerLevel = 0f;

    // Duration of fade out/in when transitioning between states (seconds)
    public float ambientFadeDuration = 0.5f;

    // Whether to stop ambient sound when expanded panel is visible
    public bool stopSoundWhenExpanded = true;

    // Sound effect played when panel expands (Resources/TransitionCueExpanded)
    public AudioClip expandSound = Resources.Load<AudioClip>("TransitionCueExpanded");

    // Sound effect played when panel collapses (Resources/TransitionCueShrunk)
    public AudioClip shrinkSound = Resources.Load<AudioClip>("TransitionCueShrunk");

    // Volume for expand/shrink sound effects (0-1)
    public float transitionSoundVolume = 0.035f;


    // === Font ===

    // Custom font for text elements (loaded from Resources/SF-Pro-Display-Regular.otf by default)
    public TMPro.TMP_FontAsset customFont = null;

    // Font for bold text (loaded from Resources/SF-Pro-Display-Bold.otf by default)
    public TMPro.TMP_FontAsset customFontBold = null;

    // Global font size multiplier applied to all text elements
    public float generalFontSizeFactor = 12f;


    // === Factory Methods for Common Presets ===

    // Creates a VR transition cue config (blue color scheme)
    public static TransitionCueConfig CreateVRConfig(Transform parent, Action onInteract)
    {
        return new TransitionCueConfig
        {
            label = "VR",
            primaryColor = new Color(0.3f, 0.4f, 0.8f),
            parent = parent,
            onInteract = onInteract,
            expandedDescription = "Lorem Ipsum",
            buttonText = "Enter VR"
        };
    }

    // Creates an AR transition cue config (orange color scheme)
    public static TransitionCueConfig CreateARConfig(Transform parent, Action onInteract)
    {
        return new TransitionCueConfig
        {
            label = "AR",
            primaryColor = new Color(0.8f, 0.4f, 0f), // Darker orange
            parent = parent,
            onInteract = onInteract,
            expandedDescription = "Lorem Ipsum",
            buttonText = "Enter AR"
        };
    }

    // Creates a Reality (R) transition cue config (red color scheme)
    public static TransitionCueConfig CreateRConfig(Transform parent, Action onInteract)
    {
        return new TransitionCueConfig
        {
            label = "R",
            primaryColor = new Color(0.8f, 0.15f, 0.15f), // Darker red
            parent = parent,
            onInteract = onInteract,
            expandedDescription = "Lorem Ipsum",
            buttonText = "Take off your HMD"
        };
    }
}
