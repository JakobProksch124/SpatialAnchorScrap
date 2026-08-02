using Meta.XR.BuildingBlocks.AIBlocks;
using Newtonsoft.Json.Linq;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Cue orchestrator for the "dock-eyes" design: five states drive the 3D head
/// (CueHeadVisuals), the face rig, the dock bar, and the card.
///
///   Idle       far away; dock collapsed to head-only
///   Available  within outer radius; dock expands with the invite line
///   Listening  auto within inner radius or A-button; halo + attentive eyes
///   Thinking   transcript sent; scanning eyes + dots; question typewritered
///   Speaking   streaming answer in the dock; mouth follows the TTS audio
///
/// A-button toggles listening and interrupts speech (barge-in). Leaving the
/// outer radius resets everything including the conversation.
/// </summary>
public class CueVoiceLoop : MonoBehaviour
{
    [SerializeField] private SpeechToTextAgent stt;
    [SerializeField] private CueLLMClient llm;
    [SerializeField] private CueSentenceSpeaker speaker;
    [SerializeField] private CueProximitySensor proximity;
    [SerializeField] private CueHeadVisuals head;
    [SerializeField] private CueFaceRig face;
    [SerializeField] private CueDockUI dock;
    [SerializeField] private CueInvitationCard invitation;
    [SerializeField] private CueAnswerRow answerRow;
    [SerializeField] private CueEvents events;

    [Tooltip("Seconds after an answer before proximity may auto-trigger listening again.")]
    [SerializeField] private float relistenCooldown = 2.5f;
    [SerializeField] private CueLanguage language = CueLanguage.English;
    [Tooltip("Soft chime on wake + blip when listening starts.")]
    [SerializeField] private bool earcons = true;

    private CueEarcons _earcons;

    // Only ONE cue in the scene may be engaged (listening/thinking/speaking) at a time.
    // Several cues can be active at once (e.g. an arrival cue + an entry cue), and the A
    // button is read globally — without this, pressing A would make every cue answer,
    // producing two overlapping voices and competing dock text.
    private static CueVoiceLoop _activeLoop;

    private CueVisualState _state = CueVisualState.Idle;
    private int _consumed;
    private bool _firstSentenceSent;
    private bool _cardAddedThisTurn;
    private float _tTranscript;
    private float _lastAnswerEnd = -99f;

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        // arrival cues appear already inside the context -> the encounter starts now;
        // entry cues start their encounter when the user comes near (below).
        var cfg = GetComponent<CueConfig>();
        if (cfg != null && cfg.IsArrival) CueLogger.Begin("cue_created");

        if (earcons)
        {
            _earcons = GetComponent<CueEarcons>();
            if (!_earcons) _earcons = gameObject.AddComponent<CueEarcons>();
        }

        // grow-in on approach instead of popping on at the wake radius (self-wires to the sensor)
        if (!GetComponent<CueApproachFade>()) gameObject.AddComponent<CueApproachFade>();

        stt.onTranscript.AddListener(OnTranscript);

        llm.OnAnswerDelta += OnAnswerDelta;
        llm.OnAnswerComplete += OnAnswerComplete;
        llm.OnError += _ => ToState(RestState());
        llm.ToolHandler = HandleTool;

        speaker.onSpeakStarted.AddListener(() =>
        {
            Debug.Log($"[CueVoiceLoop] TIMING first-audio: {Time.realtimeSinceStartup - _tTranscript:F2}s after transcript");
            ToState(CueVisualState.Speaking);
        });
        speaker.onAllFinished.AddListener(() =>
        {
            _lastAnswerEnd = Time.time;
            ToState(RestState());
        });

        proximity.onEnterOuter.AddListener(() =>
        {
            if (_state == CueVisualState.Idle)
            {
                ToState(CueVisualState.Available);
                if (_earcons) _earcons.PlayWake(); // chime only on the actual wake
            }
            CueLogger.Begin("cue_shown"); // entry encounter opens when the user is near
        });
        // Latch: once activated the cue never deactivates on walking away (study behaviour).
        // (No onExitOuter reset — it stays available until the transition is entered.)
        // NO proximity auto-listen: the mic only opens when the user presses A. Walking up to
        // a cue and having it silently listen was confusing (you could not tell whether it was
        // on). Proximity still drives the visual state (Idle -> Available) and the log encounter.

        ToState(CueVisualState.Idle);
    }

    private void Update()
    {
        if (!OVRInput.GetDown(OVRInput.Button.One)) return;
        // A only affects the cue the user is actually near (several cues can be active at once)
        if (proximity != null && !proximity.InOuter && _activeLoop != this) return;

        switch (_state)
        {
            case CueVisualState.Listening:
                stt.StopNow();
                break;
            case CueVisualState.Speaking:
                speaker.StopAll();
                StartListening();
                break;
            case CueVisualState.Thinking:
                break;
            default:
                StartListening();
                break;
        }
    }

    /// <summary>True once the user has actually engaged this cue (spoken to it / pressed A).
    /// Building_TransitionCues uses it to auto-retire an ignored arrival cue.</summary>
    public bool HasEngaged { get; private set; }

    private void StartListening()
    {
        // engagement lock: if another cue is already listening/answering, ignore
        if (_activeLoop != null && _activeLoop != this) return;
        _activeLoop = this;
        HasEngaged = true;
        if (_earcons) _earcons.PlayListen();
        stt.StartListening();
        ToState(CueVisualState.Listening);
        Debug.Log("[CueVoiceLoop] Listening...");
    }

    private void OnTranscript(string text)
    {
        _tTranscript = Time.realtimeSinceStartup;
        Debug.Log($"[CueVoiceLoop] Transcript: '{text}'");
        CueLogger.Event("text_input", text);
        if (string.IsNullOrWhiteSpace(text))
        {
            ToState(RestState());
            return;
        }

        _consumed = 0;
        _firstSentenceSent = false;
        _cardAddedThisTurn = false;
        ToState(CueVisualState.Thinking);
        dock.ShowQuestion(text);
        llm.SendUserMessage(text);
    }

    private void OnAnswerDelta(string accumulated)
    {
        dock.ShowAnswer(accumulated);
        for (var i = _consumed; i < accumulated.Length - 1; i++)
        {
            if (!IsSentenceEnd(accumulated[i]) || !char.IsWhiteSpace(accumulated[i + 1])) continue;
            EnqueueSpan(accumulated, i + 1);
        }
    }

    private void OnAnswerComplete(string full)
    {
        Debug.Log($"[CueVoiceLoop] TIMING llm-complete: {Time.realtimeSinceStartup - _tTranscript:F2}s after transcript");
        Debug.Log($"[CueVoiceLoop] LLM response: '{full}'");
        CueLogger.Event("text_output", full);
        dock.ShowAnswer(full);
        if (full.Length > _consumed) EnqueueSpan(full, full.Length);
        speaker.EndOfInput();
        if (!speaker.IsActive) ToState(RestState());
    }

    private void EnqueueSpan(string text, int end)
    {
        var sentence = text.Substring(_consumed, end - _consumed).Trim();
        _consumed = end;
        if (sentence.Length == 0) return;
        if (!_firstSentenceSent)
        {
            _firstSentenceSent = true;
            Debug.Log($"[CueVoiceLoop] TIMING first-sentence: {Time.realtimeSinceStartup - _tTranscript:F2}s after transcript");
        }

        speaker.Enqueue(sentence);
    }

    private string HandleTool(string name, JObject args)
    {
        if (answerRow == null) return "no answer row";

        // voice intents (shared with the buttons)
        if (name == "start_transition")
        {
            if (events) events.RaiseStartTransition();
            return "starting transition";
        }
        if (name == "dismiss_cue")
        {
            // Only arrival cues may be closed by voice (entry cues must be entered, not removed).
            // Mirror the Close button: raise onCloseCue, log, and deactivate the cue — one shot, no confirm.
            var cfg = GetComponent<CueConfig>();
            if (cfg != null && cfg.IsArrival)
            {
                if (events) events.RaiseCloseCue();
                CueLogger.Event("closed");
                CloseCueRoot();
                return "cue closed";
            }
            CueLogger.Event("dismiss_requested_ignored", "voice");
            return "closing is not available on this cue";
        }

        // hide is always allowed, and re-expands the invitation when the row empties
        if (name == "hide_card")
        {
            var id = (string)args["card"] ?? "";
            var r = answerRow.Remove(id);
            CueLogger.Event("panel_hidden", panel: id);
            if (answerRow.IsEmpty && invitation) invitation.SetSmall(false);
            return r;
        }

        if (name != "show_card") return $"unknown tool '{name}'";

        // one card added per user turn — the cue reveals content step by step
        if (_cardAddedThisTurn)
            return "one card was already added for this question; wait for the next question before adding another";

        var result = answerRow.ShowCard((string)args["card"] ?? "");
        _cardAddedThisTurn = true;
        if (result.StartsWith("card")) CueLogger.Event("panel_shown", panel: (string)args["card"] ?? "");
        if (invitation) invitation.SetSmall(true); // any card => shrink invitation
        return result;
    }

    // Deactivate the whole cue — the same "TransitionCue"-tagged ancestor the Close button uses.
    private void CloseCueRoot()
    {
        var t = transform;
        while (t != null)
        {
            if (t.CompareTag("TransitionCue")) { t.gameObject.SetActive(false); return; }
            t = t.parent;
        }
        gameObject.SetActive(false); // fallback if the tag isn't found
    }

    private CueVisualState RestState() => proximity.InOuter ? CueVisualState.Available : CueVisualState.Idle;

    private static bool IsSentenceEnd(char c) => c is '.' or '!' or '?';

    private void ToState(CueVisualState state)
    {
        _state = state;
        // release the engagement lock once this cue is back at rest
        if ((state is CueVisualState.Idle or CueVisualState.Available) && _activeLoop == this)
            _activeLoop = null;
        head.SetState(state);
        face.SetState(state);
        dock.SetPhase(state);
    }

    // release the lock if this cue is deactivated mid-engagement (e.g. transition entered / closed)
    private void OnDisable()
    {
        if (_activeLoop == this) _activeLoop = null;
    }
}
