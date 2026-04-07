using System.Diagnostics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Place script directly on the Building Prefab Root
public class Mensa_FriendCue : MonoBehaviour
{
    [Tooltip("Destination shown in the navigation notification after returning to AR")]
    [SerializeField] private string navigationDestination = "Next Location";
    [SerializeField] private bool leaveHMDIsBland = false;



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

    [SerializeField] private string entryAnchorName = "entryAnchor";
    [SerializeField] private Color entryPrimaryColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private string entryLabel = "AR";
    [Tooltip("Optional: The image shown inside the transition cue")]
    [SerializeField] private Texture2D entryScreenshotDisplayed;
    [SerializeField] private string entryDescription = "Start navigation to friends";
    [SerializeField] private string entryButtonText = "Start navigation";
    [SerializeField] private bool entryAlwaysExpand = true;
    [SerializeField] private bool entryIsBland = true;

    [SerializeField] private string entryArrivalAnchorName = "entryArrivalAnchor";
    [SerializeField] private Color entryArrivalPrimaryColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private string entryArrivalLabel = "AR";
    [Tooltip("Optional: The image shown inside the transition cue")]
    [SerializeField] private Texture2D entryArrivalScreenshotDisplayed;
    [SerializeField] private string entryArrivalDescription = "Started navigation";
    [SerializeField] private string entryArrivalButtonText = "X";
    [SerializeField] private bool entryArrivalAlwaysExpand = true;
    [SerializeField] private bool entryArrivalIsBland = true;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] InputActionReference switchIsBlandButton;
    private Positioner positioner;


    private bool _switchIsBlandButtonWasPressed = false;

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

    void Awake()
    {
        LoadBlandState();
        if (entryIsBland && startArrivalIsBland)
        {

            startArrivalIsBland = false;
            entryArrivalIsBland = true;
            entryIsBland = true;
            leaveHMDIsBland = false;
            SaveBlandState();
        }
    }

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

        CreateStartArrivalCue(startArrivalAnchor);
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
        if (!startArrivalIsBland)
        {
            TransitionCueConfig StartArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: StartArrivalAnchor,
                onInteract: () =>
                {
                    startArrivalCue.SetActive(false);
                    ShowFood();
                }
            );

            StartArrivalCueConfig.onCollide = (other) =>
            {
                startArrivalCue.SetActive(false);
                ShowFood();
            };

            StartArrivalCueConfig.isArrival = true;
            StartArrivalCueConfig.alwaysExpanded = true;
            StartArrivalCueConfig.primaryColor = startArrivalPrimaryColor;
            StartArrivalCueConfig.expandedDescription = startArrivalDescription;
            StartArrivalCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;
            StartArrivalCueConfig.label = startArrivalLabel;
            StartArrivalCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateCue(StartArrivalCueConfig);
            UnityEngine.Debug.Log("start arrival cue created!");

        }
        else
        {
            ShowFood();
        }
    }

    void Update()
    {
        if (positioner != null)
        {
            if (positioner.getDevMode())
            {
                CheckSwitchIsBland();
            }
        }
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


    void CheckSwitchIsBland()
    {
        bool isPressed = switchIsBlandButton.action.IsPressed();
        if (_switchIsBlandButtonWasPressed && !isPressed)
        {
            SwitchIsBland();
        }
        _switchIsBlandButtonWasPressed = isPressed;

    }

    public void showEntryCue()
    {
        UINotificationSystem.Instance.ShowPersistentMessage("Your food was ordered.", false);

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
            arrivalCue.SpawnArrivalCue(leaveHMDIsBland);
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
            onCollide = (other) => showEntryCue(),
            enableTurnTowardsUser = foodButtonsTurnToUser
        };

        return TransitionCueFactory.CreateStandaloneButton(btnConfig);
    }

    void CreateEntryCue(Transform entryAnchor)
    {
        // Base
        TransitionCueConfig entryCueConfig = TransitionCueConfig.CreateARConfig(
            parent: entryAnchor,
            onInteract: () => {
                StartNavigationToFriends();
                entryCue.SetActive(false);
            }
        );


        entryCueConfig.onCollide = (other) =>
        {
            StartNavigationToFriends();
            entryCue.SetActive(false);
        };

        if (!entryIsBland)
        {
            // Details
            UnityEngine.Debug.Log("setting cue details");
            entryCueConfig.primaryColor = entryPrimaryColor;
            entryCueConfig.expandedDescription = entryDescription;
            entryCueConfig.screenshotTexture = entryScreenshotDisplayed;
            entryCueConfig.alwaysExpanded = entryAlwaysExpand;

        }
        else
        {
            // Details
            UnityEngine.Debug.Log("setting cue details");
            entryCueConfig.primaryColor = Color.grey;
            entryCueConfig.expandedDescription = entryLabel;
            entryCueConfig.alwaysExpanded = true;
            entryCueConfig.isBland = entryIsBland;

        }

        entryCueConfig.buttonText = entryButtonText;
        entryCueConfig.label = entryLabel;
        entryCue = TransitionCueFactory.CreateCue(entryCueConfig);
    }

    void CreateEntryArrivalCue(Transform entryArrivalAnchor)
    {
        if (!entryIsBland)
        {
            // Base
            TransitionCueConfig entryArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: entryArrivalAnchor,
                onInteract: () => entryArrivalCue.SetActive(false)
            );


            entryArrivalCueConfig.onCollide = (other) =>
            {
                entryArrivalCue.SetActive(false);
            };

            // Details
            entryArrivalCueConfig.isArrival = true;
            entryArrivalCueConfig.primaryColor = entryArrivalPrimaryColor;
            entryArrivalCueConfig.expandedDescription = entryArrivalDescription;
            entryArrivalCueConfig.screenshotTexture = entryArrivalScreenshotDisplayed;
            entryArrivalCueConfig.alwaysExpanded = true;
            entryArrivalCueConfig.buttonText = entryArrivalButtonText;
            entryArrivalCueConfig.label = entryArrivalLabel;

            entryArrivalCue = TransitionCueFactory.CreateCue(entryArrivalCueConfig);
        }
    }

    public void StartNavigationToFriends()
    {
        UINotificationSystem.Instance.HidePersistentMessage();
        // Hide entry cue
        if (entryCue != null)
            entryCue.SetActive(false);

        // Create entry arrival cue
        CreateEntryArrivalCue(entryArrivalAnchor);
        EnablePathGenerator();
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
                    textToShow: navigationDestination,
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

    public void SwitchIsBland()
    {
        entryArrivalIsBland = !entryArrivalIsBland;
        entryIsBland = !entryIsBland;
        startArrivalIsBland = !startArrivalIsBland;
        leaveHMDIsBland = !leaveHMDIsBland;

        if (!startArrivalIsBland)
        {
            UnityEngine.Debug.Log("active study: AB");
        }
        else
        {
            UnityEngine.Debug.Log("active study: BA");

        }
        SaveBlandState();

        if (arrivalCue != null)
        {
            arrivalCue.SwitchIsBland();
        }
    }

    void SaveBlandState()
    {
        PlayerPrefs.SetInt("entryArrivalIsBland", entryArrivalIsBland ? 1 : 0);
        PlayerPrefs.SetInt("entryIsBland", entryIsBland ? 1 : 0);
        PlayerPrefs.SetInt("startArrivalIsBland", startArrivalIsBland ? 1 : 0);
        PlayerPrefs.SetInt("leaveHMDIsBland", leaveHMDIsBland ? 1 : 0);

        PlayerPrefs.Save();
    }

    void LoadBlandState()
    {
        entryArrivalIsBland = PlayerPrefs.GetInt("entryArrivalIsBland", 0) == 1;
        entryIsBland = PlayerPrefs.GetInt("entryIsBland", 0) == 1;
        startArrivalIsBland = PlayerPrefs.GetInt("startArrivalIsBland", 0) == 1;
        leaveHMDIsBland = PlayerPrefs.GetInt("leaveHMDIsBland", 0) == 1;
    }
}