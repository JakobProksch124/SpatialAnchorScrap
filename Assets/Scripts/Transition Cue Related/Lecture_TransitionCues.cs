using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using Oculus.Interaction;

public class Lecture_TransitionCues : MonoBehaviour
{
    [Header("General Infos")] [SerializeField]
    private VideoPlayer videoPlayer;

    [SerializeField] private float startVideoDelay = 3f;
    [SerializeField] private float exitCueDelay = 20f;

    [Header("Root containing Phase1 - Phase6")]
    public Transform objectsToSpawn;

    [Header("Delay between phases")] public float delayBetweenPhases = 2f;

    [Header("Fade Settings")] [SerializeField]
    private float fadeDuration = 1.5f;


    [Header("Start Transition Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField]
    private string startTransitionAnchorName = "startArrivalAnchor";

    [SerializeField] private Color startTransitionPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startTransitionLabel = "VR";
    [SerializeField] private Texture2D startTransitionScreenshotDisplayed;
    [SerializeField] private string startTransitionDescription = "Welcome to the VR lecture!";
    [SerializeField] private string startTransitionButtonText = "Start Video";
    [SerializeField] private bool startTransitionAlwaysExpand = false;


    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField]
    private string startArrivalAnchorName = "startArrivalAnchor";

    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "VR";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Welcome to the VR lecture!";
    [SerializeField] private string startArrivalButtonText = "Start Video";
    [SerializeField] private bool startArrivalAlwaysExpand = false;
    [SerializeField] private string startArrivalCuePath;

    [Tooltip("only used, when start arrival cue is set to blunt")] [SerializeField]
    private float videoStartDelay = 3f;

    [Header("VRExit Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField]
    private string exitAnchorName = "exitAnchor";

    [SerializeField] private Color exitPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitLabel = "Reality";
    [SerializeField] private Texture2D exitScreenshotDisplayed;
    [SerializeField] private string exitDescription = "Your friends are waiting!";
    [SerializeField] private string exitButtonText = "Stop Video";
    [SerializeField] private bool exitAlwaysExpand = false;
    [SerializeField]  private string exitCuePath;

    [Header("VRExit Trransition Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField]
    private string exitTransitionAnchorName = "exitAnchor";

    [SerializeField] private Color exitTransitionPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitTransitionLabel = "Reality";
    [SerializeField] private Texture2D exitTransitionScreenshotDisplayed;
    [SerializeField] private string exitTransitionDescription = "Your friends are waiting!";
    [SerializeField] private string exitTransitionButtonText = "Stop Video";
    [SerializeField] private bool exitTransitionAlwaysExpand = false;

    [Header("Leave HMD Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField]
    private string leaveHMDAnchorName = "leaveHMDAnchor";

    [SerializeField] private Color leaveHMDPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string leaveHMDLabel = "R";
    [SerializeField] private Texture2D leaveHMDScreenshotDisplayed;
    [SerializeField] private string leaveHMDDescription = "Take off the headmounted display";
    [SerializeField] private string leaveHMDButtonText = "";
    [SerializeField] private bool leaveHMDAlwaysExpand = false;
    [SerializeField] private VideoClip leaveHMDvideoClip;

    private GameObject leaveHMDCue;


    [SerializeField] private Transform startArrivalAnchor;
    [SerializeField] private GameObject videoPlane;
    private GameObject exitCue;
    private GameObject exitTransitionCue;
    private GameObject startArrivalCue;
    private GameObject startTransitionCue;

    void Start()
    {
        // The interaction rig ships with locomotion (teleport "jump"). The lecture is a seated
        // experience — no movement wanted — so switch the whole Locomotor off, including the
        // teleport interactors (A would otherwise blink-jump the user around the hall).
        var locomotor = GameObject.Find("Locomotor");
        if (locomotor != null) locomotor.SetActive(false);
        foreach (var ti in FindObjectsByType<Oculus.Interaction.Locomotion.TeleportInteractor>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            ti.gameObject.SetActive(false);

        HideAllChildren();

        // Start the sequence
        StartCoroutine(FadeInAll(fadeDuration));
    }

    // The TransitionCue prefab's Canvas buttons (EnterVR, NotNow, ...) ship with a
    // RayInteractable + BoxCollider, but the BoxCollider is left at Unity's default
    // 1x1x1 size while the Canvas is scaled down (~0.0005), so the actual hittable
    // volume is a sub-millimeter speck compared to the visible button. They also have
    // no hover/press feedback, since UIButtonHoverEffect only supports mesh Renderers,
    // not CanvasRenderer/Image. This fixes both so the ray interaction is visible and
    // pressable.
    private static void FixUpCanvasRayButtons(GameObject cueRoot)
    {
        Canvas.ForceUpdateCanvases();

        foreach (var ray in cueRoot.GetComponentsInChildren<RayInteractable>(true))
        {
            var rect = ray.GetComponent<RectTransform>();
            var collider = ray.GetComponent<BoxCollider>();
            if (rect == null || collider == null) continue;

            Vector2 size = rect.rect.size;
            if (size.x <= 0f || size.y <= 0f) continue; // not laid out - leave as authored

            collider.size = new Vector3(size.x, size.y, collider.size.z);
            collider.center = new Vector3(rect.rect.center.x, rect.rect.center.y, collider.center.z);
        }
    }

    void StartVideo()
    {
        videoPlayer.Play();
    }


    void CreateStartArrivalCue(Transform StartArrivalAnchor)
    {
        var prefab = Resources.Load<GameObject>(startArrivalCuePath);
        if (prefab == null)
        {
            Debug.Log($"[Building_TransitionCues] Could not find prefab for {StartArrivalAnchor.name}");
            return;
        }

        startArrivalCue = Instantiate(prefab, StartArrivalAnchor);
        FixUpCanvasRayButtons(startArrivalCue);

        startArrivalCue.GetComponent<CueEvents>().onCloseCue.AddListener(() => {
                startArrivalCue.SetActive(false); // Start Video
                if (videoPlayer != null)
                    videoPlayer.Play();
                else
                    Debug.LogWarning("VideoPlayer reference missing!");

                // Create Exit Cue after delay
                Debug.Log("invoked spawn exit cue");
                Invoke(nameof(SpawnExitCue), exitCueDelay); }
        );
    }


    void SpawnExitCue()
    {
        Debug.Log("spawning Exit Cue");
        CreateExitCue(startArrivalAnchor);
        
        // Pause Video
        if (videoPlayer != null)
            videoPlayer.Pause();
    }

    void CreateExitCue(Transform startArrivalAnchor)
    {
        var prefab = Resources.Load<GameObject>(exitCuePath);
        if (prefab == null)
        {
            Debug.Log($"[Building_TransitionCues] Could not find prefab for {startArrivalAnchor.name}");
            return;
        }

        exitCue = Instantiate(prefab, startArrivalAnchor);
        FixUpCanvasRayButtons(exitCue);

        exitCue.GetComponent<CueEvents>().onStartTransition.AddListener(() => {
                exitCue.SetActive(false);
                StartCoroutine(FadeOutAll(fadeDuration)); }
        );
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

        if (videoPlane != null)
        {
            videoPlane.SetActive(true);
        }
        else
        {
            Debug.Log("video plane does not exist");
        }

        CreateStartArrivalCue(startArrivalAnchor);
    }

    public IEnumerator FadeOutAll(float duration)
    {
        if (videoPlane != null)
        {
            videoPlane.SetActive(false);
        }
        else
        {
            Debug.Log("video plane does not exist");
        }

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

        //objectsToSpawn.gameObject.SetActive(false);
    }
}