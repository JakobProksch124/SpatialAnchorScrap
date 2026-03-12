using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WelcomeAnimation : MonoBehaviour
{
    private Transform user;
    private float triggerDistance;

    private Vector3 startScale;
    private Vector3 startPos;

    private bool hasPlayed = false;
    private bool hovering = false;

    [Header("Appear")]
    public float appearDuration = 0.9f;

    [Header("Bounce")]
    public float overshootScale = 1.08f;
    public float bounceDuration = 0.25f;

    [Header("Hover")]
    public float hoverHeight = 0.00f;
    public float hoverSpeed = 1.2f;

    private List<Renderer> renderers = new List<Renderer>();

    private Dictionary<Renderer, float> targetAlphas = new Dictionary<Renderer, float>();

    public void Initialize(float distance)
    {
        triggerDistance = distance;
        user = Camera.main.transform;

        startScale = transform.localScale;
        startPos = transform.localPosition;

        CollectRenderers();

        transform.localScale = Vector3.zero;
        SetOpacity(0f);
    }

    void Update()
    {
        if (user == null)
            return;

        float dist = Vector3.Distance(transform.position, user.position);

        if (dist < triggerDistance && !hasPlayed)
        {
            StartCoroutine(AppearSequence());
            hasPlayed = true;
        }

        if (hovering)
        {
            HoverMotion();
        }
    }

    void CollectRenderers()
    {
        renderers.AddRange(GetComponentsInChildren<Renderer>());

        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_BaseColor"))
            {
                float alpha = r.material.GetColor("_BaseColor").a;
                targetAlphas[r] = alpha;
            }
        }
    }

    void SetOpacity(float value)
    {
        foreach (var r in renderers)
        {
            if (r.material.HasProperty("_BaseColor"))
            {
                Color c = r.material.GetColor("_BaseColor");
                c.a = targetAlphas[r] * value;
                r.material.SetColor("_BaseColor", c);
            }
        }
    }

    IEnumerator AppearSequence()
    {
        float t = 0;

        // SCALE + FADE IN
        while (t < appearDuration)
        {
            t += Time.deltaTime;
            float n = t / appearDuration;

            float eased = EaseOutBack(n);

            transform.localScale = startScale * eased;
            SetOpacity(n);

            yield return null;
        }

        transform.localScale = startScale;

        // SMALL OVERSHOOT BOUNCE
        t = 0;

        /*while (t < bounceDuration)
        {
            t += Time.deltaTime;

            float n = t / bounceDuration;

            float bounce = Mathf.Sin(n * Mathf.PI);

            float scale = Mathf.Lerp(1f, overshootScale, bounce);

            transform.localScale = startScale * scale;

            yield return null;
        }*/

        transform.localScale = startScale;

        hovering = true;
    }

    void HoverMotion()
    {
        float hover = Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;

        transform.localPosition = startPos + new Vector3(0, hover, 0);
    }

    // Nice UI easing curve
    float EaseOutBack(float x)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
    }
}