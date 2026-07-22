using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;

public class Lecture_TransitionCues : MonoBehaviour
{


    [Header("General Infos")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float startVideoDelay = 3f;
    [SerializeField] private float exitCueDelay = 20f;

    [Header("Root containing Phase1 - Phase6")]
    public Transform objectsToSpawn;

    [Header("Delay between phases")]
    public float delayBetweenPhases = 2f;
    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1.5f;


    [Header("Start Transition Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string startTransitionAnchorName = "startArrivalAnchor";
    [SerializeField] private Color startTransitionPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startTransitionLabel = "VR";
    [SerializeField] private Texture2D startTransitionScreenshotDisplayed;
    [SerializeField] private string startTransitionDescription = "Welcome to the VR lecture!";
    [SerializeField] private string startTransitionButtonText = "Start Video";
    [SerializeField] private bool startTransitionAlwaysExpand = false;


    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string startArrivalAnchorName = "startArrivalAnchor";
    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "VR";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Welcome to the VR lecture!";
    [SerializeField] private string startArrivalButtonText = "Start Video";
    [SerializeField] private bool startArrivalAlwaysExpand = false;
    [Tooltip("only used, when start arrival cue is set to blunt")]
    [SerializeField] private float videoStartDelay = 3f;

    [Header("VRExit Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string exitAnchorName = "exitAnchor";
    [SerializeField] private Color exitPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitLabel = "Reality";
    [SerializeField] private Texture2D exitScreenshotDisplayed;
    [SerializeField] private string exitDescription = "Your friends are waiting!";
    [SerializeField] private string exitButtonText = "Stop Video";
    [SerializeField] private bool exitAlwaysExpand = false;

    [Header("VRExit Trransition Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string exitTransitionAnchorName = "exitAnchor";
    [SerializeField] private Color exitTransitionPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitTransitionLabel = "Reality";
    [SerializeField] private Texture2D exitTransitionScreenshotDisplayed;
    [SerializeField] private string exitTransitionDescription = "Your friends are waiting!";
    [SerializeField] private string exitTransitionButtonText = "Stop Video";
    [SerializeField] private bool exitTransitionAlwaysExpand = false;

    [Header("Leave HMD Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string leaveHMDAnchorName = "leaveHMDAnchor";
    [SerializeField] private Color leaveHMDPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string leaveHMDLabel = "R";
    [SerializeField] private Texture2D leaveHMDScreenshotDisplayed;
    [SerializeField] private string leaveHMDDescription = "Take off the headmounted display";
    [SerializeField] private string leaveHMDButtonText = "";
    [SerializeField] private bool leaveHMDAlwaysExpand = false;
    [SerializeField] private VideoClip leaveHMDvideoClip;

    private GameObject leaveHMDCue;


    [SerializeField] private Transform startArrivalAnchor;
    private GameObject exitCue;
    private GameObject exitTransitionCue;
    private GameObject startArrivalCue;
    private GameObject startTransitionCue;

    void Start()
    {
        HideAllChildren();
        
        // Start the sequence
        CreateStartTransitionCue(startArrivalAnchor);
        //StartCoroutine(FadeInAll(fadeDuration));
    }


    void CreateStartTransitionCue(Transform StartArrivalAnchor)
    {
        TransitionCueConfig StartTransitionCueConfig = TransitionCueConfig.CreateARConfig(
            parent: StartArrivalAnchor,
            onInteract: () =>
            {
                startTransitionCue.SetActive(false);
                StartCoroutine(FadeInAll(fadeDuration));
            },
            onClose: () =>
            {
            },
            isStandardClose: true
        );

            StartTransitionCueConfig.isArrival = false;
            StartTransitionCueConfig.isTransparent = false;
            StartTransitionCueConfig.alwaysExpanded = true;
            StartTransitionCueConfig.primaryColor = startTransitionPrimaryColor;
            StartTransitionCueConfig.expandedDescription = startTransitionDescription;
            StartTransitionCueConfig.screenshotTexture = startTransitionScreenshotDisplayed;
            StartTransitionCueConfig.label = startTransitionLabel;
            StartTransitionCueConfig.buttonText = startTransitionButtonText;

            startTransitionCue = TransitionCueFactory.CreateCue(StartTransitionCueConfig);
    }


    void StartVideo()
    {
        videoPlayer.Play();
    }



    void CreateStartArrivalCue(Transform StartArrivalAnchor)
    {
        //CreateExitCue(exitAnchor);
        
            TransitionCueConfig StartTransitionCueConfig = TransitionCueConfig.CreateARConfig(
                parent: StartArrivalAnchor,
                onInteract: () =>
                {
                    startArrivalCue.SetActive(false);// Start Video
                    if (videoPlayer != null)
                    {
                        videoPlayer.Play();
                    }
                    else
                    {
                        Debug.LogWarning("VideoPlayer reference missing!");
                    }

                    // Create Exit Cue after delay
                    Debug.Log("invoked spawn exit cue");
                    Invoke(nameof(SpawnExitCue), exitCueDelay);

                },
            onClose: () =>
            {
            },
            isStandardClose: true
            );

            StartTransitionCueConfig.isArrival = true;
            StartTransitionCueConfig.isTransparent = false;
            StartTransitionCueConfig.alwaysExpanded = true;
            StartTransitionCueConfig.primaryColor = startArrivalPrimaryColor;
            StartTransitionCueConfig.expandedDescription = startArrivalDescription;
            StartTransitionCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;
            StartTransitionCueConfig.label = startArrivalLabel;
            StartTransitionCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateCue(StartTransitionCueConfig);
         
    }

    

    void SpawnExitCue()
    {
        Debug.Log("spawning Exit Cue");
        CreateExitCue(startArrivalAnchor);
    }
  
    void CreateExitCue(Transform startArrivalAnchor)
    {
        TransitionCueConfig exitCueConfig = TransitionCueConfig.CreateARConfig(
        parent: startArrivalAnchor,
        onInteract: () =>
        {
                StartCoroutine(FadeOutAll(fadeDuration));
        },
            onClose: () =>
            {
                exitCue.SetActive(false);
                videoPlayer.Play();
                // Create Exit Cue again after delay
                Debug.Log("invoked spawn exit cue");
                Invoke(nameof(SpawnExitCue), exitCueDelay);
            },
            isStandardClose: false
    );

            // Pause Video
            if (videoPlayer != null)
            {
                videoPlayer.Pause();
            }
            exitCueConfig.alwaysExpanded = exitAlwaysExpand;
            exitCueConfig.primaryColor = exitPrimaryColor;
            exitCueConfig.expandedDescription = exitDescription;
            exitCueConfig.screenshotTexture = exitScreenshotDisplayed;
            exitCueConfig.label = exitLabel;
            exitCueConfig.buttonText = exitButtonText;
            exitCueConfig.leadsToAR = true;

            exitCue = TransitionCueFactory.CreateCue(exitCueConfig);
        
    }

    
    // CUE INFO:
    // This cue is placed at the doors of any vr room and allows the player to exit the vr room and return to the ar-supported world
    void CreateExitTransitionCue(Transform startArrivalAnchor)
    {
        // Base (Same basic configuration for enhanced as well as minimal cues
        TransitionCueConfig exitTransitionCueConfig = TransitionCueConfig.CreateARConfig(
            parent: startArrivalAnchor,
            onInteract: () =>
            {CreateLeaveHMDCue(startArrivalAnchor);
            },
            onClose: () =>
            {
                exitTransitionCue.SetActive(false);
                StartCoroutine(FadeInAll(fadeDuration));
            },
            isStandardClose: false
        );
        exitTransitionCueConfig.alwaysExpanded = exitTransitionAlwaysExpand;
        exitTransitionCueConfig.primaryColor = exitTransitionPrimaryColor;
        exitTransitionCueConfig.expandedDescription = exitTransitionDescription;
        exitTransitionCueConfig.screenshotTexture = exitTransitionScreenshotDisplayed;

        exitTransitionCueConfig.leadsOutOfLecture = true;
        // (Effectively not used if alwaysExpanded)
        exitTransitionCueConfig.label = exitTransitionLabel;
        exitTransitionCueConfig.buttonText = exitTransitionButtonText;
        exitTransitionCue = TransitionCueFactory.CreateCue(exitTransitionCueConfig);
    }

    // CUE INFO:
    // This cue is placed at the doors of any vr room and allows the player to exit the vr room and return to the ar-supported world
    void CreateLeaveHMDCue(Transform startArrivalAnchor)
    {
        // Base (Same basic configuration for enhanced as well as minimal cues
        TransitionCueConfig leaveHMDCueConfig = TransitionCueConfig.CreateARConfig(
            parent: startArrivalAnchor,
            onInteract: () =>
            {
            },
            onClose: () =>
            {
            },
            isStandardClose: true
        );


        // Details for enhanced cues
        leaveHMDCueConfig.alwaysExpanded = leaveHMDAlwaysExpand;
        leaveHMDCueConfig.primaryColor = leaveHMDPrimaryColor;
        leaveHMDCueConfig.expandedDescription = leaveHMDDescription;
        leaveHMDCueConfig.screenshotTexture = leaveHMDScreenshotDisplayed;
        leaveHMDCueConfig.videoClip = leaveHMDvideoClip;

        leaveHMDCueConfig.isLeaveCue = true;
        // (Effectively not used if alwaysExpanded)
        leaveHMDCueConfig.label = leaveHMDLabel;
        leaveHMDCueConfig.buttonText = leaveHMDButtonText;
        leaveHMDCue = TransitionCueFactory.CreateCue(leaveHMDCueConfig);
    }

    void HideAllChildren()
    {
        Renderer[] renderers = objectsToSpawn.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetFloat("_Fade", 1f); // Fully invisible
            r.SetPropertyBlock(block);
        }

        /*foreach (Transform child in objectsToSpawn)
        {
            child.gameObject.SetActive(false);
        }*/
    }

    public IEnumerator FadeInAll(float duration)
{
    Renderer[] renderers = objectsToSpawn.GetComponentsInChildren<Renderer>(true);
    MaterialPropertyBlock block = new MaterialPropertyBlock();

    // Start fully invisible
    foreach (Renderer r in renderers)
    {
        r.GetPropertyBlock(block);
        block.SetFloat("_Fade", 1f);
        r.SetPropertyBlock(block);
    }

    objectsToSpawn.gameObject.SetActive(true);

    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01(elapsed / duration);

        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetFloat("_Fade", fade);
            r.SetPropertyBlock(block);
        }

        yield return null;
    }

    // Ensure completely visible
    foreach (Renderer r in renderers)
    {
        r.GetPropertyBlock(block);
        block.SetFloat("_Fade", 0f);
        r.SetPropertyBlock(block);
    }
    CreateStartArrivalCue(startArrivalAnchor);
}

public IEnumerator FadeOutAll(float duration)
{
    Renderer[] renderers = objectsToSpawn.GetComponentsInChildren<Renderer>(true);
    MaterialPropertyBlock block = new MaterialPropertyBlock();

    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float fade = Mathf.Clamp01(elapsed / duration);

        foreach (Renderer r in renderers)
        {
            r.GetPropertyBlock(block);
            block.SetFloat("_Fade", fade);
            r.SetPropertyBlock(block);
        }

        yield return null;
    }

    // Ensure completely invisible
    foreach (Renderer r in renderers)
    {
        r.GetPropertyBlock(block);
        block.SetFloat("_Fade", 1f);
        r.SetPropertyBlock(block);
    }

    objectsToSpawn.gameObject.SetActive(false);
    CreateLeaveHMDCue(startArrivalAnchor);
}
}