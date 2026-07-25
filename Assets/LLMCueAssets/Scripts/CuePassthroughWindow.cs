using UnityEngine;

/// <summary>
/// Turns its RectTransform area into a "window to reality": a quad the size of the rect is
/// registered with the scene's OVRPassthroughLayer as surface-projected passthrough, so the
/// real world is drawn onto it — even while the user is inside an opaque VR room.
///
/// Used by the "Reality" answer-card kind. Needs an OVRPassthroughLayer somewhere in the
/// scene (the passthrough rig). Compositing can be finicky per project, so the depth nudge
/// (zPush) is exposed for tuning on device.
/// </summary>
[RequireComponent(typeof(RectTransform), typeof(MeshFilter), typeof(MeshRenderer))]
public class CuePassthroughWindow : MonoBehaviour
{
    [Tooltip("Push the passthrough quad slightly toward the user (local metres) so it composites in front of the panel.")]
    [SerializeField] private float zPush = 0.001f;

    private RectTransform _rt;
    private Mesh _mesh;
    private OVRPassthroughLayer _layer;
    private bool _registered;
    private Vector2 _lastSize = Vector2.negativeInfinity;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        GetComponent<MeshRenderer>().enabled = false; // passthrough is composited by the layer, not this renderer
        _mesh = new Mesh { name = "RealityWindowQuad" };
        GetComponent<MeshFilter>().sharedMesh = _mesh;
    }

    private void OnEnable()
    {
        RebuildMesh();
        Register();
    }

    private void OnDisable() => Unregister();

    private void Update()
    {
        if (_rt.rect.size != _lastSize) RebuildMesh();
    }

    private void RebuildMesh()
    {
        var r = _rt.rect;
        _lastSize = r.size;
        var z = -zPush; // local -Z faces the user for a canvas that billboards toward them
        _mesh.Clear();
        _mesh.vertices = new[]
        {
            new Vector3(r.xMin, r.yMin, z), new Vector3(r.xMin, r.yMax, z),
            new Vector3(r.xMax, r.yMax, z), new Vector3(r.xMax, r.yMin, z),
        };
        _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        _mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        _mesh.RecalculateBounds();
    }

    private void Register()
    {
        if (_registered) return;
        if (_layer == null) _layer = FindAnyObjectByType<OVRPassthroughLayer>();
        if (_layer == null) { Debug.LogWarning("[CuePassthroughWindow] No OVRPassthroughLayer in scene."); return; }
        _layer.AddSurfaceGeometry(gameObject, updateTransform: true);
        _registered = true;
    }

    private void Unregister()
    {
        if (_registered && _layer != null) _layer.RemoveSurfaceGeometry(gameObject);
        _registered = false;
    }
}
