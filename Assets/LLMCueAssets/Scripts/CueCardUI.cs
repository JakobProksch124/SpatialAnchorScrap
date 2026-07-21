using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// The invitation card below the dock: AR->VR chip, target preview, title,
/// benefit line, LLM-driven enrichment ("Where you'd land" RVC map), and the
/// Enter VR / Not now actions. Implements the same tool API the LLM calls
/// (show_preview / show_transition_info / hide_panel).
/// </summary>
public class CueCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text benefitText;
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text previewCaption;
    [SerializeField] private GameObject enrichment;

    [Header("Card content (swap per target context)")]
    [SerializeField] private string title = "Step into Aurora Live";
    [SerializeField] private string benefit = "A live concert under the auroras";
    [SerializeField] private string defaultCaption = "VR venue · preview";

    [Header("Optional real preview images per topic")]
    [SerializeField] private Sprite environmentSprite;
    [SerializeField] private Sprite controlsSprite;
    [SerializeField] private Sprite socialSprite;
    [SerializeField] private Sprite audioSprite;

    private static readonly Color BaseTint = new(78/255f, 168/255f, 255/255f, 0.30f);

    private void Start()
    {
        if (titleText) titleText.text = title;
        if (benefitText) benefitText.text = benefit;
        ResetPreview();
        if (enrichment) enrichment.SetActive(false);
    }

    public string ShowPreview(string topic)
    {
        var (caption, tint, sprite) = topic switch
        {
            "environment" => ("Venue · floating stage, night sky", new Color(0.15f, 0.35f, 0.7f, 0.55f), environmentSprite),
            "controls" => ("Controls · laser pointer, A/B, teleport", new Color(0.2f, 0.55f, 0.3f, 0.55f), controlsSprite),
            "social" => ("Social · avatars, muted by default", new Color(0.6f, 0.35f, 0.65f, 0.55f), socialSprite),
            "audio" => ("Audio · live spatial sound", new Color(0.75f, 0.5f, 0.2f, 0.55f), audioSprite),
            _ => (null, Color.clear, null)
        };
        if (caption == null) return $"unknown topic '{topic}'";

        if (previewCaption) previewCaption.text = caption;
        if (previewImage)
        {
            previewImage.sprite = sprite;
            previewImage.color = sprite ? Color.white : tint;
        }

        return $"Preview panel now shows: {topic}";
    }

    public string ShowTransitionInfo()
    {
        if (enrichment) enrichment.SetActive(true);
        return "Card now shows where the user would land on the reality-virtuality continuum";
    }

    public string Hide()
    {
        if (enrichment) enrichment.SetActive(false);
        ResetPreview();
        return "Enrichment hidden";
    }

    private void ResetPreview()
    {
        if (previewCaption) previewCaption.text = defaultCaption;
        if (previewImage)
        {
            previewImage.sprite = null;
            previewImage.color = BaseTint;
        }
    }
}
