using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Place script directly on the Building Prefab Root
public class Mensa_FriendCue : MonoBehaviour
{
    [Tooltip("Destination shown in the navigation notification after returning to AR")]
    [SerializeField] private string navigationDestination = "Next Location";

    [SerializeField] private string entryAnchorName = "entryAnchor";
    [SerializeField] private Color entryPrimaryColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private string entryLabel = "VR";
    [Tooltip("Optional: The image shown inside the transition cue")]
    [SerializeField] private Texture2D entryScreenshotDisplayed;
    [SerializeField] private string entryDescription = "Enter this virtual space.";
    [SerializeField] private string entryButtonText = "Enter VR";
    [SerializeField] private bool entryAlwaysExpand = true;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardShortcuts = true;

    // Internal references
    private Transform entryAnchor;
    private GameObject vrRoom;
    private GameObject entryCue;
    private GameObject exitCue;
    private Camera mainCamera;
    private MonoBehaviour pathGenerator;
    private LineRenderer[] pathLineRenderers;
    private Scene loadedVRScene;
    private ArrivalCue arrivalCue;
    private bool userInVRRoom = false;
    public GameObject FoodA;
    public GameObject FoodB;
    public GameObject FoodC;
    public GameObject FoodButtonCanvas;

    void Start()
    {
        mainCamera = Camera.main;

        // Find PathGenerator component
        foreach (var component in GetComponents<MonoBehaviour>())
        {
            if (component.GetType().Name == "PathGenerator")
            {
                pathGenerator = component;
                break;
            }
        }

        DisablePathGenerator();

        // Find anchor point in this building
        entryAnchor = transform.Find(entryAnchorName);
        if (entryAnchor == null)
        {
            Debug.LogWarning($"[Building_TransitionCues] Anchor '{entryAnchorName}' not found. Using this transform.");
            entryAnchor = transform;
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
                Debug.Log($"[Building_TransitionCues] T or P key pressed on {gameObject.name}");

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
        // Create entry cue
        CreateEntryCue(entryAnchor);
        if(FoodA!=null)
        FoodA.SetActive(false);
        if (FoodB != null)
            FoodB.SetActive(false);
        if (FoodC != null)
            FoodC.SetActive(false);
        if (FoodButtonCanvas != null)
            FoodButtonCanvas.SetActive(false);

        // Spawn arrival cue (Premise: ArrivalCue component is present on this GameObject)
        arrivalCue = GetComponent<ArrivalCue>();
        if (arrivalCue != null)
        {
            arrivalCue.SpawnArrivalCue();
        }
    }

    void CreateEntryCue(Transform entryAnchor)
    {
        // Base
        TransitionCueConfig entryCueConfig = TransitionCueConfig.CreateVRConfig(
            parent: entryAnchor,
            onInteract: () => StartNavigationToFriends()
        );

        // Details
        entryCueConfig.primaryColor = entryPrimaryColor;
        entryCueConfig.label = entryLabel;
        entryCueConfig.expandedDescription = entryDescription;
        entryCueConfig.buttonText = entryButtonText;
        entryCueConfig.screenshotTexture = entryScreenshotDisplayed;
        entryCueConfig.alwaysExpanded = entryAlwaysExpand;

        entryCue = TransitionCueFactory.CreateFrostedTransitionCue(entryCueConfig);
    }

    public void StartNavigationToFriends()
    {
        EnablePathGenerator();
    }

    void DisablePathGenerator()
    {
        if (pathGenerator != null)
        {
            pathGenerator.enabled = false;

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

            StartCoroutine(UINotificationSystem.Instance.ShowNavigationContinued(
                destination: navigationDestination,
                swipeSpeed: 2.0f,
                displayDuration: 3.0f,
                yOffset: -50f
            ));
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
