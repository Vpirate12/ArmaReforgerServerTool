# Sentinel Desktop — Product Brief

> Repo reference for humans and Claude Code. Pairs with `BRAND.md` (visual system).
> Stack: C# WinForms (forked from soda3x/ArmaReforgerServerTool, formerly "Longbow").
> When building UI, follow `BRAND.md` and realign `Design/Colors.cs` (drop the orange accent).

## What it is
The Arma Reforger server tool that **thinks ahead**. Turns server hosting from dark-art tuning into a confident, one-click launch — for milsim units, server admins, and esports orgs.
Sentinel Desktop **runs** the server; **Sitrep** (web panel) **controls** it remotely. Both are STG products.

## Core idea: the Mission
**Mission = Scenario + Loadout + Performance Preset.** A complete, launch-ready package — curated, predicted on your hardware, validated, shareable. STG ships official Missions; the community shares theirs.

## Four pillars
1. **Curated Loadouts** — tested, dependency-checked mod sets applied in one click. Preflight-tested; run on STG's own servers. (Maps to existing mod manager: `enabledMods`/`availableMods`, `ModValidationService`.)
2. **Performance Presets** — tune by goal; Sentinel sets the 20+ advanced params (`aiLimit`, `jobsysLongWorkerCount`, `staggeringBudget`, `streamingBudget`, `serverMaxViewDistance`, `maxFPS`, …) to hit a tick-rate target.
3. **Performance Prediction** — predict tick rate *before* launch on the user's exact hardware (cores/threads/RAM + loadout + tuning); one-tap fixes to reach 60. Baseline from real run telemetry + reference hardware.
4. **Live Watch & Alerts** — quiet when healthy; on tick-drop/crash, push the cause + action to Discord + Sitrep. (FPS = Reforger tick rate, already charted via `chartFps`; logs parsed by `ServerStatusParser`.)

## Validation model (three tiers)
"Validated" is specific and **point-in-time** (re-checked on game/mod updates):
1. **Static checks** (instant) — deps present, load order correct, versions pinned, no *known* conflicts.
2. **Preflight boot** (empirical) — headless dry-run of the real server (isolated profile/port, `noBackend`), parse boot log. Catches missing/incompatible addons, **script compile errors**, load-order/override clashes, scenario load failures, version mismatches, shader-build failures. **Auto-bisect**: on failure, re-run with halves of the list (binary search) to isolate the culprit mod. Translate engine errors → plain English. Run async/background; cache; re-run only when the mod set or game version changes.
3. **Runs on STG servers** (proof) — real play surfaces runtime issues a boot can't. Telemetry feeds a **shared known-issues DB** — deterministic, explainable, compounding (the data moat).

Honest labels: `preflight-checked · runs on STG · checked <date>`. **Never** "verified" / "conflict-free."

**On a local LLM for validation:** don't. Validation is deterministic — the boot is ground truth; an LLM hallucinates and competes with the server for resources. Use an LLM only to *explain* deterministic results (cloud call, not local); use a rules/known-issues DB for the knowledge.

## Design principle
**The right info, to the right person, at the right time.**
- **Right person** — progressive disclosure (novice: "ready? → Start"; power user: params, diffs, RCON).
- **Right time** — lifecycle-aware (setup → pre-flight → running → incident).
- **Right info** — ruthless suppression; a vital sign shouts only when it matters (no data slop).

## Pain → solved
- Cryptic 20+ tuning params → Performance Presets + live Predictor
- Mod dependency/load-order hell → Curated Loadouts + preflight boot + auto-bisect
- Slow scenario scraping (`ScenarioSelector`) → cached, incremental fast scan
- Crashes while away → Live Watch, auto-restart (`autoRestartOnCrash`), Discord + Sitrep alerts
- "Is my hardware enough?" → prediction + hardware headroom read
- Hand-editing raw `server.json` → Mission Builder (guided, validated, JSON as output)

## What we don't claim
- Not conflict-free or bug-free — only "no *known* conflicts."
- Performance numbers are estimates; real load varies (players, AI, mods).
- A clean preflight boot ≠ a clean match — runtime needs play testing.
- Validation is point-in-time; re-checked on game/mod updates.

## Naming
- Company: **STG — Signal Tactical Group** (not "Spare Time Gaming" — legacy domain → redirect only).
- **Sentinel Desktop** = Windows host tool (was "Longbow" — retire that name/logo). **Sitrep** = web panel. **Sentinel** = the host agent.

## How it's built
Design → Handoff → Claude Code. Reference mockups (in the design project): panel directions 1a/1b/1c, Loadouts 2a, Mission Builder 3a, Performance Presets 4a, Live/Incident 5a/5b, Preflight Debugger 6a.
