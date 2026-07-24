using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// The assistant dock bar hovering above the invitation card: collapses to a
/// head-only pill when idle, expands with status text when engaged. Handles
/// per-state background/border color, the typewriter transcript, thinking dots,
/// listening halo rings, and the idle float loop. Values from the design handoff.
/// </summary>
public class CueDockUI : MonoBehaviour
{
    [SerializeField] private RectTransform dockRect;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text status;
    [SerializeField] private RectTransform textGroup;
    [SerializeField] private GameObject thinkDots;
    [SerializeField] private GameObject leadingDot;
    [SerializeField] private Image[] dots;
    [SerializeField] private Image[] haloRings;
    [Tooltip("Drives the accent colour of the dock background + listening indicator " +
             "(blue for entry, green for arrival). Self-found from the cue root if left empty.")]
    [SerializeField] private CueTheme theme;

    [Header("Design tokens")]
    [SerializeField] private float collapsedWidth = 60f;
    [SerializeField] private float expandedWidth = 290f;
    [SerializeField] private float expandSeconds = 0.45f;
    [SerializeField] private float floatAmplitude = 3f;
    [SerializeField] private float haloPeriod = 1.6f;
    [SerializeField] private float typeCharSeconds = 0.035f;

    private static readonly Color IdleBg = new(20/255f, 26/255f, 42/255f, 0.62f);
    private static readonly Color ActiveBg = new(30/255f, 46/255f, 78/255f, 0.85f);
    private static readonly Color DimText = new(226/255f, 232/255f, 255/255f, 0.55f);
    private static readonly Color BrightText = Color.white;
    private static readonly Color AnswerText = new(226/255f, 232/255f, 255/255f, 0.92f);

    private CueVisualState _state = CueVisualState.Idle;
    private string _waiting = "Waiting for your question";
    private string _listening = "Listening…";
    private string _thinking = "Gathering answers";
    private Coroutine _typing;

    /// <summary>Set the status-box strings from CueConfig.</summary>
    public void SetTexts(string waiting, string listening, string thinking)
    {
        _waiting = waiting; _listening = listening; _thinking = thinking;
    }
    private CanvasGroup _group;
    private LayoutElement _layoutElement;
    private CueTextReveal _reveal;

    private void Awake()
    {
        if (dockRect)
        {
            _group = dockRect.GetComponent<CanvasGroup>();
            if (!_group) _group = dockRect.gameObject.AddComponent<CanvasGroup>();
            _layoutElement = dockRect.GetComponent<LayoutElement>();
        }

        // self-attach the streaming-text fade to the status label (no scene wiring)
        if (status)
        {
            _reveal = status.GetComponent<CueTextReveal>();
            if (!_reveal) _reveal = status.gameObject.AddComponent<CueTextReveal>();
        }

        // accent (blue=entry / green=arrival) drives the dock bg + listening indicator
        if (!theme) theme = GetComponentInParent<CueTheme>();
        if (theme) theme.Changed += OnThemeChanged;
        ApplyAccent();

        ApplyVisibility(false);
    }

    private void OnDestroy()
    {
        if (theme) theme.Changed -= OnThemeChanged;
    }

    private void OnThemeChanged(CueTheme t) => ApplyAccent();

    /// <summary>Recolour the dock background + listening halo/dots to the theme accent.</summary>
    private void ApplyAccent()
    {
        var accent = theme ? theme.Accent : new Color(0.31f, 0.66f, 1f); // fallback: entry blue
        // dark, low-key accent tint for the pill background (keeps text readable)
        if (background)
            background.color = new Color(
                Mathf.Lerp(accent.r, 0.02f, 0.80f),
                Mathf.Lerp(accent.g, 0.03f, 0.80f),
                Mathf.Lerp(accent.b, 0.05f, 0.80f), 0.86f);
        if (leadingDot && leadingDot.TryGetComponent<Image>(out var ld))
            ld.color = new Color(accent.r, accent.g, accent.b, ld.color.a);
        if (dots != null)
            foreach (var d in dots) if (d) d.color = new Color(accent.r, accent.g, accent.b, d.color.a);
        if (haloRings != null)
            foreach (var r in haloRings) if (r) r.color = new Color(accent.r, accent.g, accent.b, r.color.a);

        // the pill's OUTLINE is a RoundedRectUI border (was hard-coded blue) — recolour to accent
        var box = background ? background.GetComponent<RoundedRectUI>() : null;
        if (box == null && dockRect) box = dockRect.GetComponent<RoundedRectUI>();
        if (box == null && dockRect) box = dockRect.GetComponentInChildren<RoundedRectUI>(true);
        if (box != null)
        {
            box.borderColor = new Color(accent.r, accent.g, accent.b, box.borderColor.a);
            box.Refresh();
        }
    }

    // Pill shows only while listening or thinking; otherwise it fades out AND
    // drops out of the vertical stack (ignoreLayout) so no gap remains.
    private void ApplyVisibility(bool visible)
    {
        if (_group) _group.alpha = visible ? 1f : 0f;
        if (_layoutElement) _layoutElement.ignoreLayout = !visible;
    }

    public void SetPhase(CueVisualState state)
    {
        _state = state;
        // Box is visible whenever engaged (near/listening/thinking/answering); hidden only when far.
        ApplyVisibility(state is CueVisualState.Available or CueVisualState.Listening
            or CueVisualState.Thinking or CueVisualState.Speaking);
        if (leadingDot) leadingDot.SetActive(state is CueVisualState.Available or CueVisualState.Listening);
        switch (state)
        {
            case CueVisualState.Available: // "near" — already waiting for input
                SetText(_waiting, DimText);
                break;
            case CueVisualState.Listening:
                SetText(_listening, BrightText);
                break;
            case CueVisualState.Thinking:
                SetText(_thinking, DimText);
                break;
            default:
                break; // Speaking: answer text arrives via ShowAnswer; Idle: hidden
        }
    }

    /// <summary>Typewriters the recognized question (called when the transcript arrives).</summary>
    public void ShowQuestion(string question) =>
        StartTyping('“' + question.Trim().TrimEnd('.') + '”', BrightText);

    /// <summary>Live-updates the answer text while the LLM streams (gentle tail fade).</summary>
    public void ShowAnswer(string answer)
    {
        StopTyping();
        if (!status) return;
        status.color = AnswerText;
        if (_reveal) _reveal.SetStreaming(answer);
        else status.text = answer;
    }

    private void SetText(string text, Color color)
    {
        StopTyping();
        if (!status) return;
        status.color = color;
        if (_reveal) _reveal.ShowInstant(text);
        else status.text = text;
    }

    private void StartTyping(string text, Color color)
    {
        StopTyping();
        if (!status) return;
        status.color = color;
        if (_reveal) _reveal.ShowInstant(""); // typewriter drives status.text directly
        _typing = StartCoroutine(TypeRoutine(text));
    }

    private void StopTyping()
    {
        if (_typing != null) StopCoroutine(_typing);
        _typing = null;
    }

    private IEnumerator TypeRoutine(string text)
    {
        for (var i = 1; i <= text.Length; i++)
        {
            status.text = text.Substring(0, i);
            yield return new WaitForSeconds(typeCharSeconds);
        }
    }

    private void Update()
    {
        if (thinkDots)
        {
            thinkDots.SetActive(_state == CueVisualState.Thinking);
            if (_state == CueVisualState.Thinking && dots != null)
            {
                for (var i = 0; i < dots.Length; i++)
                {
                    var ph = Mathf.Repeat(Time.time / 1.2f - i * 0.167f, 1f);
                    var lift = ph < 0.4f ? Mathf.Sin(ph / 0.4f * Mathf.PI) : 0f;
                    var c = dots[i].color;
                    c.a = 0.25f + 0.75f * lift;
                    dots[i].color = c;
                    dots[i].rectTransform.anchoredPosition = new Vector2(dots[i].rectTransform.anchoredPosition.x, lift * 2f);
                }
            }
        }

        if (haloRings != null)
        {
            var listening = _state == CueVisualState.Listening;
            for (var i = 0; i < haloRings.Length; i++)
            {
                var ring = haloRings[i];
                if (!ring) continue;
                ring.gameObject.SetActive(listening);
                if (!listening) continue;
                var ph = Mathf.Repeat(Time.time / haloPeriod + i * 0.5f, 1f);
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.9f, ph);
                var c = ring.color;
                c.a = 0.85f * (1f - ph);
                ring.color = c;
            }
        }
    }
}
