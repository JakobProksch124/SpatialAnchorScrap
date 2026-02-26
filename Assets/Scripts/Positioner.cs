using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class Positioner : MonoBehaviour
{
    // Offset Text Element
    [SerializeField] TMP_Text offsetText;

    [SerializeField] TMP_Text vrConsoleText;

    [SerializeField] float positionSpeed = 0.03f;

    // Used for changing y-level
    [SerializeField] InputActionReference ascendButton;
    [SerializeField] InputActionReference descendButton;
    [SerializeField] InputActionReference devButton;

    // Used to change rotation mode (between x, y, and z rotation)
    [SerializeField] InputActionReference triggerLeft;
    private bool _triggerLeftWasPressed = false;
    private bool _devButtonWasPressed = false;
    private enum RotAxis { X, Y, Z }
    private RotAxis _currentAxis = RotAxis.Y;
    private Transform cameraTransform;

    [SerializeField] InputActionReference _moveAction;
    [SerializeField] InputActionReference _turnAction;
    [SerializeField] InputActionReference _saveOffsetAction;

    [SerializeField] public GameObject _objectToPosition;
    [SerializeField] private TextAsset offsetJsonTemplate;
    public float OffsetX = 0f;
    public float OffsetY = 0f;
    public float OffsetZ = 0f;
    public float OffsetRotX = 0f;
    public float OffsetRotY = 0f;
    public float OffsetRotZ = 0f;
    public bool inDevMode = true;
    //public Quaternion OffsetRotQuaternion;

    [SerializeField] Material occluderMat;
    [SerializeField] Material transparencyMat;

    public GameObject PlacedObject => _objectToPosition;

    private string RuntimeJsonPath =>
        Path.Combine(
            Application.persistentDataPath,
            offsetJsonTemplate.name + ".json"
        );

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

    void AdjustVisuals()
    {
        offsetText.gameObject.SetActive(inDevMode);
        vrConsoleText.gameObject.SetActive(inDevMode);

        Material targetMat;
        if (inDevMode)
        {
            targetMat = transparencyMat;
        } else
        {
            targetMat = occluderMat;
        }

        // We exclude the building roots since they contain the line renderer for the arrow drawn, as well as all cues objects
        ApplyMaterialToChildren(_objectToPosition, targetMat, new List<string> { "Bib_Model_NewMesh", "G62_Model", "G64_Model", "Mensa_Model", }, new List<string> {"ArrivalCue", "TransitionCue", "VirtualFood_Pancake" });
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
    }

    void OnDisable()
    {
        _moveAction.action.Disable();
        _turnAction.action.Disable();
        ascendButton.action.Disable();
        descendButton.action.Disable();
        triggerLeft.action.Disable();
        _saveOffsetAction.action.Disable();
    }

    void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Checks if it should swap to dev mode or user mode
        ChooseVisualMode();

        // if in user mode, don't allow changes to objecttoposition
        if (!inDevMode)
            return;
        
        if (_objectToPosition == null)
            return;

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

        // PREPARE FOR SAVING INTERNALLY
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
                    //$"RotQuaternion: {OffsetRotQuaternion}\n" +
                    $"RotX: {OffsetRotX}; " +
                    $"RotY: {OffsetRotY}; " +
                    $"RotZ: {OffsetRotZ}\n" + 
                    $"Aktive Rot-Achse: {_currentAxis}";
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
            //SavedOffsetRotQuaternion = OffsetRotQuaternion.ToString()
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

    public void SetObjectToPosition(GameObject obj)
    {
        _objectToPosition = obj;
        Debug.Log("[Positioner] _objectToPosition set");

        if (!File.Exists(RuntimeJsonPath))
        {
            Debug.LogWarning("Keine Offset-JSON gefunden.");
            return;
        }

        string json = File.ReadAllText(RuntimeJsonPath);
        OffsetData data = JsonUtility.FromJson<OffsetData>(json);

        if (data == null)
        {
            Debug.LogError("JSON konnte nicht geladen werden.");
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

        Debug.Log("Offset aus JSON geladen und angewendet.");
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