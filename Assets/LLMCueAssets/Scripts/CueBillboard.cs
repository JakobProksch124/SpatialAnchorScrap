using UnityEngine;

/// <summary>
/// Controls how a world-space cue faces the user. Yaw only (stays upright).
///
/// - TrackThenLock (default): turns to face the user as they approach, then FREEZES once the
///   user is within lockDistance — so it's readable on arrival and then stays put (no swinging
///   into walls, no re-orienting while you read).
/// - FaceUser: keeps following the user, but only within a ±maxYawFromBase cone of the first
///   toward-user facing.
/// - Fixed: never rotates — keeps its placed orientation (orient the anchor deliberately).
/// </summary>
public class CueBillboard : MonoBehaviour
{
    public enum FacingMode { TrackThenLock, FaceUser, Fixed }

    [SerializeField] private FacingMode mode = FacingMode.TrackThenLock;
    [Tooltip("How quickly the cue turns to face you (higher = snappier, 0 = instant).")]
    [SerializeField] private float turnSpeed = 6f;
    [Tooltip("TrackThenLock: freeze the facing once the user is this close (metres).")]
    [SerializeField] private float lockDistance = 1.5f;
    [Tooltip("FaceUser: max degrees from the first toward-user facing (each side).")]
    [SerializeField] private float maxYawFromBase = 10f;

    private Transform _head;
    private Quaternion _baseRot;
    private bool _hasBase;
    private bool _locked;

    private void LateUpdate()
    {
        if (mode == FacingMode.Fixed) return;
        if (mode == FacingMode.TrackThenLock && _locked) return;

        if (_head == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _head = cam.transform;
        }

        var dir = transform.position - _head.position;
        dir.y = 0f; // yaw only
        var flatDist = dir.magnitude;
        if (flatDist < 1e-2f) return;

        var faceUser = Quaternion.LookRotation(dir);
        Quaternion target;

        if (mode == FacingMode.FaceUser)
        {
            // cone centred on the first correct toward-user facing (never the prefab forward)
            if (!_hasBase) { _baseRot = faceUser; _hasBase = true; }
            target = Quaternion.RotateTowards(_baseRot, faceUser, maxYawFromBase);
        }
        else // TrackThenLock
        {
            target = faceUser;
        }

        // lock once the user is close enough to read it — SNAP to fully face the user first,
        // so a cue that spawns while you're already close (e.g. an arrival cue right after you
        // enter/teleport) ends up facing you instead of freezing mid-turn.
        if (mode == FacingMode.TrackThenLock && flatDist <= lockDistance)
        {
            transform.rotation = faceUser;
            _locked = true;
            return;
        }

        transform.rotation = turnSpeed <= 0f
            ? target
            : Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }
}
