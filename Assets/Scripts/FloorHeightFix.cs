using UnityEngine;

public class FloorHeightFix : MonoBehaviour
{
    public float floorOffset = -0.4f;
    public Transform vrRoot;

    void Start()
    {
        if (vrRoot == null)
            return;
        /*var trackingSpace = transform.Find("TrackingSpace");

        // Measure current head height
        float headHeight = trackingSpace.Find("CenterEyeAnchor").localPosition.y;

        // Shift tracking space downward so head height becomes correct
        floorOffset = headHeight;

        trackingSpace.localPosition = new Vector3(
            0,
            floorOffset,
            0
        );*/
        vrRoot.localPosition += new Vector3(
            0,
            floorOffset,
            0
        );
    }
}