using UnityEngine;

public class FollowCameraUI : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera;

    [SerializeField]
    private float distance = 1.5f;   // Abstand vor der Kamera

    [SerializeField]
    private Vector3 offset;          // Optionaler Versatz

    private void Awake()
    {
        if (!targetCamera)
            targetCamera = Camera.main;
    }

    private void LateUpdate()
    {
        if (!targetCamera)
            return;

        // Position: vor der Kamera
        transform.position =
            targetCamera.transform.position +
            targetCamera.transform.forward * distance +
            offset;

        // Rotation: zur Kamera ausrichten
        transform.rotation =
            Quaternion.LookRotation(
                transform.position - targetCamera.transform.position
            );
    }
}
