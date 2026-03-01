using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

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
    [SerializeField] private GameObject ExtraARContent;

    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string startArrivalAnchorName = "startArrivalAnchor";
    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "VR";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Welcome to VR!";
    [SerializeField] private string startArrivalButtonText = "X";
    [SerializeField] private bool startArrivalAlwaysExpand = false;
    [SerializeField] private bool startArrivalIsBland = false;

    [Header("VREntry Cue Infos")]
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
    [SerializeField] private bool entryIsBland = false;

    [Header("VREntry Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string entryArrivalAnchorName = "entryArrivalAnchor";
    [SerializeField] private Color entryArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string entryArrivalLabel = "VR";
    [SerializeField] private Texture2D entryArrivalScreenshotDisplayed;
    [SerializeField] private string entryArrivalDescription = "Welcome to VR!";
    [SerializeField] private string entryArrivalButtonText = "X";
    [SerializeField] private bool entryArrivalAlwaysExpand = false;
    [SerializeField] private bool entryArrivalIsBland = false;

    [Header("VRExit Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string exitAnchorName = "exitAnchor";
    [SerializeField] private Color exitPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitLabel = "AR";
    [SerializeField] private Texture2D exitScreenshotDisplayed;
    [SerializeField] private string exitDescription = "Return to Augmented Reality mode.";
    [SerializeField] private string exitButtonText = "Enter AR";
    [SerializeField] private bool exitAlwaysExpand = false;
    [SerializeField] private bool leadsToAR = false;
    [SerializeField] private bool exitIsBland = false;

    [Header("VRExit Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string exitArrivalAnchorName = "exitArrivalAnchor";
    [SerializeField] private Color exitArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitArrivalLabel = "AR";
    [SerializeField] private Texture2D exitArrivalScreenshotDisplayed;
    [SerializeField] private string exitArrivalDescription = "Welcome back to AR!";
    [SerializeField] private string exitArrivalButtonText = "X";
    [SerializeField] private bool exitArrivalAlwaysExpand = false;
    [SerializeField] private bool exitArrivalIsBland = false;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardShortcuts = true;

    // Internal references
    private Transform entryAnchor;
    private Transform exitArrivalAnchor;
    private Transform startArrivalAnchor;
    private GameObject vrRoom;
    private GameObject entryCue;
    private GameObject exitCue;
    private GameObject entryArrivalCue;
    private GameObject exitArrivalCue;
    private GameObject startArrivalCue;
    
    private Camera mainCamera;
    private PathGenerator pathGenerator;
    private LineRenderer[] pathLineRenderers;
    private Scene loadedVRScene;
    private ArrivalCue LeaveHMDCue;
    private bool userInVRRoom = false;
    GameObject overlay = null;

    void Start()
    {
        mainCamera = Camera.main;

        // Find PathGenerator component
        foreach (PathGenerator component in GetComponents<PathGenerator>())
        {
            if (component.GetType().Name == "PathGenerator")
            {
                pathGenerator = component;
                break;
            }
        }

        positioner = FindAnyObjectByType<Positioner>();
        // Find entry anchor point in this building
        startArrivalAnchor = transform.Find(startArrivalAnchorName);
        if (startArrivalAnchor == null)
        {
            Debug.LogWarning($"[Building_TransitionCues] Anchor '{startArrivalAnchorName}' not found. Using this transform.");
            startArrivalAnchor = transform;
        }

        // Find entry anchor point in this building
        entryAnchor = transform.Find(entryAnchorName);
        if (entryAnchor == null)
        {
            Debug.LogWarning($"[Building_TransitionCues] Anchor '{entryAnchorName}' not found. Using this transform.");
            entryAnchor = transform;
        }

        // Find exit arrival anchor point in this building
        exitArrivalAnchor = transform.Find(exitArrivalAnchorName);
        if (exitArrivalAnchor == null)
        {
            Debug.LogWarning($"[Building_TransitionCues] Anchor '{exitArrivalAnchorName}' not found. Using this transform.");
            exitArrivalAnchor = transform;
        }
        //Create start arrival cue
        CreateStartArrivalCue(startArrivalAnchor);
        // Create entry cue
        CreateEntryCue(entryAnchor);
        // Spawn arrival cue (Premise: ArrivalCue component is present on this GameObject)
        LeaveHMDCue = GetComponent<ArrivalCue>();
        if (LeaveHMDCue != null)
        {
            LeaveHMDCue.SpawnArrivalCue();
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
        if (!entryIsBland) {
            // Details
            entryCueConfig.alwaysExpanded = entryAlwaysExpand;
            entryCueConfig.primaryColor = entryPrimaryColor;
            entryCueConfig.expandedDescription = entryDescription;
            entryCueConfig.screenshotTexture = entryScreenshotDisplayed;
        }
        else
        {
            // Details
            entryCueConfig.alwaysExpanded = true;
            entryCueConfig.primaryColor = Color.black;
            entryCueConfig.expandedDescription = entryLabel;
        }
        entryCueConfig.buttonText = entryButtonText;
        entryCueConfig.label = entryLabel;
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
        // Disable arrival cue B while in VR
        if (exitArrivalCue != null)
        {
            exitArrivalCue.SetActive(false);
        }
        if (ExtraARContent !=null)
        {
            ExtraARContent.SetActive(false);
        }

        // Hide arrival cue while in VR
        if (LeaveHMDCue != null)
        {
            LeaveHMDCue.HideArrivalCue();
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
            //userSpawnPoint.position = new Vector3(userSpawnPoint.position.x,userPos.y, userSpawnPoint.position.z); 

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


                // Get real floor from positioned building model
                float realFloorY = 0f;

                // Ray straight down from camera
                Ray ray = new Ray(mainCamera.transform.position, Vector3.down);
                RaycastHit hit;

                // Optional: LayerMask falls VR-Rig Ray stoppt
                int layerMask = LayerMask.GetMask("Floor");

                if (Physics.Raycast(ray, out hit, 20f, layerMask))
                {
                    if (hit.collider.CompareTag("Floor"))
                    {
                        realFloorY = hit.point.y;
                        Debug.Log("Floor detected at: " + realFloorY);

                        // Debug visualization
                        Debug.DrawLine(ray.origin, hit.point, Color.green, 5f);
                    }
                    else
                    {
                        Debug.Log("none floor objected detected at: " + hit.point.y);
                    }

                    
                }
                else
                {
                    Debug.LogWarning("No floor detected below camera!");
                }

                // VR scene floor
                float vrFloorY = userSpawnPoint.position.y;

                // Horizontal alignment (camera to spawn point)
                Vector3 delta = mainCamera.transform.position - userSpawnPoint.position;

                // Overwrite vertical alignment using building floor
                delta.y = realFloorY - vrFloorY;

                bridgeRoot.transform.position += delta;

                vrRoom = bridgeRoot;
            }

            if (loadedVRScene.isLoaded)
            {
                var exitTargets = FindDeepChildrenInScene(loadedVRScene, exitAnchorName);

                if (exitTargets.Count > 0)
                {
                    foreach (var go in exitTargets)
                    {
                        CreateExitCue(go.transform);
                    }
                }
                else
                {
                    Debug.LogWarning($"[BUILDING_TRANSITIONCUE] {exitAnchorName} Objekt wurde in der Szene {vrSceneName} nicht gefunden!");
                }
                var exitArrivalTargets = FindDeepChildrenInScene(loadedVRScene, entryArrivalAnchorName);

                if (exitArrivalTargets.Count > 0)
                {
                    foreach (var go in exitArrivalTargets)
                    {
                        CreateEntryArrivalCue(go.transform);
                    }
                }
                else
                {
                    Debug.LogWarning($"[BUILDING_TRANSITIONCUE] {entryArrivalAnchorName} Objekt wurde in der Szene {vrSceneName} nicht gefunden!");
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
        
        if (!exitIsBland)
        {
        // Details
        exitCueConfig.alwaysExpanded = exitAlwaysExpand;
        exitCueConfig.primaryColor = exitPrimaryColor;
        exitCueConfig.expandedDescription = exitDescription;
        exitCueConfig.screenshotTexture = exitScreenshotDisplayed;
        }
        else
        {
            // Details
            exitCueConfig.alwaysExpanded = true;
            exitCueConfig.primaryColor = Color.black;
            exitCueConfig.expandedDescription = exitLabel;

        }
        if (leadsToAR)
        {
            exitCueConfig.leadsToAR = true;
        }
        // (Effectively not used if alwaysExpanded)
        exitCueConfig.label = exitLabel;
        exitCueConfig.buttonText = exitButtonText;
        exitCue = TransitionCueFactory.CreateFrostedTransitionCue(exitCueConfig);
    }

    void CreateEntryArrivalCue(Transform entryArrivalAnchor)
    {
        
        if (!entryArrivalIsBland)
        {
            // Base
            TransitionCueConfig entryArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: entryArrivalAnchor,
                onInteract: () =>
                {
                    entryArrivalCue.SetActive(false);
                }
            );
            // Details
            entryArrivalCueConfig.alwaysExpanded = entryArrivalAlwaysExpand;
            entryArrivalCueConfig.primaryColor = entryArrivalPrimaryColor;
            entryArrivalCueConfig.expandedDescription = entryArrivalDescription;
            entryArrivalCueConfig.screenshotTexture = entryArrivalScreenshotDisplayed;
            // (Effectively not used if alwaysExpanded)
            entryArrivalCueConfig.label = entryArrivalLabel;
            entryArrivalCueConfig.buttonText = entryArrivalButtonText;

            entryArrivalCue = TransitionCueFactory.CreateFrostedTransitionCue(entryArrivalCueConfig);
        }
    }


    void CreateExitArrivalCue(Transform exitArrivalAnchor)
    {
        

        if (!exitArrivalIsBland)
        {// Base
            TransitionCueConfig exitArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: exitArrivalAnchor,
                onInteract: () =>
                {
                    exitArrivalCue.SetActive(false);
                }
            );
            // Details
            exitArrivalCueConfig.alwaysExpanded = exitArrivalAlwaysExpand;
            exitArrivalCueConfig.primaryColor = exitArrivalPrimaryColor;
            exitArrivalCueConfig.expandedDescription = exitArrivalDescription;
            exitArrivalCueConfig.screenshotTexture = exitArrivalScreenshotDisplayed;
            // (Effectively not used if alwaysExpanded)
            exitArrivalCueConfig.buttonText = exitArrivalButtonText;
            exitArrivalCueConfig.label = exitArrivalLabel;

            exitArrivalCue = TransitionCueFactory.CreateFrostedTransitionCue(exitArrivalCueConfig);

        }
    }


    void CreateStartArrivalCue(Transform StartArrivalAnchor)
    {
        
        if (!startArrivalIsBland)
        {// Base
            TransitionCueConfig StartArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: StartArrivalAnchor,
                onInteract: () =>
                {
                    startArrivalCue.SetActive(false);
                }
            );
            // Details
            StartArrivalCueConfig.alwaysExpanded = startArrivalAlwaysExpand;
            StartArrivalCueConfig.primaryColor = startArrivalPrimaryColor;
            StartArrivalCueConfig.expandedDescription = startArrivalDescription;
            StartArrivalCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;
            // (Effectively not used if alwaysExpanded)
            StartArrivalCueConfig.label = startArrivalLabel;
            StartArrivalCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateFrostedTransitionCue(StartArrivalCueConfig);
        }
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
        // Destroy entry arrival cue 
        if ( entryArrivalCue!= null)
        {
            // Also destroy the anchor parent
            if (entryArrivalCue.transform.parent != null)
            {
                Destroy(entryArrivalCue.transform.parent.gameObject);
            }
            Destroy(entryArrivalCue);
        }
        SetPlacedBuildingVisible(true);

        // Re-enable entry cue
        /*if (entryCue != null)
        {
            entryCue.SetActive(true);
        }
        else
        {
            CreateEntryCue(entryAnchor);
        }*/
        // Enable arrival cue B
        if (exitArrivalCue == null)
        {
            CreateExitArrivalCue(exitArrivalAnchor);
        }
        if (ExtraARContent != null)
        {
            ExtraARContent.SetActive(true);
        }
        
        // Re-enable PathGenerator
        EnablePathGenerator();

        
        // Re-spawn arrival cue
        if (LeaveHMDCue != null)
        {
            LeaveHMDCue.SpawnArrivalCue();
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
            pathGenerator.ClearArrows();
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
