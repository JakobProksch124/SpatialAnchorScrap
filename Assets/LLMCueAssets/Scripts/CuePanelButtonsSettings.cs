using UnityEngine;

/// <summary>
/// ONE global switchboard for the panel-toggle buttons experiment (small icon buttons on every
/// cue that show/hide the answer panels without voice). Lives in Assets/Resources so every cue
/// finds it at runtime — edit THIS asset instead of touching any cue prefab:
///   enabled     — master switch for the whole feature
///   side        — where the strip sits (left/right of the cue, or a row under the invitation)
///   buttonSize  — square size in canvas px (cue canvas is ~1020 px wide)
///   spacing     — gap between buttons
///   edgeOffset  — distance from the cue panel's edge
/// </summary>
[CreateAssetMenu(menuName = "EntryCue/Panel Buttons Settings")]
public class CuePanelButtonsSettings : ScriptableObject
{
    public enum Side { Left, Right, BelowInvitation }

    [Tooltip("Master switch: off = no panel buttons anywhere (voice-only, as before).")]
    public bool featureEnabled = true;

    public Side side = Side.Left;

    [Tooltip("Square button size in canvas pixels (the cue canvas is ~1020 px wide).")]
    public float buttonSize = 72f;

    public float spacing = 16f;

    [Tooltip("Distance from the cue panel edge (canvas px).")]
    public float edgeOffset = 30f;

    public static CuePanelButtonsSettings Load() =>
        Resources.Load<CuePanelButtonsSettings>("CuePanelButtonsSettings");
}
