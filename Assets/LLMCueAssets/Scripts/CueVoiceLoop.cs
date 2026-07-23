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
        proximity.onEnterInner.AddListener(TryAutoListen);

        ToState(CueVisualState.Idle);
    }

    private void Update()
    {
        if (!OVRInput.GetDown(OVRInput.Button.One)) return;

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

    private void TryAutoListen()
    {
        if (_state is not (CueVisualState.Available or CueVisualState.Idle)) return;
        if (llm.IsBusy || Time.time - _lastAnswerEnd < relistenCooldown) return;
        StartListening();
    }

    private void StartListening()
    {
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
        if (name == "enter_vr")
        {
            if (events) events.RaiseStartTransition();
            return "entering VR";
        }
        if (name == "dismiss_cue")
        {
            if (events) events.RaiseStartTransition();
            return "cue dismissed";
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

    private CueVisualState RestState() => proximity.InOuter ? CueVisualState.Available : CueVisualState.Idle;

    private static bool IsSentenceEnd(char c) => c is '.' or '!' or '?';

    private void ToState(CueVisualState state)
    {
        _state = state;
        head.SetState(state);
        face.SetState(state);
        dock.SetPhase(state);
    }
}
