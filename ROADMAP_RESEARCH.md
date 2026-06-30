# Roadmap Research & Validation (AI Studio Analysis)

**Generated:** 2026-06-30  
**Status:** Phase 1B & Phase 2 features validated against Enfusion API feasibility, feature creep, ROI, and admin pain points.

---

## Document Index

This file archives the comprehensive AI Studio research that informed Phase 1B/2 scope decisions:

1. **Feature Validation Analysis** — Feasibility assessment of all 39 features (Phase 1A shipped, Phase 1B planned, Phase 2 planned)
2. **Pain Point Smoothing Research** — Identification of 20 operational friction points and 15 low-effort UX improvements
3. **Decision Summary** — Final scope, timeline, and phase-gate commitments

---

## Part 1: Feature Validation & Prioritization Matrix

### Enfusion API Feasibility Framework

Features are categorized by their integration point with the Arma Reforger Enfusion engine:

| Tier | Integration Point | Examples | Feasibility |
|------|-------------------|----------|-------------|
| **Tier 0** | App-Only (local file system, OS process space) | Config editor, workshop cache, HC manager | HIGH (no engine contact) |
| **Tier 1** | RCON-Only (standard RCON commands) | Multi-server dashboard, scheduled restarts, rotation | HIGH (proven) |
| **Tier 2** | Read Telemetry (Sentinel Link Enforce hooks) | Live map, identity resolver, stats tracking | MEDIUM (requires Phase 1 spike) |
| **Tier 3** | Remote Actions (Sentinel Link mutations) | Whisper, warn, mute, teleport, makeGM | MEDIUM-LOW (requires Phase 2-3 spike) |

### Phase 1B Final Scope: 16 MVP Features

**Approved for Phase 1B (locked):**

| # | Feature | Phase | Feasibility | Effort | ROI Rank | Status |
|---|---------|-------|-------------|--------|----------|--------|
| 1 | Mod Validator (Phase 1A) | 1A | HIGH | Shipped | 1 | ✅ SHIPPED |
| 2 | Auto-Fix (Phase 1A) | 1A | HIGH | Shipped | 2 | ✅ SHIPPED |
| 3 | Visual JSON Config Editor | 1B | HIGH | Easy | 3 | ✅ INTEGRATE |
| 4 | Workshop Download Shield | 1B | HIGH | Medium | 4 | ✅ INTEGRATE |
| 5 | Start Button Gating (Phase 1A) | 1A | HIGH | Shipped | 5 | ✅ SHIPPED |
| 6 | Multi-Server Dashboard | 1B | HIGH | Medium | 6 | ✅ INTEGRATE |
| 7 | Scenario Rotation Manager | 1B | HIGH | Easy | 7 | ✅ INTEGRATE |
| 8 | Sentinel Link Phase 0 (RCON fallback) | 1B | HIGH | Easy | 8 | ✅ INTEGRATE |
| 9 | Player Whitelist/Blacklist | 1B | HIGH | Easy | 9 | ✅ INTEGRATE |
| 10 | Audit Logging | 1B | HIGH | Easy | 10 | ✅ INTEGRATE |
| 11 | User/Role Management (RBAC) | 1B | HIGH | Easy | 11 | ✅ INTEGRATE |
| 12 | Headless Client (HC) Manager | 1B | HIGH | Medium | 12 | ✅ INTEGRATE |
| 13 | Sentinel Link Phase 1 (Telemetry Spike) | 1B | MEDIUM | Hard | 13 | ⚠️ CONDITIONAL (Spike Weeks 5-6) |
| 14 | Multi-Vector Identity Resolver | 1B | HIGH | Medium | 14 | ✅ INTEGRATE (post-spike) |
| 15 | Enforce Standard Modlist | 1B | HIGH | Medium | 15 | ✅ INTEGRATE |
| 16 | Mod Kick Diagnostic | 1B | HIGH | Easy | 25 | ✅ INTEGRATE |

**Deferred from Phase 1B (Phase 1B.1 or Phase 2):**

| # | Feature | Phase | Reason | Status |
|---|---------|-------|--------|--------|
| 20 | Crash Dump Analyzer | 1B.1 | Low ROI, high effort (binary `.dmp` parsing) | ⏸️ DEFER |
| 15 | Live Ops Map | 2 | Complex UI rendering, move to esports tier | ⏸️ DEFER |
| 16 | Admin Call Panel | 1B.1 | Depends on Phase 2 Link, complex incident UI | ⏸️ DEFER |
| 9 | Server Override Configs | 1B.1 | Merge conflict complexity, not MVP | ⏸️ DEFER |
| 2.4 | Sentinel Link Phase 3 (Teleport/MakeGM) | 3 | HIGH desync risk, esports-only | ⏸️ DEFER |
| 2.9 | Performance Prediction ML | 2.1 | Requires historical data, not available yet | ⏸️ DEFER |

### Critical Dependencies

**The Sentinel Link Phase 1 Spike is the phase-blocker for Phase 1B:**
- Must complete by end of Week 6 (Weeks 5-6 spike window)
- Unlocks: Identity Resolver, Command Auditor, stats tracking, spatial visualizer
- If spike fails: Drop dependent features, ship RCON-only baseline (still valuable)

**Architecture migration is the foundation:**
- Weeks 1-3: Tauri/React + .NET Web API base must be rock-solid before adding features
- No new WinForms work after Week 1

---

## Part 2: Pain Point Smoothing Research

### Top 20 Admin Pain Points (Ranked by Impact)

| Rank | Pain Point | Category | Frequency | Impact | Time Cost | Emotional Cost |
|------|-----------|----------|-----------|--------|-----------|-----------------|
| 1 | No preview before pushing config | Config | High | High | 5 min | Anxiety |
| 2 | No config version history/rollback | Config | Medium | High | 30 min | Disaster |
| 3 | Server crashes, manual log hunt | Crash | High | High | 20 min | Frustrating |
| 4 | Banning player across 50 servers manually | Player Mgmt | High | High | 30 min | Tedious |
| 5 | Pushing config to 50 servers sequentially | Multi-Server | High | High | 60 min | Slow |
| 6 | Mod signature kick waves (diagnosis) | Crash | High | High | 15 min | Frustrating |
| 7 | No wizard for initial server setup | Onboarding | High (1st run) | Medium | 10 min | Frustrating |
| 8 | No bulk import for multiple servers | Onboarding | High (orgs) | High | 60 min | Tedious |
| 9 | Unbanning player accidentally | Player Mgmt | Low | High | 5 min | Stressful |
| 10 | Whitelisting 20-player clan manually | Player Mgmt | Medium | Medium | 20 min | Tedious |
| 11 | No diff view for modlist changes | Config | High | Medium | 15 min | Tedious |
| 12 | Raw telemetry is noise, not insights | Telemetry | High | Medium | 15 min | Overwhelming |
| 13 | Manual search "who deleted config?" | Audit | Medium | High | 10 min | Frustrating |
| 14 | Finding specific event in 3-hr match | Telemetry | High | Medium | 30 min | Time-consuming |
| 15 | Player connection issues: no diagnostic | Crash | Medium | Medium | 10 min | Confusing |
| 16 | Manual Discord webhook token entry | Onboarding | Medium | Low | 5 min | Annoying |
| 17 | Manual copy of custom overrides | Config | Medium | Medium | 10 min | Error-prone |
| 18 | Coordinating scheduled maintenance | Multi-Server | Medium | Medium | 15 min | Confusing |
| 19 | Proving ban reason to player | Audit | Medium | Medium | 10 min | Tedious |
| 20 | Manual tournament bracket setup | Esports | Medium | Medium | 30 min | Error-prone |

### 5 Phase 1B Quick-Win Smoothing Solutions (Locked)

These ship with Phase 1B MVP (Weeks 1-12):

1. ✅ **Onboarding Wizard** (3-step setup: RCON, roles, log paths)
   - Type: Progressive Disclosure, Templates
   - Effort: Low (React components, C# config defaults)
   - Impact: High (removes first-run friction)
   - Week: Integrate during Week 4-5

2. ✅ **Crash Log Summary Widget** (dashboard auto-surfaces last error)
   - Type: Context, Visibility
   - Effort: Low (React component, C# log parsing)
   - Impact: High (immediate value, most frequent pain point)
   - Week: Integrate during Week 7-8

3. ✅ **Live Kick Diagnostic Card** (real-time mod signature kick alerts)
   - Type: Visibility, Context, Automation
   - Effort: Low (React component, C# log listener)
   - Impact: High (addresses #6 pain point, highest support ticket driver)
   - Week: Integrate during Week 7-8

4. ✅ **Ban/Unban Confirmation Dialog** (prevents accidental bans)
   - Type: Guardrail
   - Effort: Very Low (React modal)
   - Impact: High (prevents #9 disaster scenario)
   - Week: Integrate during Week 9-10

5. ✅ **Bulk SteamID Paste for Whitelist** (paste 20 SteamIDs at once)
   - Type: Shortcut
   - Effort: Very Low (React input logic)
   - Impact: High (addresses #10 tedium)
   - Week: Integrate during Week 9-10

### 8 Phase 1B.1 Deferred Smoothing (6-8 weeks post-launch)

1. Server CSV Import (bulk add servers)
2. Config Diff Preview (side-by-side before push)
3. Config Rollback (version history, one-click restore)
4. Modlist Comparator (highlight differences between servers)
5. Unified Ban/Unban Modal (apply across multiple servers at once)
6. Parallel Config Push (concurrent deployments)
7. Audit Log Filter/Search UI (rich filtering for compliance)
8. Event Timeline Filter & Jump (find events in telemetry)

---

## Part 3: Decision Summary

### Phase 1B Locked Scope

**16 Features + 5 Smoothing Solutions**

- **Engineering Estimate:** 10-12 weeks (solo developer, AI-assisted)
- **Critical Path:** Architecture migration (Weeks 1-3) → Sentinel Link Phase 1 spike (Weeks 5-6)
- **Ship Date:** Q3 2026 (end of September)
- **Risk:** Sentinel Link spike failure (contingency: drop dependent features, ship RCON baseline)

### Phase 1B.1 (Follow-up, 6-8 weeks post-launch)

- 8 deferred smoothing solutions
- Config rollback, config diff preview, parallel config push
- Esports-specific onboarding wizard (Phase 2 prep)

### Phase 2 (Q4 2026+)

- Tournament mode, leaderboards, stats tracking
- Spatial combat visualizer (esports broadcast)
- Sentinel Link Phase 3 (deep control — gated on feasibility spike)
- Performance prediction (ML baselines)

---

## Appendix: Esports Onboarding Wizard (Phase 2)

**5-step flow for tournament directors (~5 minutes):**

1. **Tournament Details:** Name, dates, game (pre-selected), optionally load from previous template
2. **Server Allocation:** Select 2-4 servers, enable "Tournament Mode" (config locking)
3. **Modlist & Config:** Select approved tournament modlist, push with diff preview, validate
4. **Roster & Spectators:** Bulk upload player SteamIDs from CSV, assign spectator slots
5. **Broadcast Setup:** Display NDI stream URL, test OBS connectivity

---

**Research completed:** 2026-06-30  
**Status:** LOCKED for Phase 1B implementation  
**Next step:** Execute implementation checklist (Week-by-week plan)
