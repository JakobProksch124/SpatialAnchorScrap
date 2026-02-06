using UnityEngine;
using Meta.XR.BuildingBlocks;
using System.Collections.Generic;

using System;
using System.IO;
using System.Threading.Tasks;

public class AnchorPositionerBinder : MonoBehaviour
{
    [SerializeField] private Positioner positioner;

    private SpatialAnchorCoreBuildingBlock _core;

    [SerializeField] public GameObject _objectToPlace;

    [SerializeField] private TextAsset anchorJsonTemplate;

    private string RuntimeJsonPath =>
        Path.Combine(
            Application.persistentDataPath,
            anchorJsonTemplate.name + ".json"
        );

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
        /*if (result != OVRSpatialAnchor.OperationResult.Success)
            Debug.LogError("Anchor creation failed");
            return;

        while (!anchor.Created)
            await Task.Yield();

        bool saved = await anchor.SaveAnchorAsync();
        if (!saved)
        {
            Debug.LogError("Anchor konnte nicht gespeichert werden");
            return;
        }

        AnchorData data = new AnchorData
        {
            uuid = anchor.Uuid.ToString()
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(RuntimeJsonPath, json);
        Debug.Log($"Anchor gespeichert: {data.uuid}");
        Debug.Log($"JSON geschrieben nach: {RuntimeJsonPath}");*/

        Debug.Log(anchor);
        Debug.Log(anchor.transform);
        Debug.Log(anchor.Uuid);

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

[System.Serializable]
public class AnchorData
{
    public string uuid;
}
