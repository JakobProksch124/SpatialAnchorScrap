using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

/// <summary>
/// The horizontal answer-card row. Cards are DATA-DRIVEN: CueController hands it
/// the CueConfig card list; the assistant shows one by id via show_card, and this
/// builds it from the definition (Image / Video / Text). Only the row grows;
/// the orb, invitation and buttons never duplicate.
/// </summary>
public class CueAnswerRow : MonoBehaviour
{
    [SerializeField] private RectTransform row;      // has HorizontalLayoutGroup
    [SerializeField] private CueTheme theme;
    [SerializeField] private Sprite panelSprite;     // rounded sprite used as a Mask for media
    [SerializeField] private Sprite mediaSprite;     // striped placeholder
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite glowSprite;
    [SerializeField] private TMP_FontAsset uiFont;
    [SerializeField] private TMP_FontAsset monoFont;
    [SerializeField] private float res = 3f;
    [SerializeField] private float cardGrow = 0.45f;

    private readonly Dictionary<string, CueCardDef> _defs = new();
    private readonly List<CueAnswerCard> _shown = new();

    public int Count => _shown.Count;
    public bool IsEmpty => _shown.Count == 0;

    /// <summary>Supply the card catalog defined on CueConfig for this cue.</summary>
    public void SetCards(List<CueCardDef> defs)
    {
        _defs.Clear();
        if (defs == null) return;
        foreach (var d in defs)
            if (d != null && !string.IsNullOrWhiteSpace(d.id))
                _defs[d.id.Trim()] = d;
    }

    /// <summary>Show a defined card by id (assistant show_card).</summary>
    public string ShowCard(string id)
    {
        id = (id ?? "").Trim();
        if (!_defs.TryGetValue(id, out var def)) return $"no card '{id}' is defined for this cue";
        if (_shown.Exists(c => c && c.Id == id)) return $"card '{id}' already shown";
        Build(def);
        return $"card '{id}' added";
    }

    /// <summary>Hide a shown card by id (assistant hide_card / user dismiss).</summary>
    public string Remove(string id)
    {
        id = (id ?? "").Trim();
        var c = _shown.Find(x => x && x.Id == id);
        if (!c) return $"no '{id}' card to hide";
        c.Close();
        _shown.Remove(c);
        return $"card '{id}' hidden";
    }

    public void Clear()
    {
        foreach (var c in _shown) if (c) Destroy(c.gameObject);
        _shown.Clear();
    }

    private float Px(float p) => p * res;

    private void Build(CueCardDef def)
    {
        var card = MakePanel(row, $"Card_{def.id}", 25f);
        var cardWidth = Px(224) * Mathf.Max(0.25f, def.widthScale);
        var le = card.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = cardWidth;
        var group = card.gameObject.AddComponent<CanvasGroup>();
        var v = card.gameObject.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset((int)Px(13), (int)Px(13), (int)Px(13), (int)Px(13));
        v.spacing = Px(9);
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (def.kind == CueCardKind.Text) BuildText(card, def);
        else BuildMedia(card, def);

        var comp = card.gameObject.AddComponent<CueAnswerCard>();
        comp.Init(def.id, card, group, le, cardWidth, cardGrow);
        _shown.Add(comp);
    }

    private void BuildMedia(RectTransform card, CueCardDef def)
    {
        // rounded media box: sprite + Mask clips content to rounded corners
        var media = MakeImage(card, "Media", panelSprite, new Color(0.5f, 0.5f, 0.5f, 1f));
        media.type = Image.Type.Sliced;
        media.pixelsPerUnitMultiplier = 81f / Px(13f);
        media.gameObject.AddComponent<LayoutElement>().preferredHeight = Px(150);
        media.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        if (def.kind == CueCardKind.Image && def.image != null)
        {
            var img = MakeRaw(media.rectTransform, "Image", def.image, Color.white);
            Stretch(img.rectTransform);
        }
        else if (def.kind == CueCardKind.Video && def.video != null)
        {
            var rt = new RenderTexture(512, 288, 0);
            var vp = media.gameObject.AddComponent<VideoPlayer>();
            vp.clip = def.video; vp.isLooping = true; vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = rt; vp.playOnAwake = true; vp.waitForFirstFrame = true;
            var raw = MakeRaw(media.rectTransform, "Video", rt, Color.white);
            Stretch(raw.rectTransform);
        }
        else if (def.kind == CueCardKind.Reality)
        {
            // a "window to reality": force the framebuffer alpha to 0 here so the passthrough
            // UNDERLAY shows through the panel, even while the user is inside an opaque VR room.
            var hole = MakeRaw(media.rectTransform, "RealityHole", null, Color.white);
            Stretch(hole.rectTransform);
            var sh = Shader.Find("EntryCue/PassthroughHole");
            if (sh != null) hole.material = new Material(sh);
        }
        else
        {
            // striped placeholder + accent wash (no asset assigned yet)
            var stripe = MakeImage(media.rectTransform, "Stripe", mediaSprite, new Color(1, 1, 1, 0.6f));
            stripe.type = Image.Type.Simple; Stretch(stripe.rectTransform);
            var wash = MakeRaw(media.rectTransform, "Wash", null, theme ? theme.AcMid : new Color(0.3f, 0.66f, 1f, 0.34f));
            Stretch(wash.rectTransform);
        }

        // tag — square with SMALL rounded corners (feedback: was an over-rounded pill)
        var tag = MakePanel(media.rectTransform, "Tag", 4f);
        var tagRR = tag.GetComponent<RoundedRectUI>();
        tagRR.fillTop = tagRR.fillBottom = new Color(0, 0, 0, 0.5f);
        tagRR.borderColor = new Color(0, 0, 0, 0); tagRR.Refresh();
        tag.anchorMin = tag.anchorMax = new Vector2(0, 1);
        tag.pivot = new Vector2(0, 1);
        tag.anchoredPosition = new Vector2(Px(7), -Px(7));
        tag.sizeDelta = new Vector2(Px(58), Px(16));
        var tagTxt = MakeText(tag, "Label", monoFont, Px(8), TextAlignmentOptions.Center, Color.white);
        Stretch(tagTxt.rectTransform); tagTxt.text = def.title; tagTxt.characterSpacing = 5f;

        if (def.kind == CueCardKind.Video)
        {
            var play = MakeImage(media.rectTransform, "Play", circleSprite, new Color(1, 1, 1, 0.16f));
            play.rectTransform.anchorMin = play.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            play.rectTransform.sizeDelta = new Vector2(Px(42), Px(42));
            var glyph = MakeText(play.rectTransform, "Glyph", uiFont, Px(15), TextAlignmentOptions.Center, Color.white);
            Stretch(glyph.rectTransform); glyph.text = "▶";
        }

        var cap = MakeText(card, "Caption", monoFont, Px(9), TextAlignmentOptions.Left, CueTheme.TextSecondary);
        cap.text = def.caption ?? "";
    }

    private void BuildText(RectTransform card, CueCardDef def)
    {
        var head = MakeText(card, "Header", monoFont, Px(9), TextAlignmentOptions.Left, CueTheme.TextSecondary);
        head.text = (def.title ?? "").ToUpperInvariant(); head.characterSpacing = 4f;
        var body = MakeText(card, "Body", uiFont, Px(12), TextAlignmentOptions.TopLeft, CueTheme.TextBody);
        body.text = def.bodyText ?? "";
    }

    // ---- builders ----

    private RectTransform MakePanel(Transform parent, string name, float radiusPx)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage), typeof(RoundedRectUI));
        go.transform.SetParent(parent, false);
        go.GetComponent<RawImage>().raycastTarget = false;
        var rr = go.GetComponent<RoundedRectUI>();
        rr.radius = Px(radiusPx);
        rr.borderWidth = Px(1f);
        rr.softness = Mathf.Max(1.2f, Px(0.6f));
        rr.fillTop = CueTheme.PanelTop;
        rr.fillBottom = CueTheme.PanelBottom;
        rr.borderColor = new Color(1f, 1f, 1f, 0.12f);
        rr.Refresh();
        return (RectTransform)go.transform;
    }

    private Image MakeImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite; img.color = color; img.raycastTarget = false;
        return img;
    }

    private RawImage MakeRaw(Transform parent, string name, Texture tex, Color color)
    {
        var go = new GameObject(name, typeof(RawImage));
        go.transform.SetParent(parent, false);
        var raw = go.GetComponent<RawImage>();
        raw.texture = tex; raw.color = color; raw.raycastTarget = false;
        return raw;
    }

    private TMP_Text MakeText(Transform parent, string name, TMP_FontAsset font, float size,
        TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font) t.font = font;
        t.fontSize = size; t.alignment = align; t.color = color; t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
