using UnityEngine;

/// <summary>
/// Holds API keys for the custom LLM client. Lives in Assets/MetaXR/ which is
/// git-ignored; filled by EntryCue > Sync OpenAI Key.
/// </summary>
public class CueSecrets : ScriptableObject
{
    public string openAiApiKey;
}
