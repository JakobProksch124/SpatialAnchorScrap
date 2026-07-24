using UnityEngine;

/// <summary>
/// Turns a world-space UI element toward the user, but only within a small cone so a cue
/// mounted near a wall follows the user a little without swinging its panel into the wall.
/// Yaw only (stays upright).
///
/// The cone is measured from the FIRST toward-the-user facing captured at runtime — NOT the
/// prefab's forward — so the cue can never end up facing backwards regardless of how the
/// prefab or anchor happens to be oriented.
/// </summary>
public class CueBillboard : MonoBehaviour
{
    [Tooltip("How quickly the cue turns to face you (higher = snappier, 0 = instant).")]
    [SerializeField] private float turnSpeed = 6f;
    [Tooltip("Max degrees the cue may rotate from its initial toward-user facing (each side).")]
    [SerializeField] private float maxYawFromBase = 10f;

    private Transform _head;
    private Quaternion _baseRot;
    private bool _hasBase;

    private void LateUpdate()
    {
        if (_head == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _head = cam.transform;
        }

        var dir = transform.position - _head.position;
        dir.y = 0f; // yaw only
        if (dir.sqrMagnitude < 1e-4f) return;

        var faceUser = Quaternion.LookRotation(dir);
        // base = the first time we actually face the user, so the cone is always centred
        // on a correct (forward-facing) orientation, never the prefab's arbitrary forward.
        if (!_hasBase) { _baseRot = faceUser; _hasBase = true; }

        var clamped = Quaternion.RotateTowards(_baseRot, faceUser, maxYawFromBase);
        transform.rotation = turnSpeed <= 0f
            ? clamped
            : Quaternion.Slerp(transform.rotation, clamped, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }
}
