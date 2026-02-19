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
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
    // Gradually makes VR objects transparent so AR passthrough shows through
    public IEnumerator FadeToAR(float fadeDuration = 1.5f, GameObject vrRoom = null)
    {
        if (vrRoom == null) yield break;

        // include inactive children too, to avoid popping
        Renderer[] renderers = vrRoom.GetComponentsInChildren<Renderer>(true);

        // Create transparent material instances per renderer
        Material[][] newMatsPerRenderer = new Material[renderers.Length][];
        int[][] colorPropIdsPerRenderer = new int[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            Material[] srcMats = r.materials; // instances
            newMatsPerRenderer[i] = new Material[srcMats.Length];
            colorPropIdsPerRenderer[i] = new int[srcMats.Length];

            for (int j = 0; j < srcMats.Length; j++)
            {
                var src = srcMats[j];
                if (src == null)
                {
                    newMatsPerRenderer[i][j] = null;
                    colorPropIdsPerRenderer[i][j] = -1;
                    continue;
                }

                var inst = new Material(src);
                TrySetURPTransparent(inst);

                if (TryGetColorProp(inst, out int pid))
                    colorPropIdsPerRenderer[i][j] = pid;
                else
                    colorPropIdsPerRenderer[i][j] = -1;

                newMatsPerRenderer[i][j] = inst;
            }

            r.materials = newMatsPerRenderer[i];
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;

                var mats = r.materials;
                var pids = colorPropIdsPerRenderer[i];
                if (mats == null || pids == null) continue;

                for (int j = 0; j < mats.Length; j++)
                {
                    var m = mats[j];
                    int pid = pids[j];
                    if (m == null || pid == -1) continue;

                    Color c = m.GetColor(pid);
                    c.a = alpha;
                    m.SetColor(pid, c);
                }
            }

            yield return null;
        }

        // Final pass: fully transparent (where supported)
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (r == null) continue;

            var mats = r.materials;
            var pids = colorPropIdsPerRenderer[i];
            if (mats == null || pids == null) continue;

            for (int j = 0; j < mats.Length; j++)
            {
                var m = mats[j];
                int pid = pids[j];
                if (m == null || pid == -1) continue;

                Color c = m.GetColor(pid);
                c.a = 0f;
                m.SetColor(pid, c);
            }
        }
    }
}
