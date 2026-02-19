/*using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Place script directly on the Building Prefab Root
public class Bib_TransitionCues : MonoBehaviour
{
    // Search for this child in the FBX model (Its blue arrow determines its facing direction)
    public string bulletinboardName = "pinnwandCue";
    Transform bulletinboardTransform;

    private GameObject bulletinboardRoom;
    private GameObject bridgeEntryCue; // AR to VR cue into the bridge
    private GameObject bridgeExitCue; // Old, may be deleted later

    // Store where the user transitioned for later recovery
    private Camera mainCamera;
    private MonoBehaviour pathGenerator; // Reference to PathGenerator script
    private LineRenderer[] pathLineRenderers; // All line renderers in PathGenerator

    void Start()
    {
        mainCamera = Camera.main;

        // If GetComponent doesn't work: Try to find it by name
        pathGenerator = GetComponent<MonoBehaviour>();
        foreach (var component in GetComponents<MonoBehaviour>())
        {
            if (component.GetType().Name == "PathGenerator")
            {
                pathGenerator = component;
                break;
            }
        }

        // Find reference point in this building
        bulletinboardTransform = transform.Find(bulletinboardName);

        // If no child, use this GameObject directly
        if (bulletinboardTransform == null) bulletinboardTransform = transform;

        // Configuration of the entry cue
        TransitionCueConfig bridgeEntryCueConfig = TransitionCueConfig.CreateVRConfig(
            parent: bulletinboardTransform,
            onInteract: () => OnBridgeInteract()
        );
        bridgeEntryCueConfig.expandedDescription = "Take a look inside the working place of the IDUX group.";
        bridgeEntryCueConfig.label = "VR";
        bridgeEntryCueConfig.primaryColor = new Color(0.3f, 0.4f, 0.8f);
        bridgeEntryCueConfig.buttonText = "Enter VR";

        bridgeEntryCue = TransitionCueFactory.CreateFrostedTransitionCue(bridgeEntryCueConfig);
    }

    void Update()
    {
        // TEMPORARY TEST, CAN RE REMOVED LATER
        // Keyboard shortcuts for testing (New Input System)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.pKey.wasPressedThisFrame || Keyboard.current.tKey.wasPressedThisFrame)
            {
                Debug.Log("DEBUG | T or P key pressed!");

                if (bulletinboardRoom == null)
                {
                    Debug.Log("DEBUG | bulletin board is null - entering VR");
                    // Not in VR yet - enter VR
                    StartCoroutine(OnBridgeInteract());
                }
                else
                {
                    Debug.Log("DEBUG | bulletin board exists - returning to AR");
                    // Already in VR - return to AR
                    StartCoroutine(ExitBridge());
                }
            }
        }
    }

    IEnumerator OnBridgeInteract()
    {
        Debug.Log("DEBUG | Successful interaction with bulletin board transition cue! Transitioning to VR...");

        // Disable the entry cue while in VR
        if (bridgeEntryCue != null)
        {
            bridgeEntryCue.SetActive(false);
        }

        // Disable PathGenerator rendering while in VR
        if (pathGenerator != null)
        {
            pathGenerator.enabled = false;

            // Get current path line renderers and disable them to hide the already drawn path
            pathLineRenderers = pathGenerator.GetComponentsInChildren<LineRenderer>();
            foreach (var lineRenderer in pathLineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled = false;
                }
            }
        }

        yield return StartCoroutine(TransitionEffects.Instance.FadeToVRWithTitle(
            "IDUX working place: The Bridge",
            Color.black,
            2.0f
        ));

        CreateWhiteRoom();

        // Create return cue using the new unified system
        GameObject returnAnchor = new GameObject("ReturnCue_Anchor");
        returnAnchor.transform.position = new Vector3(0, 1.5f, 2); // 2 meters in front, at eye level

        TransitionCueConfig bridgeExitCueConfig = TransitionCueConfig.CreateRConfig(
            parent: returnAnchor.transform,
            onInteract: () =>
            {
                Debug.Log("Returning to AR!");
                StartCoroutine(ExitBridge());
            }
        );
        bridgeExitCueConfig.alwaysExpanded = true; // Exit cues should always be visible
        bridgeExitCueConfig.expandedDescription = "Return to the bulletin board.";
        bridgeExitCueConfig.buttonText = "Enter AR";

        GameObject exitCue = TransitionCueFactory.CreateFrostedTransitionCue(bridgeExitCueConfig);
        bridgeExitCue = exitCue; // Store for later cleanup
    }

    IEnumerator ExitBridge()
    {
        Debug.Log("Returning to AR mode...");

        // Fade out the VR room
        yield return StartCoroutine(TransitionEffects.Instance.FadeToAR(1.5f, bulletinboardRoom));

        // Destroy white room
        if (bulletinboardRoom != null)
        {
            Destroy(bulletinboardRoom);
        }

        // Destroy return cube
        if (bridgeExitCue != null)
        {
            Destroy(bridgeExitCue);
        }

        // Re-enable the entry cue when returning to AR
        if (bridgeEntryCue != null)
        {
            bridgeEntryCue.SetActive(true);
        }
        else
        {
            // Configuration of the entry cue
            TransitionCueConfig bridgeEntryCueConfig = TransitionCueConfig.CreateVRConfig(
                parent: bulletinboardTransform,
                onInteract: () => OnBridgeInteract()
            );
            bridgeEntryCueConfig.expandedDescription = "Take a look inside the working place of the IDUX group.";
            bridgeEntryCueConfig.label = "VR";
            bridgeEntryCueConfig.primaryColor = new Color(0.3f, 0.4f, 0.8f);
            bridgeEntryCueConfig.buttonText = "Enter VR";

            bridgeEntryCue = TransitionCueFactory.CreateFrostedTransitionCue(bridgeEntryCueConfig);
        }

        // Re-enable PathGenerator when returning to AR
        if (pathGenerator != null)
        {
            pathGenerator.enabled = true;

            // Re-enable the stored path line renderers to show the path again
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
                destination: "Cafeteria",
                swipeSpeed: 1.0f,
                displayDuration: 3.0f,
                yOffset: -50f
            ));
        }
    }

    // Temporarily replaces the bulletin board vr room with a basic white room
    void CreateWhiteRoom()
    {
        bulletinboardRoom = new GameObject("bulletinBoardRoom");

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.SetParent(bulletinboardRoom.transform);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10);
        floor.GetComponent<Renderer>().material.color = Color.white;

        // Walls
        CreateWall(bulletinboardRoom.transform, new Vector3(0, 2.5f, 5), new Vector3(10, 5, 0.1f), Color.white);
        CreateWall(bulletinboardRoom.transform, new Vector3(0, 2.5f, -5), new Vector3(10, 5, 0.1f), Color.red);
        CreateWall(bulletinboardRoom.transform, new Vector3(5, 2.5f, 0), new Vector3(0.1f, 5, 10), Color.green);
        CreateWall(bulletinboardRoom.transform, new Vector3(-5, 2.5f, 0), new Vector3(0.1f, 5, 10), Color.blue);
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
    */