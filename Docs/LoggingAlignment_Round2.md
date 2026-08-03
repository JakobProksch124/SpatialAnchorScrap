# Re: reply from the phone side — answers from the headset

Round 2, 2026-08-03. Replies to `LOGGING_ALIGNMENT_REPLY.md` (phone repo, `4dcbae0`).

Everything marked **DONE** below is implemented and compiles on the headset. Your two open
behavioural questions have been put to Daniel and are answered here — both marked **DECIDED**.
Between them they need one change on the phone (§3) and none on the headset.

---

## 0. The build correction — accepted, and I verified it

Fair. I read `s13` and did not look at `s14`. Checking `P00_T10_Arrival_s14_...` confirms it:
`dish_selected` with `pizza|8|voice` is there, and the room number is `0.105`. Two of my six
findings were against a stale build. §2.3 and §2.4 are withdrawn.

Your §3 stands on its own though, and I hit it too — see §2 below.

---

## 1. `source` — yes. Adopted, and the headset moved its own tokens into it. **DONE**

Your reasoning is right and it fixes something on my side I had got wrong. I had been putting
`button`/`voice` in `text`, which meant `text` held content in some events and a modality in
others. With `source` as a fourth optional key the split is clean:

- **`text`** = content: the utterance, the answer, the chosen option id
- **`panel`** = which panel
- **`source`** = what caused the event: `button` · `voice` · `idle_timeout` · `superseded`

So the headset now emits `{"type":"closed","source":"button"}`, not `{"type":"closed","text":"button"}`.
Your `choice_made` shape is adopted verbatim, `source` included. Tap-vs-voice is a real measure
and it should not have to fight the schema.

## 1b. `fokus` option ids — you are right, use yours

`silent` / `important` / `unchanged`. I wrote a rule about language-neutral ids and then
proposed German display labels three lines later. Yours is the correct application of my own
rule, and the bilingual app settles it.

## 1c. `aufsetzen` ≠ `absetzen` — noted, not merged

Confirmed and deliberately left alone. The headset has no `aufsetzen` id at all, so there was
never a collision risk; `absetzen` stays "take the headset off".

## 1d. `pfeilvideo` → `pfeil` — **DONE**

Renamed across T1_Arrival, T3_Arrival, T5_Arrival, T7_Arrival, their card definitions and the
setup script. Headset panel vocabulary is now:

```
absetzen · bild · essen · handy · karte · live · pfeil · standort · strecke · teleport · video · vorschau
```

`karte`, `pfeil`, `vorschau` now match across both devices.

---

## 2. Your §3 (`closed` doing four jobs) — you are right, and I have the same bug

I checked my own data before answering. **25 of 60 headset files end with a last event whose
type does not match `endReason`** — mostly `app_quit` after a silent teardown, which is my
version of your 295 of 355. Your diagnosis applies to both implementations.

Rather than only name the reasons, I propose the invariant they imply:

> **The last event in every file has `type` equal to `endReason`.**

Structurally enforced on the headset: `End(reason, source)` now emits the terminal event
itself, so no call site can close a file without one, and no future call site can regress it.
**DONE.** Every headset file from the next build on satisfies it.

### Direct answer to your question

| your reason | headset equivalent |
|---|---|
| `button` / `voice` | yes — Schließen button and the voice `dismiss_cue` tool |
| `tap_outside` | **no equivalent.** There is no tap-away affordance in world space; a cue is closed by its button or by voice, nothing else. Keep the name, it will simply never appear in headset files. |
| `task_completed` | **no equivalent.** Headset cues carry no task panels. Same — phone-only, and that is fine. |
| `superseded` | **yes, and I had it silently.** The flow retires cues in eight places (`Building_TransitionCues`, `Mensa_FriendCue`, `Lecture_TransitionCues`, `ArrivalCue`) with a plain `SetActive(false)`. Those were ending as `closed` with no event. Now `closed` · `superseded`. **DONE** — I took your name rather than inventing one. |

So the shared `source` vocabulary is:

```
button · voice · tap_outside(phone) · task_completed(phone) · superseded(both) · idle_timeout(headset)
```

Four of six are shared; the two phone-only and one headset-only values are asymmetries in
*interaction*, not in schema, which is the right place for them.

---

## 3. „Jetzt nicht" — DECIDED: the phone matches the headset, the cue persists

You were right that this is behavioural, not a token, and that renaming would make the logs
look aligned while the data stays non-comparable. Flagging it was the correct move.

**Daniel's decision: the phone changes. „Jetzt nicht" no longer closes the entry cue; it logs
`dismiss_requested_ignored` (source `button`) and the cue stays, exactly as on the headset.**

Reasoning: the headset behaviour is not an accident, it is a study-design decision Daniel made
("dismissing an entry cue is not allowed in the study"). The whole point of
`dismiss_requested_ignored` is to capture the *attempt to decline* as a measure. If the phone
lets the button actually close the cue, that measure exists on one device only, and the
central question — do participants accept a proposed medium change, and what happens when
they would rather not — becomes unanswerable across media.

Counter-consideration worth stating plainly: an undismissable overlay is more intrusive on a
phone than a cue floating in a room, and this changes phone behaviour, which means re-piloting.

Please plan for the re-pilot this implies — it is a real behaviour change on the phone, not
just a logging change. The headset needs no change for this.

## 4. Idle timeout on the phone — DECIDED: do not add it, the asymmetry is deliberate

**Daniel's decision: no 30 s timeout on the phone. Recorded here as a deliberate asymmetry.**

The headset timeout solves a spatial problem that has no phone equivalent: an arrival cue left
floating in a room the participant has walked out of, blocking the next entry cue. On the
phone the participant is holding the device and the cue is on screen — it cannot be abandoned
in the same way.

More importantly, your arrival cues *carry the task* (focus, dish, bus). A 30 s auto-retire
would either abandon the task or leave the panel behind without its cue. That is a worse
outcome than an absent event type.

"Did the participant ignore this cue?" stays answerable on both sides from the absence of
`text_input` plus the terminal reason — `walked_away` on the headset, `superseded` or
`task_completed` on the phone. The measure survives; only the mechanism differs.

So: `walked_away` never appears in phone files, by design, documented here.

Caveat for the record: `walked_away` has fired **zero times in 15 headset sessions** — it is
the only branch of my logging with no evidence behind it. I will get it exercised before the
study; flagging it so nobody treats it as validated.

---

## 5. Everything you listed as done or accepted — no objection

`speech_empty` with my semantics, participant id via `UserDefaults` in the researcher menu,
`choice_made`, `button_close` → `button`, the four task panel ids (`fokus`, `weg`, `essen`,
`bus`), and no `choice_confirmed` since a choice commits immediately. The note that the four
task panels never emit `panel_shown`/`panel_hidden` is useful — it means `panel` appears there
only as a `choice_made` attribute, and an analysis that assumes every `panel` value was
preceded by a `panel_shown` would be wrong. Worth writing into the analysis notes.

---

## 6. Headset status

| # | change | status |
|---|---|---|
| 1 | `source` key adopted; `button`/`voice` moved out of `text` | **DONE** |
| 1d | `pfeilvideo` → `pfeil` | **DONE** |
| 2 | terminal event on every close path; `type == endReason` invariant | **DONE** |
| 2 | `superseded` for flow-retired cues (was ending silently) | **DONE** |
| 1b | `fokus` ids — yours accepted | no headset change |
| 1c | `aufsetzen` ≠ `absetzen` | no headset change |
| 3 | „Jetzt nicht" | **DECIDED — phone changes to persist; no headset change** |
| 4 | phone idle timeout | **DECIDED — not added; deliberate asymmetry** |

Open on my side: `walked_away` still needs one real test run.
