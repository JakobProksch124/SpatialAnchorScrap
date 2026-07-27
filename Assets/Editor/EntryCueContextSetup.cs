using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot: applies the lecture-navigation scenario (T1–T4) context + visible texts + answer
/// cards to the six Building-64 flow cues. Run: EntryCue > Apply Lecture-Navigation Context.
/// The long German context comes from Assets/LLMCueAssets/EntryCue/Context/*.txt; the short
/// visible texts and the card definitions are set here. Image/Video cards are created as
/// placeholders (assign the actual textures/clips per card afterwards); the Reality card needs
/// no asset. Re-run any time the .txt context files change.
/// </summary>
public static class EntryCueContextSetup
{
    const string CueDir = "Assets/Resources/LLMCues";
    const string CtxDir = "Assets/LLMCueAssets/EntryCue/Context";

    class Spec
    {
        public string prefab, ctx, title, highlight, reason, enter, dismiss, close, target, note;
        public CueConfig.Mode mode;
        public List<CueCardDef> cards;
    }

    static CueCardDef Card(string id, CueCardKind kind, string title, string when, string body = "", float widthScale = 1f)
        => new CueCardDef { id = id, kind = kind, title = title, whenToShow = when, bodyText = body, widthScale = widthScale };

    [MenuItem("EntryCue/Apply Lecture-Navigation Context (T1-T4)")]
    public static void Apply()
    {
        int done = 0;
        foreach (var s in Specs())
        {
            var path = $"{CueDir}/{s.prefab}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogError($"[CueContext] missing {path}"); continue; }
            var cfg = root.GetComponent<CueConfig>();
            if (cfg == null) { Debug.LogError($"[CueContext] no CueConfig on {s.prefab}"); PrefabUtility.UnloadPrefabContents(root); continue; }

            var ctx = AssetDatabase.LoadAssetAtPath<TextAsset>($"{CtxDir}/{s.ctx}.txt");
            if (ctx != null) cfg.contextText = ctx.text;
            else Debug.LogWarning($"[CueContext] context txt not found for {s.ctx}");

            cfg.cueName = s.prefab;
            cfg.mode = s.mode;
            if (!string.IsNullOrEmpty(s.note)) cfg.assistantNote = s.note;

            if (s.mode == CueConfig.Mode.Entry)
            {
                cfg.entryTitle = s.title; cfg.entryHighlight = s.highlight ?? ""; cfg.entryReason = s.reason ?? "";
                if (s.enter != null) cfg.enterLabel = s.enter;
                if (s.dismiss != null) cfg.dismissLabel = s.dismiss;
            }
            else
            {
                cfg.arrivalTitle = s.title; cfg.arrivalHighlight = s.highlight ?? ""; cfg.arrivalReason = s.reason ?? "";
                if (s.close != null) cfg.closeLabel = s.close;
            }
            if (s.cards != null) cfg.cards = s.cards;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            done++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[CueContext] Applied lecture-navigation context to {done} cues. " +
                  "Assign actual textures/clips to the Image/Video cards where you have them.");
    }

    static List<Spec> Specs() => new()
    {
        new Spec {
            prefab = "T1_Arrival", ctx = "T1_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Folge dem Pfeil zur Vorlesung", close = "Schließen",
            note = "Du bist der ARRIVAL Cue der AR-Navigation. Sprich nur über die aktuelle AR-Navigation, nicht über die abgeschlossene Transition.",
            cards = new List<CueCardDef> {
                Card("karte", CueCardKind.Image, "KARTE", "Zeige die Karte/Minimap der Strecke, wenn der Nutzer nach dem Weg oder einer Karte fragt."),
                Card("dauer", CueCardKind.Text, "DAUER", "Zeige die verbleibende Dauer, wenn der Nutzer nach der Zeit fragt.", "Nur noch wenige Minuten bis zur Vorlesung."),
                Card("video", CueCardKind.Video, "VIDEO", "Zeige das Video, wenn der Nutzer wissen will, wie die Pfeile funktionieren oder wie man ihnen folgt."),
            }
        },
        new Spec {
            prefab = "T2_Entry", ctx = "T2_Entry", mode = CueConfig.Mode.Entry,
            title = "Ins virtuelle Chemielabor", highlight = "Chemielabor", reason = "Zähle die verfügbaren Stühle",
            enter = "Betreten", dismiss = "Jetzt nicht", target = "das virtuelle Chemielabor (VR)",
            note = "Du bist der ENTRY Cue in AR. Biete den Wechsel ins virtuelle Chemielabor an, um dort die Stühle zu zählen. Sprich über die Transition und das Labor.",
            cards = new List<CueCardDef> {
                Card("vorschau", CueCardKind.Image, "VORSCHAU", "Zeige die Vorschau des Labors, wenn der Nutzer sehen will, wie das Labor oder die Tische aussehen."),
                Card("bewegung", CueCardKind.Text, "BEWEGUNG", "Zeige, wie man sich im Labor bewegt, wenn der Nutzer danach fragt.", "Bewegung per Teleportation: mit dem rechten Controller dorthin zeigen, wohin du möchtest."),
            }
        },
        new Spec {
            prefab = "T2_Arrival", ctx = "T2_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen im Chemielabor", close = "Schließen",
            note = "Du bist der ARRIVAL Cue im VR-Chemielabor. Sprich nur über das Labor, die Bewegung (Teleport) und die Aufgabe (Stühle zählen), nicht über die Transition.",
            cards = new List<CueCardDef> {
                Card("bewegung", CueCardKind.Text, "BEWEGUNG", "Zeige, wie man sich bewegt, wenn der Nutzer fragt.", "Teleportation mit dem rechten Controller: dorthin zeigen, wohin du möchtest, und dich teleportieren."),
            }
        },
        new Spec {
            prefab = "T3_Entry", ctx = "T3_Entry", mode = CueConfig.Mode.Entry,
            title = "Zurück nach AR", highlight = "AR", reason = "Weiter zur Vorlesung",
            enter = "Verlassen", dismiss = "Jetzt nicht", target = "AR (die reale Umgebung mit Navigation)",
            note = "Du bist der ENTRY Cue im Labor für die Rückkehr nach AR. Du zeigst ein Live-Fenster in die Realität.",
            cards = new List<CueCardDef> {
                Card("live", CueCardKind.Reality, "LIVE", "Zeige das Live-Fenster in die Realität, wenn der Nutzer sehen will, wie es draußen aussieht.", "", 3f),
            }
        },
        new Spec {
            prefab = "T3_Arrival", ctx = "T3_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Die letzten Meter zur Vorlesung", close = "Schließen",
            note = "Du bist der ARRIVAL Cue in AR für die letzten Meter zur Vorlesung. Nur Navigationskontext, nicht über die Transition.",
            cards = new List<CueCardDef> {
                Card("vorschau", CueCardKind.Image, "VORSCHAU", "Zeige Vorschau-Bilder der restlichen Strecke, wenn der Nutzer fragt, wie der Weg aussieht."),
                Card("karte", CueCardKind.Image, "KARTE", "Zeige die Minimap mit Highlight der aktuellen Position, wenn der Nutzer fragt, wo er gerade ist."),
                Card("video", CueCardKind.Video, "VIDEO", "Zeige das Erklärvideo oder die Animation, wenn der Nutzer wissen will, wie man dem Pfeil folgt."),
            }
        },
        new Spec {
            prefab = "T4_Entry", ctx = "T4_Entry", mode = CueConfig.Mode.Entry,
            title = "Ziel erreicht", highlight = "", reason = "Du bist am Vorlesungsraum – Brille absetzen",
            enter = "Absetzen", dismiss = "Jetzt nicht", target = "die Realität (Brille absetzen)",
            note = "Du bist der ENTRY Cue in AR am Vorlesungsraum. Biete an, die Brille abzusetzen (Rückkehr in die Realität). Bestätigend antworten.",
            cards = new List<CueCardDef> {
                Card("tuer", CueCardKind.Image, "TÜR", "Zeige das Foto der Tür, wenn der Nutzer fragt, ob er beim richtigen Raum ist."),
                Card("absetzen", CueCardKind.Video, "ABSETZEN", "Zeige die Animation zum Absetzen der Brille, wenn der Nutzer fragt, wie er wechselt."),
            }
        },
    };
}
