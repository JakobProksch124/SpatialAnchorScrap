using UnityEngine;

/// <summary>Rotates a world-space UI element to smoothly face the user's head.</summary>
public class CueBillboard : MonoBehaviour
{
    [Tooltip("How quickly the cue turns to face you (higher = snappier, 0 = instant).")]
    [SerializeField] private float turnSpeed = 6f;

    private Transform _head;

    private void LateUpdate()
    {
        if (_head == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _head = cam.transform;
        }

        // yaw only: face the user horizontally and stay upright, so the cue never
        // tilts up/down with the user's head height (sitting vs standing)
        var dir = transform.position - _head.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;

        var target = Quaternion.LookRotation(dir);
        transform.rotation = turnSpeed <= 0f
            ? target
            // frame-rate independent damping toward the facing rotation
            : Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
    }
}
