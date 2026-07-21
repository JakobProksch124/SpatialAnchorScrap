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

    private const string ToolsJson = @"[
      {""type"":""function"",""function"":{
        ""name"":""show_preview"",
        ""description"":""Show a preview panel about one aspect of the VR target context. Call this whenever the user asks what something looks like, how the controls work, who else is there, or how it sounds."",
        ""parameters"":{""type"":""object"",""properties"":{
            ""topic"":{""type"":""string"",""enum"":[""environment"",""controls"",""social"",""audio""]}},
          ""required"":[""topic""]}}},
      {""type"":""function"",""function"":{
        ""name"":""show_transition_info"",
        ""description"":""Show the panel explaining how the transition into VR works and how to come back."",
        ""parameters"":{""type"":""object"",""properties"":{}}}},
      {""type"":""function"",""function"":{
        ""name"":""hide_panel"",
        ""description"":""Hide the preview/info panel, e.g. when the user says hide it, thanks, or is done."",
        ""parameters"":{""type"":""object"",""properties"":{}}}}
    ]";

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

    private string BuildSystemPrompt() =>
        (systemPromptAsset ? systemPromptAsset.text : "You are a helpful voice assistant.") +
        "\n\n# Knowledge base — the ONLY source of truth about the target context\n\n" +
        (knowledgeBaseAsset ? knowledgeBaseAsset.text : "(no knowledge base provided)");

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
            ["tools"] = JArray.Parse(ToolsJson),
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
            var tcArray = new JArray();
            foreach (var b in toolCalls.Values)
            {
                tcArray.Add(new JObject
                {
                    ["id"] = b.Id,
                    ["type"] = "function",
                    ["function"] = new JObject { ["name"] = b.Name, ["arguments"] = b.Args.ToString() }
                });
            }

            _history.Add(new JObject
            {
                ["role"] = "assistant",
                ["content"] = roundContent.Length > 0 ? roundContent.ToString() : null,
                ["tool_calls"] = tcArray
            });

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
                _history.Add(new JObject { ["role"] = "tool", ["tool_call_id"] = b.Id, ["content"] = result });
            }

            yield return RunTurn(depth + 1);
        }
        else
        {
            _history.Add(new JObject { ["role"] = "assistant", ["content"] = _answerSoFar });
            IsBusy = false;
            OnAnswerComplete?.Invoke(_answerSoFar);
        }
    }

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
