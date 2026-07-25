using UnityEngine;
using UnityEngine.Video;

public enum CueCardKind { Image, Video, Text, Reality }

/// <summary>
/// One user-defined answer card (a "blank" you fill in per cue). The assistant
/// can show it by <see cref="id"/> when the user's question matches
/// <see cref="whenToShow"/>. Kind decides what's rendered: an image, a video,
/// or a text panel.
/// </summary>
[System.Serializable]
public class CueCardDef
{
    [Tooltip("Stable id the assistant uses to show/hide this card (lowercase, no spaces).")]
    public string id = "preview";

    public CueCardKind kind = CueCardKind.Image;

    [Tooltip("Tag shown top-left on media cards / heading on text cards.")]
    public string title = "PREVIEW";

    [Tooltip("Caption under an image/video card.")]
    public string caption = "";

    [Header("Content (by kind)")]
    public Texture image;          // Image kind
    public VideoClip video;        // Video kind
    [TextArea(2, 6)] public string bodyText = ""; // Text kind

    [Header("For the assistant")]
    [Tooltip("Tell the LLM WHEN to show this card and WHAT it is, e.g. 'Show when the user asks what the lab looks like.'")]
    [TextArea(2, 4)] public string whenToShow = "";
}
