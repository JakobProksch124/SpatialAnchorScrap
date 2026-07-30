using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Small world-space hint panel in the cue UI style (dark rounded panel, green accent border,
/// billboard facing). Used e.g. at the library door button instead of the old red arrows —
/// it also DISABLES any sibling "Arrow_3D_Icon_03" objects so the arrows and the panel can't
/// show at the same time.
/// </summary>
public class DoorHintPanel : MonoBehaviour
{
    [SerializeField] private string text = "Tür öffnen";
    [SerializeField] private float width = 0.55f;   // metres
    [SerializeField] private float height = 0.18f;

    private void Start()
    {
        // replace the legacy red arrows next to us
        if (transform.parent != null)
            foreach (Transform sib in transform.parent)
                if (sib.name.StartsWith("Arrow_3D_Icon"))
                    sib.gameObject.SetActive(false);

        const float res = 1000f; // canvas px per metre
        var canvasGo = new GameObject("HintCanvas", typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)canvasGo.transform;
        rt.sizeDelta = new Vector2(width * res, height * res);
        rt.localScale = Vector3.one / res;

        var bg = new GameObject("BG", typeof(RawImage), typeof(RoundedRectUI));
        bg.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)bg.transform);
        bg.GetComponent<RawImage>().raycastTarget = false;
        var rr = bg.GetComponent<RoundedRectUI>();
        rr.radius = 42f;
        rr.borderWidth = 3f;
        rr.softness = 2f;
        rr.fillTop = CueTheme.PanelTop;
        rr.fillBottom = CueTheme.PanelBottom;
        rr.borderColor = new Color(0.35f, 1f, 0.55f, 0.6f); // arrival green accent
        rr.Refresh();

        var txtGo = new GameObject("Text", typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(canvasGo.transform, false);
        Stretch((RectTransform)txtGo.transform);
        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 78f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        gameObject.AddComponent<CueBillboard>(); // face the user like the cues do
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
