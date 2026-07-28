using UnityEngine;

/// <summary>
/// Controls how a world-space cue faces the user. Yaw only (stays upright).
///
/// - TrackThenLock (default): continuously turns (yaw only, smoothed by turnSpeed) to keep facing
///   the user as they move around the room, so the cue stays readable from wherever they stand.
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

    private void LateUpdate()
    {
        if (mode == FacingMode.Fixed) return;

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

        transform.rotation = turnSpeed <= 0f
            ? target
            : Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }
}
