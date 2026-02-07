using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Place script directly on the Building Prefab Root
public class Building_TransitionCues : MonoBehaviour
{
    [Header("Building Configuration")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string anchorChildName = "transitionAnchor";

    [Tooltip("Title shown during the VR transition fade")]
    [SerializeField] private string vrRoomTitle = "Virtual Room";

    [Tooltip("Description shown on the entry cue when expanded")]
    [SerializeField] private string entryDescription = "Enter this virtual space.";

    [Tooltip("Description shown on the exit cue")]
    [SerializeField] private string exitDescription = "Return to Augmented Reality mode.";

    [Tooltip("Destination shown in the navigation notification after returning to AR")]
    [SerializeField] private string navigationDestination = "Next Location";

    [Header("VR Room")]
    [Tooltip("Optional: Prefab to instantiate as VR room. If null, a basic white room is created.")]
    [SerializeField] private GameObject vrRoomPrefab;

    [Tooltip("Optional: Scene name to load additively. Takes priority over vrRoomPrefab if set.")]
    [SerializeField] private string vrSceneName;

    [Header("Cue Appearance")]
    [SerializeField] private Color primaryColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private string entryLabel = "VR";
    [SerializeField] private string entryButtonText = "Enter VR";
    [SerializeField] private string exitButtonText = "Enter AR";

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardShortcuts = true;

    // Internal references
    private Transform anchorTransform;
    private GameObject vrRoom;
    private GameObject entryCue;
    private GameObject exitCue;
    private Camera mainCamera;
    private MonoBehaviour pathGenerator;
    private LineRenderer[] pathLineRenderers;

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

        // Find anchor point in this building
        anchorTransform = transform.Find(anchorChildName);
        if (anchorTransform == null)
        {
            Debug.LogWarning($"[Building_TransitionCues] Anchor '{anchorChildName}' not found. Using this transform.");
            anchorTransform = transform;
        }

        // Create entry cue
        CreateEntryCue();
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

                if (vrRoom == null)
                {
                    StartCoroutine(EnterVR());
                }
                else
                {
                    StartCoroutine(ExitVR());
                }
            }
        }
    }

    void CreateEntryCue()
    {
        TransitionCueConfig entryCueConfig = TransitionCueConfig.CreateVRConfig(
            parent: anchorTransform,
            onInteract: () => EnterVR()
        );
        entryCueConfig.label = entryLabel;
        entryCueConfig.primaryColor = primaryColor;
        entryCueConfig.expandedDescription = entryDescription;
        entryCueConfig.buttonText = entryButtonText;

        entryCue = TransitionCueFactory.CreateFrostedTransitionCue(entryCueConfig);
    }

    IEnumerator EnterVR()
    {
        Debug.Log($"[Building_TransitionCues] Entering VR: {vrRoomTitle}");

        // Disable the entry cue while in VR
        if (entryCue != null)
        {
            entryCue.SetActive(false);
        }

        // Disable PathGenerator rendering while in VR
        DisablePathGenerator();

        // Fade transition
        yield return StartCoroutine(TransitionEffects.Instance.FadeToVRWithTitle(
            vrRoomTitle,
            Color.black,
            2.0f
        ));

        // Load the VR room
        yield return StartCoroutine(LoadVRRoom());

        // Create exit cue
        CreateExitCue();
    }

    IEnumerator LoadVRRoom()
    {
        // Priority 1: Load scene additively
        if (!string.IsNullOrEmpty(vrSceneName))
        {
            Debug.Log("Variant 1");

            var operation = SceneManager.LoadSceneAsync(vrSceneName, LoadSceneMode.Additive);
            yield return operation;

            // Store reference to unload later (scene root objects)
            Scene loadedScene = SceneManager.GetSceneByName(vrSceneName);

            // New way
            GameObject bridgeRoot = loadedScene.GetRootGameObjects()[0];
            Transform userSpawnPoint = bridgeRoot.transform.Find("UserSpawnPoint");
            Vector3 userPos = mainCamera.transform.position;
            if (userSpawnPoint != null)
            {
                bridgeRoot.transform.position = userPos - userSpawnPoint.localPosition;
                bridgeRoot.transform.rotation = Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0);
            }

            // Old way
            /*if (loadedScene.IsValid())
            {
                // Create container to track the loaded scene
                vrRoom = new GameObject($"VRRoom_SceneMarker_{vrSceneName}");
            }*/
        }
        // Priority 2: Instantiate prefab
        else if (vrRoomPrefab != null)
        {
            Debug.Log("Variant 2");
            vrRoom = Instantiate(vrRoomPrefab, Vector3.zero, Quaternion.identity);
            vrRoom.name = $"VRRoom_{vrRoomPrefab.name}";
        }
        // Fallback: Create basic white room
        else
        {
            Debug.Log("Variant 3");
            CreateWhiteRoom();
        }
    }

    void CreateExitCue()
    {
        GameObject returnAnchor = new GameObject("ReturnCue_Anchor");
        returnAnchor.transform.position = new Vector3(0, 1.5f, 2);

        TransitionCueConfig exitCueConfig = TransitionCueConfig.CreateARConfig(
            parent: returnAnchor.transform,
            onInteract: () =>
            {
                StartCoroutine(ExitVR());
            }
        );
        exitCueConfig.alwaysExpanded = true;
        exitCueConfig.expandedDescription = exitDescription;
        exitCueConfig.buttonText = exitButtonText;

        exitCue = TransitionCueFactory.CreateFrostedTransitionCue(exitCueConfig);
    }

    IEnumerator ExitVR()
    {
        Debug.Log($"[Building_TransitionCues] Exiting VR, returning to AR");

        // Fade out
        yield return StartCoroutine(TransitionEffects.Instance.FadeToAR(1.5f, vrRoom));

        // Unload VR room
        yield return StartCoroutine(UnloadVRRoom());

        // Destroy exit cue
        if (exitCue != null)
        {
            // Also destroy the anchor parent
            if (exitCue.transform.parent != null)
            {
                Destroy(exitCue.transform.parent.gameObject);
            }
            Destroy(exitCue);
        }

        // Re-enable entry cue
        if (entryCue != null)
        {
            entryCue.SetActive(true);
        }
        else
        {
            CreateEntryCue();
        }

        // Re-enable PathGenerator
        EnablePathGenerator();
    }

    IEnumerator UnloadVRRoom()
    {
        // If scene was loaded, unload it
        if (!string.IsNullOrEmpty(vrSceneName))
        {
            Scene scene = SceneManager.GetSceneByName(vrSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(vrSceneName);
            }
        }

        // Destroy the room object (prefab instance or white room or scene marker)
        if (vrRoom != null)
        {
            Destroy(vrRoom);
            vrRoom = null;
        }
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
                swipeSpeed: 1.0f,
                displayDuration: 3.0f,
                yOffset: -50f
            ));
        }
    }

    void CreateWhiteRoom()
    {
        vrRoom = new GameObject("VRRoom_WhiteRoom");

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.SetParent(vrRoom.transform);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10);
        floor.GetComponent<Renderer>().material.color = Color.white;

        // Walls
        CreateWall(vrRoom.transform, new Vector3(0, 2.5f, 5), new Vector3(10, 5, 0.1f), Color.white);
        CreateWall(vrRoom.transform, new Vector3(0, 2.5f, -5), new Vector3(10, 5, 0.1f), Color.red);
        CreateWall(vrRoom.transform, new Vector3(5, 2.5f, 0), new Vector3(0.1f, 5, 10), Color.green);
        CreateWall(vrRoom.transform, new Vector3(-5, 2.5f, 0), new Vector3(0.1f, 5, 10), Color.blue);
    }

    void CreateWall(Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material.color = color;
    }
}
