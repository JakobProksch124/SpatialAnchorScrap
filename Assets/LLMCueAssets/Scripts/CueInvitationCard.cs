using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// The invitation card. Text is driven by CueConfig (via CueController.Configure),
/// so it works for entry ("Wechsel zu VR" + reason) and arrival (a longer
/// in-context question) alike. The title auto-sizes so long text never overflows.
/// Big at rest; shrinks to the small variant (accent dot to the right) once answered.
/// </summary>
public class CueInvitationCard : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text reason;
    [SerializeField] private RectTransform accentDot;
    [SerializeField] private VerticalLayoutGroup pad;
    [SerializeField] private CueTheme theme;
    [SerializeField] private float res = 3f;

    private bool _small;
    private string _title = "Wechsel zu VR";
    private string _highlight = "VR";
    private string _reason = "";

    /// <summary>Set the invitation text (title, an optional accent-highlighted word, and the big-state reason line).</summary>
    public void Configure(string titleText, string highlight, string reasonText)
    {
        _title = titleText ?? "";
        _highlight = highlight ?? "";
        _reason = reasonText ?? "";
        Apply();
    }

    public void SetSmall(bool small)
    {
        _small = small;
        Apply();
    }

    private void Start()
    {
        Apply();
        if (theme) theme.Changed += _ => Apply();
    }

    private void Apply()
    {
        if (title)
        {
            var accentHex = ColorUtility.ToHtmlStringRGB(theme ? theme.Accent : CueTheme.EntryAccent);
            var text = _title;
            if (!string.IsNullOrEmpty(_highlight) && text.Contains(_highlight))
                text = text.Replace(_highlight, $"<color=#{accentHex}>{_highlight}</color>");
            title.text = text;
            title.richText = true;
            title.alignment = TextAlignmentOptions.Center;
            title.enableWordWrapping = true;
            // auto-size so long (arrival) text shrinks to fit the box instead of overflowing
            title.enableAutoSizing = true;
            title.fontSizeMin = res * 12f;
            title.fontSizeMax = res * (_small ? 17f : 27f);
        }

        if (reason)
        {
            var show = !_small && !string.IsNullOrEmpty(_reason);
            reason.gameObject.SetActive(show);
            if (show) { reason.text = _reason; reason.fontSize = res * 13f; }
        }

        if (pad)
        {
            var v = _small ? 13f : 22f;
            var h = _small ? 22f : 26f;
            pad.padding = new RectOffset((int)(res * h), (int)(res * h), (int)(res * v), (int)(res * v));
        }

        if (accentDot) accentDot.gameObject.SetActive(_small);
    }
}
