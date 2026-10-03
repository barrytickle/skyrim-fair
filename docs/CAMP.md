# 2.0.2: the fair comes and goes (progress)

The working log for 2.0.2, kept up to date as the work goes, so it can stop at any point and
pick up in another session. The design is `docs/MCM.md`, "Planned for 2.0.2". Started 2026-10-03.

## The plan

- **Pass 1, the core:** one switch for the fair's outside, Claudius's camp, and Claudius's talk
  (the schedule question with "always" and "only after the main story", bring the fair back),
  the fade swap. Barry tests.
- **Pass 2:** the other schedules (festival days, fair week, wandering), the courier letter,
  the Passport paused while the fair is away (pass 1 already hides its lines at the camp).
- **Pass 3:** polish: Garrick and the horse at the camp (the boulder), test findings.

## Status

| Step | State |
| --- | --- |
| Recon (code, vanilla records) | done |
| Pass 1 design | done (below) |
| Pass 1 code: config, `FairCamp.cs`, scripts, voices | done |
| Pass 1 build, verify, deploy | done: plugin `5a601dbbe6f1987c`, deployed |
| Barry's pass 1 test | **waiting** (below) |
| Pass 2 | not started |
| Pass 3 | not started |

## Pass 1 design (decided 2026-10-03)

**Its own FormID range, 0xD0000** (`fairWorld.camp`, `FairCamp.cs`), built last of all (after the
fairgoers). Inside it, append only.

**Records:**
- Globals: `SkyrimFairAway` (0 the fair is here, 1 the camp), `SkyrimFairSchedule` (0 always;
  pass 2 adds 1 festivals, 2 monthly, 3 wandering), `SkyrimFairStoryGate` (1: only after the main
  story), `SkyrimFairStoryDone` (his "the dragon matter is concluded" line said).
- Persistent markers in Tamriel: `SkyrimFairPresentMarker` (XMarker, the enable parent of
  everything outside), `SkyrimFairCampMarker` (Claudius's spot at the camp),
  `SkyrimFairCampViewMarker` (where the player stands after "bring it back", facing the gate).
- **The switch:** every fair reference in Tamriel (the wall, the gate door, the silhouettes, the
  outside show, the flags, the signs, the decor) gets the present marker as enable parent. The
  vanilla references the fair disables lose their "initially disabled" flag and take the marker
  with "opposite", so they come back while the fair's away. Kept as they are: the map marker
  (it's on the road, by the camp), the large references sunk (their LOD), the approach's cleared
  references (the worn way in from the road stays clear, and the camp stands on it), anything
  already initially disabled of the fair's own (the tower banners, the reserved sign).
- **The camp** (vanilla pieces, enable parent the marker, "opposite"): a burning campfire and
  its light, a small Imperial tent, a bedroll, a table, a stool by the fire. Laid by the road in
  front of the gate (`camp.pieces`: side and forward from the gate, as `exterior.decor`).
- **Claudius:** a sandbox at the camp marker on top of his packages, on `Away == 1`. The
  Passport's force greet and stop line get `Away == 0`; his fair greetings (and the Passport's)
  get `Away == 0` too, so only the camp's lines play there.
- **The controller:** a start-game quest `SkyrimFairCamp` with `SkyrimFairCamp.psc`. Every few
  seconds: does the fair want to be here (the story gate: `MQ305` (046EF2) completed)? If that
  differs from `SkyrimFairAway`, swap, but only out of sight: never with the player in the fair's
  worldspace, and in Tamriel only beyond `camp.swapDistance` of the site. A swap: the marker
  enabled or disabled, Claudius moved (`MoveTo`) to the camp or the fair's entrance.
- **"Bring it back" at the camp:** after his line ends, fade to black (vanilla
  `FadeToBlackImod` 0F756D, `FadeToBlackHoldImod` 0F756E, `FadeToBlackBackImod` 0F756F), swap,
  move the player to the view marker, fade in.
- **Dialogue** (in the cameos' quest, no topic EditorIDs, so the voice files keep the proven
  `skyrimfaircameos__<info>_1` names):
  - Hello at the camp: `camp_story_wait` while waiting on the story, else `camp_hello_01/02`.
  - Hello at the fair, once: `story_done` (story gate on, `MQ305` done, not said yet).
  - "When will the fair be back?" (camp) → `camp_when`
  - "Never mind the story. Bring the fair now." (camp, waiting on the story) → `story_skip`,
    the gate off, then the fade
  - "Can you bring the fair back now?" (camp, otherwise) → `camp_bring_back`, then the fade
  - "About the fair's schedule..." (both places) → `schedule_ask`, then
    "Every day, as it is." → `schedule_always` (the gate off) and, before the main story is done,
    "Only after I've finished the main story." → `schedule_story` (the gate on)
- **Voices:** `build_voices.py` builds the `claudius_camp` lines into `build/cameos/Camp/`.

**Vanilla records used:** `ImperialTentSmall` 0800DC, `Campfire01LandBurningFieldGrass01` (MSTT)
0FB9B0, `LightCampFire01` 0AF8BA, `Bedroll01Static` 094AC6, `CommonTableSquare01` 02F239,
`WoodenBarStool` (FURN) 074EC6, `WICourier` 039F82 (pass 2).

**The site:** the gate is at (-7050, -10800), the road ~740 north of it (y -10064). The camp sits
~350 left of the path and ~380 out from the gate; the tent ~170 out, ~330 left (moved off the
returning rock pile's spot). Vanilla references that come back while the
fair's away, near there: two rock piles by the gate (-7588, -10776) and (-7080, -10692; on the
approach, so it stays off), shrubs to the east (-6560..-6209, -11100). Nothing on the camp's spot.

## Log (newest first)

### 2026-10-03
- **Pass 1 built and deployed** (plugin `5a601dbbe6f1987c`, deterministic). The camp's range is
  0xD0000-0xD0022 (35 records). 359 fair refs switched, 32 vanilla ones back while away. Checked
  against 2.0.1.3: nothing removed or renumbered; the new branches and topics match vanilla's
  subrecords. Detail in `docs/AUDIT.md`'s current pass.
- Code: `FairCamp.cs`, `CampConfig` (`fairWorld.camp`), `SkyrimFairCamp.psc` (the controller),
  `SkyrimFairCampLine.psc` (the lines' fragment), `build_voices.py --only camp`;
  `FairExterior` now records the approach's cleared refs (`ExteriorResult.Approach`).
- Three of the 13 camp recordings aren't used yet: `schedule_festivals`, `schedule_monthly`,
  `schedule_wandering` (pass 2).
- Started. This file made.
- Recon: read the exterior, Passport, cameos and stage script; looked up the vanilla records
  (above). The deployed plugin has 399 references in Tamriel: 361 the exterior's (0x30000),
  the map marker, 37 vanilla overrides (35 initially disabled, 2 sunk large ones).

## Barry's test for pass 1

The "only after the main story" option shows only while `MQ305` isn't completed. On a save where
Alduin is dead, use a new game from the main menu (`coc Riverwood`, then walk or `coc` near the fair) instead.

1. **At the fair** (on an existing save): everything as before. Claudius at the entrance, the
   Passport's stop line if not issued. New: a topic "About the fair's schedule...". Ask it:
   "The schedule. Yes. How often...", then the two choices. Choose "Only after I've finished
   the main story." (voiced: "Main story? I have no idea...").
2. **Leave**, and go out of sight: through the gate and fast travel somewhere (Whiterun, or any
   interior). The Papyrus log: `SkyrimFairCamp: the fair is away (the camp)`.
3. **Come back** (fast travel to the fair's map marker): no wall, no gate, the shrubs and rocks
   back; the camp by the road (fire burning, tent, bedroll, table, stool) and Claudius there.
   Talk to him: "The fair is postponed. Pending dragon. I have the paperwork." Then:
   - "When will the fair be back?": "According to my ledger, soon..."
   - "Never mind the story. Bring the fair now.": "Skipping ahead? Highly irregular. ...Close
     your eyes." The talk ends, the screen fades to black, and back in front of the gate with
     the fair there. Claudius is inside at the entrance again.
4. **Things to look at:** the tent's opening faces the fire (if it's backwards, it's one number);
   nothing floating or sunk; nothing of the fair left standing at the camp, or vanilla rocks
   poking through anything; Claudius stays round the camp (sits on the stool now and then).
5. The log: `SkyrimFairCamp: started, the fair is here` once, on the first load.

Console shortcuts: `set SkyrimFairStoryGate to 1` (the same as choosing it), `set
SkyrimFairStoryGate to 0` (the fair comes back next time you're out of sight).

## Next step

Barry's in-game test of pass 1. Then pass 2: the festival, monthly and wandering schedules (the
calendar from `GameDay`/`GameMonth`), the courier letter, and the Passport paused while away.
