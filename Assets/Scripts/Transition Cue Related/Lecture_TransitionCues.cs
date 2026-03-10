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
    [SerializeField] private bool startArrivalIsBland = false;
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
    [SerializeField] private bool exitIsBland = false;
    [SerializeField] InputActionReference switchIsBlandButton;


    private bool _switchIsBlandButtonWasPressed = false;


    [SerializeField] private Transform startArrivalAnchor;
    [SerializeField] private Transform exitAnchor;
    private GameObject exitCue;
    private GameObject startArrivalCue;

    void Awake()
    {
        LoadBlandState();
    }

    void Start()
    {
        if (startArrivalAnchor == null)
            Debug.LogError($"Start Arrival Anchor '{startArrivalAnchorName}' not found!");

        if (exitAnchor == null)
            Debug.LogError($"Exit Anchor '{exitAnchorName}' not found!");


        // Start the sequence
        if(!startArrivalIsBland)
        {
            HideAllChildren();
            StartCoroutine(SpawnPhases());
        }
        else
        {
            CreateStartArrivalCue(startArrivalAnchor);
        }
    }

    void Update()
    {
        CheckSwitchIsBland();
    }
        

    void StartVideo()
    {
        videoPlayer.Play();

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
                    Debug.Log("invoked spawn exit cue");
                    Invoke(nameof(SpawnExitCue), exitCueDelay);

                }
            );
            StartArrivalCueConfig.isArrival = true;
            StartArrivalCueConfig.isTransparent = false;
            StartArrivalCueConfig.alwaysExpanded = true;
            StartArrivalCueConfig.primaryColor = startArrivalPrimaryColor;
            StartArrivalCueConfig.expandedDescription = startArrivalDescription;
            StartArrivalCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;
            StartArrivalCueConfig.label = startArrivalLabel;
            StartArrivalCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateCue(StartArrivalCueConfig);
        }
        else
        {
            Invoke(nameof(StartVideo), startVideoDelay);
            Debug.Log("invoked spawn exit cue");
            Invoke(nameof(SpawnExitCue), exitCueDelay);
            //   Invoke(nameof(videoPlayer.Play), videoStartDelay);
        }
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
            exitCueConfig.leadsToAR = true;
            exitCueConfig.primaryColor = exitPrimaryColor;
            exitCueConfig.expandedDescription = exitDescription;
            exitCueConfig.screenshotTexture = exitScreenshotDisplayed;
            exitCueConfig.label = exitLabel;
            exitCueConfig.buttonText = exitButtonText;
            exitCueConfig.leadsOutOfLecture = true;
            exitCue = TransitionCueFactory.CreateCue(exitCueConfig);
        }
    }

    void HideAllChildren()
    {
        foreach (Transform phase in objectsToSpawn)
        {
            phase.gameObject.SetActive(false);
        }
    }

    IEnumerator SpawnPhases()
    {
            Transform phase1 = objectsToSpawn.GetChild(0);

            // Prepare alpha BEFORE enabling
            SetPhaseAlpha(phase1, 0f);
        if(phase1 != null)
        {

            phase1.gameObject.SetActive(true);

            yield return StartCoroutine(FadeInPhase(phase1));
        }
        Transform phase2 = objectsToSpawn.GetChild(1);

        // Prepare alpha BEFORE enabling
        SetPhaseAlpha(phase2, 0f);
        if (phase2 != null)
        {

            phase2.gameObject.SetActive(true);

            yield return StartCoroutine(FadeInPhase(phase2));
        }

        Transform phase3 = objectsToSpawn.GetChild(2);
        phase3.gameObject.SetActive(true);
        if(phase3 != null)
        {

        foreach (Transform child in phase3)
        {
            child.gameObject.SetActive(true);
            }
        }
        CreateStartArrivalCue(startArrivalAnchor);
    }

    void SetPhaseAlpha(Transform phase, float alpha)
    {
        Renderer[] renderers = phase.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }
        }
    }


    IEnumerator FadeInPhase(Transform phase)
    {
        Renderer[] renderers = phase.GetComponentsInChildren<Renderer>(true);

        float time = 0f;

        // Set all materials to transparent and alpha = 0
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = 0f;
                    mat.color = c;
                }
            }
        }

        while (time < fadeDuration)
        {
            float alpha = time / fadeDuration;

            foreach (Renderer r in renderers)
            {
                foreach (Material mat in r.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;
                    }
                }
            }

            time += Time.deltaTime;
            yield return null;
        }

        // Ensure fully visible
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = 1f;
                    mat.color = c;
                }
            }
        }
    }

    void CheckSwitchIsBland()
    {
        bool isPressed = switchIsBlandButton.action.IsPressed();
        if (_switchIsBlandButtonWasPressed && !isPressed)
        {
            SwitchIsBland();
        }
        _switchIsBlandButtonWasPressed = isPressed;

    }
    void SwitchIsBland()
    {
        exitIsBland = !exitIsBland;
        startArrivalIsBland = !startArrivalIsBland;
        SaveBlandState();
    }

    void SaveBlandState()
    {
        PlayerPrefs.SetInt("exitIsBland", exitIsBland ? 1 : 0);
        PlayerPrefs.SetInt("startArrivalIsBland", startArrivalIsBland ? 1 : 0);

        PlayerPrefs.Save();
    }

    void LoadBlandState()
    {
        exitIsBland = PlayerPrefs.GetInt("exitIsBland", 0) == 1;
        startArrivalIsBland = PlayerPrefs.GetInt("startArrivalIsBland", 0) == 1;
    }




}
