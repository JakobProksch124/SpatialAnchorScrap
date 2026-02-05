using UnityEngine;

// Handles expansion and collapse of transition cue panels based on user proximity and gaze direction
public class TransitionCueExpander : MonoBehaviour
{
    // === References ===
    private Transform playerTransform;
    private GameObject smallPanel;
    private GameObject expandedPanel;
    private GameObject buttonPanel;
    private TransitionCueConfig config;
    private AudioSource audioSource;

    // === State ===
    private bool isExpanded = false;
    private float currentExpansionT = 0f; // 0 = collapsed, 1 = expanded

    // === Audio Fade State ===
    private float targetVolume = 0f;
    private float currentVolume = 0f;

    // === Initial States ===
    private Vector3 expandedPanelStartScale;
    private Vector3 expandedPanelTargetScale;
    private Vector3 buttonStartPos;
    private Vector3 buttonTargetPos;

    // Initializes the expander with the necessary references and configuration
    public void Initialize(TransitionCueConfig cfg, GameObject smPanel, GameObject expPanel, GameObject btnPanel)
    {
        config = cfg;
        smallPanel = smPanel;
        expandedPanel = expPanel;
        buttonPanel = btnPanel;

        // Get AudioSource reference from parent (root)
        audioSource = GetComponent<AudioSource>();

        // Store initial and target states
        expandedPanelTargetScale = expandedPanel.transform.localScale;
        expandedPanelStartScale = Vector3.zero;
        expandedPanel.transform.localScale = expandedPanelStartScale;

        buttonTargetPos = buttonPanel.transform.localPosition;
        buttonStartPos = buttonTargetPos + Vector3.up * 0.3f; // Start 0.3m above
        buttonPanel.transform.localPosition = buttonStartPos;

        // Initialize audio state
        if (audioSource != null && config.stopSoundWhenExpanded)
        {
            targetVolume = config.ambientVolume;
            currentVolume = config.ambientVolume;
            audioSource.volume = currentVolume;
        }

        // If always expanded, set to expanded state immediately
        if (config.alwaysExpanded)
        {
            isExpanded = true;
            currentExpansionT = 1f;
            expandedPanel.transform.localScale = expandedPanelTargetScale;
            buttonPanel.transform.localPosition = buttonTargetPos;
            smallPanel.SetActive(false); // Hide small panel in expanded state
            expandedPanel.SetActive(true);
            buttonPanel.SetActive(true);

            // Stop audio if configured
            if (audioSource != null && config.stopSoundWhenExpanded)
            {
                targetVolume = 0f;
                currentVolume = 0f;
                audioSource.volume = 0f;
            }
        }
        else
        {
            // Start collapsed
            smallPanel.SetActive(true);
            expandedPanel.SetActive(false);
            buttonPanel.SetActive(false);
        }
    }

    void Start()
    {
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            playerTransform = mainCam.transform;
        } else
        {
            Debug.Log("[TransitionCueExpander] NO MAIN CAMERA FOUND!!!");
        }
    }

    void Update()
    {
        if (config.alwaysExpanded || playerTransform == null)
            return;

        // Check if should be expanded based on proximity and gaze
        bool shouldExpand = ShouldExpand();

        // Update target state
        if (shouldExpand && !isExpanded)
        {
            isExpanded = true;
            smallPanel.SetActive(false); 
            expandedPanel.SetActive(true);
            buttonPanel.SetActive(true);

            // Play expand sound effect
            PlayTransitionSound(config.expandSound, config.transitionSoundVolume);

            // Fade out audio when expanding
            if (audioSource != null && config.stopSoundWhenExpanded)
            {
                targetVolume = 0f;
            }
        }
        else if (!shouldExpand && isExpanded)
        {
            isExpanded = false;

            // Play shrink sound effect
            PlayTransitionSound(config.shrinkSound, config.transitionSoundVolume);

            // Fade in audio when collapsing
            if (audioSource != null && config.stopSoundWhenExpanded)
            {
                targetVolume = config.ambientVolume;
            }
        }

        // Animate toward target state
        float targetT = isExpanded ? 1f : 0f;
        currentExpansionT = Mathf.MoveTowards(currentExpansionT, targetT, Time.deltaTime * config.expansionSpeed);

        // Apply animation
        expandedPanel.transform.localScale = Vector3.Lerp(expandedPanelStartScale, expandedPanelTargetScale, currentExpansionT);
        buttonPanel.transform.localPosition = Vector3.Lerp(buttonStartPos, buttonTargetPos, currentExpansionT);

        // Update audio volume with smooth fade
        if (audioSource != null && config.stopSoundWhenExpanded)
        {
            float fadeSpeed = config.ambientFadeDuration > 0 ? 1f / config.ambientFadeDuration : 10f;
            currentVolume = Mathf.MoveTowards(currentVolume, targetVolume, Time.deltaTime * fadeSpeed);
            audioSource.volume = currentVolume;
        }

        // Deactivate when fully collapsed to improve performance
        if (currentExpansionT <= 0f && !isExpanded)
        {
            smallPanel.SetActive(true); 
            expandedPanel.SetActive(false);
            buttonPanel.SetActive(false);
        }
    }

    // Determines if the panel should expand based on proximity and gaze direction
    private bool ShouldExpand()
    {
        if (playerTransform == null)
            return false;

        // Check proximity
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        if (distance > config.expansionTriggerDistance)
            return false;

        // Check gaze direction
        Vector3 toPanel = (transform.position - playerTransform.position).normalized;
        Vector3 cameraForward = playerTransform.forward;

        float dot = Vector3.Dot(cameraForward, toPanel);

        return dot >= config.gazeThreshold;
    }

    // Gets the current expansion state (0 = collapsed, 1 = expanded)
    public float GetExpansionT()
    {
        return currentExpansionT;
    }

    // Forces the panel to expand or collapse, regardless of proximity/gaze
    public void ForceExpand(bool expand)
    {
        isExpanded = expand;
        if (expand)
        {
            smallPanel.SetActive(false); 
            expandedPanel.SetActive(true);
            buttonPanel.SetActive(true);

            PlayTransitionSound(config.expandSound, config.transitionSoundVolume);
        }
        else
        {
            smallPanel.SetActive(true); 
            expandedPanel.SetActive(false);
            buttonPanel.SetActive(false);

            PlayTransitionSound(config.shrinkSound, config.transitionSoundVolume);
        }
    }

    // Plays a one-shot transition sound effect at the transition cue's position
    private void PlayTransitionSound(AudioClip clip, float volume)
    {
        if (clip == null)
            return;

        // Use this method for spatial one-shot sounds
        AudioSource.PlayClipAtPoint(clip, transform.position, volume);
    }
}
