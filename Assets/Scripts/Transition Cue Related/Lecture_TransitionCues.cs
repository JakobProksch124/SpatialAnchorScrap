using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class Lecture_TransitionCues : MonoBehaviour
{


    [Header("General Infos")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private float exitCueDelay = 30f;

    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string startArrivalAnchorName = "startArrivalAnchor";
    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "VR";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Welcome to the VR lecture!";
    [SerializeField] private string startArrivalButtonText = "Start Video";
    [SerializeField] private bool startArrivalAlwaysExpand = false;
    [SerializeField] private bool startArrivalIsBland = false;

    [Header("VRExit Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string exitAnchorName = "exitAnchor";
    [SerializeField] private Color exitPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitLabel = "Reality";
    [SerializeField] private Texture2D exitScreenshotDisplayed;
    [SerializeField] private string exitDescription = "Your friends are waiting!";
    [SerializeField] private string exitButtonText = "Stop Video";
    [SerializeField] private bool exitAlwaysExpand = false;
    [SerializeField] private bool exitIsBland = false;


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
        CreateStartArrivalCue(startArrivalAnchor);
    }

    void CreateStartArrivalCue(Transform StartArrivalAnchor)
    {

        if (!startArrivalIsBland)
        {
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
                    Invoke(nameof(SpawnExitCue), exitCueDelay);

                }
            );
            StartArrivalCueConfig.alwaysExpanded = startArrivalAlwaysExpand;
            StartArrivalCueConfig.primaryColor = startArrivalPrimaryColor;
            StartArrivalCueConfig.expandedDescription = startArrivalDescription;
            StartArrivalCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;
            StartArrivalCueConfig.label = startArrivalLabel;
            StartArrivalCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateFrostedTransitionCue(StartArrivalCueConfig);
        }
    }
    void SpawnExitCue()
    {
        CreateExitCue(exitAnchor);
    }

    void CreateExitCue(Transform exitAnchor)
    {
        TransitionCueConfig exitCueConfig = TransitionCueConfig.CreateARConfig(
        parent: exitAnchor,
        onInteract: () =>
        {
            exitCue.SetActive(false);
            // Pause Video
            if (videoPlayer != null)
            {
                videoPlayer.Pause();
            }
        }
    );

        if (!exitIsBland)
        {
            exitCueConfig.alwaysExpanded = exitAlwaysExpand;
            exitCueConfig.primaryColor = exitPrimaryColor;
            exitCueConfig.expandedDescription = exitDescription;
            exitCueConfig.screenshotTexture = exitScreenshotDisplayed;
        }
        else
        {
            exitCueConfig.alwaysExpanded = true;
            exitCueConfig.primaryColor = Color.black;
            exitCueConfig.expandedDescription = exitLabel;

        }
        exitCueConfig.label = exitLabel;
        exitCueConfig.buttonText = exitButtonText;
        exitCue = TransitionCueFactory.CreateFrostedTransitionCue(exitCueConfig);
    }
}
