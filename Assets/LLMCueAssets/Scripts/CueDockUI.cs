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
    [SerializeField] private Image[] dots;
    [SerializeField] private Image[] haloRings;

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
    private CueLanguage _language = CueLanguage.English;
    private float _width;
    private float _baseY;
    private Coroutine _typing;

    private void Awake()
    {
        _width = collapsedWidth;
        _baseY = dockRect ? dockRect.anchoredPosition.y : 0f;
    }

    public void SetLanguage(CueLanguage language) => _language = language;

    public void SetPhase(CueVisualState state)
    {
        _state = state;
        var german = _language == CueLanguage.German;
        switch (state)
        {
            case CueVisualState.Available:
                SetText(german ? "Frag mich zu diesem Übergang" : "Ask about this transition", DimText);
                break;
            case CueVisualState.Listening:
                SetText(german ? "Ich höre zu…" : "Listening…", DimText);
                break;
            case CueVisualState.Thinking:
                // transcript arrives via ShowQuestion right before/after this
                break;
            case CueVisualState.Speaking:
                break;
            default:
                SetText("", DimText);
                break;
        }
    }

    /// <summary>Typewriters the recognized question (called when the transcript arrives).</summary>
    public void ShowQuestion(string question) =>
        StartTyping('“' + question.Trim().TrimEnd('.') + '”', BrightText);

    /// <summary>Live-updates the answer text while the LLM streams.</summary>
    public void ShowAnswer(string answer)
    {
        StopTyping();
        SetText(answer, AnswerText);
    }

    private void SetText(string text, Color color)
    {
        StopTyping();
        if (!status) return;
        status.text = text;
        status.color = color;
    }

    private void StartTyping(string text, Color color)
    {
        StopTyping();
        if (!status) return;
        status.color = color;
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
        var expanded = _state != CueVisualState.Idle;
        var targetW = expanded ? expandedWidth : collapsedWidth;
        // exponential ease-out approximating the design's cubic-bezier(.4,0,.2,1)
        _width = Mathf.Lerp(_width, targetW, 1f - Mathf.Exp(-Time.deltaTime * (4.6f / expandSeconds)));
        if (dockRect)
        {
            dockRect.sizeDelta = new Vector2(_width, dockRect.sizeDelta.y);
            var p = dockRect.anchoredPosition;
            p.y = _baseY + Mathf.Sin(Time.time * (2f * Mathf.PI / 4.5f)) * floatAmplitude;
            dockRect.anchoredPosition = p;
        }

        if (textGroup) textGroup.gameObject.SetActive(_width > collapsedWidth + 30f);

        if (background)
        {
            var active = _state is CueVisualState.Listening or CueVisualState.Speaking;
            background.color = Color.Lerp(background.color, active ? ActiveBg : IdleBg, Time.deltaTime * 8f);
        }

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
