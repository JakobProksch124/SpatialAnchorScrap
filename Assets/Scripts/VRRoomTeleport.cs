using UnityEngine;

/// <summary>
/// Self-contained VR teleport that moves the VR ROOM to the aimed spot (never the player rig),
/// so the spatial-anchor AR mapping is never disturbed. Push the RIGHT thumbstick forward to aim
/// a beam onto the floor; release to teleport (the room slides so the target comes to your feet).
///
/// Does NOT depend on the Interaction SDK teleport building block, and is collider-independent:
/// it intersects the aim ray with the horizontal plane at the player's foot height, so it works
/// even if the VR environment has no floor colliders. Created/destroyed by Building_TransitionCues
/// on VR enter/exit.
/// </summary>
public class VRRoomTeleport : MonoBehaviour
{
    [SerializeField] private float aimThreshold = 0.6f; // stick-forward amount to start aiming
    [SerializeField] private float maxDistance = 15f;

    private static readonly Color Valid = new(0.35f, 1f, 0.55f, 1f);
    private static readonly Color Invalid = new(1f, 0.35f, 0.35f, 1f);

    private Building_TransitionCues _building;
    private Transform _controller;
    private Transform _cam;
    private LineRenderer _beam;
    private Transform _marker;
    private Renderer _markerRenderer;
    private bool _aiming;
    private bool _hasTarget;
    private Vector3 _target;

    public void Init(Building_TransitionCues building) => _building = building;

    private void Awake()
    {
        var rig = FindAnyObjectByType<OVRCameraRig>();
        if (rig != null)
            _controller = rig.rightControllerAnchor != null ? rig.rightControllerAnchor : rig.rightHandAnchor;
        _cam = Camera.main != null ? Camera.main.transform : null;
        if (_controller == null) _controller = _cam;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

        var beamGo = new GameObject("Beam");
        beamGo.transform.SetParent(transform, false);
        _beam = beamGo.AddComponent<LineRenderer>();
        _beam.positionCount = 2;
        _beam.widthMultiplier = 0.012f;
        _beam.numCapVertices = 4;
        _beam.material = mat;
        _beam.enabled = false;

        var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(m.GetComponent<Collider>());
        m.name = "Marker";
        m.transform.SetParent(transform, false);
        m.transform.localScale = new Vector3(0.3f, 0.005f, 0.3f);
        _markerRenderer = m.GetComponent<Renderer>();
        _markerRenderer.material = new Material(mat);
        _marker = m.transform;
        m.SetActive(false);
    }

    private void Update()
    {
        if (_controller == null) return;

        var stickY = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).y; // right thumbstick forward
        if (stickY > aimThreshold)
        {
            _aiming = true;
            Aim();
        }
        else if (_aiming)
        {
            _aiming = false;
            _beam.enabled = false;
            if (_marker) _marker.gameObject.SetActive(false);
            if (_hasTarget && _building != null) _building.MoveVRRoomToHit(_target);
            _hasTarget = false;
        }
    }

    private void Aim()
    {
        var origin = _controller.position;
        var dir = _controller.forward;

        // floor height = the player's feet (rig root), the same reference MoveVRRoomToHit uses
        var floorY = (_cam != null && _cam.parent != null) ? _cam.parent.position.y
                   : (_cam != null ? _cam.position.y - 1.6f : origin.y - 1.2f);

        _hasTarget = false;
        if (dir.y < -0.02f) // aiming down toward the floor plane
        {
            var t = (floorY - origin.y) / dir.y;
            if (t > 0f && t <= maxDistance)
            {
                _target = origin + dir * t;
                _hasTarget = true;
            }
        }

        var end = _hasTarget ? _target : origin + dir * maxDistance;
        var col = _hasTarget ? Valid : Invalid;
        _beam.enabled = true;
        _beam.startColor = _beam.endColor = col;
        _beam.SetPosition(0, origin);
        _beam.SetPosition(1, end);

        if (_marker)
        {
            _marker.gameObject.SetActive(_hasTarget);
            if (_hasTarget)
            {
                _marker.position = _target + Vector3.up * 0.01f;
                _markerRenderer.material.color = col;
            }
        }
    }
}
