# Longbow Product Roadmap: Phase 1A → 1B → 2

**Vision:** Enable server admins to manage Arma Reforger mods with confidence. Start with individual players (Phase 1A), scale to gaming orgs (Phase 1B), then reach esports (Phase 2).

---

## Phase 1A: End User (MVP) ✅ SHIPPED

**Target:** Individual players, small groups, casual server admins

**Features:**
- ✅ Mod validator (detects missing deps, version conflicts, circular deps)
- ✅ Auto-fix (adds missing mods, reorders for correct load order)
- ✅ Check Mods button with progress feedback
- ✅ Results display (shows what was fixed)
- ✅ Save/load mod configurations (local JSON)
- ✅ Start button gated on validation (RED=invalid, GREEN=valid)

**User flow:**
1. Load mod config or build manually
2. Click "Check Mods"
3. Validation runs with progress feedback
4. If errors → auto-fix, re-validate, show what was fixed
5. If valid → show "Ready to launch"
6. Click "Start Server"

**Success metric:** "I don't have to dig through logs anymore. The tool tells me exactly what's wrong and fixes it."

**Status:** 🚀 **SHIPPED** (June 29, 2026)

---

## Phase 1B: Org Admin (Professional) + Live Ops

**Target:** Gaming orgs, clans, small hosting providers (1–50 servers)

**Features:**

### Core Admin
- Multi-server dashboard (manage 50+ servers from one UI)
- Enforce standard modlist across all servers
- Push config updates to all servers at once
- User/role management (who can change mods)
- **Audit logging** (who changed what, when)
- Mod version tracking per server
- Performance monitoring (player count, crashes, health)
- Scheduled restarts/updates
- Player whitelist/blacklist management
- Server-specific override configs

### Live Ops (via Sentinel Link server mod)
- **Sentinel Link mod** (server-side Enfusion addon — bridge between game state and Sentinel Desktop)
  - Telemetry collection (player positions, kills, chat, objectives)
  - Admin report channel (`!admin` command in-game)
  - Remote admin controls (whisper, warn, mute, kick, ban — no client needed)
  - Performance metrics (tick rate vs. load correlation)
- **Live Ops map** (9a design — real-time situational awareness)
  - Top-down view of server with live player positions
  - Objective ownership tracking
  - Activity heatmaps
- **Admin Call panel** (9b design — incident response)
  - Spawn-kill reports with evidence (chat, kill events, proximity)
  - Direct whisper to player without joining game
  - Quick ban/warn/mute with audit trail
  - Target: resolve incidents in <30 seconds from desk

**Architecture shift:** Web-based UI (React/TypeScript) + .NET backend. Sentinel Link runs server-side only (no client mod required).

**User flow:**
1. Admin loads org standard modlist
2. Pushes to all 10 servers at once
3. Validation runs in parallel on each server
4. Audit log shows which admin pushed, which servers accepted/rejected
5. Performance dashboard shows all servers healthy

**Success metric:** "We know exactly what mods are running everywhere and who changed them."

**Timeline:** Q3 2026 (estimated)

---

## Phase 2: Esports (Competitive)

**Target:** Esports venues, tournament organizers, professional leagues

**Features:**

### Tournament Infrastructure
- **Tournament mode** (lock configs during matches, prevent mid-match changes)
- **Rapid config switching** (<30 seconds between matches)
- **Live bracket management** (auto-update tournament bracket, schedule matches)
- **Spectator management** (observer-only accounts, caster tools)

### Deep Live Ops (Sentinel Link full capabilities)
- **Advanced admin controls** (teleport, makeGM, auto-moderation rules)
- **Cheat detection** (flag unauthorized mod use, track anomalies)
- **Match recording** (auto-record for review/broadcast)
- **Performance prediction** (Pillar 3 — real baselines from live tick vs. load data)
- **Incident alerts** (Pillar 4 — precise cause detection when tick rate drops)

### Analytics & Insights
- **Statistics tracking** (kills, objectives, player performance, economy)
- **Leaderboards/rankings** (track competitive results, season standings)
- **Player stats export** (CSV/JSON for commentators, broadcast overlay integration)
- **Replay system** (correlate tick rate / AI count / objectives with match events)
- **Known-issues DB** (shared telemetry feeds competitive baselines)

**User flow:**
1. Org sets up tournament bracket
2. Deploy tournament modlist (locked, no changes)
3. Match starts → live admin controls available
4. Match ends → stats auto-exported, leaderboard updated
5. Next match → swap config in 20 seconds
6. Post-tournament → recordings available for review

**Success metric:** "Run a 16-team tournament in a day with zero mod issues."

**Timeline:** Q4 2026+ (estimated)

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
| Sentinel Link mod | ❌ | ✅ (MVP) | ✅ (Full) |
| Live Ops map (9a) | ❌ | ✅ | ✅ |
| Admin Call panel (9b) | ❌ | ✅ | ✅ |
| Remote whisper/mute | ❌ | ✅ | ✅ |
| Teleport/makeGM | ❌ | ❌ | ✅ |
| Tournament mode | ❌ | ❌ | ✅ |
| Live bracket mgmt | ❌ | ❌ | ✅ |
| Stats/leaderboards | ❌ | ❌ | ✅ |

---

## Monetization

**Free Tier:**
- Basic validator (no auto-fix)
- Single server only
- Community support

**Premium (Phase 1A):** $79 perpetual or $9.99/mo
- Validator + auto-fix
- Config save/load/share
- Crash recovery
- Priority support

**Enterprise (Phase 1B):** $249/yr or $24.99/mo per server
- Multi-server management
- Audit logging
- User/role management
- Team collaboration

**Esports (Phase 2):** Custom pricing
- Tournament mode
- Live admin controls
- Statistics/leaderboards
- Match recording

---

## Why This Order?

1. **Phase 1A first:** Smallest scope, fastest time-to-market. Validates the validator + auto-fix + UX works with real users.
2. **Phase 1B second:** Builds on Phase 1A foundation. Adds multi-server orchestration. Attracts professional orgs.
3. **Phase 2 third:** Premium market segment. Highest revenue potential. Most complex feature set.

Each layer builds on the previous. No rework needed.

---

## Success Criteria

**Phase 1A:** Users trust validator enough to launch servers without manually checking logs.

**Phase 1B:** Orgs can manage 50+ servers from one dashboard with confidence.

**Phase 2:** Esports orgs run tournaments without mod-related issues.

---

---

## Sentinel Link: Server Mod Roadmap

**Overview:** Sentinel Link is a server-side Enfusion mod that acts as a bridge between the running game simulation and Sentinel Desktop. It enables Live Ops features (real-time player positions, kill events, admin reports) and unlocks the entire Pillars 3 & 4 (prediction + alerts).

**Why a mod is required:**
1. Vanilla RCON (BattlEye) only exposes basic commands (kick, ban, say) — no structured game state
2. Headless server has no video feed, but scripting API has full access to entities, positions, events, chat
3. Enfusion scripting is the only layer that can read/write live game state and expose it outward

### Sentinel Link Phases

**Phase 0: RCON-only (no mod required)**
- Roster from RCON/logs, kick, ban, broadcast
- ✅ Already in codebase; ships with base tool
- Proves Live Ops shell without server mod
- Status: Part of Phase 1B baseline

**Phase 1: MVP (read-only + reports)**
- Telemetry out: roster, positions, kills, objectives, chat (sampled on tick budget)
- Admin report channel (`!admin` command in-game)
- Lights up: Live Ops map (9a) + Admin Call panel (9b)
- Transport: File/NDJSON bridge (fallback) or local WebSocket (if API permits)
- Status: Target Phase 1B Q3 2026

**Phase 2: Remote actions via mod**
- Whisper, warn, mute commands
- Full 9b incident resolution from desk without joining game
- Status: Phase 1B Q3 2026 (depends on Phase 1)

**Phase 3: Deep control** ⚠️ *Spike required*
- Teleport, makeGM, auto-moderation rules
- Gated on Enfusion API surface (confirm position write, GM grant, hooks)
- Status: Phase 2 Q4 2026

**Phase 4: Sitrep parity + shared telemetry DB**
- Same telemetry stream powers web panel (Sitrep)
- Aggregate data feeds predictor baselines
- Status: Phase 2 Q4 2026

### Critical Open Questions
1. **API surface:** Which of {position read, kill/chat hooks, mute, teleport, makeGM, outbound HTTP/WebSocket, file IO} are reachable from a **server-only** mod?
2. **Server-only assumption:** Can Sentinel Link be host-only (clients don't download it)?
3. **Stable player identity:** How to track players across reconnects?
4. **Transport:** NDJSON file bridge, local socket, or HTTP — what does the sandbox permit?

→ **Next action:** Time-box a spike proving Phase 1 telemetry + a single command round-trip end-to-end.

---

**Started:** March 2026 (Aaron began learning to code)  
**Phase 1A Shipped:** June 29, 2026  
**Next:** Gather user feedback, plan Phase 1B (multi-server + Sentinel Link spike)
