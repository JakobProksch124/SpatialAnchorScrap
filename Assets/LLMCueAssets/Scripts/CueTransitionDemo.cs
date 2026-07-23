using UnityEngine;

/// <summary>
/// Demo listener for <see cref="CueEvents.onStartTransition"/>: spawns a large blue
/// square in front of the user, so the whole flow can be verified end-to-end —
/// "start the transition" (voice or Enter button) → cue fades away → this fires →
/// blue square appears. In the real project you replace this with the actual
/// transition technique (or just drop your own function onto the same event).
/// </summary>
public class CueTransitionDemo : MonoBehaviour
{
    public void SpawnBlueSquare()
    {
        var cam = Camera.main;
        var origin = cam ? cam.transform.position : Vector3.zero;
        var fwd = cam ? cam.transform.forward : Vector3.forward;
        var flat = new Vector3(fwd.x, 0f, fwd.z).normalized; // straight ahead, level

        var square = GameObject.CreatePrimitive(PrimitiveType.Cube);
        square.name = "TransitionBlueSquare";
        square.transform.position = origin + flat * 2.5f + Vector3.up * 0f;
        square.transform.rotation = Quaternion.LookRotation(flat);
        square.transform.localScale = new Vector3(3f, 3f, 0.15f); // huge flat square slab

        square.GetComponent<Renderer>().material =
            new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.1f, 0.45f, 1f) };
    }
}
