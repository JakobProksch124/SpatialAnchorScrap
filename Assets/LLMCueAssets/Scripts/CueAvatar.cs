using UnityEngine;

/// <summary>
/// Motion controller for the avatar (blob + face + halos) living on top of the
/// dock bar. Far/idle: small, resting just above the bar. Engaged (user near
/// or interacting): rises upward and grows to ~4x volume (~1.6x diameter).
/// The face and halo are children, so they scale and move with the head.
/// </summary>
public class CueAvatar : MonoBehaviour
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private float idleScale = 1f;
    [SerializeField] private float engagedScale = 1.59f; // ~4x volume
    [SerializeField] private float idleY = 72f;          // canvas units above bar top
    [SerializeField] private float engagedY = 156f;
    [SerializeField] private float ease = 6f;

    private float _scale = 1f;
    private float _y;
    private bool _engaged;

    private void Awake()
    {
        if (!rect) rect = GetComponent<RectTransform>();
        _y = idleY;
        _scale = idleScale;
    }

    public void SetState(CueVisualState state)
    {
        _engaged = state != CueVisualState.Idle;
    }

    private void Update()
    {
        var k = 1f - Mathf.Exp(-Time.deltaTime * ease);
        _scale = Mathf.Lerp(_scale, _engaged ? engagedScale : idleScale, k);
        _y = Mathf.Lerp(_y, _engaged ? engagedY : idleY, k);

        if (!rect) return;
        rect.localScale = Vector3.one * _scale;
        rect.anchoredPosition = new Vector2(0f, _y);
    }
}
