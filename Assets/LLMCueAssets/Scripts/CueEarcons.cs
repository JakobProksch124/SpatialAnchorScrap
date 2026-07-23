using UnityEngine;

/// <summary>
/// Tiny procedural earcons (no audio assets needed): a soft two-note "wake"
/// chime when the cue becomes available, and a short single blip when it starts
/// listening. Clips are synthesized once at runtime and played at low volume
/// through a dedicated 2D AudioSource. Self-attaches — no scene wiring required.
/// </summary>
public class CueEarcons : MonoBehaviour
{
    [SerializeField] private float volume = 0.22f;

    private AudioSource _src;
    private AudioClip _wake;
    private AudioClip _listen;

    private void Awake()
    {
        _src = gameObject.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f; // UI feedback, not world-positioned
        _wake = BuildChime(new[] { 660f, 988f }, 0.15f);   // gentle rising fifth
        _listen = BuildChime(new[] { 880f }, 0.08f);         // short attentive blip
    }

    public void PlayWake() { if (_wake) _src.PlayOneShot(_wake, volume); }
    public void PlayListen() { if (_listen) _src.PlayOneShot(_listen, volume * 0.9f); }

    // Sine notes played in sequence, each with a soft sin() attack/decay envelope.
    private static AudioClip BuildChime(float[] freqs, float noteSeconds)
    {
        const int rate = 44100;
        var perNote = Mathf.RoundToInt(rate * noteSeconds);
        var total = perNote * freqs.Length;
        var data = new float[total];

        for (var n = 0; n < freqs.Length; n++)
            for (var i = 0; i < perNote; i++)
            {
                var t = i / (float)rate;
                var p = i / (float)perNote;         // 0..1 within the note
                var env = Mathf.Sin(p * Mathf.PI);   // soft attack + decay
                data[n * perNote + i] = Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * 0.6f;
            }

        var clip = AudioClip.Create("cue_earcon", total, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
