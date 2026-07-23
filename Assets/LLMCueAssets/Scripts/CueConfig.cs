using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All per-instance, Inspector-editable content for one cue. This turns the cue
/// into a reusable blueprint: drop the prefab, flip Mode, and edit every text /
/// which cards may appear — no code or plugin menu needed. CueController reads
/// this and applies it to the theme, invitation, status box, buttons, answer row
/// and the assistant.
/// </summary>
public class CueConfig : MonoBehaviour
{
    public enum Mode { Entry, Arrival }

    [Header("Identity")]
    [Tooltip("Unique name for this cue, e.g. 'EntryCue1' / 'ArrivalCue1'. Used as the log file name.")]
    public string cueName = "EntryCue1";

    [Header("Mode  (Entry = before the transition · Arrival = already inside the context)")]
    public Mode mode = Mode.Entry;

    [Header("Invitation — Entry")]
    public string entryTitle = "Wechsel zu VR";
    [Tooltip("Word inside the title tinted with the accent colour (leave empty for none).")]
    public string entryHighlight = "VR";
    [TextArea] public string entryReason = "Um dir das Chemielabor anzuschauen";

    [Header("Invitation — Arrival")]
    [TextArea] public string arrivalTitle = "Hast du Fragen über den Kontext, in dem du dich gerade befindest?";
    public string arrivalHighlight = "";
    [TextArea] public string arrivalReason = "";

    [Header("Status box (both modes)")]
    public string waitingText = "Ich warte auf deine Frage";
    public string listeningText = "Ich höre zu…";
    public string thinkingText = "Sammle Antworten";

    [Header("Buttons")]
    public string enterLabel = "Enter VR";     // entry only
    public string dismissLabel = "Jetzt nicht"; // entry only
    public string closeLabel = "Schließen";     // arrival (single button)

    [Header("Assistant")]
    [Tooltip("Extra line appended to the system prompt for this cue (e.g. arrival = in-context helper).")]
    [TextArea] public string assistantNote =
        "You are an ENTRY cue shown before the user enters the target context.";

    [Tooltip("The FULL context this cue may talk about — the only source of truth for the assistant. " +
             "Editing this here makes the cue a self-contained blueprint.")]
    [TextArea(8, 30)] public string contextText = "";

    [Header("Answer cards (define which panels can appear; add as many as you like)")]
    public List<CueCardDef> cards = new();

    public bool IsArrival => mode == Mode.Arrival;
}
