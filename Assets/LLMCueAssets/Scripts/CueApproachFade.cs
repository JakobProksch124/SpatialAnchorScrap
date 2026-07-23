using UnityEngine;

/// <summary>
/// Materialize-on-approach: the whole cue grows from a small distant size to full
/// as the user comes within range, instead of popping on at the wake radius.
/// Driven by the proximity sensor's live distance; purely visual — the state
/// machine is unchanged. Self-finds the sensor if not assigned.
/// </summary>
public class CueApproachFade : MonoBehaviour
{
    [SerializeField] private CueProximitySensor proximity;
    [Tooltip("Metres over which the cue grows in, ending at the wake radius.")]
    [SerializeField] private float fadeBand = 1.2f;
    [Tooltip("Scale when far away (0..1 of full size). Kept high so the cue is always clearly visible.")]
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float smooth = 6f;

    private Transform _head;
    private Vector3 _baseScale;
    private float _k;

    private void Awake()
    {
        _baseScale = transform.localScale;
        if (!proximity) proximity = GetComponent<CueProximitySensor>();
        _k = 1f; // assume near until proven far, avoids a first-frame pop-in
    }

    private void LateUpdate()
    {
        if (!proximity) return;
        if (!_head) { var c = Camera.main; if (!c) return; _head = c.transform; }

        var d = Vector3.Distance(_head.position, transform.position);
        var outer = proximity.OuterRadius;
        var target = Mathf.InverseLerp(outer + fadeBand, outer, d); // 0 far -> 1 near
        _k = Mathf.Lerp(_k, target, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        transform.localScale = _baseScale * Mathf.Lerp(minScale, 1f, _k);
    }
}
