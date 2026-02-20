using UnityEngine;
using Meta.XR.BuildingBlocks;
using System.Collections.Generic;

public class AnchorPositionerBinder : MonoBehaviour
{
    [SerializeField] private Positioner positioner;

    private SpatialAnchorCoreBuildingBlock _core;
    [SerializeField] public GameObject _objectToPlace;

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
            return;

        Bind(anchor);
    }

    private void OnAnchorsLoaded(List<OVRSpatialAnchor> anchors)
    {
        if (anchors == null || anchors.Count == 0)
            return;

        // Only the last loaded anchor (your requirement)
        Bind(anchors[^1]);
    }

    private void Bind(OVRSpatialAnchor anchor)
    {
        if (anchor == null)
            return;

        Debug.Log("binding anchor root");
        GameObject instance = Instantiate(_objectToPlace, anchor.transform);
        positioner.SetObjectToPosition(instance);
    }

}
