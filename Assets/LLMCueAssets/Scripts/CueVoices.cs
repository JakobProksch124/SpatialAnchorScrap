using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A user-editable list of named ElevenLabs voices. Edit the list and the
/// active index in the Inspector, then EntryCue > Apply Voice pushes the
/// active voice id onto the ElevenLabs provider profile. Not secret — just ids.
/// </summary>
[CreateAssetMenu(menuName = "EntryCue/Cue Voices")]
public class CueVoices : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string name = "Voice";
        public string voiceId = "";
    }

    [Tooltip("Add as many ElevenLabs voices as you like; switch with 'active'.")]
    public List<Entry> voices = new();

    [Tooltip("Index into the list above that is currently used.")]
    public int active;

    public string ActiveVoiceId =>
        voices.Count == 0 ? "" : voices[Mathf.Clamp(active, 0, voices.Count - 1)].voiceId;

    public string ActiveName =>
        voices.Count == 0 ? "(none)" : voices[Mathf.Clamp(active, 0, voices.Count - 1)].name;
}
