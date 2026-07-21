using UnityEngine;

/// <summary>Rotates a world-space UI element to face the user's head.</summary>
public class CueBillboard : MonoBehaviour
{
    private Transform _head;

    private void LateUpdate()
    {
        if (_head == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _head = cam.transform;
        }

        transform.rotation = Quaternion.LookRotation(transform.position - _head.position);
    }
}
