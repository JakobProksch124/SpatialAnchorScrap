using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// One-shot: applies the full lecture-day scenario (T1–T12) context + visible texts + answer
/// cards (incl. their media) to every in-headset cue prefab. Run: EntryCue > Apply Lecture-Navigation Context.
///
/// Scope: this fills the 16 AR/VR cues that exist as prefabs in Assets/Resources/LLMCues, plus
/// the tutorial cues. The 8 smartphone-side cues (Entry of T1/T5/T9/T11 and Arrival of
/// T4/T8/T10/T12) have NO prefab — they run on the phone, not the headset.
/// NOTE: the old T9 (Essensauswahl in AR) and its AR→AR follow-up were dropped; everything
/// after them moved down by one, so the scenario now runs T1–T12.
///
/// The long German context comes from Assets/LLMCueAssets/EntryCue/Context/*.txt; the short
/// visible texts and the card definitions (kind, tag title, caption, when-to-show) are set here.
/// Card media (Image/Video) is loaded from Assets/Resources/LLMCues/Materiialien/ and assigned
/// automatically where a matching file exists; cards with no matching asset (route preview, door
/// photo) stay as placeholders. The Reality "Live-Fenster" card needs no asset. Re-run any time the
/// .txt context files or the media change.
/// </summary>
public static class EntryCueContextSetup
{
    const string CueDir = "Assets/Resources/LLMCues";
    const string CtxDir = "Assets/LLMCueAssets/EntryCue/Context";

    // scenario media (Daniel's Materiialien folder)
    const string MediaDir = "Assets/Resources/LLMCues/Materiialien";
    const string MiniG64   = MediaDir + "/Minimaps/minimap Gb64.png";
    const string MiniBib   = MediaDir + "/Minimaps/Minimap Bibliothek.png";
    const string MiniMensa = MediaDir + "/Minimaps/Minimap Mensa.png";
    const string PrevLab   = MediaDir + "/Screnshots/screenshotEntryLaboratory 1.png";
    const string PrevImis  = MediaDir + "/Screnshots/screenshotEntryBridge 1.png";
    const string VidTele   = MediaDir + "/Videos/Teleportation_4zu3.mp4";
    const string VidAbset  = MediaDir + "/Videos/HMD_Absetzen-4zu3.mp4";
    const string VidPfeil  = MediaDir + "/Videos/pfeil video.mp4";
    const string ImgBus    = MediaDir + "/Immages/Busfahrt.png";

    // standard arrival header (README convention)
    const string ArrTitle = "Wie kann ich dir helfen?";
    const string ArrSub = "Hast du Fragen zum Kontext, in dem du dich gerade befindest?";

    class Spec
    {
        public string prefab, ctx, title, highlight, reason, enter, dismiss, close, note;
        public CueConfig.Mode mode;
        public List<CueCardDef> cards;
    }

    static CueCardDef Card(string id, CueCardKind kind, string title, string when,
        string caption = "", string body = "", float widthScale = 1f, string asset = null,
        float heightScale = 1f)
    {
        var def = new CueCardDef
        {
            id = id, kind = kind, title = title, whenToShow = when,
            caption = caption, bodyText = body, widthScale = widthScale, heightScale = heightScale
        };
        if (!string.IsNullOrEmpty(asset))
        {
            if (kind == CueCardKind.Image)
            {
                def.image = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
                if (def.image == null) Debug.LogWarning($"[CueContext] card '{id}': image not found at {asset}");
            }
            else if (kind == CueCardKind.Video)
            {
                def.video = AssetDatabase.LoadAssetAtPath<VideoClip>(asset);
                if (def.video == null) Debug.LogWarning($"[CueContext] card '{id}': video not found at {asset}");
            }
        }
        return def;
    }

    [MenuItem("EntryCue/Apply Lecture-Navigation Context (T1-T12)")]
    public static void Apply()
    {
        int done = 0, missing = 0;
        foreach (var s in Specs())
        {
            var path = $"{CueDir}/{s.prefab}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) { Debug.LogError($"[CueContext] missing prefab {path}"); missing++; continue; }
            var cfg = root.GetComponent<CueConfig>();
            if (cfg == null) { Debug.LogError($"[CueContext] no CueConfig on {s.prefab}"); PrefabUtility.UnloadPrefabContents(root); continue; }

            var ctx = AssetDatabase.LoadAssetAtPath<TextAsset>($"{CtxDir}/{s.ctx}.txt");
            if (ctx != null) cfg.contextText = ctx.text;
            else Debug.LogWarning($"[CueContext] context txt not found for {s.ctx} (context left unchanged)");

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
        Debug.Log($"[CueContext] Applied scenario context + media to {done} cues ({missing} prefabs missing). " +
                  "Cards with no matching media (route preview, door photo) stay as placeholders; " +
                  "the smartphone-side cues (Entry T1/T5/T9/T11, Arrival T4/T8/T10/T12) have no prefab.");
    }

    // German assistant notes (short, appended to the system prompt)
    const string NoteEntry = "Du bist der ENTRY Cue, gezeigt VOR der Transition. Sprich über die Transition und den Zielkontext.";
    const string NoteArr = "Du bist der ARRIVAL Cue im Zielkontext. Sprich NUR über den aktuellen Kontext, nicht über die abgeschlossene Transition.";

    static List<Spec> Specs() => new()
    {
        // ---------------- G64 / Laboratory ----------------
        new Spec {
            prefab = "T1_Arrival", ctx = "T1_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen in AR!", reason = "Wie kann ich dir helfen?", close = "Schließen", note = NoteArr,
            cards = new List<CueCardDef> {
                Card("karte", CueCardKind.Image, "KARTE", "Zeige die Karte der Strecke, wenn der Nutzer nach dem Weg, der Strecke oder einer Karte fragt.", "Deine Strecke im Überblick.", asset: MiniG64),
                Card("pfeilvideo", CueCardKind.Video, "VIDEO", "Zeige das Video, wenn der Nutzer wissen will, wie die Pfeile funktionieren oder wie man ihnen folgt.", "So entstehen die Pfeile und so folgst du ihnen.", asset: VidPfeil),
            }
        },
        new Spec {
            prefab = "T2_Entry", ctx = "T2_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zu VR", highlight = "VR",
            reason = "Peters Auftrag wartet im virtuellen Chemielabor.",
            enter = "Betreten", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("vorschau", CueCardKind.Image, "VORSCHAU", "Zeige die Vorschau des Labors, wenn der Nutzer sehen will, wie das Labor oder die Tische aussehen.", "So sieht das virtuelle Labor mit Tischen und Stühlen aus.", asset: PrevLab),
                Card("teleport", CueCardKind.Video, "TELEPORT", "Zeige die Teleport-Animation, wenn der Nutzer fragt, wie man sich im Labor bewegt.", "Mit dem rechten Controller auf eine Stelle zeigen und dich dorthin teleportieren.", asset: VidTele),
            }
        },
        new Spec {
            prefab = "T2_Arrival", ctx = "T2_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen im Chemielabor!", reason = "Wie kann ich dir helfen?", close = "Schließen", note = NoteArr,
            cards = new List<CueCardDef> {
                Card("teleport", CueCardKind.Video, "TELEPORT", "Zeige die Teleport-Animation, wenn der Nutzer fragt, wie er sich bewegt.", "Mit dem rechten Controller zeigen und teleportieren.", asset: VidTele),
            }
        },
        new Spec {
            prefab = "T3_Entry", ctx = "T3_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zu AR", highlight = "AR",
            reason = "Draußen geht es weiter zur Vorlesung.",
            enter = "Verlassen", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("live", CueCardKind.Reality, "LIVE", "Zeige das Live-Fenster in die Realität, wenn der Nutzer sehen will, wie es draußen gerade aussieht.", "Live-Ansicht der echten Welt vor dir, kein Standbild.", "", 1.3f, heightScale: 1.6f),
            }
        },
        new Spec {
            prefab = "T3_Arrival", ctx = "T3_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen zurück in AR!", reason = "Wie kann ich dir helfen?", close = "Schließen", note = NoteArr,
            cards = new List<CueCardDef> {
                Card("strecke", CueCardKind.Image, "STRECKE", "Zeige die Vorschau der Reststrecke, wenn der Nutzer fragt, wie der restliche Weg aussieht.", "So sieht der restliche Weg aus.", asset: MiniG64),
                Card("standort", CueCardKind.Image, "KARTE", "Zeige die Karte mit Standort-Highlight, wenn der Nutzer fragt, wo er gerade ist.", "Deine Strecke mit Highlight, wo du dich gerade befindest.", asset: MiniG64),
                Card("pfeilvideo", CueCardKind.Video, "VIDEO", "Zeige das Erklärvideo, wenn der Nutzer wissen will, wie man dem Pfeil folgt.", "Kurze Animation, wie die Pfeile funktionieren und wie du ihnen folgst.", asset: VidPfeil),
            }
        },
        new Spec {
            prefab = "T4_Entry", ctx = "T4_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zum Smartphone", highlight = "Smartphone",
            reason = "Du hast den Vorlesungsraum erreicht – setz die Brille ab und schau auf dein Smartphone.",
            enter = "Absetzen", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("absetzen", CueCardKind.Video, "ABSETZEN", "Zeige die Absetzen-Animation, wenn der Nutzer fragt, wie er wechselt oder die Brille absetzt.", "Einfach das Headset absetzen, mehr ist nicht zu tun.", asset: VidAbset),
            }
        },

        // ---------------- Bib / Bridge (BRIDGE Lab Open Day) ----------------
        new Spec {
            prefab = "T5_Arrival", ctx = "T5_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen in AR!", reason = "Wie kann ich dir helfen?", close = "Schließen", note = NoteArr,
            cards = new List<CueCardDef> {
                Card("standort", CueCardKind.Image, "KARTE", "Zeige die Karte mit Standort, wenn der Nutzer nach dem Weg oder wo er ist fragt.", "Deine Strecke durch die Bibliothek mit Highlight, wo du gerade bist.", asset: MiniBib),
                Card("pfeilvideo", CueCardKind.Video, "VIDEO", "Zeige das Video, wenn der Nutzer wissen will, wie die Pfeile funktionieren.", "Kurze Animation, wie die Pfeile funktionieren.", asset: VidPfeil),
            }
        },
        new Spec {
            prefab = "T6_Entry", ctx = "T6_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zu VR", highlight = "VR",
            reason = "Open Lab Day – wirf einen Blick ins BRIDGE Lab.",
            enter = "Betreten", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("vorschau", CueCardKind.Image, "VORSCHAU", "Zeige die Vorschau des BRIDGE Labs, wenn der Nutzer sehen will, wie es drinnen aussieht.", "So sieht das Labor der AG Jetter von innen aus.", asset: PrevImis),
                Card("teleport", CueCardKind.Video, "TELEPORT", "Zeige die Teleport-Animation, wenn der Nutzer fragt, wie man sich bewegt.", "Mit dem rechten Controller auf eine Stelle zeigen und dich dorthin teleportieren.", asset: VidTele),
            }
        },
        new Spec {
            prefab = "T6_Arrival", ctx = "T6_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen im BRIDGE Lab!", reason = "Wie kann ich dir helfen?", close = "Schließen", note = NoteArr,
            cards = new List<CueCardDef> {
                Card("teleport", CueCardKind.Video, "TELEPORT", "Zeige die Teleport-Animation, wenn der Nutzer fragt, wie er sich bewegt.", "Mit dem rechten Controller zeigen und teleportieren.", asset: VidTele),
            }
        },
        new Spec {
            prefab = "T7_Entry", ctx = "T7_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zu AR", highlight = "AR",
            reason = "Weiter geht es zur Mensa.",
            enter = "Verlassen", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("live", CueCardKind.Reality, "LIVE", "Zeige das Live-Fenster in die Bibliothek, wenn der Nutzer sehen will, wie es draußen gerade aussieht.", "Live-Ansicht der echten Welt vor dir, kein Standbild.", "", 1.3f, heightScale: 1.6f),
            }
        },
        new Spec {
            prefab = "T7_Arrival", ctx = "T7_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen zurück in AR!", reason = "Wie kann ich dir helfen?", close = "Schließen", note = NoteArr,
            cards = new List<CueCardDef> {
                Card("standort", CueCardKind.Image, "KARTE", "Zeige die Karte mit Standort, wenn der Nutzer nach dem Weg oder wo er ist fragt.", "Der restliche Weg zum Hauptausgang mit Highlight, wo du gerade bist.", asset: MiniBib),
                Card("pfeilvideo", CueCardKind.Video, "VIDEO", "Zeige das Video, wenn der Nutzer wissen will, wie die Pfeile funktionieren.", "Kurze Animation, wie die Pfeile funktionieren.", asset: VidPfeil),
            }
        },
        new Spec {
            prefab = "T8_Entry", ctx = "T8_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zum Smartphone", highlight = "Smartphone",
            reason = "Bibliothek geschafft – setz die Brille ab und schau auf dein Smartphone.",
            enter = "Absetzen", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("absetzen", CueCardKind.Video, "ABSETZEN", "Zeige die Absetzen-Animation, wenn der Nutzer fragt, wie er die Brille absetzt.", "Einfach das Headset absetzen, mehr ist nicht zu tun.", asset: VidAbset),
                Card("karte", CueCardKind.Image, "KARTE", "Zeige die Karte, wenn der Nutzer wissen will, wohin es geht oder wie er von hier zur Mensa kommt.", "Dein Weg von der Bibliothek zur Mensa – nur noch etwa 100 Meter.", asset: MediaDir + "/Immages/WegBibMensa.png"),
            }
        },

        // ---------------- Mensa (T9 arrival + T10 leave-HMD; the food selection was removed) ----------------
        new Spec {
            prefab = "T9_Arrival", ctx = "T9_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen in der Mensa!", reason = "Wie kann ich dir helfen?", close = "Schließen", note = NoteArr,
            cards = new List<CueCardDef> {
                Card("karte", CueCardKind.Image, "KARTE", "Zeige die Karte, wenn der Nutzer nach dem Weg, der Strecke oder einer Karte fragt.", "Dein Weg nach oben zu Peter, etwa 2 Minuten.", asset: MiniMensa),
            }
        },
        new Spec {
            prefab = "T10_Entry", ctx = "T10_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zum Smartphone", highlight = "Smartphone",
            reason = "Du hast Peter erreicht – setz die Brille ab und schau auf dein Smartphone.",
            enter = "Absetzen", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("absetzen", CueCardKind.Video, "ABSETZEN", "Zeige die Absetzen-Animation, wenn der Nutzer fragt, wie er die Brille absetzt.", "Einfach das Headset absetzen, mehr ist nicht zu tun.", asset: VidAbset),
            }
        },

        // ---------------- Lecture ----------------
        new Spec {
            prefab = "T11_Arrival", ctx = "T11_Arrival", mode = CueConfig.Mode.Arrival,
            title = "Willkommen im Hörsaal!", reason = "Hast du noch Fragen, bevor die Vorlesung startet?",
            close = "Start", note = NoteArr,
            cards = new List<CueCardDef>()  // bewusst keine Karten (nur Start-Button im Hörsaal)
        },
        new Spec {
            prefab = "T12_Entry", ctx = "T12_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zum Smartphone", highlight = "Smartphone",
            reason = "Dein Bus kommt bald – setz die Brille ab und schau auf dein Smartphone.",
            enter = "Absetzen", dismiss = "Jetzt nicht", note = NoteEntry,
            cards = new List<CueCardDef> {
                Card("busplan", CueCardKind.Image, "FAHRPLAN", "Zeige den Busfahrplan, wenn der Nutzer nach dem Bus, der Abfahrt oder dem Fahrplan fragt.", "Die nächsten Abfahrten ab Haltestelle Technische Hochschule.", asset: ImgBus),
                Card("live", CueCardKind.Reality, "LIVE", "Zeige das Live-Fenster in die Realität, wenn der Nutzer sehen will, wie es draußen gerade aussieht.", "Live-Ansicht der echten Welt vor dir, kein Standbild.", "", 1.3f, heightScale: 1.6f),
            }
        },

        // ---------------- Tutorial (Ausprobieren) ----------------
        new Spec {
            prefab = "TutorialCue", ctx = "TutorialCue", mode = CueConfig.Mode.Arrival,
            title = "Willkommen! Probier mich aus.", reason = "Stell mir Fragen, teste die Panels und übe das Teleportieren.",
            close = "Schließen",
            note = "Du bist ein Übungs-Arrival-Cue. Keine echte Aufgabe. Ermutige den Nutzer, zu sprechen, die Panels und die Teleportation auszuprobieren.",
            cards = new List<CueCardDef> {
                Card("bild", CueCardKind.Image, "BILD", "Zeige ein Bild-Panel, wenn der Nutzer ein Bild sehen will.", "So sieht ein Bild-Panel aus."),
                Card("video", CueCardKind.Video, "VIDEO", "Zeige ein Video-Panel, wenn der Nutzer ein Video sehen will.", "So sieht ein Video-Panel aus.", asset: VidTele),
                Card("live", CueCardKind.Reality, "LIVE", "Zeige ein Live-Fenster in die Realität, wenn der Nutzer es sehen will.", "Live-Blick in die echte Welt.", "", 1.3f, heightScale: 1.6f),
            }
        },
        new Spec {
            prefab = "TutorialCue_Entry", ctx = "TutorialCue_Entry", mode = CueConfig.Mode.Entry,
            title = "Wechseln zu VR", highlight = "VR",
            reason = "Übung: Sprich mit mir oder nutz die Knöpfe, um den Wechsel auszuprobieren.",
            enter = "Betreten", dismiss = "Jetzt nicht",
            note = "Du bist ein Übungs-Entry-Cue. Keine echte Aufgabe. Erkläre geduldig, wie man mit Cues spricht, klickt und den Wechsel startet.",
            cards = new List<CueCardDef> {
                Card("teleport", CueCardKind.Video, "TELEPORT", "Zeige die Teleport-Animation, wenn der Nutzer wissen will, wie man sich in VR bewegt.", "So bewegst du dich gleich in VR: rechter Stick, zielen, loslassen.", asset: VidTele),
            }
        },
        new Spec {
            prefab = "TutorialCue_Exit", ctx = "TutorialCue_Exit", mode = CueConfig.Mode.Entry,
            title = "Wechseln zu AR", highlight = "AR",
            reason = "Zurück in die echte Welt – probier vorher das Live-Fenster aus.",
            enter = "Verlassen", dismiss = "Jetzt nicht",
            note = "Du bist ein Übungs-Entry-Cue für den Rückweg nach AR. Lade zum Live-Fenster ein; der Wechsel passiert erst auf klaren Wunsch.",
            cards = new List<CueCardDef> {
                Card("live", CueCardKind.Reality, "LIVE", "Zeige das Live-Fenster in die Realität, wenn der Nutzer sehen will, wie es draußen gerade aussieht.", "Live-Ansicht der echten Welt vor dir, kein Standbild.", "", 1.3f, heightScale: 1.6f),
            }
        },
    };
}
