using UnityEngine;

public class FollowHeadPanel : MonoBehaviour
{
    private Transform cameraTransform;
    private float distance;
    private float verticalOffset;
    private float smoothSpeed = 5f;

    public void Initialize(Transform camTransform, float dist, float vertOffset)
    {
        cameraTransform = camTransform;
        distance = dist;
        verticalOffset = vertOffset;
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 targetPosition =
            cameraTransform.position +
            cameraTransform.forward * distance +
            Vector3.up * verticalOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * smoothSpeed
        );

        transform.rotation = Quaternion.LookRotation(
            transform.position - cameraTransform.position
        );
    }
}