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
//transition cues for G64 and Bib
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
    [SerializeField] private IMessageTransitionCue IMessageCueScript;



    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string startArrivalAnchorName = "startArrivalAnchor";
    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "VR";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Welcome to VR!";
    [SerializeField] private string startArrivalButtonText = "X";
    [SerializeField] private bool startArrivalAlwaysExpand = false;

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

    [Header("VREntry Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string entryArrivalAnchorName = "entryArrivalAnchor";
    [SerializeField] private Color entryArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string entryArrivalLabel = "VR";
    [SerializeField] private Texture2D entryArrivalScreenshotDisplayed;
    [SerializeField] private string entryArrivalDescription = "Welcome to VR!";
    [SerializeField] private string entryArrivalButtonText = "X";
    [SerializeField] private bool entryArrivalAlwaysExpand = false;

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
    [SerializeField] private float exitCueDelay = 20f;

    [Header("VRExit Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string exitArrivalAnchorName = "exitArrivalAnchor";
    [SerializeField] private Color exitArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string exitArrivalLabel = "AR";
    [SerializeField] private Texture2D exitArrivalScreenshotDisplayed;
    [SerializeField] private string exitArrivalDescription = "Welcome back to AR!";
    [SerializeField] private string exitArrivalButtonText = "X";
    [SerializeField] private bool exitArrivalAlwaysExpand = false;

    [Header("Transition Particles")]
    [SerializeField] private Color enterVRParticleColor = new Color(0.3f, 0.4f, 0.8f);
    [SerializeField] private Color exitVRParticleColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private float particleDuration = 4f;

    [Header("Debug")]
    [SerializeField] private bool enableKeyboardShortcuts = true;


    // Internal references
    private Transform entryAnchor;
    private Transform exitArrivalAnchor;
    private Transform startArrivalAnchor;
    private Transform exitCueAnchor;
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

    // New fields to add
    private int floorMask;
    private Transform spawnPoint;
    private float alignTimer;
    private const float AlignInterval = 0.001f;  // 0.1 Hz
    private float targetFloorDeltaY;
    private bool exitingVR = false;



    void Start()
    {
        mainCamera = Camera.main;

        // Cache in Start or when vrRoom is assigned
        floorMask = LayerMask.GetMask("Floor");
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

        Debug.Log("creating start arrival cue");
        //Create start arrival cue
        CreateStartArrivalCue(startArrivalAnchor);

        Debug.Log("creating entry cue");
        // Create entry cue
        CreateEntryCue(entryAnchor);

        Debug.Log("getting arrival cue component");
        // Spawn arrival cue (Premise: ArrivalCue component is present on this GameObject)
        LeaveHMDCue = GetComponent<ArrivalCue>();
        Debug.Log("calling arrival cue spawner");
        if (LeaveHMDCue != null)
        {
            Debug.Log("spawning arrival cue");
            LeaveHMDCue.SpawnArrivalCue();
        }
    }

    /*public void RegisterTeleportRedirects()
    {
        ARTeleportRedirect[] redirects = FindObjectsByType<ARTeleportRedirect>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var redirect in redirects)
        {
            redirect.SetBuildingTransitionCues(this);
        }
    }*/

    public void Update()
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
        if (!userInVRRoom || vrRoom == null)
            return;

        alignTimer += Time.deltaTime;
        if (alignTimer >= AlignInterval)
        {
            alignTimer = 0f;
            AlignVRFloorToRealFloor();
        }
    }

    void UpdateFloorTarget()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (spawnPoint == null) spawnPoint = vrRoom.transform.Find("UserSpawnPoint");
        if (spawnPoint == null) return;

        Ray ray = new Ray(mainCamera.transform.position, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 20f, floorMask))
        {
            if (!hit.collider.CompareTag("Floor")) return;
            targetFloorDeltaY = hit.point.y - spawnPoint.position.y;
        }
    }

    void ApplyFloorAlignment()
    {
        if (Mathf.Abs(targetFloorDeltaY) < 0.0005f) return;
        Vector3 pos = vrRoom.transform.position;
        pos.y = Mathf.Lerp(pos.y, pos.y + targetFloorDeltaY, Time.deltaTime * 10f);
        vrRoom.transform.position = pos;
        // NO Physics.SyncTransforms() � let Unity handle it
    }

    void AlignVRFloorToRealFloor()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Vector3 origin = mainCamera.transform.position;

        Ray ray = new Ray(origin, Vector3.down);
        RaycastHit hit;

        int floorMask = LayerMask.GetMask("Floor");

        if (Physics.Raycast(ray, out hit, 20f, floorMask))
        {
            if (!hit.collider.CompareTag("Floor"))
                return;

            float realFloorY = hit.point.y;

            // find VR scene floor reference
            Transform spawn = vrRoom.transform.Find("UserSpawnPoint");
            if (spawn == null)
                return;

            float vrFloorY = spawn.position.y;

            float deltaY = realFloorY - vrFloorY;

            if (Mathf.Abs(deltaY) < 0.001f)
                return;

            Vector3 pos = vrRoom.transform.position;
            pos.y += deltaY;
            vrRoom.transform.position = pos;

            Physics.SyncTransforms();
        }
    }

   

    void CreateEntryCue(Transform entryAnchor)
    {

        // Base
        TransitionCueConfig entryCueConfig = TransitionCueConfig.CreateVRConfig(
           parent: entryAnchor,
           onInteract: () => StartCoroutine(EnterVR()),
            onClose: () =>
            {
            },
            isStandardClose: true
        );


        entryCueConfig.onCollide = (other) =>
        {
            StartCoroutine(EnterVR());
        };
    
            // Details
            entryCueConfig.alwaysExpanded = entryAlwaysExpand;
            entryCueConfig.primaryColor = entryPrimaryColor;
            entryCueConfig.expandedDescription = entryDescription;
            entryCueConfig.screenshotTexture = entryScreenshotDisplayed;
        

        entryCueConfig.buttonText = entryButtonText;
        entryCueConfig.label = entryLabel;
        Debug.Log("creating entry cue");
        entryCue = TransitionCueFactory.CreateCue(entryCueConfig);
        Debug.Log("created entry cue successfully");
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

        if (ExtraARContent != null)
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

        // XX hier einfach black mit blue austauschen
        // Fade transition
        /* yield return StartCoroutine(TransitionEffects.Instance.FadeToBlackWithTitle(
            roomTitle: vrRoomTitle,
            fadeColor: entryPrimaryColor,
            fadeDuration: 1f,
            titleHoldSeconds: 1.0f,
            onOverlayReady: go => overlay = go
        )); */

        Debug.Log("starting vr room coroutine 1");

        // Load the VR room
        yield return StartCoroutine(LoadVRRoom());
        
        // Fade out
        yield return StartCoroutine(TransitionEffects.Instance.FadeToVR(3f, vrRoom));
        yield return null;

        /* yield return StartCoroutine(TransitionEffects.Instance.FadeFromBlackAndDestroy(
            overlayCanvas: overlay,
            fadeColor: entryPrimaryColor,
            fadeDuration: 2f
        )); */
        TransitionParticleEffect.Spawn(mainCamera, enterVRParticleColor, particleDuration * 2);
        userInVRRoom = true;
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

            // Important: First rotation, then translation
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
                        exitCueAnchor = go.transform;
                        Invoke(nameof(SpawnExitCue), exitCueDelay);
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

    void SpawnExitCue()
    {
        if (exitCueAnchor != null)
        {
            CreateExitCue(exitCueAnchor);
        }
    }

    /*void RepositionVRFloorAfterTeleport()
    {
        if (vrRoom == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        Ray ray = new Ray(mainCamera.transform.position, Vector3.down);
        RaycastHit hit;

        int layerMask = LayerMask.GetMask("Floor");

        if (Physics.Raycast(ray, out hit, 20f, layerMask))
        {
            if (hit.collider.CompareTag("Floor"))
            {
                float realFloorY = hit.point.y;
                float cameraY = mainCamera.transform.position.y;

                float deltaY = realFloorY - cameraY;

                Vector3 pos = vrRoom.transform.position;
                pos.y += deltaY;
                vrRoom.transform.position = pos;

                Physics.SyncTransforms();

                Debug.Log($"[VR] Floor corrected by {deltaY}");
            }
        }
    }

    public void OnTeleportFinished()
    {
        Debug.Log("TELEPORT FINISHED CALLED AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        RepositionVRFloorAfterTeleport();
    }*/

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

    // CUE INFO:
    // This cue is placed at the doors of any vr room and allows the player to exit the vr room and return to the ar-supported world
    void CreateExitCue(Transform exitAnchor)
    {
        /*

        // --- New addition: search for doors and deactivate ---
        GameObject doorL = GameObject.Find("Door_L");
        GameObject doorR = GameObject.Find("Door_R");

        if (doorL != null)
            doorL.SetActive(false);
        else
            Debug.LogWarning("[Building_TransitionCues] Door_L not found in scene!");

        if (doorR != null)
            doorR.SetActive(false);
        else
            Debug.LogWarning("[Building_TransitionCues] Door_R not found in scene!");
        */
        // Base (Same basic configuration for enhanced as well as minimal cues
        TransitionCueConfig exitCueConfig = TransitionCueConfig.CreateARConfig(
            parent: exitAnchor,
            onInteract: () =>
            {
                StartCoroutine(ExitVR());
            },
            onClose: () =>
            {
            },
            isStandardClose: true
        );

        exitCueConfig.onCollide = (other) =>
        {
            StartCoroutine(ExitVR());
        };

            // Details for enhanced cues
            exitCueConfig.alwaysExpanded = exitAlwaysExpand;
            exitCueConfig.primaryColor = exitPrimaryColor;
            exitCueConfig.expandedDescription = exitDescription;
            exitCueConfig.screenshotTexture = exitScreenshotDisplayed;
       

        exitCueConfig.leadsToAR = this.leadsToAR;


        // (Effectively not used if alwaysExpanded)
        exitCueConfig.label = exitLabel;
        exitCueConfig.buttonText = exitButtonText;
        exitCue = TransitionCueFactory.CreateCue(exitCueConfig);
    }

    // CUE INFO:
    // This cue spawns in front of the user when he freshly entered a vr room and gives him some info or instructions about what he can explore
    void CreateEntryArrivalCue(Transform entryArrivalAnchor)
    {
        
            // Base
            TransitionCueConfig entryArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: entryArrivalAnchor,
                onInteract: () =>
                {
                    entryArrivalCue.SetActive(false);
                },
            onClose: () =>
            {
            },
            isStandardClose: true
            );
            entryArrivalCueConfig.onCollide = (other) =>
            {
                entryArrivalCue.SetActive(false);
            };
            // Details
            entryArrivalCueConfig.isArrival = true;
            entryArrivalCueConfig.isTransparent = false;
            entryArrivalCueConfig.alwaysExpanded = true;
            entryArrivalCueConfig.primaryColor = entryArrivalPrimaryColor;
            entryArrivalCueConfig.expandedDescription = entryArrivalDescription;
            entryArrivalCueConfig.screenshotTexture = entryArrivalScreenshotDisplayed;

            // (Effectively not used if alwaysExpanded)
            entryArrivalCueConfig.label = entryArrivalLabel;
            entryArrivalCueConfig.buttonText = entryArrivalButtonText;

            entryArrivalCue = TransitionCueFactory.CreateCue(entryArrivalCueConfig);
    }

    // CUE INFO:
    // This cue spawns when the user exited vr, lands in ar, and conforms him with a successful landing and info about where he went off
    void CreateExitArrivalCue(Transform exitArrivalAnchor)
    {
            // Base
            TransitionCueConfig exitArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: exitArrivalAnchor,
                onInteract: () =>
                {
                    exitArrivalCue.SetActive(false);
                },
            onClose: () =>
            {
            },
            isStandardClose: true
            );

            exitArrivalCueConfig.onCollide = (other) =>
            {
                exitArrivalCue.SetActive(false);
            };

            // Details
            exitArrivalCueConfig.isArrival = true;
            exitArrivalCueConfig.alwaysExpanded = true;
            exitArrivalCueConfig.primaryColor = exitArrivalPrimaryColor;
            exitArrivalCueConfig.expandedDescription = exitArrivalDescription;
            exitArrivalCueConfig.screenshotTexture = exitArrivalScreenshotDisplayed;

            // (Effectively not used if alwaysExpanded)
            exitArrivalCueConfig.buttonText = exitArrivalButtonText;
            exitArrivalCueConfig.label = exitArrivalLabel;

            exitArrivalCue = TransitionCueFactory.CreateCue(exitArrivalCueConfig);
        
    }

    // CUE INFO:
    // This cue spawns in the face of the user when starting a new application that has this script (i.e., G64 or Bib),
    // confronting them with orders to follow the arrow
    void CreateStartArrivalCue(Transform StartArrivalAnchor)
    {
            // Base
            TransitionCueConfig StartArrivalCueConfig = TransitionCueConfig.CreateARConfig(
                parent: StartArrivalAnchor,
                onInteract: () =>
                {
                    startArrivalCue.SetActive(false);
                },
            onClose: () =>
            {
            },
            isStandardClose: true
            );

            StartArrivalCueConfig.onCollide = (other) =>
            {
                startArrivalCue.SetActive(false);
            };

            // Details
            StartArrivalCueConfig.isArrival = true;
            StartArrivalCueConfig.alwaysExpanded = true;
            StartArrivalCueConfig.primaryColor = startArrivalPrimaryColor;
            StartArrivalCueConfig.expandedDescription = startArrivalDescription;
            StartArrivalCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;

            // (Effectively not used if alwaysExpanded)
            StartArrivalCueConfig.label = startArrivalLabel;
            StartArrivalCueConfig.buttonText = startArrivalButtonText;

            startArrivalCue = TransitionCueFactory.CreateCue(StartArrivalCueConfig);
        
    }

    IEnumerator ExitVR()
    {
        if (!exitingVR)
        {
            exitingVR = true;


            Debug.Log($"[Building_TransitionCues] Exiting VR, returning to AR");
            
                // Fade out
                yield return StartCoroutine(TransitionEffects.Instance.FadeToAR(3f, vrRoom));
        // Unload VR room
        yield return StartCoroutine(UnloadVRRoom());

        TransitionParticleEffect.Spawn(mainCamera, exitVRParticleColor, particleDuration);

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
        if (entryArrivalCue != null)
        {
            // Also destroy the anchor parent
            if (entryArrivalCue.transform.parent != null)
            {
                Destroy(entryArrivalCue.transform.parent.gameObject);
            }
            Destroy(entryArrivalCue);
        }
        SetPlacedBuildingVisible(true);

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

        userInVRRoom = false;
        }
        exitingVR= false;
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

            StartCoroutine(UINotificationSystem.Instance.ShowTextInUI(
                textToShow: "Continuing navigation to the "+navigationDestination+" ",
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

    public void MoveVRRoomToHit(Vector3 hitPoint)
    {
        if (vrRoom == null) return;

        // Player feet position (OVRCameraRig root position)
        Vector3 playerFeet = Camera.main.transform.parent.position;

        // Calculate horizontal offset
        Vector3 offset = playerFeet - hitPoint;
        offset.y = 0f; // Keep real world Y stable

        vrRoom.transform.position += offset;

        Physics.SyncTransforms();
    }
}