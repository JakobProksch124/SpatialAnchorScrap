using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

// Place script directly on the Building Prefab Root
public class Building_TransitionCues : MonoBehaviour
{
    [Header("General Reference Configuration")]
    [Tooltip("Optional: Prefab to instantiate as VR room. If null, a basic white room is created.")]
    [SerializeField] private GameObject vrRoomPrefab;
    [Tooltip("Optional: Scene name to load additively. Takes priority over vrRoomPrefab if set.")]
    [SerializeField] private string vrSceneName;
    [Tooltip("Destination shown in the navigation notification after returning to AR")]
    [SerializeField] private string navigationDestination = "Next Location";
    private Positioner positioner;

    [Header("Entry Cue Infos")]
    [Tooltip("Title shown during the VR transition fade")]
    [SerializeField] private string vrRoomTitle = "Virtual Room";
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string entryAnchorName = "entryAnchor";
    [SerializeField] private Color entryPrimaryColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private string entryLabel = "VR";
    [Tooltip("Optional: The image shown inside the transition cue")]
    [SerializeField] private Texture2D entryScreenshotDisplayed;
    [SerializeField] private string entryDescription = "Enter this virtual space.";
    [SerializeField] private string entryButtonText = "Enter VR";
    [SerializeField] private bool entryAlwaysExpand = false;

    [Header("Exit Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string exitAnchorName = "exitAnchor";
    [SerializeField] private Color exitPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitLabel = "AR";
    [SerializeField] private Texture2D exitScreenshotDisplayed;
    [SerializeField] private string exitDescription = "Return to Augmented Reality mode.";
    [SerializeField] private string exitButtonText = "Enter AR";
    [SerializeField] private bool exitAlwaysExpand = false;

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
    GameObject overlay = null;

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

        positioner = FindAnyObjectByType<Positioner>();

        // Find anchor point in this building
        entryAnchor = transform.Find(entryAnchorName);
        if (entryAnchor == null)
        {
            Debug.LogWarning($"[Building_TransitionCues] Anchor '{entryAnchorName}' not found. Using this transform.");
            entryAnchor = transform;
        }

        // Create entry cue
        CreateEntryCue(entryAnchor);

        // Spawn arrival cue (Premise: ArrivalCue component is present on this GameObject)
        arrivalCue = GetComponent<ArrivalCue>();
        if (arrivalCue != null)
        {
            arrivalCue.SpawnArrivalCue();
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
                    StartCoroutine(EnterVR());
                    userInVRRoom = !userInVRRoom;
                }
                else
                {
                    StartCoroutine(ExitVR());
                    userInVRRoom = !userInVRRoom;
                }
            }
        }
    }

    void CreateEntryCue(Transform entryAnchor)
    {
        // Base
        TransitionCueConfig entryCueConfig = TransitionCueConfig.CreateVRConfig(
            parent: entryAnchor,
            onInteract: () => StartCoroutine(EnterVR())
        );

        // Details
        entryCueConfig.primaryColor = entryPrimaryColor;
        entryCueConfig.label = entryLabel;
        entryCueConfig.expandedDescription = entryDescription;
        entryCueConfig.buttonText = entryButtonText;
        entryCueConfig.screenshotTexture = entryScreenshotDisplayed;

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

        // Hide arrival cue while in VR
        if (arrivalCue != null)
        {
            arrivalCue.HideArrivalCue();
        }

        // Hide instantiated building model in order to avoid visual overlaps
        SetPlacedBuildingVisible(false);
        Debug.Log("Building now invisible");

        // Disable PathGenerator rendering while in VR
        DisablePathGenerator();
        Debug.Log("Path Gen disabled");

        GameObject overlay = null;

        // Fade transition
        yield return StartCoroutine(TransitionEffects.Instance.FadeToBlackWithTitle(
            roomTitle: vrRoomTitle,
            fadeColor: Color.black,
            fadeDuration: 0.5f,
            titleHoldSeconds: 1.0f,           
            onOverlayReady: go => overlay = go
        ));
        Debug.Log("starting vr room coroutine 1");
        // Load the VR room
        yield return StartCoroutine(LoadVRRoom());
        yield return null;

        yield return StartCoroutine(TransitionEffects.Instance.FadeFromBlackAndDestroy(
            overlayCanvas: overlay,
            fadeColor: Color.black,
            fadeDuration: 0.5f
        ));
        Debug.Log("initializing vr room coroutine 2");

        // Create exit cue
        // CreateExitCue(); // THIS WAS THE OLD CALL; NOW HAPPENS INSIDE LOADVRROOM()
    }

    void SetPlacedBuildingVisible(bool visible)
    {
        if (positioner == null)
            positioner = FindAnyObjectByType<Positioner>();

        if (positioner == null || positioner.PlacedObject == null)
            return;

        var renderers = positioner.PlacedObject.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            if (r) r.enabled = visible;
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
            loadedVRScene = SceneManager.GetSceneByName(vrSceneName);
            yield return null; // safety wait

            // New way
            // Important: First rotation, then translation
            // We use this formula for the rotation angle: angle = atan2( dot(up, cross(a, b)), dot(a, b) )
            GameObject bridgeRoot = loadedVRScene.GetRootGameObjects()[0];
            Transform userSpawnPoint = bridgeRoot.transform.Find("UserSpawnPoint");
            Vector3 userPos = mainCamera.transform.position;

            if (userSpawnPoint != null)
            {
                Vector3 spawnPointFwd = userSpawnPoint.forward;
                Vector3 userFwd = mainCamera.transform.forward;

                // Projection onto a plane orthogonal to up (XZ plane)
                Vector3 up = Vector3.up;
                spawnPointFwd = Vector3.ProjectOnPlane(spawnPointFwd, up).normalized;
                userFwd = Vector3.ProjectOnPlane(userFwd, up).normalized;

                // signed angle (atan2 form)
                float sin = Vector3.Dot(up, Vector3.Cross(spawnPointFwd, userFwd));
                float cos = Vector3.Dot(spawnPointFwd, userFwd);
                float angleRad = Mathf.Atan2(sin, cos);
                float angleDeg = angleRad * Mathf.Rad2Deg;

                // Rotate root around world up
                bridgeRoot.transform.RotateAround(userSpawnPoint.position, up, angleDeg);

                Vector3 delta = mainCamera.transform.position - userSpawnPoint.position;
                bridgeRoot.transform.position += delta;

                vrRoom = bridgeRoot;
            }

            if (loadedVRScene.isLoaded)
            {
                var targets = FindDeepChildrenInScene(loadedVRScene, exitAnchorName);

                if (targets.Count > 0)
                {
                    foreach (var go in targets)
                    {
                        CreateExitCue(go.transform);
                    }
                }
                else
                {
                    Debug.LogWarning($"[BUILDING_TRANSITIONCUE] {exitAnchorName} Objekt wurde in der Szene {vrSceneName} nicht gefunden!");
                }
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
            CreateExitCue(entryAnchor.transform); // Testwise
        }
    }

    List<GameObject> FindDeepChildrenInScene(Scene scene, string name)
    {
        var results = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in allChildren)
            {
                if (child.name == name)
                    results.Add(child.gameObject);
            }
        }
        return results;
    }

    void CreateExitCue(Transform exitAnchor)
    {

        // Base
        TransitionCueConfig exitCueConfig = TransitionCueConfig.CreateARConfig(
            parent: exitAnchor,
            onInteract: () =>
            {
                StartCoroutine(ExitVR());
            }
        );

        // Details
        exitCueConfig.alwaysExpanded = true;
        exitCueConfig.primaryColor = exitPrimaryColor;
        exitCueConfig.expandedDescription = exitDescription;
        exitCueConfig.buttonText = exitButtonText;
        exitCueConfig.screenshotTexture = exitScreenshotDisplayed;

        // (Effectively not used if alwaysExpanded)
        exitCueConfig.label = exitLabel;

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

        SetPlacedBuildingVisible(true);

        // Re-enable entry cue
        if (entryCue != null)
        {
            entryCue.SetActive(true);
        }
        else
        {
            CreateEntryCue(entryAnchor);
        }

        // Re-enable PathGenerator
        EnablePathGenerator();

        // Re-spawn arrival cue
        if (arrivalCue != null)
        {
            arrivalCue.SpawnArrivalCue();
        }
    }

    IEnumerator UnloadVRRoom()
    {
        // If scene was loaded, unload it
        if (!string.IsNullOrEmpty(vrSceneName))
        {
            Scene scene = SceneManager.GetSceneByName(vrSceneName);
            if (scene.IsValid() && scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(loadedVRScene);
            }
        }

        // Destroy the room object (prefab instance or white room or scene marker)
        /*if (vrRoom != null)
        {
            Destroy(vrRoom);
            vrRoom = null;
        }*/
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

    void CreateWhiteRoom()
    {
        vrRoom = new GameObject("VRRoom_WhiteRoom");

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.SetParent(vrRoom.transform);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(2.5f, 1, 2.5f);
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
