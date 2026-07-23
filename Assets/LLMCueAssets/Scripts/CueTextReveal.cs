using UnityEngine;
using TMPro;

/// <summary>
/// Gentle leading-edge fade for streaming answer text: as LLM tokens arrive, the
/// newest characters ramp from transparent to full over a short window, so the
/// answer "breathes in" word by word instead of popping. Non-streaming text
/// (status words, the typewritered question) is shown instantly via
/// <see cref="ShowInstant"/>. Self-attaches to the dock's status label — no
/// scene wiring required.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class CueTextReveal : MonoBehaviour
{
    [Tooltip("Max reveal speed of the leading edge, characters/second.")]
    [SerializeField] private float charsPerSecond = 55f;
    [Tooltip("Width of the soft fade edge, in characters.")]
    [SerializeField] private float fadeChars = 6f;

    private TMP_Text _tmp;
    private float _revealed; // leading edge position (float char index)
    private bool _active;    // a streaming fade is running

    private void Awake() => _tmp = GetComponent<TMP_Text>();

    /// <summary>Set the full accumulated streaming text; the new tail fades in.</summary>
    public void SetStreaming(string full)
    {
        full ??= "";
        // a fresh answer (not an extension of what we had) restarts the reveal
        if (!full.StartsWith(_tmp.text)) _revealed = 0f;
        _tmp.text = full;
        _active = true;
    }

    /// <summary>Show text immediately, no fade (status words, typewriter).</summary>
    public void ShowInstant(string text)
    {
        _tmp.text = text ?? "";
        _revealed = _tmp.text.Length + fadeChars + 1f;
        _active = false;
    }

    private void LateUpdate()
    {
        if (!_active) return;

        var len = _tmp.text.Length;
        _revealed = Mathf.MoveTowards(_revealed, len, charsPerSecond * Time.unscaledDeltaTime);

        _tmp.ForceMeshUpdate();
        var info = _tmp.textInfo;
        var baseColor = _tmp.color;
        var edge = Mathf.Max(0.001f, fadeChars);

        for (var i = 0; i < info.characterCount; i++)
        {
            var ch = info.characterInfo[i];
            if (!ch.isVisible) continue;
            var ramp = Mathf.Clamp01((_revealed - i) / edge);
            Color32 c = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * ramp);
            var cols = info.meshInfo[ch.materialReferenceIndex].colors32;
            var vi = ch.vertexIndex;
            cols[vi + 0] = c; cols[vi + 1] = c; cols[vi + 2] = c; cols[vi + 3] = c;
        }

        _tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        if (_revealed >= len + fadeChars) _active = false; // fully shown -> stop churning
    }
}
