using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;

// Hover/press feedback for Canvas UI buttons driven by a RayInteractable.
// UIButtonHoverEffect only supports mesh Renderers (used by the procedural cue
// buttons in TransitionCueFactory); this covers CanvasRenderer/Graphic buttons instead.
public class UICanvasButtonHoverEffect : MonoBehaviour
{
    public float hoverAlphaBoost = 0.2f;
    public float pressAlphaBoost = 0.35f;
    public float animationSpeed = 10f;

    private Graphic _graphic;
    private Color _baseColor;
    private Color _targetColor;
    private RayInteractable _rayInteractable;

    void Awake()
    {
        _graphic = GetComponent<Graphic>();
        if (_graphic == null) return;

        _baseColor = _graphic.color;
        _targetColor = _baseColor;

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
                _targetColor = BoostAlpha(hoverAlphaBoost);
                break;

            case InteractableState.Select:
                _targetColor = BoostAlpha(pressAlphaBoost);
                break;

            case InteractableState.Normal:
                _targetColor = _baseColor;
                break;
        }
    }

    private Color BoostAlpha(float boost)
    {
        return new Color(_baseColor.r, _baseColor.g, _baseColor.b, Mathf.Clamp01(_baseColor.a + boost));
    }

    void Update()
    {
        if (_graphic == null) return;
        _graphic.color = Color.Lerp(_graphic.color, _targetColor, Time.deltaTime * animationSpeed);
    }
}
