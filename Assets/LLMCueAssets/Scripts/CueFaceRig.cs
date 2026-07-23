using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The cue's face: two white capsule eyes and a talking mouth, rendered as UI
/// images floating in front of the 3D head. States per approved design tokens:
///   Idle/Available  calm capsules + blink
///   Listening       taller attentive capsules, gentle sway
///   Thinking        low capsules scanning side-to-side ("scan" variant)
///   Speaking        open eyes + mouth whose height follows the live TTS level
/// All dimensions derive from headSizePx so the face scales with the head.
/// </summary>
public class CueFaceRig : MonoBehaviour
{
    [SerializeField] private RectTransform leftEye;
    [SerializeField] private RectTransform rightEye;
    [SerializeField] private RectTransform mouth;
    [SerializeField] private CueHeadVisuals head;

    [Header("Design tokens (px at headSize=24)")]
    [SerializeField] private float headSizePx = 24f;
    [SerializeField] private float eyeGapPx = 6.5f;
    [SerializeField] private float blinkPeriod = 4.5f;
    [SerializeField] private float talkFallbackSpeed = 0.9f;
    [Tooltip("How far the eyes drift toward the user, as a fraction of head size.")]
    [SerializeField] private float gazeAmount = 0.09f;

    private Transform _cam;

    private const float PillBorder = 81f; // must match the generated pill sprite border

    private CueVisualState _state = CueVisualState.Idle;
    private float _blinkTimer;
    private float _blinkScale = 1f;
    private Vector2 _eyeSize, _eyeOffset;
    private float _mouthH;
    private bool _mouthVisible;
    private Image _leftImg, _rightImg, _mouthImg;

    private void Awake()
    {
        if (leftEye) _leftImg = leftEye.GetComponent<Image>();
        if (rightEye) _rightImg = rightEye.GetComponent<Image>();
        if (mouth) _mouthImg = mouth.GetComponent<Image>();
    }

    /// <summary>Keeps a sliced pill sprite fully capsule-round at any element size.</summary>
    private static void Round(Image img, Vector2 size)
    {
        if (!img || !img.sprite) return;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = PillBorder / Mathf.Max(1f, Mathf.Min(size.x, size.y) * 0.5f);
    }

    public void SetState(CueVisualState state)
    {
        _state = state;
    }

    private void Update()
    {
        var em = headSizePx; // design used em units relative to head size
        var t = Time.time;

        // blink (calm + speaking states)
        _blinkTimer += Time.deltaTime;
        var blinking = _blinkTimer % blinkPeriod > blinkPeriod - 0.13f
                       && _state is CueVisualState.Idle or CueVisualState.Available or CueVisualState.Speaking;
        _blinkScale = Mathf.Lerp(_blinkScale, blinking ? 0.1f : 1f, Time.deltaTime * 30f);

        Vector2 size;
        var xShift = 0f;
        var mouthTarget = 0f;
        switch (_state)
        {
            case CueVisualState.Listening:
                size = new Vector2(0.17f, 0.42f) * em;
                xShift = Mathf.Sin(t * 2.6f) * 0.085f * em;
                break;
            case CueVisualState.Thinking: // scan variant
                size = new Vector2(0.15f, 0.20f) * em;
                xShift = Mathf.Sin(t * 3.7f) * 0.1f * em;
                break;
            case CueVisualState.Speaking:
                size = new Vector2(0.14f, 0.28f) * em;
                var level = head ? head.AudioLevel : 0f;
                // fallback chatter keeps the mouth alive between clips
                var fallback = (0.5f + 0.5f * Mathf.Sin(t * (2f * Mathf.PI / talkFallbackSpeed))) * 0.25f;
                mouthTarget = Mathf.Lerp(0.07f, 0.23f, Mathf.Max(level * 1.6f, fallback)) * em;
                break;
            default:
                size = new Vector2(0.14f, 0.31f) * em;
                break;
        }

        // gaze: eyes drift toward the user. Since the panel billboards to face the
        // user horizontally, this is mostly a vertical look (up when you're standing,
        // level when you're lower). Damped, and eased off while thinking/listening.
        var gaze = Vector2.zero;
        if (!_cam) { var c = Camera.main; if (c) _cam = c.transform; }
        if (_cam)
        {
            var local = transform.InverseTransformDirection((_cam.position - transform.position).normalized);
            gaze = new Vector2(Mathf.Clamp(local.x, -1f, 1f), Mathf.Clamp(local.y, -1f, 1f)) * (gazeAmount * em);
        }
        var gazeWeight = _state switch
        {
            CueVisualState.Thinking => 0f,   // scanning has its own motion
            CueVisualState.Listening => 0.5f,
            _ => 1f
        };

        var baseY = _state == CueVisualState.Thinking ? 0.04f * em : 0f;
        _eyeSize = Vector2.Lerp(_eyeSize, size, Time.deltaTime * 10f);
        _eyeOffset = Vector2.Lerp(_eyeOffset,
            new Vector2(xShift + gaze.x * gazeWeight, baseY + gaze.y * gazeWeight), Time.deltaTime * 10f);

        var half = (eyeGapPx / 24f * em + _eyeSize.x) * 0.5f;
        ApplyEye(leftEye, -half);
        ApplyEye(rightEye, half);

        _mouthVisible = _state == CueVisualState.Speaking;
        _mouthH = Mathf.Lerp(_mouthH, mouthTarget, Time.deltaTime * 22f);
        if (mouth)
        {
            mouth.gameObject.SetActive(_mouthVisible);
            if (_mouthVisible)
            {
                mouth.sizeDelta = new Vector2(0.22f * em, Mathf.Max(0.05f * em, _mouthH));
                mouth.anchoredPosition = new Vector2(0f, -(_eyeSize.y * 0.5f + 0.14f * em));
                Round(_mouthImg, mouth.sizeDelta);
            }
        }
    }

    private void ApplyEye(RectTransform eye, float x)
    {
        if (!eye) return;
        eye.sizeDelta = _eyeSize;
        eye.anchoredPosition = new Vector2(x + _eyeOffset.x, _eyeOffset.y + (_mouthVisible ? 0.06f * headSizePx : 0f));
        eye.localScale = new Vector3(1f, _blinkScale, 1f);
        Round(eye == leftEye ? _leftImg : _rightImg, _eyeSize);
    }
}
