using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Study log — ONE JSON file per queue (cue) encounter, matching the agreed schema.
/// An encounter opens when the cue is shown (entry) or created (arrival) and closes on
/// transition_entered / closed / walked_away / app_quit. Header: sessionId (ongoing per
/// app launch), participant, queue, cueType, transition, start/end time + reason. Events:
/// { t, type, [text], [panel] } for text_input, text_output, panel_shown/hidden, and
/// lifecycle markers. Written crash-safe (events streamed to a .partial, assembled to
/// valid .json on close; orphaned partials recovered on next launch). On the headset under
/// persistentDataPath/EntryCueLogs.
///
/// ONE LOGGER PER CUE. There is deliberately NO static Instance: several cues are alive at
/// the same time (an arrival cue plus the next entry cue), so a shared slot would attribute
/// every event to whichever cue awoke last — that is exactly how T1_Arrival's encounters
/// ended up inside the T2_Entry files. Call sites resolve their own cue's logger with
/// <see cref="For"/>, which walks up from a button or child object to the cue root.
/// </summary>
public class CueLogger : MonoBehaviour
{
    [Header("Study")]
    [Tooltip("Fallback only. The real id comes from participant.txt in persistentDataPath " +
             "when that file exists, so a participant can be set once per session instead of " +
             "on every cue prefab.")]
    [SerializeField] private string participantId = "P00";

    [Tooltip("Fallback only. Normally derived from the cue name (T7_Entry -> T7), so a cue " +
             "can never log under a stale transition number.")]
    [SerializeField] private string transitionCode = "T1";

    private static int _sessionId = -1;
    private static string _participant;          // resolved once per app launch, shared by all cues
    private static bool _quitting;               // true once the app is actually shutting down
    private static bool _sweptOrphans;           // crash recovery runs ONCE per app launch

    // Statics must not survive into the next play session (Editor domain reload can be off).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    { _sessionId = -1; _participant = null; _quitting = false; _sweptOrphans = false; }

    private string _dir;
    private string _queue = "Cue";
    private string _cueType = "entry";
    private string _transition = "T1";
    private string _openReason = "cue_shown";    // reason used if an event arrives with no open encounter
    private JObject _header;
    private JArray _events;
    private string _partialPath;
    private bool _open;
    private bool _finished;   // a terminal End() happened on this cue

    /// <summary>
    /// The logger belonging to <paramref name="c"/>'s cue. Buttons and other children resolve
    /// upwards to the cue root. Returns null outside a cue, so every call site can use ?.
    /// </summary>
    public static CueLogger For(Component c)
    {
        if (c == null) return null;
        var own = c.GetComponent<CueLogger>();          // cue root asking for itself
        return own != null ? own : c.GetComponentInParent<CueLogger>(true);
    }

    private void Awake()
    {
        var cfg = GetComponent<CueConfig>();
        _queue = cfg != null && !string.IsNullOrWhiteSpace(cfg.cueName) ? cfg.cueName.Trim() : "Cue";
        _cueType = cfg != null && cfg.IsArrival ? "arrival" : "entry";
        _openReason = _cueType == "arrival" ? "cue_created" : "cue_shown";
        _transition = ResolveTransition(_queue, transitionCode);

        try
        {
            _dir = Path.Combine(Application.persistentDataPath, "EntryCueLogs");
            Directory.CreateDirectory(_dir);
            GetOrCreateSessionId(_dir);
            ResolveParticipant(_dir, participantId);
            RecoverOrphans(_dir); // assemble any .partial left by a previous crash
            _sweptOrphans = true;
        }
        catch (Exception e) { Debug.LogError($"[CueLogger] init failed: {e.Message}"); }
    }

    // ---- public API (called by the cue this logger belongs to) ----

    /// <summary>Open a new encounter file. reason = cue_shown (entry) | cue_created (arrival). Idempotent while open.</summary>
    public void Begin(string reason)
    {
        if (_open || string.IsNullOrEmpty(_dir)) return;
        _finished = false;       // a deliberate new encounter on this cue is allowed
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        _header = new JObject
        {
            ["sessionId"] = _sessionId,
            ["participant"] = _participant,
            ["queue"] = _queue,
            ["cueType"] = _cueType,
            ["transition"] = _transition,
            ["startTime"] = Now(),
            ["startReason"] = reason,
            ["endTime"] = null,
            ["endReason"] = null
        };
        _events = new JArray();
        _partialPath = Path.Combine(_dir, Sanitize($"{_participant}_{_queue}_s{_sessionId}_{stamp}") + ".partial.jsonl");
        _open = true;
        try { File.WriteAllText(_partialPath, _header.ToString(Formatting.None) + "\n"); } catch { }
        Event(reason); // first event mirrors the start reason
    }

    /// <summary>Add an event: type = text_input|text_output|panel_shown|panel_hidden|... text/panel optional.</summary>
    public void Event(string type, string text = null, string panel = null, string source = null)
    {
        if (_finished) return;   // trailing tail of a closed encounter — never resurrect it
        if (!_open) Begin(_openReason);
        var ev = new JObject { ["t"] = Now(), ["type"] = type };
        if (text != null) ev["text"] = text;
        if (panel != null) ev["panel"] = panel;
        if (source != null) ev["source"] = source;
        _events.Add(ev);
        try { File.AppendAllText(_partialPath, ev.ToString(Formatting.None) + "\n"); } catch { }
    }

    /// <summary>
    /// Close the encounter and write the final JSON. reason = transition_entered|closed|walked_away|app_quit,
    /// source = button|voice|idle_timeout|superseded (optional).
    /// End() emits the terminal event itself, which guarantees the invariant the phone side asked for:
    /// THE LAST EVENT'S TYPE ALWAYS EQUALS endReason. No file can end silently, so "did the participant
    /// dismiss this, or did it just go away?" is answerable from every single record.
    /// </summary>
    /// <param name="openIfNeeded">
    /// TRUE for terminal USER actions (Betreten, Schließen, the voice equivalents). An entry cue
    /// only opens its encounter from the proximity sensor, so if the user acts on a cue before
    /// onEnterOuter has fired the encounter is not open yet — and the whole record used to be
    /// dropped on the floor. That is how T7_Entry vanished from library session 11: the exit cue
    /// appears 10 s after the arrival cue is closed, and it was entered before proximity fired.
    /// FALSE for teardown (OnDisable/OnDestroy), so cues the user never saw create no file.
    /// </param>
    public void End(string reason, string source = null, bool openIfNeeded = false)
    {
        if (!_open)
        {
            if (!openIfNeeded) return;
            Begin(_openReason);
            if (!_open) return;          // logging directory unavailable
        }
        Event(reason, source: source);   // terminal event, written while the encounter is still open
        _open = false;
        _finished = true;
        _header["endTime"] = Now();
        _header["endReason"] = reason;
        _header["events"] = _events;
        var finalPath = Path.ChangeExtension(_partialPath.Replace(".partial", ""), ".json");
        try
        {
            File.WriteAllText(finalPath, _header.ToString(Formatting.Indented));
            if (File.Exists(_partialPath)) File.Delete(_partialPath);
            Debug.Log($"[CueLogger] wrote {finalPath} ({_events.Count} events, end={reason})");
        }
        catch (Exception e) { Debug.LogError($"[CueLogger] write failed: {e.Message}"); }
    }

    // ---- impl ----

    private static string Now() =>
        DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

    // "T7_Entry" -> "T7". The cue name is the single source of truth, so renumbering the
    // scenario can never leave a prefab logging a stale inspector value. Names that are not
    // T<n>_… (the tutorial cues) keep whatever the field says.
    private static string ResolveTransition(string queue, string field)
    {
        var m = Regex.Match(queue ?? "", @"^T(\d+)_");
        if (!m.Success) return string.IsNullOrWhiteSpace(field) ? "T0" : field.Trim();
        var derived = "T" + m.Groups[1].Value;
        if (!string.IsNullOrWhiteSpace(field) && field.Trim() != derived)
            Debug.LogWarning($"[CueLogger] {queue}: transitionCode field says '{field}', " +
                             $"logging '{derived}' from the cue name instead.");
        return derived;
    }

    private static void GetOrCreateSessionId(string dir)
    {
        if (_sessionId >= 0) return;
        var counter = Path.Combine(dir, "session_counter.txt");
        var last = 0;
        try { if (File.Exists(counter)) int.TryParse(File.ReadAllText(counter).Trim(), out last); } catch { }
        _sessionId = last + 1;
        try { File.WriteAllText(counter, _sessionId.ToString()); } catch { }
    }

    // One participant id for the whole app launch. participant.txt (pushed with adb, or edited
    // on the headset) wins, so a session can be labelled without rebuilding or touching prefabs.
    private static void ResolveParticipant(string dir, string fallback)
    {
        if (!string.IsNullOrEmpty(_participant)) return;
        try
        {
            var f = Path.Combine(Application.persistentDataPath, "participant.txt");
            if (File.Exists(f))
            {
                var id = File.ReadAllText(f).Trim();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _participant = id;
                    Debug.Log($"[CueLogger] participant '{_participant}' (from participant.txt)");
                    return;
                }
            }
        }
        catch { }
        _participant = string.IsNullOrWhiteSpace(fallback) ? "P00" : fallback.Trim();
    }

    // Assemble any .partial.jsonl left by a crash into a valid .json (endReason app_quit).
    private static void RecoverOrphans(string dir)
    {
        if (_sweptOrphans) return;   // only the first cue of this launch may sweep
        string[] partials;
        try { partials = Directory.GetFiles(dir, "*.partial.jsonl"); } catch { return; }
        foreach (var p in partials)
        {
            try
            {
                var lines = File.ReadAllLines(p);
                if (lines.Length == 0) { File.Delete(p); continue; }
                var header = JObject.Parse(lines[0]);
                var events = new JArray();
                for (var i = 1; i < lines.Length; i++)
                    if (!string.IsNullOrWhiteSpace(lines[i])) events.Add(JObject.Parse(lines[i]));
                header["endTime"] = events.Count > 0 ? events[events.Count - 1]["t"] : header["startTime"];
                header["endReason"] = "app_quit";
                header["events"] = events;
                File.WriteAllText(Path.ChangeExtension(p.Replace(".partial", ""), ".json"),
                    header.ToString(Formatting.Indented));
                File.Delete(p);
            }
            catch { /* leave the partial for manual inspection */ }
        }
    }

    private static string Sanitize(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s) sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        return sb.ToString();
    }

    private void OnApplicationQuit() => _quitting = true;

    // Catch-all so no encounter can stay open. The scene scripts retire cues in many places
    // (Building_TransitionCues, Mensa_FriendCue, Lecture_TransitionCues, ArrivalCue) with a
    // plain SetActive(false), which never reaches OnDestroy — those files used to be stamped
    // app_quit with the timestamp of scene teardown instead of the moment the cue went away.
    // Unity runs OnDisable before OnDestroy; on a real destroy activeSelf is still true, which
    // is how we tell "the flow hid this cue" from "this object is going away".
    private void OnDisable()
    {
        if (!_open) return;
        if (_quitting) { End("app_quit"); return; }
        // deactivated by the flow: the next cue fired, the section moved on, the building
        // retired it. No user action — same category as the phone's "superseded".
        if (!gameObject.activeSelf) End("closed", "superseded");
    }

    private void OnDestroy()
    {
        if (_open) End(_quitting ? "app_quit" : "closed", _quitting ? null : "superseded");
    }
}
