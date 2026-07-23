using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One answer card in the row. Enters with a smooth width-grow + fade
/// (layout-driven, so the row expands smoothly and existing cards never jump)
/// and leaves with the reverse before destroying itself. Identified by its
/// config id (any user-defined card).
/// </summary>
public class CueAnswerCard : MonoBehaviour
{
    private RectTransform _rt;
    private CanvasGroup _group;
    private LayoutElement _le;
    private float _fullWidth;
    private float _t;
    private bool _closing;
    private float _growSeconds = 0.45f;

    public string Id { get; private set; }

    public void Init(string id, RectTransform rt, CanvasGroup group, LayoutElement le, float fullWidth, float growSeconds)
    {
        Id = id;
        _rt = rt;
        _group = group;
        _le = le;
        _fullWidth = fullWidth;
        _growSeconds = Mathf.Max(0.05f, growSeconds);
        _t = 0f;
        _group.alpha = 0f;
        _le.preferredWidth = 0f;
    }

    /// <summary>Animates the card closed, then destroys it.</summary>
    public void Close() => _closing = true;

    private void Update()
    {
        if (_closing)
        {
            _t = Mathf.Max(0f, _t - Time.deltaTime / _growSeconds);
            Apply();
            if (_t <= 0f) Destroy(gameObject);
            return;
        }

        if (_t >= 1f) return;
        _t = Mathf.Min(1f, _t + Time.deltaTime / _growSeconds);
        Apply();
    }

    private void Apply()
    {
        // open with a gentle spring (slight overshoot); close smoothly (no overshoot)
        var e = _closing ? 1f - Mathf.Pow(1f - _t, 3f) : EaseOutBack(_t);
        if (_group) _group.alpha = Mathf.Clamp01(e);
        if (_le) _le.preferredWidth = _fullWidth * e;
    }

    // ease-out-back with a soft overshoot (~3–4%); s below the standard 1.70158 keeps it gentle
    private static float EaseOutBack(float t)
    {
        const float s = 1.1f;
        t -= 1f;
        return 1f + (s + 1f) * t * t * t + s * t * t;
    }
}
