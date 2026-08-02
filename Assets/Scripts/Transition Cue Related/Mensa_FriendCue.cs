using System.Diagnostics;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Place script directly on the Building Prefab Root
public class Mensa_FriendCue : MonoBehaviour
{
    [Tooltip("Destination shown in the navigation notification after returning to AR")]
    [SerializeField] private string navigationDestination = "Next Location";



    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string startArrivalAnchorName = "startArrivalAnchor";
    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "VR";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Welcome to the VR lecture!";
    [SerializeField] private string startArrivalButtonText = "Start Video";
    [SerializeField] private bool startArrivalAlwaysExpand = false;
    [SerializeField] private string startArrivalCuePath;

    [SerializeField] private string entryAnchorName = "entryAnchor";
    [SerializeField] private Color entryPrimaryColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private string entryLabel = "AR";
    [Tooltip("Optional: The image shown inside the transition cue")]
    [SerializeField] private Texture2D entryScreenshotDisplayed;
    [SerializeField] private string entryDescription = "Start navigation to friends";
    [SerializeField] private string entryButtonText = "Start navigation";
    [SerializeField] private bool entryAlwaysExpand = true;
    [SerializeField] private string entryCuePath;

    [SerializeField] private string entryArrivalAnchorName = "entryArrivalAnchor";
    [SerializeField] private Color entryArrivalPrimaryColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private string entryArrivalLabel = "AR";
    [Tooltip("Optional: The image shown inside the transition cue")]
    [SerializeField] private Texture2D entryArrivalScreenshotDisplayed;
    [SerializeField] private string entryArrivalDescription = "Started navigation";
    [SerializeField] private string entryArrivalButtonText = "X";
    [SerializeField] private bool entryArrivalAlwaysExpand = true;
    [SerializeField] private string entryArrivalCuePath;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    private Positioner positioner;


    // Internal references
    private Transform entryAnchor;
    private Transform entryArrivalAnchor;
    private Transform startArrivalAnchor;
    private GameObject vrRoom;
    private GameObject entryCue;
    private GameObject entryArrivalCue;
    private GameObject startArrivalCue;
    private Camera mainCamera;
    private PathGenerator pathGenerator;
    private LineRenderer[] pathLineRenderers;
    private Scene loadedVRScene;
    private ArrivalCue arrivalCue;
    private bool userInVRRoom = false;

    public GameObject FoodA;
    public GameObject FoodB;
    public GameObject FoodC;
    public GameObject FoodButtonCanvas;

    [SerializeField] private string foodButtonAnchor1Name = "foodButtonAnchor1";
    [SerializeField] private string foodButtonAnchor2Name = "foodButtonAnchor2";
    [SerializeField] private string foodButtonAnchor3Name = "foodButtonAnchor3";
    [SerializeField] private Color foodButtonColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string foodButton1Text = "Pick pancakes";
    [SerializeField] private string foodButton2Text = "Pick sandwich";
    [SerializeField] private string foodButton3Text = "Pick omelet";
    [SerializeField] private bool foodButtonsTurnToUser = false;
    private Transform foodButtonAnchor1;
    private Transform foodButtonAnchor2;
    private Transform foodButtonAnchor3;
    private GameObject foodButton1;
    private GameObject foodButton2;
    private GameObject foodButton3;


    void Start()
    {
        mainCamera = Camera.main;
        positioner = FindAnyObjectByType<Positioner>();

        // Find PathGenerator component
        foreach (var component in GetComponents<PathGenerator>())
        {
            if (component.GetType().Name == "PathGenerator")
            {
                pathGenerator = component;
                break;
            }
        }

        DisablePathGenerator();

        // Find start arrival anchor point in this building
        startArrivalAnchor = transform.Find(startArrivalAnchorName);
        if (startArrivalAnchor == null)
        {
            UnityEngine.Debug.LogWarning($"[Building_TransitionCues] Anchor '{startArrivalAnchorName}' not found. Using this transform.");
            startArrivalAnchor = transform;
        }
        else
        {
            UnityEngine.Debug.Log("start arrival anchor set!");
        }

        // Find anchor point in this building
        entryAnchor = transform.Find(entryAnchorName);
        if (entryAnchor == null)
        {
            UnityEngine.Debug.LogWarning($"[Building_TransitionCues] Anchor '{entryAnchorName}' not found. Using this transform.");
            entryAnchor = transform;
        }
        else
        {
            UnityEngine.Debug.Log("entry anchor set!");
        }

        // Find arrival anchor point in this building
        entryArrivalAnchor = transform.Find(entryArrivalAnchorName);
        if (entryArrivalAnchor == null)
        {
            UnityEngine.Debug.LogWarning($"[Building_TransitionCues] Anchor '{entryArrivalAnchorName}' not found. Using this transform.");
            entryArrivalAnchor = transform;
        }
        else
        {
            UnityEngine.Debug.Log("entry arrival anchor set!");
        }

        // T9 (Smartphone -> AR): the entry cue happened on the phone, so AR opens with the
        // ARRIVAL cue and the navigation to Peter is already running. There is no
        // "Navigation starten" cue any more (that transition was dropped with the food
        // selection); the next cue is T10 at Peter's table.
        CreateStartArrivalCue(startArrivalAnchor);
        StartNavigationToFriends();
    }

    void ShowFood()
    {

        // Create styled food buttons at anchor positions
        foodButtonAnchor1 = transform.Find(foodButtonAnchor1Name);
        foodButtonAnchor2 = transform.Find(foodButtonAnchor2Name);
        foodButtonAnchor3 = transform.Find(foodButtonAnchor3Name);

        if (foodButtonAnchor1 != null)
            foodButton1 = CreateFoodButton(foodButtonAnchor1, foodButton1Text);
        if (foodButtonAnchor2 != null)
            foodButton2 = CreateFoodButton(foodButtonAnchor2, foodButton2Text);
        if (foodButtonAnchor3 != null)
            foodButton3 = CreateFoodButton(foodButtonAnchor3, foodButton3Text);


        if (FoodA != null)
            FoodA.SetActive(true);

        if (FoodB != null)
            FoodB.SetActive(true);

        if (FoodC != null)
            FoodC.SetActive(true);

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
        
        startArrivalCue.GetComponent<CueEvents>().onCloseCue.AddListener(() =>
            startArrivalCue.SetActive(false)); // navigation already runs; T10 waits at Peter's table
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
    
    void Update()
    {
        if (!enableKeyboardShortcuts) return;

        // Keyboard shortcuts for testing (New Input System)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.tKey.wasPressedThisFrame)
            {
                UnityEngine.Debug.Log($"[Building_TransitionCues] T or P key pressed on {gameObject.name}");

                if (!userInVRRoom)
                {
                    StartNavigationToFriends();
                    userInVRRoom = !userInVRRoom;
                }
                else
                {
                    DisablePathGenerator();
                    userInVRRoom = !userInVRRoom;
                }
            }
        }
    }

    public void showEntryCue()
    {
        //UINotificationSystem.Instance.ShowPersistentMessage("Your food was ordered.", false);

        UnityEngine.Debug.Log("Food Button Pressed!");
        if (startArrivalCue != null)
        {
            startArrivalCue.SetActive(false);
        }

        if (FoodA != null)
            FoodA.SetActive(false);

        if (FoodB != null)
            FoodB.SetActive(false);

        if (FoodC != null)
            FoodC.SetActive(false);

        /*if (FoodButtonCanvas != null)
            FoodButtonCanvas.SetActive(false);*/
        if (foodButton1 != null)
        {
            Destroy(foodButton1);
        }
        if (foodButton2 != null)
        {
            Destroy(foodButton2);
        }
        if (foodButton3 != null)
        {
            Destroy(foodButton3);
        }

        CreateEntryCue(entryAnchor);
        //Spawn arrival cue (Premise: ArrivalCue component is present on this GameObject)
        arrivalCue = GetComponent<ArrivalCue>();
        if (arrivalCue != null)
        {
            arrivalCue.SpawnArrivalCue();
            UnityEngine.Debug.Log("Spawned LeaveHMD cue!");
        }
    }

    // Helper function for creating a button below the mensa meal
    private GameObject CreateFoodButton(Transform anchor, string text)
    {
        TransitionCueConfig btnConfig = new TransitionCueConfig
        {
            parent = anchor,
            primaryColor = foodButtonColor,
            buttonText = text,
            onInteract = () => showEntryCue(),
            enableTurnTowardsUser = foodButtonsTurnToUser
        };

        return TransitionCueFactory.CreateStandaloneButton(btnConfig);
    }

    void CreateEntryCue(Transform entryAnchor)
    {
        var prefab = Resources.Load<GameObject>(entryCuePath);
        if (prefab == null)
        {
            Debug.Log($"[Building_TransitionCues] Could not find prefab for {entryAnchor.name}");
            return;
        }

        entryCue = Instantiate(prefab, entryAnchor);
        FixUpCanvasRayButtons(entryCue);

        entryCue.GetComponent<CueEvents>().onStartTransition.AddListener(StartNavigationToFriends);
    }

    void CreateEntryArrivalCue(Transform entryArrivalAnchor)
    {
        var prefab = Resources.Load<GameObject>(entryArrivalCuePath);
        if (prefab == null)
        {
            Debug.Log($"[Building_TransitionCues] Could not find prefab for {entryArrivalAnchor.name}");
            return;
        }

        entryArrivalCue = Instantiate(prefab, entryArrivalAnchor);
        FixUpCanvasRayButtons(entryArrivalCue);
    }

    public void StartNavigationToFriends()
    {
        UINotificationSystem.Instance.HidePersistentMessage();
        if (entryCue != null)
            entryCue.SetActive(false);

        EnablePathGenerator();

        // goal marker at Peter's table; reaching it shows T10 ("Brille absetzen")
        arrivalCue = GetComponent<ArrivalCue>();
        if (arrivalCue != null) arrivalCue.SpawnArrivalCue();
    }

    void DisablePathGenerator()
    {
        if (pathGenerator != null)
        {
            pathGenerator.enabled = false;
            pathGenerator._pathing = false;
            pathGenerator.ClearArrows();

            pathLineRenderers = pathGenerator.GetComponentsInChildren<LineRenderer>();
            foreach (var lineRenderer in pathLineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }
            }
        }

    }

    void EnablePathGenerator()
    {
        if (pathGenerator != null)
        {
            pathGenerator.enabled = true;
            pathGenerator.firstDraw = true;
            pathGenerator._pathing = true;

            if (pathLineRenderers != null)
            {
                foreach (var lineRenderer in pathLineRenderers)
                {
                    if (lineRenderer != null)
                    {
                        lineRenderer.enabled = true;
                    }
                }
            }
            if (UINotificationSystem.Instance != null)
            {
                StartCoroutine(UINotificationSystem.Instance.ShowTextInUI(
                    textToShow: "Navigation zu " + navigationDestination,
                    swipeSpeed: 2.0f,
                    displayDuration: 3.0f,
                    yOffset: -50f
                ));
            }
            else
            {
                UnityEngine.Debug.LogError("UINotificationSystem.Instance is NULL!");
            }
        }
    }

    // Help function for depth search
    private GameObject FindDeepChildInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name == name) return child.gameObject;
            }
        }
        return null;
    }
}