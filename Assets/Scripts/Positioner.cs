using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks;
using Oculus.Platform;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class Positioner : MonoBehaviour
{


    [Header("Start Arrival Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private Color startArrivalPrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string startArrivalLabel = "VR";
    [SerializeField] private Texture2D startArrivalScreenshotDisplayed;
    [SerializeField] private string startArrivalDescription = "Welcome to VR!";
    [SerializeField] private string startArrivalButtonText = "X";
    [SerializeField] private bool startArrivalAlwaysExpand = false;
    [SerializeField] private Transform eraseCueAnchor;
    [SerializeField] private AnchorPositionerBinder anchorPositionerBinder;
    private Transform eraseCueBaseAnchor;
    private GameObject eraseCue;




    // Debug text elements for showing the current Offset and console output
    [SerializeField] TMP_Text offsetText;
    [SerializeField] TMP_Text vrConsoleText;

    // Used for changing y-level
    [SerializeField] InputActionReference ascendButton;
    [SerializeField] InputActionReference descendButton;
    [SerializeField] InputActionReference devButton;

    // Used to change rotation mode (between x, y, and z rotation)
    [SerializeField] float positionSpeed = 0.03f;
    private bool _triggerLeftWasPressed = false;
    private bool _devButtonWasPressed = false;
    private enum RotAxis { X, Y, Z }
    private RotAxis _currentAxis = RotAxis.Y;
    private Transform cameraTransform;
    public bool inDevMode = false;

    [SerializeField] InputActionReference triggerLeft; // Used for toggling dev mode
    [SerializeField] InputActionReference _moveAction;
    [SerializeField] InputActionReference _turnAction;
    [SerializeField] InputActionReference _saveOffsetAction;

    [SerializeField] InputActionReference _deleteAnchors;

    // Used for bringing the relevant 3D model to its correct position
    [SerializeField] public GameObject _objectToPosition;
    [SerializeField] private TextAsset offsetJsonTemplate;
    public float OffsetX = 0f;
    public float OffsetY = 0f;
    public float OffsetZ = 0f;
    public float OffsetRotX = 0f;
    public float OffsetRotY = 0f;
    public float OffsetRotZ = 0f;

    // Materials used for dev mode
    [SerializeField] Material occluderMat;
    [SerializeField] Material transparencyMat;
    public GameObject PlacedObject => _objectToPosition;
    private SpatialAnchorCoreBuildingBlock _core;

    // Place where json containing translation and rotation info is stored
    private string RuntimeJsonPath =>
        Path.Combine(
            UnityEngine.Application.persistentDataPath,
            offsetJsonTemplate.name + ".json"
        );

    private void Awake()
    {
        if(eraseCueAnchor != null)
        {
            eraseCueBaseAnchor = eraseCueAnchor;
        }
        _core = FindAnyObjectByType<SpatialAnchorCoreBuildingBlock>();
        if (_core == null)
        {
            Debug.LogError("SpatialAnchorCoreBuildingBlock not found in scene.");
        }
    }

    // Determines the current mode of the application (dev mode shows more visual information)
    void ChooseVisualMode()
    {
        bool isPressed = devButton.action.IsPressed();
        if (_devButtonWasPressed && !isPressed)
        {
            inDevMode = !inDevMode;

            AdjustVisuals();
        }
        _devButtonWasPressed = isPressed;
    }

    public bool getDevMode()
    {
        return inDevMode;
    }

    // Blends in extra-information (visually)
    void AdjustVisuals()
    {
        offsetText.gameObject.SetActive(inDevMode);
        vrConsoleText.gameObject.SetActive(inDevMode);

        Material targetMat;
        if (inDevMode)
        {
            targetMat = transparencyMat;
        }
        else
        {
            targetMat = occluderMat;
        }

        // We exclude the building roots since they contain the line renderer for the arrow drawn, as well as all cues objects
        ApplyMaterialToChildren(_objectToPosition, targetMat, new List<string> { "Bib_Model_NewMesh", "G62_Model", "G64_Model", "Mensa_Model", "Arrow_3D_Icon_03 (1)", "Arrow_3D_Icon_03", "iMessageAnchor" },
            new List<string> { "ArrivalCue", "TransitionCue", "VirtualFood_Pancake", "FoodInteractionCanvas", "foodButtonAnchor1", "foodButtonAnchor2", "foodButtonAnchor3", "VirtualFood_Pizza", "VirtualFood_Sandwich", "MinimalCue_", "iMessageCollider", "MinimalButtonCue" });

    }

    public void ApplyMaterialToChildren(
        GameObject root,
        Material newMaterial,
        List<string> singleTargetsToExclude,
        List<string> groupTargetsToExclude
    )
    {
        if (root == null || newMaterial == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in renderers)
        {
            GameObject current = rend.gameObject;

            if (singleTargetsToExclude != null)
            {
                foreach (string excludedName in singleTargetsToExclude)
                {
                    if (current.name.StartsWith(excludedName))
                    {
                        goto SkipRenderer;
                    }
                }
            }

            if (groupTargetsToExclude != null)
            {
                Transform t = current.transform;

                while (t != null && t != root.transform.parent)
                {
                    foreach (string excludedRoot in groupTargetsToExclude)
                    {
                        if (t.gameObject.name.StartsWith(excludedRoot))
                        {
                            goto SkipRenderer;
                        }
                    }

                    t = t.parent;
                }
            }

            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = newMaterial;
            }

            rend.materials = mats;

        SkipRenderer:
            continue;
        }
    }

    void OnEnable()
    {
        _moveAction.action.Enable();
        _turnAction.action.Enable();
        ascendButton.action.Enable();
        descendButton.action.Enable();
        triggerLeft.action.Enable();
        _saveOffsetAction.action.Enable();
        _deleteAnchors.action.Enable();
    }

    void OnDisable()
    {
        _moveAction.action.Disable();
        _turnAction.action.Disable();
        ascendButton.action.Disable();
        descendButton.action.Disable();
        triggerLeft.action.Disable();
        _saveOffsetAction.action.Disable();
        _deleteAnchors.action.Disable();
    }

    void Start()
    {
        cameraTransform = Camera.main.transform;
        AdjustVisuals();
    }

    void Update()
    {
        // Checks if it should swap to dev mode or user mode
        ChooseVisualMode();

        // If in user mode, don't allow changes to objecttoposition
        if (!inDevMode)
            return;

        if (_objectToPosition == null)
        {
            string trackingInfo = GetTrackingDebugInfo();

            offsetText.text = trackingInfo;
            return;
        }

        if (_moveAction == null || _turnAction == null)
            return;

        if (_saveOffsetAction.action.WasReleasedThisFrame())
            SaveOffsetToJson();

        ChooseAxisMode();

        // Movement in x and z direction (horizontal / left and right)
        var moveValue = _moveAction.action.ReadValue<Vector2>();
        _objectToPosition.transform.localPosition += new Vector3(moveValue.x * positionSpeed, 0, moveValue.y * positionSpeed);

        // Rotation
        var turnValue = _turnAction.action.ReadValue<Vector2>();
        float rotAmount = turnValue.x * positionSpeed * 10f;
        Quaternion deltaRot = Quaternion.identity;

        switch (_currentAxis)
        {
            case RotAxis.X:
                deltaRot = Quaternion.Euler(rotAmount, 0f, 0f);
                break;

            case RotAxis.Y:
                deltaRot = Quaternion.Euler(0f, rotAmount, 0f);
                break;

            case RotAxis.Z:
                deltaRot = Quaternion.Euler(0f, 0f, rotAmount);
                break;
        }

        _objectToPosition.transform.localRotation *= deltaRot;


        // Movement in y direction (vertical)
        if (ascendButton.action.IsPressed())
            _objectToPosition.transform.localPosition += new Vector3(0, positionSpeed, 0);

        if (descendButton.action.IsPressed())
            _objectToPosition.transform.localPosition += new Vector3(0, -positionSpeed, 0);

        // Update Offset Text
        Vector3 p = _objectToPosition.transform.localPosition;
        Vector3 r = _objectToPosition.transform.localRotation.eulerAngles;

        // Prepare for saving internally
        OffsetX = Mathf.Round(p.x * 100) / 100;
        OffsetY = Mathf.Round(p.y * 100) / 100;
        OffsetZ = Mathf.Round(p.z * 100) / 100;

        OffsetRotX = Mathf.Round(r.x * 100) / 100;
        OffsetRotY = Mathf.Round(r.y * 100) / 100;
        OffsetRotZ = Mathf.Round(r.z * 100) / 100;

        if (offsetText != null)
        {
            offsetText.text =
                    $"X: {OffsetX}; " +
                    $"Y: {OffsetY}; " +
                    $"Z: {OffsetZ}\n" +
                    $"RotX: {OffsetRotX}; " +
                    $"RotY: {OffsetRotY}; " +
                    $"RotZ: {OffsetRotZ}\n" +
                    $"Aktive Rot-Achse: {_currentAxis}";
        }
    }



    public void CreateEraseCue()
    {
        Debug.Log("delete anchors button was pressed");
        if (_core != null && inDevMode && eraseCue ==null)
        {
            eraseCueAnchor=eraseCueBaseAnchor;
            // Position canvas in front of headset
            Camera cam = Camera.main;
            if (cam != null) 
            {
                Debug.Log("camera found");
                

                eraseCueAnchor.transform.position =
                cam.transform.position +
                cam.transform.forward * 2f; // raise panel
                Debug.Log("set erase cue position");

                eraseCueAnchor.transform.rotation =
                    Quaternion.LookRotation(
                        eraseCueAnchor.transform.position - cam.transform.position) * Quaternion.Euler(0f, 180f, 0f);
                Debug.Log("set erase cue rotation");

                eraseCueAnchor.transform.localScale = Vector3.one;
                Debug.Log("set erase cue scale");
            }
            Debug.Log("Creating erase Cue");

            // Base
            TransitionCueConfig EraseCueConfig = TransitionCueConfig.CreateARConfig(
                parent: eraseCueAnchor.transform,
                onInteract: () =>
                {
                    _core.EraseAllAnchors();
                    anchorPositionerBinder.SetFirstAnchorFound(false);
                    Destroy(eraseCue);
                },
            onClose: () =>
            {
                Destroy(eraseCue);
            },
            isStandardClose: false
            );

            // Details
            EraseCueConfig.alwaysExpanded = true;
            EraseCueConfig.primaryColor = startArrivalPrimaryColor;
            EraseCueConfig.expandedDescription = startArrivalDescription;
            EraseCueConfig.screenshotTexture = startArrivalScreenshotDisplayed;

            // (Effectively not used if alwaysExpanded)
            EraseCueConfig.label = startArrivalLabel;
            EraseCueConfig.buttonText = startArrivalButtonText;

            Debug.Log("Erase Cue Created!");
            eraseCue = TransitionCueFactory.CreateCue(EraseCueConfig);
        }
    }

    void SaveOffsetToJson()
    {
        OffsetData data = new OffsetData
        {
            SavedOffsetX = OffsetX.ToString(),
            SavedOffsetY = OffsetY.ToString(),
            SavedOffsetZ = OffsetZ.ToString(),
            SavedOffsetRotX = OffsetRotX.ToString(),
            SavedOffsetRotY = OffsetRotY.ToString(),
            SavedOffsetRotZ = OffsetRotZ.ToString()
        };
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(RuntimeJsonPath, json);
        Debug.Log($"JSON geschrieben nach: {RuntimeJsonPath}");
    }

    void ChooseAxisMode()
    {
        // This logic makes it a IsReleased instead of a IsPressed
        bool isPressed = triggerLeft.action.IsPressed();
        if (_triggerLeftWasPressed && !isPressed)
        {
            _currentAxis = (RotAxis)(((int)_currentAxis + 1) % 3);
        }
        _triggerLeftWasPressed = isPressed;
    }

    public void SetObjectToPosition(GameObject obj, Transform rightController)
    {
        _objectToPosition = obj;
        Building_TransitionCues buildingTransitionCues = _objectToPosition.GetComponent<Building_TransitionCues>();
        buildingTransitionCues.SetRightController(rightController);
        AdjustVisuals();
        //Debug.Log("[Positioner] _objectToPosition set");

        if (!File.Exists(RuntimeJsonPath))
        {
            //Debug.LogWarning("Keine Offset-JSON gefunden.");
            return;
        }

        string json = File.ReadAllText(RuntimeJsonPath);
        OffsetData data = JsonUtility.FromJson<OffsetData>(json);

        if (data == null)
        {
            //Debug.LogError("JSON konnte nicht geladen werden.");
            return;
        }

        // Sicheres Parsen
        float.TryParse(data.SavedOffsetX, out OffsetX);
        float.TryParse(data.SavedOffsetY, out OffsetY);
        float.TryParse(data.SavedOffsetZ, out OffsetZ);
        float.TryParse(data.SavedOffsetRotX, out OffsetRotX);
        float.TryParse(data.SavedOffsetRotY, out OffsetRotY);
        float.TryParse(data.SavedOffsetRotZ, out OffsetRotZ);

        // Werte direkt anwenden
        _objectToPosition.transform.localPosition = new Vector3(OffsetX, OffsetY, OffsetZ);
        _objectToPosition.transform.localRotation = Quaternion.Euler(OffsetRotX, OffsetRotY, OffsetRotZ);

        //Debug.Log("Offset aus JSON geladen und angewendet.");
    }

    private string GetTrackingDebugInfo()
    {
        UnityEngine.XR.InputDevice hmd =
            UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);

        if (!hmd.isValid)
            return "HMD Device: INVALID";

        bool isTracked = false;
        hmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out isTracked);

        Vector3 pos;
        bool hasPosition =
            hmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out pos);

        return $"HMD Valid: {hmd.isValid}\n" +
               $"Pose Tracked: {isTracked}\n" +
               $"Has Position Data: {hasPosition}";
    }
}

[System.Serializable]
public class OffsetData
{
    public string SavedOffsetX;
    public string SavedOffsetY;
    public string SavedOffsetZ;
    public string SavedOffsetRotX;
    public string SavedOffsetRotY;
    public string SavedOffsetRotZ;
    //public string SavedOffsetRotQuaternion;
}