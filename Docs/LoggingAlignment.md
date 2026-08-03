# Study logging — alignment proposal (AR/VR headset ↔ phone)

Status: proposal, 2026-08-03. Nothing here is implemented on the headset yet except where marked **DONE**.

Basis for comparison: headset sessions 14–15 (`StudyLogs/`) against two phone files,
`P00_T1_Entry_s13_20260803_025819.json` and `P00_T4_Arrival_s13_20260803_025911.json`.

---

## 0. What already matches — do not change

The two implementations agree on the whole envelope. This is the part that must stay stable.

**Header** — same ten keys, same order, same types:

```json
{ "sessionId": 13, "participant": "P00", "queue": "T1_Entry", "cueType": "entry",
  "transition": "T1", "startTime": "...", "startReason": "cue_shown",
  "endTime": "...", "endReason": "transition_entered", "events": [ ... ] }
```

- Timestamps: `yyyy-MM-dd'T'HH:mm:ss.fff` + numeric UTC offset, local time. Both sides identical.
- Filename: `{participant}_{queue}_s{sessionId}_{yyyyMMdd_HHmmss}.json`. Both sides identical.
- Event shape: `{ "t", "type" }` plus optional `"text"` and/or `"panel"`.
- `startReason`: `cue_created` for arrival cues, `cue_shown` for entry cues.
- Ordering: `panel_shown` is emitted *before* the `text_output` that refers to it.
- The final action event is written *before* the cue is torn down, so it lands inside its own file.

**No `device` field is needed.** `queue` already identifies the medium — no `(transition, cueType)`
pair exists on both sides:

| device | queues |
|---|---|
| headset | T1_Arrival, T2_Entry, T2_Arrival, T3_Entry, T3_Arrival, T4_Entry, T5_Arrival, T6_Entry, T6_Arrival, T7_Entry, T7_Arrival, T8_Entry, T9_Arrival, T10_Entry, T11_Arrival, T12_Entry |
| phone | T1_Entry, T4_Arrival, T5_Entry, T8_Arrival, T9_Entry, T10_Arrival, T11_Entry, T12_Arrival |

24 cues, 12 transitions, two per transition, split across the two devices. Please confirm the
phone list is exactly these eight.

`sessionId` is per-device and will collide between the two. Agreed not to fix: join on
`participant` + `queue` + timestamps, never on `sessionId` alone.

Formatting differs (phone: one event per line; headset: indented). Cosmetic, both parse
identically. No change.

---

## 1. Changes proposed on the HEADSET side

### 1.1 Rename panel id `pfeilvideo` → `pfeil`

The phone logs `pfeil`, the headset logs `pfeilvideo`, for the same affordance (the explanation
of the AR arrows). `karte` already matches. This is the only id collision, and it blocks
counting "how often did participants ask about the arrows" across the two media without a
lookup table.

Proposed: the headset renames, because the id should name the *concept*, not the medium
(the headset's happens to be a video, the phone's does not — that difference belongs in the
data, not in the key). Affects four cues: T1_Arrival, T3_Arrival, T5_Arrival, T7_Arrival,
plus their context files and the setup script.

*Not yet applied — one word and it is done.*

### 1.2 Already done tonight

| change | status |
|---|---|
| One logger per cue (was a shared static; T1_Arrival's data was landing in T2_Entry files) | **DONE** |
| `transition` derived from the cue name, so it cannot go stale | **DONE** |
| Arrival cues end with `closed`/`walked_away` instead of always `app_quit` | **DONE** |
| Empty voice input logs `speech_empty` instead of `text_input: ""` | **DONE** |
| Crash-recovery sweep no longer eats a live encounter's `.partial` | **DONE** |
| Log written before the cue is torn down (was splitting T2_Entry across two files) | **DONE** |
| `text_input` trimmed — the STT was appending `\n`, the phone's values are clean | **DONE** |
| Entering by **voice** now logs `transition_entered`; previously it logged nothing at all | **DONE** |
| Action-source token normalised to plain `button` / `voice` (was `button:jetzt_nicht`) | **DONE** |

---

## 2. Changes proposed on the PHONE side

### 2.1 Add a `choice_made` event — highest priority

The phone's defining interaction is *selecting*: focus mode, dish, bus line. Neither example
file contains any event that records a choice, and no existing type fits. As it stands, the
single most study-relevant thing the phone does leaves no trace in the log.

Proposed shape, reusing the existing keys:

```json
{"t":"...","type":"choice_made","panel":"essen","text":"pizza"}
```

- `panel` = the screen/panel the choice was made on, using the shared id vocabulary (§3)
- `text` = a stable lowercase option id, not the display label

Suggested option ids:

| panel | option ids |
|---|---|
| `fokus` | `alles_stumm`, `nur_wichtiges`, `nichts_aendern` |
| `essen` | `pizza`, `burger`, `pasta` |
| `bus` | `linie_17`, `linie_2` |

If a choice can be changed before confirming, please also log the final one — or add
`choice_confirmed` — so the analysis can distinguish browsing from deciding.

### 2.2 Normalise the action-source token

`closed` carries `"text": "button_close"` while `transition_entered` carries `"text": "button"`.
The event type already says *what* happened, so the text slot should only say *how*:
plain `button` or `voice`. Change `button_close` → `button`.

The headset now follows this rule everywhere (`closed`, `transition_entered`,
`dismiss_requested_ignored`). `walked_away` is the one exception and carries `idle_timeout`,
since it has neither a button nor a voice source.

### 2.3 Fix the room number in the T1_Entry context

`text_output` says "Gebäude 64, Raum 0105". The correct form is **0.105** — that is what the
phone's own Fokus screenshot shows, and the headset contexts (T1–T4) were normalised to it.
Content fix, not a schema fix.

### 2.4 Trim whitespace on `text_input`

The observed values are already clean, but if the phone uses speech-to-text, trim defensively.
The headset's STT was appending `\n` to every utterance, which would have broken any
string comparison across the two logs.

### 2.5 Participant id must be settable without a rebuild

The headset reads `participant.txt` from its persistent data directory, falling back to the
serialized value, so a participant can be set once per session with a single adb push. If the
phone hardcodes `P00`, the two will drift apart mid-study and the join fails silently. Any
runtime mechanism is fine as long as it does not require a rebuild and both devices are set
to the same value.

---

## 3. Shared vocabularies

### 3.1 Event types — headset emits these eleven

| type | payload | meaning |
|---|---|---|
| `cue_created` | — | arrival cue exists; opens the encounter |
| `cue_shown` | — | entry cue became available to the user; opens the encounter |
| `text_input` | `text` | what the participant said/typed (trimmed) |
| `text_output` | `text` | the assistant's full answer |
| `panel_shown` | `panel` | a card/panel was opened |
| `panel_hidden` | `panel` | a card/panel was closed |
| `closed` | `text`: `button`\|`voice` | arrival cue dismissed; **ends** the encounter |
| `transition_entered` | `text`: `button`\|`voice` | the user changed medium; **ends** the encounter |
| `dismiss_requested_ignored` | `text`: `button`\|`voice` | entry cue "Jetzt nicht"; cue deliberately stays |
| `walked_away` | `text`: `idle_timeout` | arrival cue ignored for 30 s and auto-retired; **ends** the encounter |
| `speech_empty` | — | the mic opened and returned nothing |

Please confirm which of these the phone emits, and whether it emits any type not on this list.
Specifically: does the phone emit `panel_hidden`, and does it have an equivalent of
`speech_empty`?

### 3.2 endReason — exactly four values

`transition_entered` · `closed` · `walked_away` · `app_quit`

`app_quit` should mean the app actually quit, not "the cue went away". Please confirm the
phone uses only these four.

### 3.3 Panel ids — headset vocabulary

```
absetzen · bild · essen · handy · karte · live · pfeilvideo(→pfeil) · standort · strecke · teleport · video · vorschau
```

Per cue:

| cue | panels |
|---|---|
| T1_Arrival | karte, pfeilvideo |
| T2_Entry | vorschau, teleport |
| T2_Arrival | teleport |
| T3_Entry | live |
| T3_Arrival | strecke, standort, pfeilvideo |
| T4_Entry | absetzen, handy |
| T5_Arrival | standort, pfeilvideo |
| T6_Entry | vorschau, teleport |
| T6_Arrival | teleport |
| T7_Entry | live |
| T7_Arrival | standort, pfeilvideo |
| T8_Entry | absetzen, karte, handy |
| T9_Arrival | karte |
| T10_Entry | absetzen, essen |
| T11_Arrival | — (none by design) |
| T12_Entry | handy, live |

Any phone panel showing the same concept should reuse the same id. Please send the phone's
list so the remaining overlaps can be checked — `karte` and `pfeil`/`pfeilvideo` are the only
two seen so far.

---

## 4. Summary

| # | change | owner | priority |
|---|---|---|---|
| 2.1 | add `choice_made` | phone | **high** — the phone's core interaction is currently unlogged |
| 1.1 | rename `pfeilvideo` → `pfeil` | headset | medium |
| 2.5 | participant id settable at runtime | phone | medium — silent data loss if it drifts |
| 2.2 | `button_close` → `button` | phone | low |
| 2.3 | room number `0105` → `0.105` | phone | low (content) |
| 2.4 | trim `text_input` | phone | low (defensive) |
| 3.1–3.3 | confirm the three vocabularies | phone | — |
