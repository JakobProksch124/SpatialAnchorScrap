using UnityEngine;
using Oculus.Interaction;

public class UIButtonHoverEffect : MonoBehaviour
{
    [Header("Scale")]
    public float hoverScaleMultiplier = 1.1f;
    public float animationSpeed = 10f;

    [Header("Transparency")]
    [Tooltip("How much LESS transparent the button becomes on hover (0–1)")]
    public float hoverAlphaBoost = 0.2f;

    private Vector3 _initialScale;
    private Vector3 _targetScale;

    private Renderer _renderer;
    private Color _originalColor;
    private Color _targetColor;

    private RayInteractable _rayInteractable;

    void Awake()
    {
        _initialScale = transform.localScale;
        _targetScale = _initialScale;

        _renderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        if (_renderer != null && _renderer.material.HasProperty("_BaseColor"))
        {
            _originalColor = _renderer.material.GetColor("_BaseColor");
            _targetColor = _originalColor;
        }

        _rayInteractable = GetComponent<RayInteractable>();
        if (_rayInteractable != null)
        {
            _rayInteractable.WhenStateChanged += OnStateChanged;
        }
    }

    private void OnStateChanged(InteractableStateChangeArgs args)
    {
        switch (args.NewState)
        {
            case InteractableState.Hover:
                ApplyHover(true);
                break;

            case InteractableState.Normal:
                ApplyHover(false);
                break;

            case InteractableState.Select:
                _targetScale = _initialScale * 0.95f;
                break;
        }
    }

    private void ApplyHover(bool isHovering)
    {
        // === Scale ===
        _targetScale = isHovering
            ? _initialScale * hoverScaleMultiplier
            : _initialScale;

        // === Transparency (alpha only) ===
        if (_renderer != null && _renderer.material.HasProperty("_BaseColor"))
        {
            float baseAlpha = _originalColor.a;

            float newAlpha = isHovering
                ? Mathf.Clamp01(baseAlpha + hoverAlphaBoost)
                : baseAlpha;

            _targetColor = new Color(
                _originalColor.r,
                _originalColor.g,
                _originalColor.b,
                newAlpha
            );
        }
    }

    void Update()
    {
        // Smooth scale
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            _targetScale,
            Time.deltaTime * animationSpeed
        );

        // Smooth alpha transition
        if (_renderer != null && _renderer.material.HasProperty("_BaseColor"))
        {
            Color current = _renderer.material.GetColor("_BaseColor");

            Color lerped = Color.Lerp(
                current,
                _targetColor,
                Time.deltaTime * animationSpeed
            );

            _renderer.material.SetColor("_BaseColor", lerped);
        }
    }
}