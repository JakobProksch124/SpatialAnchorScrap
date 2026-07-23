using System;
using System.Globalization;
using System.IO;
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
/// </summary>
public class CueLogger : MonoBehaviour
{
    [Header("Study (set per participant)")]
    [SerializeField] private string participantId = "P00";
    [SerializeField] private string transitionCode = "T1";

    public static CueLogger Instance { get; private set; }
    private static int _sessionId = -1;

    private string _dir;
    private string _queue = "Cue";
    private string _cueType = "entry";
    private JObject _header;
    private JArray _events;
    private string _partialPath;
    private bool _open;

    private void Awake()
    {
        Instance = this;
        var cfg = GetComponent<CueConfig>();
        _queue = cfg != null && !string.IsNullOrWhiteSpace(cfg.cueName) ? cfg.cueName.Trim() : "Cue";
        _cueType = cfg != null && cfg.IsArrival ? "arrival" : "entry";

        try
        {
            _dir = Path.Combine(Application.persistentDataPath, "EntryCueLogs");
            Directory.CreateDirectory(_dir);
            GetOrCreateSessionId(_dir);
            RecoverOrphans(_dir); // assemble any .partial left by a previous crash
        }
        catch (Exception e) { Debug.LogError($"[CueLogger] init failed: {e.Message}"); }
    }

    // ---- public API (called by the cue) ----

    /// <summary>Open a new encounter file. reason = cue_shown (entry) | cue_created (arrival). Idempotent while open.</summary>
    public static void Begin(string reason) => Instance?.BeginImpl(reason);

    /// <summary>Add an event: type = text_input|text_output|panel_shown|panel_hidden|... text/panel optional.</summary>
    public static void Event(string type, string text = null, string panel = null) =>
        Instance?.EventImpl(type, text, panel);

    /// <summary>Close the encounter and write the final JSON. reason = transition_entered|closed|walked_away|app_quit.</summary>
    public static void End(string reason) => Instance?.EndImpl(reason);

    // ---- impl ----

    private void BeginImpl(string reason)
    {
        if (_open) return;
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        _header = new JObject
        {
            ["sessionId"] = _sessionId,
            ["participant"] = participantId,
            ["queue"] = _queue,
            ["cueType"] = _cueType,
            ["transition"] = transitionCode,
            ["startTime"] = Now(),
            ["startReason"] = reason,
            ["endTime"] = null,
            ["endReason"] = null
        };
        _events = new JArray();
        _partialPath = Path.Combine(_dir, Sanitize($"{participantId}_{_queue}_s{_sessionId}_{stamp}") + ".partial.jsonl");
        _open = true;
        try { File.WriteAllText(_partialPath, _header.ToString(Formatting.None) + "\n"); } catch { }
        EventImpl(reason, null, null); // first event mirrors the start reason
    }

    private void EventImpl(string type, string text, string panel)
    {
        if (!_open) BeginImpl("cue_shown");
        var ev = new JObject { ["t"] = Now(), ["type"] = type };
        if (text != null) ev["text"] = text;
        if (panel != null) ev["panel"] = panel;
        _events.Add(ev);
        try { File.AppendAllText(_partialPath, ev.ToString(Formatting.None) + "\n"); } catch { }
    }

    private void EndImpl(string reason)
    {
        if (!_open) return;
        _open = false;
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

    private static string Now() =>
        DateTime.Now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);

    private static void GetOrCreateSessionId(string dir)
    {
        if (_sessionId >= 0) return;
        var counter = Path.Combine(dir, "session_counter.txt");
        var last = 0;
        try { if (File.Exists(counter)) int.TryParse(File.ReadAllText(counter).Trim(), out last); } catch { }
        _sessionId = last + 1;
        try { File.WriteAllText(counter, _sessionId.ToString()); } catch { }
    }

    // Assemble any .partial.jsonl left by a crash into a valid .json (endReason app_quit).
    private static void RecoverOrphans(string dir)
    {
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

    private void OnDestroy()
    {
        if (_open) EndImpl("app_quit");
        if (Instance == this) Instance = null;
    }
}
