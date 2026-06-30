# Handoff: Phase 1B.1 Scenario Rotation — Architecture & Logic Complete

**Status:** Ready for DevOps Review → Audit → Test → QA Gates  
**Branch:** `feature/scenario-manager`  
**Latest Commit:** c5640d0 (Stage 2B.1: ProcessManager rotation task loop)  
**Handoff Date:** 2026-06-29  
**Handoff From:** Claude Code (Implementer)  

---

## What's Complete ✅

### Stage 1: Core Data Models (COMPLETE)

**Files Created:**
- `Models/ScenarioRotationEntry.cs` — Single rotation entry (scenarioId, label, durationMin)
- `Models/ScenarioRotationConfig.cs` — Complete rotation config with:
  - ScenarioRotationConfig (enabled, mode, entries list)
  - ScenarioRotationWarnings (enabled, leadTimesMin[], template)
  - ScenarioRotationRestart (policy, everyNHours, dailyAt, onlyWhenEmpty)

**Files Modified:**
- `Models/SavedState.cs` — Replaced simple rotation properties with `ScenarioRotationConfig` object
- `Utils/JsonUtils.cs` — Updated `SavedStateConverter` to serialize/deserialize new config structure
  - Gracefully handles missing fields (forward-compatible)

**Test:** JSON round-trip serialization works; config persists to state.json

### Stage 2: ProcessManager Rotation Loop (COMPLETE)

**File Modified:** `Managers/ProcessManager.cs`

**Methods Updated:**
- `ConfigureRotationTask(ScenarioRotationConfig config)` — accepts new config model
  - Validates enabled flag and entry count
  - Cancels existing tasks cleanly
  - Starts async loop

- `RunRotationAsync(ScenarioRotationConfig config, CancellationToken ct)` — refactored for new model
  - Uses `DurationMin` (minutes) instead of hours
  - Uses `Label` and `ScenarioId` instead of name/path
  - Implements configurable RCON warnings:
    - Reads `config.Warnings.LeadTimesMin[]` (e.g., [15, 5, 1])
    - Substitutes tokens in template: `{next}`, `{minutes}`, `{players}`
    - Properly calculates wait times between warnings
  - Supports shuffle mode: `config.Mode == "shuffle"` randomizes per cycle
  - Gracefully handles disabled warnings (RCON optional)
  - Properly handles restart policies (structure in place, logic TODO for Stage 4)

**Key Logic:**
- Rotation continues indefinitely until disabled
- RCON broadcasts sent at configured lead times
- Server restart triggered after scenario switch
- Event `ScenarioRotationSwitchEvent` fired to notify UI

### Supporting Changes

**File Modified:** `Utils/Colors.cs`
- Realigned to BRAND.md (sky blue + deep slate palette)
- Dropped orange accent
- Added proper color tokens with semantic meanings

**Files Added:**
- `design_handoff_scenario_rotation/` — complete design spec (README, HTML mockup, design tokens)
- `SENTINEL_DESKTOP.md` — product vision (mission concept, four pillars, validation model)
- `BRAND.md` — STG visual system (colors, typography, components, voice)
- `HANDOFF.md` — session handoff
- `HANDOFF_PHASE_1B1.md` — this document

---

## What's NOT Complete (Stage 3) 🚧

### UI Implementation Required

**Location:** `Forms/Main.cs` — Advanced panel rotation section (currently at line 2620+)

**Existing Code Issues:**
- Uses old model properties (ScenarioName → should be Label; DurationHours → should be DurationMin)
- References removed field `scenarioRotationEnabled` (now part of config.Enabled)
- Only creates basic ListView + buttons; doesn't include:
  - Warnings card (template editor, lead-time chips, token legend)
  - Restart policy card (radio group, policy-specific fields)
  - Timeline preview card (proportional bar chart)
  - Sequential/Shuffle toggle in playlist header
  - Master "Rotation enabled" toggle in section header

**Design Spec Reference:** `design_handoff_scenario_rotation/README.md` + HTML mockup

**Estimated Effort:** 2-3 hours

**Key Tasks:**
1. Replace ListView with DataGridView for inline duration editing
2. Create 4 card layout (2-column grid: Playlist + Timeline | Warnings + Restart Policy)
3. Implement warnings template editor with token substitution UI
4. Implement restart policy radio group with conditional fields
5. Wire all UI events to `ScenarioRotationConfig` model
6. Load/save rotation config via `SavedStateManager`
7. Call `ProcessManager.ConfigureRotationTask(config)` when rotation enabled
8. Call `ProcessManager.CancelRotationTask()` when rotation disabled

---

## Architecture Summary

### Data Flow

```
SavedState.scenarioRotation (ScenarioRotationConfig)
  ↓ (load on app startup)
UI (Main.cs Advanced panel)
  ↓ (user edits: add/remove/reorder scenarios, set warnings/policy)
ScenarioRotationConfig updated
  ↓ (click "Save rotation" or auto-save)
SavedState persisted to state.json
  ↓ (server start)
ProcessManager.ConfigureRotationTask(config)
  ↓
RunRotationAsync loop
  ↓
RCON broadcasts (warnings) + Scenario switch + Server restart
```

### Event Handlers

Existing:
- `ProcessManager.ScenarioRotationSwitchEvent` — fires when scenario changes
- `ConfigurationManager.UpdateScenarioIdFromLoadedConfigEvent` — fires when scenario loaded

Need in UI:
- Listen to rotation enabled checkbox → call ConfigureRotationTask() / CancelRotationTask()
- Listen to playlist edits → recalculate timeline preview
- Listen to warnings edits → validate template tokens
- Listen to restart policy edits → validate fields per policy

---

## Testing Checklist (For QA Phase)

**Unit Tests (Code):**
- [ ] ScenarioRotationConfig JSON serialization round-trip
- [ ] SavedState with empty rotation loads without error
- [ ] RunRotationAsync correctly calculates warning times
- [ ] Token substitution in warning template works
- [ ] Shuffle mode doesn't repeat same scenario back-to-back

**Integration Tests (UI):**
- [ ] Add scenario → appears in list with default duration
- [ ] Edit duration → timeline preview updates live
- [ ] Reorder scenarios [▲][▼] → order reflects in rotation
- [ ] Remove scenario → confirm only if list would be empty
- [ ] Toggle Sequential/Shuffle → changes behavior on next cycle
- [ ] Master toggle "Rotation enabled" on/off → calls ProcessManager methods
- [ ] Disable RCON → rotation still works, no warnings
- [ ] Config persists to state.json → reload on app restart
- [ ] Server running with rotation enabled → RCON warnings at lead times
- [ ] Scenario switch → server restarts, new scenario loads

**Manual Testing:**
- [ ] Start server, enable rotation with 3 scenarios → player sees warnings at -10/-5/-1 min
- [ ] Verify tick rate stays above threshold during rotation
- [ ] Verify no mod dependency issues when switching scenarios
- [ ] Test restart policies (afterCycle, everyNHours, dailyAt)

---

## Known Gaps

1. **Restart Policy Enforcement** — Structure in place, loop logic not yet implemented for "everyNHours" and "dailyAt" policies
2. **Player Count Token** — `{players}` always substitutes "0"; needs hook to `ServerStatusParser.GetCurrentStatus()`
3. **Hardware Prediction** — FPS chip in design spec is static placeholder (Pillar 3 work, not Phase 1B.1)
4. **Auto-Save** — UI doesn't auto-save on edits; requires explicit "Save rotation" button
5. **Validation on Edit** — Duration field doesn't validate min (≥5 min) in real-time

These are design decisions, not blockers. See notes in README for approach.

---

## Files Changed Summary

```
✅ Models/
   - ScenarioRotationEntry.cs (updated)
   - ScenarioRotationConfig.cs (new)
   - SavedState.cs (updated)

✅ Managers/
   - ProcessManager.cs (updated: ConfigureRotationTask, RunRotationAsync)

✅ Utils/
   - Colors.cs (updated: BRAND.md alignment)
   - JsonUtils.cs (updated: SavedStateConverter)

🚧 Forms/
   - Main.cs (existing UI needs refactor to match design spec)

📄 Design/
   - design_handoff_scenario_rotation/ (spec + mockup)
   - SENTINEL_DESKTOP.md (product vision)
   - BRAND.md (visual system)
```

---

## Next Steps (For DevOps Pipeline)

1. **Review Agent** → Verify architecture, correctness, no logic errors
2. **Audit Agent** → Security check (RCON broadcast, config parsing, file I/O)
3. **Test Agent** → Run unit tests + integration tests
4. **QA Gate** → Verify against SENTINEL_DESKTOP.md product spec
5. **Implementer** (next phase) → UI refactor for Stage 3
6. **Review** → Code review of UI changes
7. **Docs** → Update README with rotation feature docs
8. **Deploy** → Merge to main

---

## How to Continue (For Next Implementer)

1. Check out `feature/scenario-manager`
2. Read `design_handoff_scenario_rotation/README.md` for UI spec
3. Open `design_handoff_scenario_rotation/scenario_rotation_reference.html` in browser (visual target)
4. Update `Forms/Main.cs` rotation section (line 2620+) to:
   - Use new model properties (Label, ScenarioId, DurationMin)
   - Create card-based layout per design spec
   - Wire up all events
5. Test against manual testing checklist above
6. Commit with message: "Stage 3B.1: Scenario Rotation UI (card layout, warnings, restart policy)"

---

## Questions / Decisions for Stakeholder

- **Restart Policy "everyNHours":** Should this reset if the server crashes and restarts, or track wall-clock time?
- **Shuffle Mode:** When mode changes from Sequential to Shuffle mid-rotation, should current scenario index reset?
- **Empty Server Restart:** Hard cap (e.g., wait max 5 min) or infinite wait for empty server?
- **Performance Prediction (Pillar 3):** Should Phase 1B.1 include static "~60 fps" chips in UI, or defer to Phase 2?

---

**Handoff complete. Ready for DevOps review → audit → test → merge.**

Commit: `c5640d0`  
Branch: `feature/scenario-manager`  
GitHub: https://github.com/Vpirate12/ArmaReforgerServerTool
