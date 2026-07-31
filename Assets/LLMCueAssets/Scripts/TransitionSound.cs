using UnityEngine;

/// <summary>
/// Soft, unobtrusive transition sound (gentle air-swell, ~1.2 s, quiet) played when the user
/// changes context (enter/leave VR). Synthesized at runtime so no asset is required — but if a
/// clip exists at Assets/Resources/TransitionSound.(wav/mp3/ogg), that one is used instead, so
/// the sound can be replaced without touching code.
/// </summary>
public static class TransitionSound
{
    private static AudioClip _clip;

    public static void Play(float volume = 0.32f)
    {
        var cam = Camera.main;
        if (cam == null) return;
        if (_clip == null)
            _clip = Resources.Load<AudioClip>("TransitionSound") ?? Synthesize();
        AudioSource.PlayClipAtPoint(_clip, cam.transform.position, volume);
    }

    /// <summary>A smooth "air swell": low-passed noise with a sin² envelope and a faint low pad —
    /// deliberately unspecific, no melody, no attack.</summary>
    private static AudioClip Synthesize()
    {
        const int sr = 44100;
        const float dur = 1.2f;
        var n = (int)(sr * dur);
        var data = new float[n];
        var rng = new System.Random(12345);
        float lp = 0f;
        for (var i = 0; i < n; i++)
        {
            var t = (float)i / n;
            var env = Mathf.Sin(t * Mathf.PI);
            env *= env; // sin^2: soft rise, soft fall

            // gently sweeping low-pass noise = airy "whoosh" without hiss
            var cutoff = Mathf.Lerp(0.015f, 0.12f, Mathf.Sin(t * Mathf.PI));
            var white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += cutoff * (white - lp);

            // faint warm pad underneath (fixed low note, very quiet)
            var pad = 0.12f * Mathf.Sin(2f * Mathf.PI * 174f * i / sr);

            data[i] = (lp * 0.85f + pad) * env * 0.8f;
        }
        var clip = AudioClip.Create("TransitionWhoosh", n, 1, sr, false);
        clip.SetData(data, 0);
        return clip;
    }
}
