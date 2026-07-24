using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Handles visual transition effects between AR and VR modes
// Provides fade effects and title displays during mode transitions
public class TransitionEffects : MonoBehaviour
{
    private static TransitionEffects instance;

    public static TransitionEffects Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("TransitionEffects");
                instance = go.AddComponent<TransitionEffects>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private static bool TryGetColorProp(Material m, out int pid)
    {
        if (m == null) { pid = -1; return false; }
        if (m.HasProperty(BaseColorId)) { pid = BaseColorId; return true; }
        if (m.HasProperty(ColorId)) { pid = ColorId; return true; }
        pid = -1;
        return false; 
    }

    private static void SetMatAlpha(Material m, float a)
    {
        if (!TryGetColorProp(m, out int pid)) return;
        Color c = m.GetColor(pid);
        c.a = a;
        m.SetColor(pid, c);
    }

    private static void TrySetURPTransparent(Material m)
    {
        if (m == null) return;

        // Guard every URP-specific property (avoids errors on TMP/custom shaders)
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // Transparent
        if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);     // Alpha
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);

        m.renderQueue = 3000;
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }


    private class FadeOverlay
    {
        public GameObject canvasGO;
        public Image fadeImage;
        public TextMeshProUGUI titleText;
        public Color fadeColor;
    }

    private FadeOverlay CreateOverlay(string roomTitle, Color fadeColor)
    {
        GameObject fadeCanvas = new GameObject("FadeCanvas");
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        //XX
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 1000; 
        fadeCanvas.AddComponent<CanvasScaler>();
        fadeCanvas.AddComponent<GraphicRaycaster>();

        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(fadeCanvas.transform, false);
        Image fadeImage = fadePanel.AddComponent<Image>();
        fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        RectTransform fadePanelRect = fadePanel.GetComponent<RectTransform>();
        fadePanelRect.anchorMin = Vector2.zero;
        fadePanelRect.anchorMax = Vector2.one;
        fadePanelRect.sizeDelta = Vector2.zero;

        GameObject titleObj = new GameObject("RoomTitle");
        titleObj.transform.SetParent(fadeCanvas.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = roomTitle;
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.alpha = 0f;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(800, 200);
        titleRect.anchoredPosition = Vector2.zero;

        return new FadeOverlay
        {
            canvasGO = fadeCanvas,
            fadeImage = fadeImage,
            titleText = titleText,
            fadeColor = fadeColor
        };
    }

    public IEnumerator FadeToBlackWithTitle(string roomTitle, Color fadeColor, float fadeDuration, float titleHoldSeconds, System.Action<GameObject> onOverlayReady)
    {
        var overlay = CreateOverlay(roomTitle, fadeColor);

        float elapsed = 0f;
        Color from = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        Color to = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            overlay.fadeImage.color = Color.Lerp(from, to, t);
            overlay.titleText.alpha = t;
            yield return null;
        }

        overlay.fadeImage.color = to;
        overlay.titleText.alpha = 1f;

        // Give caller access to overlay canvas to keep it alive while loading
        onOverlayReady?.Invoke(overlay.canvasGO);

        // optional: title holds while already black (useful if you want)
        if (titleHoldSeconds > 0f)
            yield return new WaitForSeconds(titleHoldSeconds);
    }

    public IEnumerator FadeFromBlackAndDestroy(GameObject overlayCanvas, Color fadeColor, float fadeDuration)
    {
        if (overlayCanvas == null) yield break;

        var fadeImage = overlayCanvas.GetComponentInChildren<Image>(true);
        var titleText = overlayCanvas.GetComponentInChildren<TextMeshProUGUI>(true);

        float elapsed = 0f;
        Color from = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        Color to = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (fadeImage) fadeImage.color = Color.Lerp(from, to, t);
            if (titleText) titleText.alpha = 1f - t;
            yield return null;
        }

        Destroy(overlayCanvas);
    }

    // Fades out the VR room when returning to AR mode
public IEnumerator FadeToAR(float fadeDuration = 1.5f, GameObject vrRoom = null)
{
    if (vrRoom == null)
        yield break;

    Renderer[] renderers = vrRoom.GetComponentsInChildren<Renderer>(true);
    MaterialPropertyBlock block = new MaterialPropertyBlock();

    float elapsed = 0f;

    while (elapsed < fadeDuration)
    {
        elapsed += Time.deltaTime;

        // Fade from 0 -> 0.75
        float fade = Mathf.Lerp(0f, 0.75f, Mathf.Clamp01(elapsed / fadeDuration));

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            for (int m = 0; m < r.sharedMaterials.Length; m++)
            {
                r.GetPropertyBlock(block, m);
                block.SetFloat("_Fade", fade);
                r.SetPropertyBlock(block, m);
            }
        }

        yield return null;
    }

    // Ensure fully invisible
    foreach (Renderer r in renderers)
    {
        if (r == null)
            continue;

        for (int m = 0; m < r.sharedMaterials.Length; m++)
        {
            r.GetPropertyBlock(block, m);
            block.SetFloat("_Fade", 0.75f);
            r.SetPropertyBlock(block, m);
        }
    }
}


// Fades in the VR room when entering VR mode
public IEnumerator FadeToVR(float fadeDuration = 1.5f, GameObject vrRoom = null)
{
    if (vrRoom == null)
        yield break;

    Renderer[] renderers = vrRoom.GetComponentsInChildren<Renderer>(true);
    MaterialPropertyBlock block = new MaterialPropertyBlock();

    float elapsed = 0f;

    while (elapsed < fadeDuration)
    {
        elapsed += Time.deltaTime;

        // Fade from 0.75 -> 0
        float fade = Mathf.Lerp(0.75f, 0f, Mathf.Clamp01(elapsed / fadeDuration));

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            for (int m = 0; m < r.sharedMaterials.Length; m++)
            {
                r.GetPropertyBlock(block, m);
                block.SetFloat("_Fade", fade);
                r.SetPropertyBlock(block, m);
            }
        }

        yield return null;
    }

    // Ensure fully visible
    foreach (Renderer r in renderers)
    {
        if (r == null)
            continue;

        for (int m = 0; m < r.sharedMaterials.Length; m++)
        {
            r.GetPropertyBlock(block, m);
            block.SetFloat("_Fade", 0f);
            r.SetPropertyBlock(block, m);
        }
    }
}
}
