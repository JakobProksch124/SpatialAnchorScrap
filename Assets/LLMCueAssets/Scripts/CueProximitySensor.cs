using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Distance-based wake sensor: outer radius = cue becomes available (wakes),
/// inner radius = auto-listen trigger. Hysteresis prevents flicker at the edges.
/// </summary>
public class CueProximitySensor : MonoBehaviour
{
    [SerializeField] private float outerRadius = 2.5f;
    [SerializeField] private float innerRadius = 1.2f;
    [SerializeField] private float hysteresis = 0.25f;

    public UnityEvent onEnterOuter = new();
    public UnityEvent onExitOuter = new();
    public UnityEvent onEnterInner = new();

    public bool InOuter { get; private set; }
    public bool InInner { get; private set; }

    private Transform _head;

    private void Update()
    {
        if (_head == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            _head = cam.transform;
        }

        var d = Vector3.Distance(_head.position, transform.position);

        if (!InOuter && d <= outerRadius)
        {
            InOuter = true;
            onEnterOuter.Invoke();
        }
        else if (InOuter && d > outerRadius + hysteresis)
        {
            InOuter = false;
            InInner = false;
            onExitOuter.Invoke();
        }

        if (InOuter && !InInner && d <= innerRadius)
        {
            InInner = true;
            onEnterInner.Invoke();
        }
        else if (InInner && d > innerRadius + hysteresis)
        {
            InInner = false;
        }
    }
}
