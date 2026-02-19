/*using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Place script directly on the Building Prefab Root
public class G64_TransitionCues : MonoBehaviour
{
    // Search for this child in the FBX model (Its blue arrow determines its facing direction)
    public string labTransformName = "cosimlabAnchor";
    Transform labTransform;

    private GameObject labRoom; // The 3D virtual room the user takes a look at
    private GameObject labEntryCue; // AR to VR cue into the lab
    private GameObject labExitCue; // VR to AR cue out of the lab

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
        labTransform = transform.Find(labTransformName);

        // If no child, use this GameObject directly
        if (labTransform == null) labTransform = transform;

        // Configuration of the entry cue
        TransitionCueConfig labEntryCueConfig = TransitionCueConfig.CreateVRConfig(
            parent: labTransform,
            onInteract: () => OnLabInteract()
        );
        labEntryCueConfig.label = "VR";
        labEntryCueConfig.primaryColor = new Color(0.3f, 0.4f, 0.8f);
        labEntryCueConfig.expandedDescription = "You can enter VR to take a look inside the driving simulator laboratory.";
        labEntryCueConfig.buttonText = "Enter VR";

        labEntryCue = TransitionCueFactory.CreateFrostedTransitionCue(labEntryCueConfig);
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

                if (labRoom == null)
                {
                    Debug.Log("DEBUG | cosimlabRoom is null - entering VR");
                    // Not in VR yet - enter VR
                    StartCoroutine(OnLabInteract());
                }
                else
                {
                    Debug.Log("DEBUG | cosimlabRoom exists - returning to AR");
                    // Already in VR - return to AR
                    StartCoroutine(ExitLab());
                }
            }
        }
    }

    IEnumerator OnLabInteract()
    {
        Debug.Log("DEBUG | Successful interaction with lab entry cue! Transitioning to VR...");

        // Disable the entry cue while in VR
        if (labEntryCue != null)
        {
            labEntryCue.SetActive(false);
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
            "EcoSimLab",
            Color.black,
            2.0f
        ));

        CreateWhiteRoom();

        // Create return cue
        GameObject returnAnchor = new GameObject("ReturnCue_Anchor");
        returnAnchor.transform.position = new Vector3(0, 1.5f, 2); // 2 meters in front, at eye level

        TransitionCueConfig labExitCueConfig = TransitionCueConfig.CreateARConfig(
            parent: returnAnchor.transform,
            onInteract: () =>
            {
                Debug.Log("Returning to AR!");
                StartCoroutine(ExitLab());
            }
        );
        labExitCueConfig.alwaysExpanded = true; // Exit cues should always be visible
        labExitCueConfig.expandedDescription = "Return to Augmented Reality mode";
        labExitCueConfig.buttonText = "Enter AR";

        GameObject exitCue = TransitionCueFactory.CreateFrostedTransitionCue(labExitCueConfig);
        labExitCue = exitCue; // Store for later cleanup
    }

    IEnumerator ExitLab()
    {
        Debug.Log("Returning to AR mode...");

        // Fade out the VR room
        yield return StartCoroutine(TransitionEffects.Instance.FadeToAR(1.5f, labRoom));

        // Destroy white room
        if (labRoom != null)
        {
            Destroy(labRoom);
        }

        // Destroy return cube
        if (labExitCue != null)
        {
            Destroy(labExitCue);
        }

        // Re-enable the entry cue when returning to AR
        if (labEntryCue != null)
        {
            labEntryCue.SetActive(true);
        }
        else
        {
            // If it was destroyed, recreate it
            // Configuration of the entry cue
            TransitionCueConfig labEntryCueConfig = TransitionCueConfig.CreateVRConfig(
                parent: labTransform,
                onInteract: () => OnLabInteract()
            );
            labEntryCueConfig.label = "VR";
            labEntryCueConfig.primaryColor = new Color(0.3f, 0.4f, 0.8f);
            labEntryCueConfig.expandedDescription = "You can enter VR to take a look inside the driving simulator laboratory.";
            labEntryCueConfig.buttonText = "Enter VR";

            labEntryCue = TransitionCueFactory.CreateFrostedTransitionCue(labEntryCueConfig);
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
                destination: "Library",
                swipeSpeed: 1.0f,
                displayDuration: 3.0f,
                yOffset: -50f
            ));
        }
    }

    // Temporarily replaces the cosimlab room with a basic white room
    void CreateWhiteRoom()
    {
        labRoom = new GameObject("cosimlabRoom");

        // Floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.transform.SetParent(labRoom.transform);
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10);
        floor.GetComponent<Renderer>().material.color = Color.white;

        // Walls
        CreateWall(labRoom.transform, new Vector3(0, 2.5f, 5), new Vector3(10, 5, 0.1f), Color.white);
        CreateWall(labRoom.transform, new Vector3(0, 2.5f, -5), new Vector3(10, 5, 0.1f), Color.red);
        CreateWall(labRoom.transform, new Vector3(5, 2.5f, 0), new Vector3(0.1f, 5, 10), Color.green);
        CreateWall(labRoom.transform, new Vector3(-5, 2.5f, 0), new Vector3(0.1f, 5, 10), Color.blue);
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