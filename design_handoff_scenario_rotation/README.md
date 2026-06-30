# Handoff: Scenario Rotation (Phase 1B.1)

## Overview
Scenario Rotation lets a server admin build an ordered **playlist** of Arma Reforger scenarios, each with its own duration. The server plays them in turn (sequential or shuffle), warns connected players over **RCON** before each map change, and restarts on a configurable policy. It lives in the **Advanced** tab of Sentinel Desktop's main window.

This is the first feature of the product's "validation + prediction engine" vision (see `SENTINEL_DESKTOP.md`). It is **Pillar 1 (Loadouts/curated scenarios)** groundwork and introduces UI patterns the later pillars reuse (the predicted-fps chip is a Pillar 3 hook — static placeholder for now).

## About the Design Files
`scenario_rotation_reference.html` is a **design reference created in HTML** — a static prototype showing the intended look, layout, and copy. It is **not production code to copy directly**.

The target codebase is **C# WinForms** (`ArmaReforgerServerTool`, product name **Sentinel Desktop**, formerly "Longbow"). The task is to **recreate this design in WinForms** using the project's existing patterns: the `Design/` token classes (`Colors.cs`, `AppTypography.cs`, `AppSpacing.cs`), `HoverableButton`, and the existing Advanced-panel/TabControl structure. Open the HTML in a browser side-by-side while building the form. Do **not** embed the HTML in a WebView — this is native UI.

> ⚠️ Before building: realign `Design/Colors.cs` to `BRAND.md` (drop the `#FF8C00` orange accent; use the sky-blue + slate tokens below). The mock already uses the corrected palette.

## Fidelity
**High-fidelity.** Colors, typography, spacing, and copy are final. Recreate pixel-faithfully using the codebase's controls. Exact values are in **Design Tokens** below.

## Screens / Views

### Scenario Rotation (Advanced panel)
- **Purpose:** Admin curates an ordered scenario playlist with per-scenario durations, sets player-warning behavior, and chooses a restart policy.
- **Layout:** The panel sits inside the existing main window (title bar + context/tab bar are shown in the mock for context only — do not rebuild them; they already exist). The Scenario Rotation content is:
  - **Section header row** (full width): title "Scenario Rotation" + a `NEW · 1B.1` pill on the left; a "Rotation enabled" master toggle on the right. Padding 15px 24px, bg `#0B1219`, 1px bottom border `#1C2632`.
  - **Body:** CSS-grid two columns `1.55fr / 1fr`, 18px gap, padding 22px 24px, bg `#0A0E13`. In WinForms use a `TableLayoutPanel` (2 columns ~61%/39%) or split container.
    - **Left column** (vertical stack, 14px gap): (1) the **Playlist card**, (2) the **Timeline preview card**.
    - **Right column** (vertical stack, 14px gap): (1) **Player Warnings card**, (2) **Restart Policy card**, (3) **Save rotation** button.

#### Components

**Playlist card** — bg `#0E151D`, 1px border `#25323F`, radius 14px (use `AppSpacing` radius L), `overflow:hidden`.
- *Card header row:* mono label `ROTATION PLAYLIST · 4` (JetBrains Mono 10px, `#5B6877`, letter-spacing 1px, uppercase) on the left; a **segmented toggle** `Sequential | Shuffle` on the right. Selected segment: bg `#38BDF8`, text `#06121C`, weight 600; unselected: text `#8A97A6`. Segment container: bg `#070B10`, 1px border `#25323F`, radius 8px. Header padding 15px 18px, 1px bottom border `#1C2632`.
- *Scenario rows* (4 in the mock; data-driven list). Each row: `display:flex; align-items:center; gap:14px; padding:13px 18px;` with 1px bottom border `#161F29` (last row no border). Left→right:
  1. **Drag handle** glyph `⠿`, `#3A4756`, `cursor:grab`. (WinForms: implement drag-reorder of the list; handle is the affordance.)
  2. **Order number** (mono 12px, `#5B6877`, min-width 16px).
  3. **Scenario name + tags** (flex:1): name = Inter 14px/500 `#E7EDF3`; subline = mono 12px `#5B6877` (e.g. `PvP · 64p`).
  4. **Predicted-fps chip** (mono 10px, radius 5px, padding 4px 8px): green `#34D399` on `rgba(52,211,153,.12)` when ≥60; amber `#FBBF24` on `rgba(251,191,36,.12)` when <60. *Static placeholder for 1B.1 — wire to the predictor in a later phase.*
  5. **Duration field** — small number input + "min" suffix. Container: bg `#070B10`, 1px border `#25323F`, radius 7px, padding 6px 11px. Value mono 13px `#E7EDF3`; "min" 11px `#5B6877`.
  6. **Remove** `✕` (`#5B6877`, hover `#F87171`).
- *Card footer row:* `+ Add scenario` ghost button (1px border `#2F5E78`, text `#38BDF8`, radius 8px, padding 8px 15px, weight 600) on the left; right side mono 12px `#8A97A6` "full cycle · **6h 0m**" (the bold total in `#E7EDF3`). Footer bg `#0B1219`, 1px top border `#1C2632`, padding 13px 18px. **Total cycle = sum of all durations**; recompute on edit.

**Timeline preview card** — bg `#0E151D`, border `#25323F`, radius 14px, padding 16px 20px.
- Header: mono label `NEXT 6 HOURS` left; `↻ restart after cycle` mono 10px `#5B6877` right.
- **Proportional bar:** a 30px-tall flex row, radius 7px, 2px gaps between segments. Each scenario = one segment with `flex` proportional to its duration (90/60/120/90 in the mock). Segment colors step through the blue scale: `#0EA5E9`, `#0284C7`, `#0369A1`, `#075985`. Restart marker = thin `#F87171` segment (`flex:4`) at the end. Segment label centered, 10px/600, white or `#DBEAFE` on darker blues.
- Tick labels below: mono 10px `#5B6877`, space-between (now / +1.5h / +2.5h / +4.5h / +6h ↻).

**Player Warnings card** — bg `#0E151D`, border `#25323F`, radius 14px, `overflow:hidden`.
- Header row: mono label `PLAYER WARNINGS` + a small `RCON` tag (mono 9px, `#38BDF8`, 1px border `#2F5E78`, radius 4px, padding 2px 6px) on the left; a **toggle** (on) on the right. Padding 15px 18px, 1px bottom border `#1C2632`.
- Body (padding 15px 18px):
  - Caption "Broadcast before the map changes:" (Inter 13px `#8A97A6`).
  - **Lead-time chips** row: `15 min`, `5 min`, `1 min` as selected chips (bg `#38BDF8`, text `#06121C`, mono 12px/600, radius 7px, padding 6px 12px) + a dashed `+` add-chip (1px dashed `#25323F`, text `#8A97A6`). These are the offsets before rotation at which a warning fires. Each removable; `+` adds a custom offset.
  - "Message template" label (12px `#5B6877`), then an editable text field: bg `#070B10`, 1px border `#25323F`, radius 8px, padding 11px 13px, mono 12px `#CDD7E1`. Tokens `{next}` and `{minutes}` render in `#38BDF8`.
  - **Token legend** chips: `{next}`, `{minutes}`, `{players}` (mono 10px `#8A97A6`, bg `#0B1219`, 1px border `#25323F`, radius 5px). These are substituted at broadcast time.

**Restart Policy card** — bg `#0E151D`, border `#25323F`, radius 14px, padding 15px 18px.
- Mono label `RESTART POLICY`.
- **Radio group** (3 options, single-select; mock has option 1 selected):
  1. "After full rotation cycle" (selected: filled radio = 16px circle, 5px `#0EA5E9` ring on `#070B10` center).
  2. "Every ⟨N⟩h" — radio + inline value field (`6h`), field styled like the duration field.
  3. "Daily at ⟨HH:MM⟩" — radio + inline time field (`03:00`).
  - Unselected radios: 16px circle, 1.5px border `#3A4756`.
- Divider (1px `#161F29`), then a row: "Empty-server restart" label + toggle (off in mock). When on, restart only proceeds if the server is empty.

**Save rotation button** — full width, bg `#0EA5E9`, white text, Space Grotesk 15px/600, radius 10px, padding 13px. Hover `#0369A1`, active `#075985`. Persists the rotation config.

## Interactions & Behavior
- **Add scenario:** opens the existing scenario picker (reuse `ScenarioSelector` / the cached fast-scan list); appends a row with a default duration (e.g. 60 min).
- **Reorder:** drag rows by the `⠿` handle; order numbers and the timeline update live.
- **Edit duration:** numeric, minutes; clamp to a sane min (e.g. ≥5). Editing recomputes "full cycle" total and the timeline segment proportions.
- **Remove:** `✕` deletes the row (confirm only if list would become empty).
- **Sequential/Shuffle:** Sequential plays in listed order; Shuffle randomizes per cycle (avoid immediate repeat of the last scenario).
- **Master "Rotation enabled" toggle:** off = server stays on its single configured scenario; the rest of the panel can dim/disable.
- **Warning lead-times:** for each offset, fire one RCON broadcast at `(scenario_end - offset)`. Substitute `{next}` (next scenario name), `{minutes}` (offset), `{players}` (current count) into the template.
- **Restart policy:** mutually exclusive. "Empty-server restart" gates any restart on zero players (waits for empty, or forces at a hard cap — your call; document it).
- **Save rotation:** validates (≥1 scenario, all durations valid) then writes config; disabled/spinner while saving.
- **Hover states:** rows lighten background to `#101A24`; ghost buttons → border `#38BDF8`; chips → subtle lighten. Use `HoverableButton` patterns already in the repo.

## State Management
Rotation config object (persist to the server profile / config file the run loop reads):
```jsonc
{
  "enabled": true,
  "mode": "sequential",            // "sequential" | "shuffle"
  "entries": [
    { "scenarioId": "{ECC61978EDCC2B5A}Missions/23_Campaign.conf", "label": "Conflict — Everon", "durationMin": 90 },
    { "scenarioId": "…", "label": "Conflict — Arland", "durationMin": 60 },
    { "scenarioId": "…", "label": "Combat Ops — Everon", "durationMin": 120 },
    { "scenarioId": "…", "label": "Game Master — Everon", "durationMin": 90 }
  ],
  "warnings": {
    "enabled": true,
    "leadTimesMin": [15, 5, 1],
    "template": "Map changes to {next} in {minutes} min"
  },
  "restart": {
    "policy": "afterCycle",        // "afterCycle" | "everyNHours" | "dailyAt"
    "everyNHours": 6,
    "dailyAt": "03:00",
    "onlyWhenEmpty": false
  }
}
```
Runtime state needed by the rotation loop: current entry index, current scenario start time, computed end time, set of warning offsets already fired for the current entry, next-restart timestamp. Derived/UI: total cycle length (sum of durations), timeline proportions, per-row predicted fps (later: from predictor service).

## Design Tokens
**Colors** — brand: `#38BDF8` (accent), `#0EA5E9` (primary action), `#0284C7`/`#0369A1`/`#075985` (scale + hover/active). Neutrals: canvas `#05080C`, app bg `#0A0E13`, surface `#0E151D`, panel-dark `#070B10`, raised `#101A24`/`#0B1219`, border `#25323F`, hairline `#161F29`, brand-border `#2F5E78`, muted `#8A97A6`, faint `#5B6877`, dim `#3A4756`, text `#E7EDF3`, text-2 `#CDD7E1`. Semantic: ok `#34D399`, danger `#F87171`, warn `#FBBF24`. (Full system in bundled `BRAND.md`.)
**Type** — Display: Space Grotesk 600/700 (titles, Save button). UI/body: Inter 400–700. Mono: JetBrains Mono 400/500 (labels, durations, timeline ticks, token legend). Mono labels uppercase, letter-spacing ~1px.
**Spacing** — card padding 15–22px; grid gap 18px; column stack gap 14px; row padding 13px 18px.
**Radius** — cards 14px; buttons/fields 7–10px; pills 9999px; small tags 4–5px.
**Shadow** — window only: `0 30px 90px -40px rgba(0,0,0,.9)`. Brand glow (optional, selected/active): `0 18px 50px -28px rgba(56,189,248,.5)`.

## Assets
None bundled. Icons are simple glyphs in the mock (drag `⠿`, remove `✕`, restart `↻`) — replace with the repo's existing icon set (lucide-style line icons, stroke 1.5, per `BRAND.md`). The ascending-bars logo is drawn with three CSS rectangles; reuse the app's existing logo asset instead.

## Files
- `scenario_rotation_reference.html` — the high-fidelity design reference (open in a browser).
- `BRAND.md` — full STG visual system (colors, type, components, voice). Realign `Design/Colors.cs` to this.
- `SENTINEL_DESKTOP.md` — product brief / north star (the four pillars, validation model, naming, design principle).

The source mock also exists as turn **7a** in the larger design file `Sentinel Desktop — Main Panel.dc.html` (alongside related future screens 2a Loadouts, 3a Mission Builder, 4a Performance Presets, 5a/5b Live/Incident, 6a Preflight Debugger) for broader context.
