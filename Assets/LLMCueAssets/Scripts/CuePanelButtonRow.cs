using System.Collections.Generic;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EXPERIMENT (branch panels-through-button): a strip of SMALL square icon buttons on the cue —
/// one per answer card — so users can peek at panels without the voice interface. Click shows
/// the panel, click again hides it; the highlight mirrors the live state, so voice-opened panels
/// light their button too.
///
/// Fully data-driven: built at runtime by CueController from CueConfig.cards — nothing to edit
/// per cue. Global tuning/off-switch: the single asset Resources/CuePanelButtonsSettings.
/// Clicks are logged (panel_shown / panel_hidden, text "button") for the study data.
/// </summary>
public class CuePanelButtonRow : MonoBehaviour
{
    private CueAnswerRow _row;
    private CueInvitationCard _invitation;
    private CueTheme _theme;
    private readonly List<(CueCardDef def, RoundedRectUI rr, bool lastShown)> _buttons = new();
    private float _sync;

    /// <summary>Build the strip. Parent = the cue's canvas (invitation's parent).</summary>
    public static void Attach(RectTransform canvas, CueAnswerRow row, CueInvitationCard invitation,
        CueTheme theme, List<CueCardDef> cards, CuePanelButtonsSettings s)
    {
        if (canvas == null || row == null || cards == null || cards.Count == 0) return;

        // idempotent (Apply can run more than once)
        var old = canvas.Find("PanelButtons");
        if (old != null) Destroy(old.gameObject);

        var go = new GameObject("PanelButtons", typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas, false);
        go.AddComponent<LayoutElement>().ignoreLayout = true; // never disturb the cue's layout

        var strip = go.AddComponent<CuePanelButtonRow>();
        strip._row = row;
        strip._invitation = invitation;
        strip._theme = theme;
        strip.Build(rt, cards, s);
    }

    private void Build(RectTransform rt, List<CueCardDef> cards, CuePanelButtonsSettings s)
    {
        var n = 0;
        foreach (var d in cards) if (d != null && !string.IsNullOrWhiteSpace(d.id)) n++;
        if (n == 0) return;

        var size = s.buttonSize;
        var vertical = s.side != CuePanelButtonsSettings.Side.BelowInvitation;
        var length = n * size + (n - 1) * s.spacing;

        switch (s.side)
        {
            case CuePanelButtonsSettings.Side.Left:
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(1f, 0.5f);
                rt.anchoredPosition = new Vector2(-s.edgeOffset, 0f);
                rt.sizeDelta = new Vector2(size, length);
                break;
            case CuePanelButtonsSettings.Side.Right:
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.anchoredPosition = new Vector2(s.edgeOffset, 0f);
                rt.sizeDelta = new Vector2(size, length);
                break;
            default: // BelowInvitation: a horizontal row across the top area of the cue
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, s.edgeOffset);
                rt.sizeDelta = new Vector2(length, size);
                break;
        }

        var i = 0;
        foreach (var d in cards)
        {
            if (d == null || string.IsNullOrWhiteSpace(d.id)) continue;
            var b = MakeButton(rt, d, size, s.spacing, i, vertical);
            _buttons.Add((d, b, false));
            i++;
        }
    }

    private RoundedRectUI MakeButton(RectTransform parent, CueCardDef def, float size, float spacing,
        int index, bool vertical)
    {
        var go = new GameObject($"PanelButton_{def.id}", typeof(RectTransform), typeof(RawImage), typeof(RoundedRectUI));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchorMin = rt.anchorMax = vertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
        rt.pivot = vertical ? new Vector2(0.5f, 1f) : new Vector2(0f, 0.5f);
        rt.anchoredPosition = vertical
            ? new Vector2(0f, -index * (size + spacing))
            : new Vector2(index * (size + spacing), 0f);

        go.GetComponent<RawImage>().raycastTarget = false;
        var rr = go.GetComponent<RoundedRectUI>();
        rr.radius = size * 0.28f;               // square with rounded corners (dismiss-button style)
        rr.borderWidth = 2.5f;
        rr.softness = 2f;
        rr.fillTop = CueTheme.PanelTop;
        rr.fillBottom = CueTheme.PanelBottom;
        rr.borderColor = CueTheme.PanelBorder;
        rr.Refresh();

        // icon: supplied texture, else a placeholder glyph by card kind
        if (def.icon != null)
        {
            var icon = new GameObject("Icon", typeof(RawImage));
            var irt = (RectTransform)icon.transform;
            irt.SetParent(rt, false);
            irt.anchorMin = Vector2.zero; irt.anchorMax = Vector2.one;
            var pad = size * 0.18f;
            irt.offsetMin = new Vector2(pad, pad); irt.offsetMax = new Vector2(-pad, -pad);
            var img = icon.GetComponent<RawImage>();
            img.texture = def.icon;
            img.raycastTarget = false;
        }
        else
        {
            var txt = new GameObject("Glyph", typeof(TextMeshProUGUI));
            var trt = (RectTransform)txt.transform;
            trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = Glyph(def.kind);
            tmp.fontSize = size * 0.5f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.9f);
            tmp.raycastTarget = false;
        }

        // ISDK ray interaction — same wiring as the existing cue buttons
        var col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(size, size, 20f);
        var surface = go.AddComponent<ColliderSurface>();
        surface.InjectAllColliderSurface(col);
        var ray = go.AddComponent<RayInteractable>();
        ray.InjectAllRayInteractable(surface);
        ray.WhenStateChanged += state =>
        {
            if (state.NewState == InteractableState.Select) Toggle(def);
        };

        return rr;
    }

    private static string Glyph(CueCardKind kind) => kind switch
    {
        CueCardKind.Video => "▶",   // ▶
        CueCardKind.Text => "i",
        CueCardKind.Reality => "◉", // ◉
        _ => "▣",                   // ▣ (image)
    };

    private void Toggle(CueCardDef def)
    {
        if (_row == null) return;
        if (_row.IsShown(def.id))
        {
            _row.Remove(def.id);
            CueLogger.Event("panel_hidden", "button", def.id);
            if (_row.IsEmpty && _invitation) _invitation.SetSmall(false);
        }
        else
        {
            var result = _row.ShowCard(def.id);
            if (result.StartsWith("card"))
            {
                CueLogger.Event("panel_shown", "button", def.id);
                if (_invitation) _invitation.SetSmall(true);
            }
        }
        SyncHighlights(true);
    }

    private void Update()
    {
        _sync += Time.deltaTime;
        if (_sync < 0.25f) return; // also mirrors voice-driven show/hide
        _sync = 0f;
        SyncHighlights(false);
    }

    private void SyncHighlights(bool force)
    {
        for (var i = 0; i < _buttons.Count; i++)
        {
            var (def, rr, last) = _buttons[i];
            if (rr == null) continue;
            var shown = _row != null && _row.IsShown(def.id);
            if (!force && shown == last) continue;
            var accent = _theme ? _theme.Accent : new Color(0.31f, 0.66f, 1f);
            rr.borderColor = shown ? new Color(accent.r, accent.g, accent.b, 0.95f) : CueTheme.PanelBorder;
            rr.fillTop = shown
                ? new Color(Mathf.Lerp(accent.r, 0.09f, 0.72f), Mathf.Lerp(accent.g, 0.10f, 0.72f), Mathf.Lerp(accent.b, 0.12f, 0.72f), 0.92f)
                : CueTheme.PanelTop;
            rr.Refresh();
            _buttons[i] = (def, rr, shown);
        }
    }
}
