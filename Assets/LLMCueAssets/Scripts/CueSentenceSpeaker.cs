using System.Collections;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks.AIBlocks;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Speaks text sentence-by-sentence with prefetch: while sentence n plays,
/// sentence n+1 is already being synthesized via the assigned TTS provider.
/// Replaces TextToSpeechAgent for playback (that agent cancels current audio
/// on every SpeakText call, so it cannot queue).
/// </summary>
public class CueSentenceSpeaker : MonoBehaviour
{
    [Tooltip("Provider profile implementing ITextToSpeechTask (OpenAI or ElevenLabs).")]
    [SerializeField] private AIProviderBase ttsProvider;
    [SerializeField] private AudioSource audioSource;

    public UnityEvent onSpeakStarted = new();
    public UnityEvent onAllFinished = new();

    private readonly Queue<string> _pendingText = new();
    private readonly Queue<AudioClip> _readyClips = new();
    private bool _synthRunning;
    private bool _playRunning;
    private bool _endOfInput = true;

    public bool IsActive => _synthRunning || _playRunning || _pendingText.Count > 0 || _readyClips.Count > 0;

    private void Awake()
    {
        if (!audioSource && !TryGetComponent(out audioSource))
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void Enqueue(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence)) return;
        _endOfInput = false;
        _pendingText.Enqueue(sentence.Trim());
        if (!_synthRunning) StartCoroutine(SynthLoop());
        if (!_playRunning) StartCoroutine(PlayLoop());
    }

    /// <summary>Signal that no more sentences will come for the current answer.</summary>
    public void EndOfInput() => _endOfInput = true;

    public void StopAll()
    {
        StopAllCoroutines();
        _synthRunning = false;
        _playRunning = false;
        _endOfInput = true;
        _pendingText.Clear();
        while (_readyClips.Count > 0) Destroy(_readyClips.Dequeue());
        if (audioSource.isPlaying) audioSource.Stop();
    }

    private IEnumerator SynthLoop()
    {
        _synthRunning = true;
        while (_pendingText.Count > 0)
        {
            if (ttsProvider is not ITextToSpeechTask task)
            {
                Debug.LogError("[CueSentenceSpeaker] ttsProvider does not implement ITextToSpeechTask.");
                _pendingText.Clear();
                break;
            }

            var text = _pendingText.Dequeue();
            AudioClip clip = null;
            yield return task.SynthesizeStreamCoroutine(text, null, c => clip = c);
            if (clip != null) _readyClips.Enqueue(clip);
            else Debug.LogWarning($"[CueSentenceSpeaker] Synthesis returned no clip for: '{text}'");
        }

        _synthRunning = false;
    }

    private IEnumerator PlayLoop()
    {
        _playRunning = true;
        var started = false;
        while (true)
        {
            if (_readyClips.Count > 0)
            {
                var clip = _readyClips.Dequeue();
                if (!started)
                {
                    started = true;
                    onSpeakStarted.Invoke();
                }

                audioSource.clip = clip;
                audioSource.Play();
                while (audioSource.isPlaying) yield return null;
                audioSource.clip = null;
                Destroy(clip);
            }
            else if (_endOfInput && !_synthRunning && _pendingText.Count == 0)
            {
                break;
            }
            else
            {
                yield return null;
            }
        }

        _playRunning = false;
        onAllFinished.Invoke();
    }
}
