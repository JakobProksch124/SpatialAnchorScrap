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

    // Displays a fade overlay with room title when transitioning from AR to VR
    public IEnumerator FadeToVRWithTitle(string roomTitle, Color fadeColor, float displayDuration = 2.0f)
    {
        // Create canvas for fade overlay
        GameObject fadeCanvas = new GameObject("FadeCanvas");
        Canvas canvas = fadeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        fadeCanvas.AddComponent<CanvasScaler>();
        fadeCanvas.AddComponent<GraphicRaycaster>();

        // Black background panel
        GameObject fadePanel = new GameObject("FadePanel");
        fadePanel.transform.SetParent(fadeCanvas.transform, false);
        Image fadeImage = fadePanel.AddComponent<Image>();
        fadeImage.color = fadeColor;
        RectTransform fadePanelRect = fadePanel.GetComponent<RectTransform>();
        fadePanelRect.anchorMin = Vector2.zero;
        fadePanelRect.anchorMax = Vector2.one;
        fadePanelRect.sizeDelta = Vector2.zero;

        // Title text
        GameObject titleObj = new GameObject("RoomTitle");
        titleObj.transform.SetParent(fadeCanvas.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = roomTitle;
        titleText.fontSize = 40;
        titleText.fontStyle = FontStyles.Bold;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(800, 200);
        titleRect.anchoredPosition = Vector2.zero;

        // Fade in
        float fadeDuration = 0.5f;
        float elapsed = 0f;
        Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
        Color targetColor = fadeColor;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fadeImage.color = Color.Lerp(startColor, targetColor, t);
            titleText.alpha = t;
            yield return null;
        }

        fadeImage.color = targetColor;
        titleText.alpha = 1f;

        // Display title
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fadeImage.color = Color.Lerp(targetColor, startColor, t);
            titleText.alpha = 1f - t;
            yield return null;
        }

        Destroy(fadeCanvas);
    }

    // Fades out the VR room when returning to AR mode
    // Gradually makes VR objects transparent so AR passthrough shows through
    public IEnumerator FadeToAR(float fadeDuration = 1.5f, GameObject vrRoom = null)
    {
        if (vrRoom == null)
        {
            yield return null;
            yield break;
        }

        // Collect all renderers in the VR room
        Renderer[] renderers = vrRoom.GetComponentsInChildren<Renderer>();

        // Store original materials and create transparent versions
        Material[][] originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;

            // Create new materials with transparent rendering mode
            Material[] newMaterials = new Material[originalMaterials[i].Length];
            for (int j = 0; j < originalMaterials[i].Length; j++)
            {
                newMaterials[j] = new Material(originalMaterials[i][j]);

                // Enable transparency
                newMaterials[j].SetFloat("_Surface", 1); // Transparent mode in URP
                newMaterials[j].SetFloat("_Blend", 0); // Alpha blend
                newMaterials[j].SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                newMaterials[j].SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                newMaterials[j].SetInt("_ZWrite", 0);
                newMaterials[j].renderQueue = 3000;
                newMaterials[j].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            renderers[i].materials = newMaterials;
        }

        // Fade out over time
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeDuration);

            // Update alpha for all materials
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    foreach (Material mat in renderers[i].materials)
                    {
                        if (mat != null)
                        {
                            Color color = mat.color;
                            color.a = alpha;
                            mat.color = color;
                        }
                    }
                }
            }

            yield return null;
        }

        // Final pass - make completely transparent
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                foreach (Material mat in renderers[i].materials)
                {
                    if (mat != null)
                    {
                        Color color = mat.color;
                        color.a = 0f;
                        mat.color = color;
                    }
                }
            }
        }
    }
}
