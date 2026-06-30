# Phase 1B Implementation Checklist: Week-by-Week

**Timeline:** 10-12 weeks (Q3 2026)  
**Goal:** 16 features + 5 smoothing solutions, ship-ready  
**Architecture:** Tauri/React frontend + .NET 8 Web API + SQLite backend

---

## FOUNDATION (Weeks 1-3)

### Week 1: Architecture Setup & Base Infrastructure

**Goal:** Establish the Tauri/React + .NET 8 foundation. No features yet — just the skeleton.

- [ ] **Create Tauri project structure**
  - [ ] Initialize Tauri with React template
  - [ ] Configure webpack/Vite for Tauri
  - [ ] Set up hot reload (Tauri dev server)
  
- [ ] **Create .NET 8 Web API backend**
  - [ ] Initialize ASP.NET Core 8 project
  - [ ] Set up dependency injection (Autofac or built-in)
  - [ ] Configure CORS for localhost Tauri bridge
  - [ ] Add logging (Serilog)
  
- [ ] **SQLite Database Schema**
  - [ ] Create migrations (EF Core Code-First)
  - [ ] Schema: Users, Roles, Servers, AuditLog, PlayerProfiles
  - [ ] Set up connection pooling
  
- [ ] **Tauri-to-.NET Bridge**
  - [ ] HTTP client from React → .NET API (localhost:5000)
  - [ ] Token-based auth (JWT for future multi-user)
  - [ ] Error handling + logging
  
- [ ] **Basic Tauri Window & Menu**
  - [ ] Main window (1200x800 minimum)
  - [ ] Menu bar (File, Server, Help)
  - [ ] No features yet, just shell
  
- [ ] **Git checkpoint:** Commit "Phase 1B foundation: Tauri + .NET 8 + SQLite"

---

### Week 2: Database Layer & Core Services

**Goal:** Build the data persistence layer and core business logic.

- [ ] **User & Role Management Models**
  - [ ] User entity (username, password_hash, created_at, updated_at)
  - [ ] Role entity (name, permissions)
  - [ ] UserRole junction table
  - [ ] DbContext setup
  
- [ ] **Server Management Models**
  - [ ] Server entity (name, ip, port, rcon_password, created_by, last_status)
  - [ ] ServerStatus enum (online, offline, error, updating)
  - [ ] Migrations for new tables
  
- [ ] **Audit Log Models**
  - [ ] AuditLog entity (actor_id, action, target, timestamp, details)
  - [ ] AuditAction enum (ConfigPushed, BanApplied, RoleChanged, etc.)
  - [ ] Index on (actor_id, timestamp) for quick filtering
  
- [ ] **C# Service Layer**
  - [ ] `ServerService` — CRUD for servers
  - [ ] `RoleService` — CRUD for roles + permission checking
  - [ ] `AuditService` — Log all actions atomically
  - [ ] `ConfigService` — Load/validate/save server configs from disk
  
- [ ] **Web API Controllers (Minimal)**
  - [ ] `GET /api/servers` — List all connected servers
  - [ ] `POST /api/servers` — Add new server (test RCON connection)
  - [ ] `GET /api/audit?actor_id=X&start=Y&end=Z` — Query audit logs
  
- [ ] **React Context Setup**
  - [ ] AuthContext (login/logout, JWT token storage)
  - [ ] ServerContext (selected servers, connection status)
  - [ ] AuditContext (filter state for audit log)
  
- [ ] **Git checkpoint:** Commit "Database layer + service foundation"

---

### Week 3: Core UI Shell & Navigation

**Goal:** Build the main dashboard layout with navigation. No feature screens yet.

- [ ] **React Component Structure**
  - [ ] Layout component (sidebar, header, main content area)
  - [ ] Navigation menu (Servers, Config, Players, Audit, Help)
  - [ ] Error boundary + error toast notifications
  - [ ] Loading spinners + skeletons
  
- [ ] **Styling (Sitrep Sky Blue Theme)**
  - [ ] CSS/Tailwind setup with sky blue (#0ea5e9) as primary
  - [ ] Dark theme (charcoal bg, light text)
  - [ ] Responsive grid (mobile-friendly, though primary target is desktop)
  - [ ] Button styles (primary, secondary, danger, disabled states)
  
- [ ] **Authentication UI**
  - [ ] Login screen (username/password)
  - [ ] Session persistence (store JWT in localStorage or secure storage)
  - [ ] Logout button in header
  - [ ] Role-based menu visibility (hide restricted features)
  
- [ ] **Placeholder Screens (No Functionality Yet)**
  - [ ] Servers page (empty state with "Add Server" button)
  - [ ] Config page (empty state)
  - [ ] Players page (empty state)
  - [ ] Audit Log page (empty state, but with filter UI skeleton)
  
- [ ] **Onboarding Wizard Skeleton** (will complete in Week 4-5)
  - [ ] Step 1, 2, 3 components (not wired up yet)
  - [ ] Navigation between steps
  
- [ ] **Git checkpoint:** Commit "Core UI shell + navigation"

---

## FEATURE DEVELOPMENT (Weeks 4-5)

### Week 4: Multi-Server Dashboard & Config Management

**Goal:** Implement the core multi-server dashboard and config editor.

- [ ] **Multi-Server Dashboard Feature**
  - [ ] `ServerListView` component (table of servers with status)
  - [ ] Real-time server status polling (every 10 sec via RCON)
  - [ ] `GET /api/servers/{id}/status` endpoint (player count, tick rate, crashes)
  - [ ] Status indicator (green = online, red = offline, yellow = warning)
  - [ ] "Add Server" modal (IP, port, RCON password validation)
  - [ ] Server detail view (click → see more info)
  
- [ ] **Scenario Rotation Manager**
  - [ ] `ScenarioRotationView` component (drag-drop playlist editor)
  - [ ] Editable fields: scenario name, duration (minutes)
  - [ ] Shuffle toggle, loop toggle
  - [ ] "Save Rotation" button (POST to /api/servers/{id}/rotation)
  - [ ] Test rotation logic (C# ProcessManager integration)
  
- [ ] **Scenario Warnings UI**
  - [ ] Template editor (RCON `#say` template with tokens: {next}, {minutes}, {players})
  - [ ] Lead time picker (T-10, T-5, T-1 minute broadcasts)
  - [ ] Preview button (show what broadcast will look like)
  - [ ] Save button
  
- [ ] **Visual JSON Config Editor**
  - [ ] Forms-based editor for `config.json` keys
  - [ ] Fields: server name, mission, mod list (array), player count limits
  - [ ] JSON validation before save (C# validator)
  - [ ] "Diff from saved" view (highlight changes)
  - [ ] Save/discard buttons
  
- [ ] **RCON Integration Core**
  - [ ] `RCONClient` C# class (connection pooling, command queueing)
  - [ ] Commands: `#listPlayers`, `#dump`, `#scenario X`, `#say`, `#restart`
  - [ ] Error handling (connection timeouts, command timeouts)
  - [ ] Logging (all RCON commands + responses)
  
- [ ] **Config Management Endpoints**
  - [ ] `GET /api/servers/{id}/config` — Load current config
  - [ ] `POST /api/servers/{id}/config/validate` — Validate before push
  - [ ] `POST /api/servers/{id}/config/push` — Push config + trigger validation
  - [ ] Response: success/failure + list of changed keys
  
- [ ] **Audit Logging Integration**
  - [ ] Log all config pushes (who, what, when, server, diff)
  - [ ] Log all RCON commands (who, which server, command, result)
  - [ ] Ensure AuditService.LogAction() is called atomically
  
- [ ] **Git checkpoint:** Commit "Multi-server dashboard + config editor + RCON core"

---

### Week 5: User Management, Audit UI, and Onboarding Wizard

**Goal:** Implement RBAC, audit log UI, and onboarding.

- [ ] **User/Role Management UI**
  - [ ] Users page: list existing users, create/edit/delete (admin only)
  - [ ] Roles page: CRUD roles, assign permissions (admin only)
  - [ ] Permissions checkboxes: CanPushConfig, CanBanPlayers, CanViewAuditLog, CanManageUsers
  - [ ] User detail modal: edit name, role assignments, last login
  
- [ ] **Audit Log UI**
  - [ ] AuditLogView component (table with columns: Actor, Action, Target, Timestamp, Details)
  - [ ] Filters: Actor dropdown, Action type dropdown, Date range picker
  - [ ] Search box (free text on Details column)
  - [ ] Export button (CSV of filtered results)
  - [ ] Sorting (by timestamp descending by default)
  
- [ ] **Onboarding Wizard (Complete)**
  - [ ] Step 1: RCON Connection (IP, port, password, test button)
  - [ ] Step 2: Default Roles (select which roles to create)
  - [ ] Step 3: Log Paths (browse for server log location)
  - [ ] Completion: Confirm, save config, redirect to dashboard
  - [ ] Test with first-run experience
  
- [ ] **Player Whitelist/Blacklist**
  - [ ] PlayersView component (two tabs: Whitelist, Blacklist)
  - [ ] Table: SteamID, Name, Added By, Added On, Remove button
  - [ ] Add modal: SteamID input (with validation), optional comment
  - [ ] **Bulk SteamID Paste:** Large textarea, paste multiple (comma/newline separated), bulk add
  - [ ] Sync across all selected servers (checkbox list)
  
- [ ] **Performance Monitoring UI**
  - [ ] Dashboard widget: Player count (current / max), Uptime, Last status check
  - [ ] Status card: Server name, status indicator, ping time, crash count (last 7 days)
  - [ ] Drill-down: Click server → crash history, uptime graph (7-day)
  - [ ] Auto-refresh every 10 seconds
  
- [ ] **HTTP Endpoints for Week 5 Features**
  - [ ] `GET /api/users` — List users
  - [ ] `POST /api/users` — Create user
  - [ ] `PUT /api/users/{id}` — Update user
  - [ ] `DELETE /api/users/{id}` — Delete user
  - [ ] `GET /api/roles` — List roles
  - [ ] `POST /api/servers/{id}/players/whitelist` — Add to whitelist (bulk)
  - [ ] `DELETE /api/servers/{id}/players/whitelist/{steamid}` — Remove
  
- [ ] **Scheduled Restarts**
  - [ ] ScheduleView component (calendar + time picker)
  - [ ] Create/edit/delete schedule entries
  - [ ] BackgroundService in .NET: monitor scheduled times, send RCON #restart at T-5 min with warning broadcast
  
- [ ] **Crash Log Summary Widget (Quick-Win Smoothing)**
  - [ ] Dashboard widget: "Last Server Error"
  - [ ] Reads last error from server log (via file watcher or polling)
  - [ ] Displays error message + timestamp + "Restart" button
  - [ ] Integrates with crash detection logic
  
- [ ] **Git checkpoint:** Commit "User/role management + audit UI + onboarding wizard + crash log widget"

---

## TELEMETRY & LIVE OPS (Weeks 5-6 SPIKE + Weeks 7-8 Integration)

### Week 5-6: Sentinel Link Phase 1 Telemetry Spike ⚠️ CRITICAL

**Goal:** Prove that Enforce scripting can stream telemetry without degrading tick rate.

**Success Criteria:**
- Console app running in Sentinel Desktop receives real player coordinates every 1-2 seconds
- No server tick rate degradation (FPS stays within 1 frame of baseline)
- RCON command round-trip works (push whisper, verify delivery in-game)
- Local HTTP POST loop established

- [ ] **Enforce Mod Scaffolding**
  - [ ] Create basic Enforce server mod project structure
  - [ ] Initialize Enforce scripting templates (PlayerManager, EventSystem hooks)
  - [ ] Verify compilation (Workbench tools)
  
- [ ] **Telemetry Collection Script (Enforce)**
  - [ ] Hook `OnPlayerConnected` and `OnPlayerDisconnected` events
  - [ ] Collect player data: SteamID, name, position (world transform)
  - [ ] Serialize to JSON: `{ "t": "snapshot", "ts": <unix>, "players": [ { "pid": "...", "name": "...", "pos": [...] } ] }`
  - [ ] Test position accuracy (compare to in-game visual)
  
- [ ] **Kill Event Tracking (Enforce)**
  - [ ] Hook damage system to capture kill events
  - [ ] Serialize: `{ "t": "event", "ts": <unix>, "kind": "kill", "actor": "...", "victim": "...", "weapon": "..." }`
  - [ ] Test with controlled PvP scenario
  
- [ ] **Chat Hooking (Enforce)**
  - [ ] Hook chat system to capture global/side chat
  - [ ] Serialize: `{ "t": "event", "ts": <unix>, "kind": "chat", "from": "...", "channel": "global|side", "text": "..." }`
  - [ ] Test with test players chatting
  
- [ ] **Local HTTP Transport (Enforce)**
  - [ ] Write HTTP POST to localhost:5000/api/telemetry
  - [ ] Send telemetry snapshots every 2 seconds
  - [ ] Send events (kills, chat) immediately
  - [ ] Retry logic if API unreachable
  - [ ] Measure and log latency
  
- [ ] **.NET API Endpoint for Telemetry**
  - [ ] `POST /api/telemetry` — Accept telemetry payloads
  - [ ] Parse JSON, validate schema
  - [ ] Store snapshots in memory (rolling 10-minute buffer)
  - [ ] Broadcast to connected React clients via WebSocket (for live map)
  
- [ ] **Spike Testing & Validation**
  - [ ] Run mod on test server with 30 players
  - [ ] Monitor server tick rate (baseline vs. with mod)
  - [ ] Measure telemetry latency (game event → received by API)
  - [ ] Verify no server crashes, no unexpected lag
  - [ ] **Decision gate:** If spike fails here, drop Phase 1 dependent features (1B.15, 1B.16, 1B.17, 1B.18)
  
- [ ] **Git checkpoint:** Commit "Sentinel Link Phase 1 telemetry spike (proof of concept)"

---

### Week 7-8: Telemetry Integration & Live Ops UI

**Goal:** Integrate Phase 1 spike results into Phase 1B features. Build Live Ops map (if spike passed).

#### If Spike PASSED:

- [ ] **Telemetry WebSocket Bridge**
  - [ ] Tauri client connects to WebSocket: `ws://localhost:5000/ws/telemetry`
  - [ ] React receives live telemetry stream
  - [ ] Update state in real-time (player positions, events)
  
- [ ] **Live Kick Diagnostic Card (Quick-Win Smoothing)**
  - [ ] Listen to server logs for signature kick events
  - [ ] Parse log: extract kicked player name + mod name
  - [ ] Display card on dashboard: "Player X kicked for mod Y"
  - [ ] Copyable message for Discord: "Verify local files in Steam Workshop for mod Y"
  - [ ] Auto-remove card after 5 minutes or when resolved
  
- [ ] **Multi-Vector Identity Resolver**
  - [ ] Build local identity database (SteamID → IP → GUID mappings)
  - [ ] OnPlayerConnected: record (SteamID, name, IP, hardware ID if available)
  - [ ] Query interface: enter any identifier (SteamID/IP/GUID) → see all related identities
  - [ ] "Cascade Ban" button: apply ban to all related identities across all servers
  - [ ] Audit log all cascade bans
  
- [ ] **Live Admin Console Auditor**
  - [ ] Enforce mod logs all admin commands to HTTP endpoint
  - [ ] Dashboard widget: "Admin Actions (Last Hour)"
  - [ ] Table: Admin name, command, timestamp, target, result
  - [ ] Discord webhook integration (post to #admin-log channel)
  
- [ ] **Live Ops Map (9a Design) — OPTIONAL if time permits**
  - [ ] 2D canvas component (React, Pixi.js or Babylon.js)
  - [ ] Render top-down map with player positions (updated in real-time)
  - [ ] Color by faction (blue = BLUFOR, red = OPFOR)
  - [ ] Click player → show name, ping, role
  - [ ] Objective markers (capture flags, etc.)
  
- [ ] **Git checkpoint:** Commit "Telemetry integration + live ops features (Phase 1 complete)"

#### If Spike FAILED:

- [ ] **Fallback to RCON-Only Baseline**
  - [ ] Drop: Multi-Vector Identity Resolver, Live Admin Auditor, Live Ops Map
  - [ ] Keep: All Tier 0 & Tier 1 features (still professional-tier tool)
  - [ ] Document: "Live Ops features deferred to Phase 2 (Enfusion API constraints discovered)"
  - [ ] Plan Phase 2 re-architecture based on spike findings
  
- [ ] **Git checkpoint:** Commit "Phase 1B revised scope: RCON-only baseline (telemetry spike inconclusive)"

---

## QA & POLISH (Weeks 9-12)

### Week 9-10: Player Management & Final Smoothing

**Goal:** Complete player management features and final smoothing solutions.

- [ ] **Ban/Unban Confirmation Dialog (Quick-Win Smoothing)**
  - [ ] Modal before any ban/unban action
  - [ ] Confirms: Server(s) affected, player name, action (ban/unban)
  - [ ] Optional comment field (reason for audit log)
  - [ ] Requires explicit "Confirm" button
  
- [ ] **Bulk Whitelist SteamID Paste (Already in Week 5, but complete testing)**
  - [ ] Test: Paste 20+ SteamIDs at once
  - [ ] Test: Mixed formats (with/without commas, newlines)
  - [ ] Test: Validation (reject invalid SteamIDs)
  - [ ] Test: Sync across multiple servers
  
- [ ] **Enforce Standard Modlist Distribution**
  - [ ] Select modlist template (org standard)
  - [ ] Select target servers
  - [ ] Push button → validate on each, confirm on success
  - [ ] Parallel deployment (if Phase 1 supports; else sequential)
  - [ ] Audit log all pushes
  
- [ ] **Headless Client Manager**
  - [ ] UI to spawn HC processes (select main server, assign HC profile)
  - [ ] Monitor HC health (restart if crashed)
  - [ ] Keep HC modlist in sync with main server
  - [ ] CPU affinity binding (ensure HC doesn't starve main simulation)
  
- [ ] **Mod Kick Diagnostic (Enhanced)**
  - [ ] Real-time log listener (tail server log)
  - [ ] Regex detection: signature kick patterns
  - [ ] Extract mod name from error message
  - [ ] Post diagnostic card to dashboard
  - [ ] Auto-generate Discord message
  
- [ ] **Git checkpoint:** Commit "Player management + final smoothing solutions"

---

### Week 11: Integration Testing & QA Gates

**Goal:** Run full QA suite, catch bugs, prepare for release.

- [ ] **Functional Testing**
  - [ ] Test all 16 features end-to-end
  - [ ] Test all 5 smoothing solutions
  - [ ] Test error paths (invalid RCON, network timeouts, etc.)
  - [ ] Test role-based access (verify RBAC enforcement)
  
- [ ] **Performance Testing**
  - [ ] Load test RCON pooling (100+ concurrent queries)
  - [ ] Load test audit log queries (filter 10k entries)
  - [ ] Load test telemetry stream (1k events/sec)
  - [ ] Verify UI remains responsive
  
- [ ] **Security Testing**
  - [ ] Verify JWT token validation on all endpoints
  - [ ] Verify RCON password not logged/exposed
  - [ ] Verify SQL injection protection (parameterized queries)
  - [ ] Verify CORS restricted to localhost
  
- [ ] **Run QA Gates**
  - [ ] `/qa mechanical` — Linter, build, type checking
  - [ ] `/qa-admin` — Dependency audit, license compliance
  - [ ] `/qa-reviewer` — Code review against security/correctness standards
  - [ ] Fix any blocking issues
  
- [ ] **Regression Testing**
  - [ ] Test Phase 1A validator still works
  - [ ] Test config save/load (backward compatibility)
  - [ ] Test existing workflows aren't broken
  
- [ ] **Documentation**
  - [ ] User manual (features, walkthrough, FAQ)
  - [ ] Admin guide (roles, audit logging, backup)
  - [ ] API docs (Swagger/OpenAPI)
  - [ ] Release notes (what's new in 1B)
  
- [ ] **Git checkpoint:** Commit "QA complete, ready for release"

---

### Week 12: Release Prep & Deployment

**Goal:** Build installer, package, deploy.

- [ ] **Release Build**
  - [ ] Build Tauri app (production mode, optimized)
  - [ ] Build .NET backend (publish self-contained, framework-dependent optional)
  - [ ] Verify all assets bundled correctly
  
- [ ] **Installer Creation**
  - [ ] NSIS or WiX for Windows installer (.exe)
  - [ ] Desktop shortcut, uninstaller
  - [ ] Registry entries (if needed)
  
- [ ] **Documentation for End Users**
  - [ ] Quick start guide (install, first-run wizard, add first server)
  - [ ] Troubleshooting guide (common errors, RCON issues)
  - [ ] FAQ (how to reset admin password, restore from backup)
  
- [ ] **Deployment**
  - [ ] Push final commits to GitHub
  - [ ] Create GitHub release (tag v1.0.0 or v1B.0.0)
  - [ ] Upload installer artifacts
  - [ ] Update README with download links
  
- [ ] **Post-Launch**
  - [ ] Monitor for crash reports (integrate Sentry or similar)
  - [ ] Create issue templates for user feedback
  - [ ] Plan Phase 1B.1 (deferred smoothing solutions)
  
- [ ] **Git checkpoint:** Commit "Phase 1B release v1.0.0 — Ready for users"

---

## Checkpoints & Decision Gates

### Week 6 (End of Spike)
**Gate:** Sentinel Link Phase 1 telemetry feasibility
- **PASS:** Proceed with telemetry integration (Week 7-8)
- **FAIL:** Pivot to RCON-only baseline, defer telemetry features to Phase 2

### Week 8 (Telemetry Integration Complete)
**Gate:** All Tier 0 & Tier 1 features + Phase 1 results working
- **PASS:** Proceed to QA (Week 11)
- **FAIL:** Extend feature completion, defer QA start

### Week 11 (QA Complete)
**Gate:** All tests pass, zero critical/blocking issues
- **PASS:** Proceed to release (Week 12)
- **FAIL:** Bug fix sprint, extend QA, delay release by 1 week

---

## Success Metrics

By end of Week 12:

- ✅ 16 features fully integrated and tested
- ✅ 5 smoothing solutions shipped
- ✅ Zero critical security issues
- ✅ All QA gates passing
- ✅ Installer working on clean Windows machines
- ✅ User documentation complete
- ✅ Pushed to GitHub, ready for distribution

---

## Notes

- **AI-Assisted Development:** Use Claude Code for implementation, code review, testing. This accelerates feature delivery.
- **Session Protocol:** Always push to GitHub at end of each session (hotspot reliability).
- **Risk Management:** If Sentinel Link spike fails (Week 6), ship RCON-only baseline — it's still a professional-tier tool worth $249/yr.
- **Scope Lock:** No new features added during Phase 1B. Smoothing solutions are integrated, not added.

---

**Status:** 🔒 LOCKED & READY FOR EXECUTION  
**Start Date:** July 2026  
**Target Ship Date:** September 2026 (Q3)
