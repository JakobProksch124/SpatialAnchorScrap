using UnityEngine;

/// <summary>
/// Turns a world-space UI element toward the user, but only within a small cone of its
/// placed ("mounted") facing — so a cue mounted near a wall can't swing its panel into
/// the wall. Yaw only (stays upright).
/// </summary>
public class CueBillboard : MonoBehaviour
{
    [Tooltip("How quickly the cue turns to face you (higher = snappier, 0 = instant).")]
    [SerializeField] private float turnSpeed = 6f;
    [Tooltip("Max degrees the cue may rotate from its placed facing toward the user (each side).")]
    [SerializeField] private float maxYawFromBase = 5f;

    private Transform _head;
    private Quaternion _baseRot;
    private bool _hasBase;

    private void Awake() => CaptureBase();

    // The placed facing, flattened to yaw only.
    private void CaptureBase()
    {
        var f = transform.forward; f.y = 0f;
        _baseRot = f.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(f) : transform.rotation;
        _hasBase = true;
    }

    private void LateUpdate()
    {
        if (_head == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _head = cam.transform;
        }
        if (!_hasBase) CaptureBase();

        var dir = transform.position - _head.position;
        dir.y = 0f; // yaw only
        if (dir.sqrMagnitude < 1e-4f) return;

        var faceUser = Quaternion.LookRotation(dir);
        // never rotate more than maxYawFromBase away from the placed facing
        var clamped = Quaternion.RotateTowards(_baseRot, faceUser, maxYawFromBase);

        transform.rotation = turnSpeed <= 0f
            ? clamped
            : Quaternion.Slerp(transform.rotation, clamped, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }
}
