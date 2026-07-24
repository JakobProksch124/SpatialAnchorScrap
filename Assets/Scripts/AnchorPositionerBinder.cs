using UnityEngine;
using Meta.XR.BuildingBlocks;
using System.Collections.Generic;
using JetBrains.Annotations;

public class AnchorPositionerBinder : MonoBehaviour
{
    [SerializeField] private Positioner positioner;
    [SerializeField] public GameObject _objectToPlace;

    // Relevant building blocks for anchor spawning
    private SpatialAnchorCoreBuildingBlock _core;
    [SerializeField] public SpatialAnchorLoaderBuildingBlock SpatialAnchorLoadBuildingBlock;
    public SpatialAnchorSpawnerBuildingBlock SpatialAnchorSpawner;
    public bool firstAnchorFound = false;

    public GameObject joystickController;
    private float _loadInterval = 0.05f;
    private float _loadTimer = 0f;

    private void Awake()
    {
        _core = FindAnyObjectByType<SpatialAnchorCoreBuildingBlock>();
        if (_core == null)
        {
            Debug.LogError("SpatialAnchorCoreBuildingBlock not found in scene.");
            enabled = false;
            return;
        }

        _core.OnAnchorCreateCompleted.AddListener(OnAnchorCreated);
        _core.OnAnchorsLoadCompleted.AddListener(OnAnchorsLoaded);
        Debug.Log("Application Identifier: " + Application.identifier);
    }
    
    private void Start()
    {

        UINotificationSystem.Instance.ShowPersistentMessage("Loading path.", true);
    }

    private void Update()
    {
        if (this.firstAnchorFound)
            return;
        _loadTimer += Time.deltaTime;
        if (_loadTimer >= _loadInterval)
        {
            _loadTimer = 0f;
            //Debug.Log("First Anchor Not Found Yet");
            if (SpatialAnchorLoadBuildingBlock != null)
            {
                SpatialAnchorLoadBuildingBlock.LoadAnchorsFromDefaultLocalStorage();
            }
            else
            {
                Debug.Log("can not automatically load anchor bcs SpatialAnchorLoadBuildingBlock reference is not set");
            }
        }
    }


    private void OnDestroy()
    {
        if (_core == null) return;

        _core.OnAnchorCreateCompleted.RemoveListener(OnAnchorCreated);
        _core.OnAnchorsLoadCompleted.RemoveListener(OnAnchorsLoaded);
    }

    private void OnAnchorCreated(
        OVRSpatialAnchor anchor,
        OVRSpatialAnchor.OperationResult result)
    {
        if (result != OVRSpatialAnchor.OperationResult.Success)
        {
            Debug.Log("OnAnchorCreated: Not successful");
            return;
        }
        this.firstAnchorFound = true;
        Debug.Log("anchor created with id: " + anchor.Uuid);
        Bind(anchor);
    }

    private void OnAnchorsLoaded(List<OVRSpatialAnchor> anchors)
    {
        if (anchors == null || anchors.Count == 0)
        {
            Debug.Log("(!) KEINE ANCHOR IN LISTE");
            return;
        }

        // Only the last loaded anchor
        Debug.Log("loaded " + anchors.Count + " anchors");
        for (int i = 0; i < anchors.Count; i++)
        {
            Debug.Log("[looping through loaded anchors] anchor loaded with uuid: " + anchors[i].Uuid);
        }
        this.firstAnchorFound = true;
        Bind(anchors[^1]);
    }

    private void Bind(OVRSpatialAnchor anchor)
    {
        if (anchor == null)
            return;
        Transform buildingTransform = anchor.transform;
        UINotificationSystem.Instance.HidePersistentMessage();

        GameObject instance = Instantiate(_objectToPlace, buildingTransform);
        Debug.Log("building instance created at " + anchor.transform.position);
        positioner.SetObjectToPosition(instance, joystickController.transform);
        Debug.Log("building instance now got positioned at: " + instance.transform.position);
    }

    public void createFirstAnchor()
    {
        if (!firstAnchorFound)
        {
            Debug.Log("Spawning first anchor");
            if (this.joystickController != null)
            {
                SpatialAnchorSpawner.SpawnSpatialAnchor(joystickController.transform.position, joystickController.transform.rotation);
            }
            else
            {
                Debug.Log("[AnchorPositionerBinder] No controller reference set.");
            }
        }
    }

    public void SetFirstAnchorFound(bool firstAnchorFound)
    {
        this.firstAnchorFound = firstAnchorFound;
        Debug.Log("allowing placement of new anchor");
    }
}