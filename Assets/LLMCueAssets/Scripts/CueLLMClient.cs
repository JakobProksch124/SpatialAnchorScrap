using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Streaming OpenAI chat client with conversation history and function calling —
/// replaces Meta's LlmAgent (which supports neither). Tool calls are dispatched
/// to <see cref="ToolHandler"/> and their results fed back for a follow-up round,
/// so the model can reference what the UI now shows.
/// </summary>
public class CueLLMClient : MonoBehaviour
{
    [SerializeField] private CueSecrets secrets;
    [SerializeField] private TextAsset systemPromptAsset;
    [SerializeField] private TextAsset knowledgeBaseAsset;
    [SerializeField] private string model = "gpt-4.1-mini";
    [SerializeField] private int maxHistoryMessages = 12;
    [SerializeField] private int maxCompletionTokens = 220;

    /// <summary>Accumulated spoken answer text (across tool rounds) after each token.</summary>
    public Action<string> OnAnswerDelta;
    public Action<string> OnAnswerComplete;
    public Action<string> OnError;

    /// <summary>Executes a tool call (name, parsed args) and returns a short result string for the model.</summary>
    public Func<string, JObject, string> ToolHandler;

    public bool IsBusy { get; private set; }

    private readonly List<JObject> _history = new();
    private string _answerSoFar;

    private const string Endpoint = "https://api.openai.com/v1/chat/completions";

    // card catalog (id, title, when-to-show) supplied per cue by CueController from CueConfig
    private readonly List<(string id, string title, string when)> _cards = new();

    /// <summary>The full context this cue may talk about (from CueConfig); overrides the KB asset when set.</summary>
    public string ContextText { get; set; } = "";

    public void SetCards(List<CueCardDef> defs)
    {
        _cards.Clear();
        if (defs == null) return;
        foreach (var d in defs)
            if (d != null && !string.IsNullOrWhiteSpace(d.id))
                _cards.Add((d.id.Trim(), d.title, d.whenToShow));
    }

    private JArray BuildToolsJson()
    {
        var tools = new JArray();

        if (_cards.Count > 0)
        {
            var ids = new JArray();
            foreach (var c in _cards) ids.Add(c.id);
            tools.Add(Fn("show_card",
                "Show one answer card next to the cue. Pick the card whose purpose matches the user's question " +
                "(see the card list in the context). Add at most one card per question. You MUST call this to " +
                "actually show a card — never just say you did.",
                new JObject { ["card"] = EnumParam(ids) }, "card"));
            tools.Add(Fn("hide_card",
                "Remove ONE info card the user no longer wants (e.g. 'hide the preview', 'close the video'). " +
                "Removes only that card, NOT the whole cue. You MUST call this to actually hide it — never just " +
                "say you did.",
                new JObject { ["card"] = EnumParam(ids) }, "card"));
        }
        
        tools.Add(Fn("start_transition",
            "Start the transition into the new context (AR or VR). Call ONLY on a clear, explicit intent to enter/join or leave " +
            "('take me in', 'take me out', 'ich moechte beitreten', 'ich will rein', 'ich will raus'). Never just because the user asked for info.",
            new JObject(), null));
        tools.Add(Fn("dismiss_cue",
            "Close the ENTIRE cue (not a single card). Only for when the user clearly wants the whole cue gone " +
            "AND confirms. To close one info card use hide_card, never this.",
            new JObject(), null));
        return tools;
    }

    private static JObject Fn(string name, string desc, JObject props, string required)
    {
        var req = new JArray();
        if (required != null) req.Add(required);
        return new JObject
        {
            ["type"] = "function",
            ["function"] = new JObject
            {
                ["name"] = name,
                ["description"] = desc,
                ["parameters"] = new JObject { ["type"] = "object", ["properties"] = props, ["required"] = req }
            }
        };
    }

    private static JObject EnumParam(JArray values) => new() { ["type"] = "string", ["enum"] = values };

    public void SendUserMessage(string text)
    {
        if (IsBusy)
        {
            Debug.LogWarning("[CueLLMClient] Busy — dropping message.");
            return;
        }

        _history.Add(new JObject { ["role"] = "user", ["content"] = text });
        TrimHistory();
        _answerSoFar = string.Empty;
        IsBusy = true;
        StartCoroutine(RunTurn(0));
    }

    public void ResetConversation() => _history.Clear();

    /// <summary>Extra per-cue instruction (e.g. entry vs arrival role), set from CueConfig.</summary>
    public string ExtraInstruction { get; set; } = "";

    private string BuildSystemPrompt()
    {
        var ctx = !string.IsNullOrWhiteSpace(ContextText)
            ? ContextText
            : (knowledgeBaseAsset ? knowledgeBaseAsset.text : "(no context provided)");

        var cards = "";
        if (_cards.Count > 0)
        {
            var sb = new System.Text.StringBuilder("\n\n# Cards you can show (use show_card with the id)\n");
            foreach (var c in _cards) sb.Append($"- {c.id}: {c.title} — {c.when}\n");
            cards = sb.ToString();
        }

        return (systemPromptAsset ? systemPromptAsset.text : "You are a helpful voice assistant.") +
               (string.IsNullOrEmpty(ExtraInstruction) ? "" : "\n\n# This cue\n" + ExtraInstruction) +
               "\n\n# Context — the ONLY source of truth about the target context\n\n" + ctx +
               cards;
    }

    private IEnumerator RunTurn(int depth)
    {
        if (depth > 3)
        {
            Fail("Too many consecutive tool rounds.");
            yield break;
        }

        var messages = new JArray { new JObject { ["role"] = "system", ["content"] = BuildSystemPrompt() } };
        foreach (var m in _history) messages.Add(m);

        var payload = new JObject
        {
            ["model"] = model,
            ["stream"] = true,
            ["messages"] = messages,
            ["tools"] = BuildToolsJson(),
            ["max_completion_tokens"] = maxCompletionTokens
        };

        var roundContent = new StringBuilder();
        var toolCalls = new SortedDictionary<int, ToolCallBuilder>();

        var handler = new SseHandler
        {
            OnPayload = data =>
            {
                if (data == "[DONE]") return;
                JObject obj;
                try { obj = JObject.Parse(data); }
                catch { return; }

                if (obj["choices"]?.First is not JObject choice) return;
                if (choice["delta"] is not JObject delta) return;

                var content = (string)delta["content"];
                if (!string.IsNullOrEmpty(content))
                {
                    roundContent.Append(content);
                    _answerSoFar += content;
                    OnAnswerDelta?.Invoke(_answerSoFar);
                }

                if (delta["tool_calls"] is not JArray tcs) return;
                foreach (var tcToken in tcs)
                {
                    if (tcToken is not JObject tc) continue;
                    var idx = (int?)tc["index"] ?? 0;
                    if (!toolCalls.TryGetValue(idx, out var b)) toolCalls[idx] = b = new ToolCallBuilder();
                    b.Id ??= (string)tc["id"];
                    if (tc["function"] is not JObject fn) continue;
                    b.Name ??= (string)fn["name"];
                    b.Args.Append((string)fn["arguments"] ?? "");
                }
            }
        };

        using var req = new UnityWebRequest(Endpoint, "POST");
        req.uploadHandler = new UploadHandlerRaw(
            Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None)));
        req.downloadHandler = handler;
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + (secrets ? secrets.openAiApiKey : ""));
        req.timeout = 60;

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Fail($"HTTP {req.responseCode} ({req.error}): {handler.RawTail}");
            yield break;
        }

        if (toolCalls.Count > 0)
        {
            // Execute the tools locally (UI updates now).
            foreach (var b in toolCalls.Values)
            {
                JObject args;
                var raw = b.Args.ToString();
                try { args = JObject.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw); }
                catch { args = new JObject(); }

                string result;
                try { result = ToolHandler?.Invoke(b.Name, args) ?? "ok"; }
                catch (Exception e) { result = "error: " + e.Message; }
                Debug.Log($"[CueLLMClient] tool {b.Name}({raw}) -> {result}");
                _lastToolResult = result;
            }

            // FAST PATH: the model already spoke alongside the tool call -> use that answer
            // directly, no second round-trip. Store it as a plain assistant turn so history stays valid.
            if (roundContent.Length > 0)
            {
                _history.Add(new JObject { ["role"] = "assistant", ["content"] = _answerSoFar });
                IsBusy = false;
                OnAnswerComplete?.Invoke(_answerSoFar);
                yield break;
            }

            // Tool-only response (model didn't speak): do the proper tool round to get the reply.
            var tcArray = new JArray();
            foreach (var b in toolCalls.Values)
                tcArray.Add(new JObject
                {
                    ["id"] = b.Id, ["type"] = "function",
                    ["function"] = new JObject { ["name"] = b.Name, ["arguments"] = b.Args.ToString() }
                });
            _history.Add(new JObject { ["role"] = "assistant", ["content"] = null, ["tool_calls"] = tcArray });
            foreach (var b in toolCalls.Values)
                _history.Add(new JObject { ["role"] = "tool", ["tool_call_id"] = b.Id, ["content"] = _lastToolResult });

            yield return RunTurn(depth + 1);
        }
        else
        {
            _history.Add(new JObject { ["role"] = "assistant", ["content"] = _answerSoFar });
            IsBusy = false;
            OnAnswerComplete?.Invoke(_answerSoFar);
        }
    }

    private string _lastToolResult = "ok";

    private void Fail(string message)
    {
        IsBusy = false;
        Debug.LogError($"[CueLLMClient] {message}");
        OnError?.Invoke(message);
    }

    private void TrimHistory()
    {
        while (_history.Count > maxHistoryMessages) _history.RemoveAt(0);
        // Never start the window mid-tool-exchange.
        while (_history.Count > 0 && (string)_history[0]["role"] != "user") _history.RemoveAt(0);
    }

    private class ToolCallBuilder
    {
        public string Id;
        public string Name;
        public readonly StringBuilder Args = new();
    }

    /// <summary>Parses server-sent-event lines out of the response stream as bytes arrive.</summary>
    private class SseHandler : DownloadHandlerScript
    {
        public Action<string> OnPayload;
        private readonly List<byte> _buf = new();
        private readonly StringBuilder _rawTail = new();
        public string RawTail => _rawTail.ToString();

        protected override bool ReceiveData(byte[] data, int len)
        {
            for (var i = 0; i < len; i++)
            {
                if (data[i] == (byte)'\n')
                {
                    var line = Encoding.UTF8.GetString(_buf.ToArray()).TrimEnd('\r');
                    _buf.Clear();
                    HandleLine(line);
                }
                else
                {
                    _buf.Add(data[i]);
                }
            }

            return true;
        }

        private void HandleLine(string line)
        {
            if (line.StartsWith("data:"))
                OnPayload?.Invoke(line.Substring(5).Trim());
            else if (line.Length > 0 && _rawTail.Length < 2000)
                _rawTail.Append(line); // non-SSE output = probably an error body; keep for diagnostics
        }
    }
}
