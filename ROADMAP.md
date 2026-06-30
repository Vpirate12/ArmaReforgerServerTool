# Sentinel Desktop Product Roadmap: Phase 1A → 1B → 2

**Vision:** Enable server admins to manage Arma Reforger mods and operations with confidence. Start with individual players (Phase 1A), scale to gaming orgs (Phase 1B), then reach esports (Phase 2).

**Last Updated:** 2026-06-30 (Phase 1B LOCKED)

---

## Phase 1A: End User (MVP) ✅ SHIPPED

**Target:** Individual players, small groups, casual server admins

**Status:** 🚀 **SHIPPED** (June 29, 2026)

### Features (All Complete)
- ✅ Mod validator (detects missing deps, version conflicts, circular deps)
- ✅ Auto-fix (adds missing mods, reorders for correct load order)
- ✅ Check Mods button with progress feedback
- ✅ Results display (shows what was fixed)
- ✅ Save/load mod configurations (local JSON)
- ✅ Start button gated on validation (RED=invalid, GREEN=valid)
- ✅ Steam Workshop API integration (real mod metadata)
- ✅ Sitrep branding (sky blue, dark theme, SteamOS UX)

### User Flow
1. Load mod config or build manually
2. Click "Check Mods"
3. Validation runs with progress feedback
4. If errors → auto-fix, re-validate, show what was fixed
5. If valid → show "Ready to launch"
6. Click "Start Server"

### Success Metric
"I don't have to dig through logs anymore. The tool tells me exactly what's wrong and fixes it."

---

## Phase 1B: Org Admin (Professional) + Live Ops — LOCKED

**Target:** Gaming orgs, clans, small hosting providers (1–50 servers)

**Status:** 🔒 **LOCKED SCOPE** (16 Features + 5 Smoothing Solutions)

**Timeline:** 10-12 weeks from architecture start → Q3 2026

### Core Admin Features (RCON-Based, No Server Mod Required)

#### Base Infrastructure (Weeks 1-3)
1. **Tauri/React Frontend + .NET 8 Web API** (architecture migration from WinForms)
   - SQLite audit log + player profile database
   - User/role management (RBAC)
   - Multi-server RCON connection pooling

#### Multi-Server Controls (Weeks 4-5, Weeks 9-10)
2. **Multi-Server Dashboard** — Manage 50+ servers from one UI (real-time status, player count, version tracking)
3. **User/Role Management (RBAC)** — Control who can push configs, who can ban, who can view logs
4. **Audit Logging** — Full trail of who changed what, when (config pushes, bans, role changes)
5. **Enforce Standard Modlist** — One-click push to 50 servers in parallel (validation runs on each)
6. **Mod Version Tracking Per Server** — Dashboard shows active mod versions, last update time, discrepancies
7. **Performance Monitoring** — Real-time player count, uptime, crash detection, auto-restart flags
8. **Scheduled Restarts/Updates** — Maintenance windows with player warnings, graceful shutdown
9. **Player Whitelist/Blacklist** — Central list synced across all servers, auto-kick on spawn
10. **Scenario Rotation Manager** — Visual playlist editor, drag-drop reordering, shuffle mode, RCON broadcasts

#### Local Desktop Tools (No Server Mod Needed)
11. **Visual JSON Config Editor** — GUI for editing `config.json` (no more manual syntax errors)
12. **Workshop Download Shield** — External cache with SHA-256 verification (prevent download corruption)
13. **Headless Client (HC) Manager** — Auto-launch HC processes, keep in sync with main server
14. **Mod Kick Diagnostic** — Real-time detection of signature kicks, identify outdated mod, post solution to Discord
15. **Scenario Warnings** — Configurable RCON broadcasts at T-10/5/1 min with token substitution ({next}, {minutes})

#### Sentinel Link Phase 0 (RCON Fallback)
16. **Sentinel Link Phase 0 Baseline** — RCON-only roster, kick, ban, broadcast (works without server mod, proves Live Ops shell)

### Live Ops (Depends on Sentinel Link Phase 1 Spike — Weeks 5-6)

#### Sentinel Link Phase 1 (Telemetry - Read-Only)
- **Status:** ⚠️ CONDITIONAL (Spike required, Weeks 5-6)
- **Feature:** Server mod streams live telemetry (roster, positions, kills, chat, objectives) to desktop app
- **Unlocks:** Multi-Vector Identity Resolver, Command Auditor, stats tracking, spatial visualizer
- **Contingency:** If spike fails, drop dependent features, ship RCON-only baseline (still professional-tier)

#### Dependent on Phase 1 (Post-Spike Integration)
17. **Multi-Vector Identity Resolver** — Unified identity database, visual "Connection Web", cascade ban across servers
18. **Live Admin Console Auditor** — Real-time audit of admin commands (whisper, warn, kick, ban) → Discord webhook + dashboard

### Phase 1B Quick-Win Smoothing Solutions (Integrated into Timeline)

These ship with Phase 1B MVP (low effort, high impact on UX):

1. ✅ **Onboarding Wizard** (3-step setup: RCON, roles, log paths) — *Week 4-5, Low effort*
2. ✅ **Crash Log Summary Widget** (dashboard surfaces last error + restart button) — *Week 7-8, Low effort*
3. ✅ **Live Kick Diagnostic Card** (real-time mod signature alerts with copyable solution) — *Week 7-8, Low effort*
4. ✅ **Ban/Unban Confirmation Dialog** (prevents accidental bans) — *Week 9-10, Very Low effort*
5. ✅ **Bulk SteamID Paste for Whitelist** (paste 20 SteamIDs at once) — *Week 9-10, Very Low effort*

### User Flow
1. Admin loads org standard modlist
2. Pushes to all 10 servers at once (with diff preview guardrail)
3. Validation runs in parallel on each server
4. Audit log shows which admin pushed, which servers accepted/rejected
5. Performance dashboard shows all servers healthy
6. Crash Log Widget alerts admin if a server goes down
7. Live Kick Diagnostic auto-detects mod mismatch waves, suggests fix

### Success Metric
"We know exactly what mods are running everywhere and who changed them. We sleep better at night."

### Deferred from Phase 1B (Phase 1B.1 or Phase 2)

These are valuable but moved to post-MVP to preserve timeline:

- **Server CSV Import** — Bulk import server connection details (Phase 1B.1)
- **Config Diff Preview** — Show differences before pushing (Phase 1B.1)
- **Config Rollback** — Version history, one-click restore (Phase 1B.1)
- **Modlist Comparator** — Side-by-side mod differences (Phase 1B.1)
- **Unified Ban/Unban Modal** — Apply across 50 servers at once (Phase 1B.1)
- **Parallel Config Push** — Concurrent deployments (Phase 1B.1)
- **Audit Log Filter/Search** — Rich UI for compliance queries (Phase 1B.1)
- **Crash Dump Analyzer** — Binary `.dmp` parsing (deferred, low ROI)
- **Live Ops Map (9a)** — 2D tactical map, real-time positions (Phase 2, esports tier)
- **Admin Call Panel (9b)** — Incident response UI (Phase 1B.1)

---

## Phase 2: Esports (Competitive)

**Target:** Esports venues, tournament organizers, professional leagues

**Status:** 📋 **PLANNED** (Q4 2026+)

### Features
- **Tournament Mode** (lock configs during matches)
- **Rapid Config Switching** (<30 seconds between matches)
- **Spectator Management** (observer-only accounts, caster tools)
- **Sentinel Link Phase 2** (remote actions: whisper, warn, mute)
- **Sentinel Link Phase 3** (deep control: teleport, makeGM — esports-only, high-risk)
- **Statistics Tracking** (kills, objectives, player performance, economy)
- **Leaderboards/Rankings** (track competitive results, season standings)
- **Spatial Combat Visualizer** (hardware-accelerated 2D tactical map, NDI stream for OBS)
- **Match Recording** (auto-record telemetry for review/broadcast)
- **Performance Prediction** (ML baselines for tick rate vs. load correlation)
- **Incident Alerts** (precise cause detection: "tick fell when AI hit 96 on Objective B")
- **Esports Onboarding Wizard** (5-step tournament setup for tournament directors)

### User Flow
1. Org sets up tournament bracket
2. Deploy tournament modlist (locked, no changes)
3. Match starts → live admin controls available
4. Match ends → stats auto-exported, leaderboard updated
5. Next match → swap config in 20 seconds
6. Post-tournament → recordings available for review

### Success Metric
"Run a 16-team tournament in a day with zero mod issues. Broadcast-ready stats without manual work."

---

## Sentinel Link: Server Mod Roadmap

**Overview:** Sentinel Link is a server-side Enfusion mod that acts as a bridge between the running game simulation and Sentinel Desktop. It enables Live Ops features (real-time player positions, kill events, admin reports) and unlocks the entire Pillars 3 & 4 (prediction + alerts).

### Why a Mod is Required
1. **Headless server has no video feed**, but scripting API has full access to entities, positions, events, chat
2. **Vanilla RCON only exposes basic commands** (kick, ban, say) — no structured game state
3. **Enfusion scripting is the only layer** that can read/write live game state

### Development Phases

| Phase | Name | Features | Scope | Timeline | Dependency |
|-------|------|----------|-------|----------|------------|
| **0** | RCON-only | Roster, kick, ban, broadcast (no mod) | Phase 1B baseline | ✅ Done | None |
| **1** | MVP (read-only) | Telemetry (positions, kills, chat), admin reports, Live Ops map (9a), Admin Call (9b) | Phase 1B | **SPIKE Weeks 5-6** | Enfusion API proof |
| **2** | Remote actions | Whisper, warn, mute — full incident resolution from desk | Phase 1B+ | Q3 2026 | Phase 1 result |
| **3** | Deep control | Teleport, makeGM, auto-moderation rules | Phase 2 | Q4 2026+ | Phase 2 spike (HIGH RISK) |
| **4** | Sitrep parity | Same telemetry to web panel, shared baselines DB | Phase 2 | Q4 2026+ | Phase 3 complete |

### Critical Open Questions (Phase 1 Spike)

**Spike Window: Weeks 5-6 of Phase 1B**

1. **API Surface:** Which of {position read, kill/chat hooks, mute, teleport, makeGM, HTTP/WebSocket, file IO} are reachable from a **server-only** mod?
2. **Server-Only Assumption:** Clients don't need to download the mod?
3. **Stable Player Identity:** How to track players across reconnects?
4. **Transport Mechanism:** NDJSON file, local socket, or HTTP — what does sandbox permit?

**Proof of Concept Acceptance Criteria:**
- Console script in Sentinel Desktop outputs real player coordinates streamed from server-side Enforce mod (no frame rate degradation)
- Local HTTP POST loop established between mod and .NET API
- Command round-trip verified (push whisper command, mod delivers it in-game)

---

## Feature Layering

| Feature | Phase 1A | Phase 1B | Phase 2 |
|---------|----------|----------|---------|
| Mod validation | ✅ Core | ✅ Core | ✅ Core |
| Auto-fix | ✅ | ✅ | ✅ |
| Config save/load | ✅ | ✅ | ✅ |
| Check Mods button | ✅ | ✅ | ✅ |
| Multi-server | ❌ | ✅ | ✅ |
| Audit logging | ❌ | ✅ | ✅ |
| Sentinel Link Phase 0 | ❌ | ✅ | ✅ |
| Sentinel Link Phase 1 (telemetry) | ❌ | ✅ (spike) | ✅ |
| Sentinel Link Phase 2 (actions) | ❌ | ✅ (post-spike) | ✅ |
| Live Ops map | ❌ | ❌ (defer to 2) | ✅ |
| Tournament mode | ❌ | ❌ | ✅ |
| Live controls (deep) | ❌ | ❌ | ✅ (gated on 3) |
| Stats/leaderboards | ❌ | ❌ | ✅ |
| Performance prediction | ❌ | ❌ | ✅ (Phase 2.1) |

---

## Monetization

**Free Tier (Phase 1A):**
- Basic validator (no auto-fix)
- Single server only
- Community support

**Premium (Phase 1A):** $79 perpetual or $9.99/mo
- Validator + auto-fix
- Config save/load/share
- Crash recovery
- Priority support

**Professional (Phase 1B):** $249/yr or $24.99/mo per org
- Multi-server management
- Audit logging
- User/role management
- Team collaboration
- Sentinel Link Phase 0 (RCON fallback included)

**Enterprise (Phase 1B+):** Custom pricing
- ⬆️ Professional +
- Sentinel Link Phase 1 (telemetry)
- Multi-Vector Identity Resolver
- Live Admin Auditing

**Esports (Phase 2):** Custom pricing
- ⬆️ Enterprise +
- Tournament mode
- Live admin controls (whisper, warn, mute)
- Statistics/leaderboards
- Spatial visualizer (broadcast-ready)
- Match recording

---

## Why This Order

1. **Phase 1A first:** Smallest scope, fastest time-to-market. Validates the validator + auto-fix + UX works with real users.
2. **Phase 1B second:** Builds on Phase 1A foundation. Adds multi-server orchestration. Attracts professional orgs.
3. **Phase 2 third:** Premium market segment. Highest revenue potential. Most complex feature set.

Each layer builds on the previous. No rework needed.

---

## Success Criteria

**Phase 1A:** Users trust validator enough to launch servers without manually checking logs. ✅ ACHIEVED

**Phase 1B:** Orgs can manage 50+ servers from one dashboard with confidence. Admin pain points smoothed (5 quick wins integrated). Critical path: Sentinel Link Phase 1 spike.

**Phase 2:** Esports orgs run tournaments without mod-related issues. Broadcast-ready tools. Performance baselines prevent tick rate surprises.

---

## Timeline Summary

- **Phase 1A Shipped:** June 29, 2026
- **Phase 1B Start:** July 2026 (architecture migration)
- **Phase 1B Sentinel Link Spike:** Weeks 5-6 (critical gate)
- **Phase 1B Ship:** September 2026 (Q3, 10-12 weeks)
- **Phase 1B.1 (Smoothing Extensions):** October-November 2026 (6-8 weeks post-launch)
- **Phase 2 Start:** December 2026
- **Phase 2 Ship:** Q4 2026+ (timeline depends on Phase 1B learnings + spike results)

---

**Document Status:** 🔒 LOCKED for Phase 1B implementation  
**Next Action:** Execute Phase 1B Implementation Checklist (week-by-week)  
**Research:** See ROADMAP_RESEARCH.md for AI Studio analysis details
