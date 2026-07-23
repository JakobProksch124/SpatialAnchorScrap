using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the EntryCue/RoundedRect shader on a RawImage: crisp rounded panel
/// with fill gradient + inner border, correct at any (dynamic) size. Feeds the
/// live rect size to the material so corners stay a fixed pixel radius.
/// Replaces the fragile sprite/9-slice panels.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class RoundedRectUI : MonoBehaviour
{
    public float radius = 16f;
    public float borderWidth = 0f;
    public float softness = 1.6f;
    public Color fillTop = new(0.1f, 0.1f, 0.12f, 0.92f);
    public Color fillBottom = new(0.06f, 0.06f, 0.08f, 0.95f);
    public Color borderColor = new(1, 1, 1, 0);

    private static readonly int TopId = Shader.PropertyToID("_ColorTop");
    private static readonly int BottomId = Shader.PropertyToID("_ColorBottom");
    private static readonly int BorderColId = Shader.PropertyToID("_BorderColor");
    private static readonly int SizeId = Shader.PropertyToID("_Size");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int BorderId = Shader.PropertyToID("_Border");
    private static readonly int SoftId = Shader.PropertyToID("_Softness");

    private RawImage _img;
    private RectTransform _rt;
    private Material _mat;
    private Vector2 _lastSize;

    private void Awake() => EnsureSetup();

    private void OnEnable() { EnsureSetup(); Refresh(); }

    private void EnsureSetup()
    {
        if (_mat) return;
        _rt = (RectTransform)transform;
        _img = GetComponent<RawImage>();
        _img.texture = null;
        _img.color = Color.white;
        var sh = Shader.Find("EntryCue/RoundedRect");
        _mat = new Material(sh) { name = "RoundedRectMat" };
        _img.material = _mat;
    }

    /// <summary>Push all styling to the material (call after changing fields at runtime).</summary>
    public void Refresh()
    {
        EnsureSetup();
        _mat.SetColor(TopId, fillTop);
        _mat.SetColor(BottomId, fillBottom);
        _mat.SetColor(BorderColId, borderColor);
        _mat.SetFloat(RadiusId, radius);
        _mat.SetFloat(BorderId, borderWidth);
        _mat.SetFloat(SoftId, softness);
        PushSize();
    }

    private void PushSize()
    {
        var s = _rt.rect.size;
        if (s == _lastSize) return;
        _lastSize = s;
        _mat.SetVector(SizeId, new Vector4(s.x, s.y, 0, 0));
    }

    private void LateUpdate()
    {
        if (_mat) PushSize(); // tracks ContentSizeFitter / layout-driven resizes
    }
}
