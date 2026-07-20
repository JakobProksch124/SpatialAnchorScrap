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




    [SerializeField] private Transform startArrivalAnchor;
    [SerializeField] private Transform exitAnchor;
    private GameObject exitCue;
    private GameObject startArrivalCue;

    void Start()
    {
        if (startArrivalAnchor == null)
            Debug.LogError($"Start Arrival Anchor '{startArrivalAnchorName}' not found!");

        if (exitAnchor == null)
            Debug.LogError($"Exit Anchor '{exitAnchorName}' not found!");


        // Start the sequence
        
            //HideAllChildren();
            StartCoroutine(FadeInAll(fadeDuration));
       
    }



    void StartVideo()
    {
        videoPlayer.Play();

    }

    void CreateStartArrivalCue(Transform StartArrivalAnchor)
    {
        CreateExitCue(exitAnchor);
        /* 
            TransitionCueConfig StartArrivalCueConfig = TransitionCueConfig.CreateARConfig(
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

                }
            );


            StartArrivalCueConfig.onCollide = (other) =>
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
            };


            StartArrivalCueConfig.isArrival = true;
            StartArrivalCueConfig.isTransparent = false;
            StartArrivalCueConfig.alwaysExpanded = true;
            StartArrivalCueConfig.primaryColor = startArrivalPrimaryColor;
            StartArrivalCueConfig.expandedDescription = startArrivalDescription;
            StartArrivalCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;
            StartArrivalCueConfig.label = startArrivalLabel;
            StartArrivalCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateCue(StartArrivalCueConfig);
         */
    }

    void SpawnExitCue()
    {
        Debug.Log("spawning Exit Cue");
        CreateExitCue(exitAnchor);
    }

    void CreateExitCue(Transform exitAnchor)
    {
        TransitionCueConfig exitCueConfig = TransitionCueConfig.CreateARConfig(
        parent: exitAnchor,
        onInteract: () =>
        {
        }
    );

        exitCueConfig.onCollide = (other) =>
        {
        };

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
            exitCueConfig.leadsOutOfLecture = true;
            exitCue = TransitionCueFactory.CreateCue(exitCueConfig);
        
    }

    void HideAllChildren()
{
    foreach (Transform child in objectsToSpawn)
    {
        child.gameObject.SetActive(false);
    }
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
}
}