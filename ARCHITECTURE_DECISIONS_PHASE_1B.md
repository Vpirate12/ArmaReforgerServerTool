# Phase 1B Architecture Decision Records (ADRs)

**Date Locked:** 2026-06-30  
**Status:** Final (unblocks implementer weeks 1-3)

---

## ADR-001: RCON Passwords Storage — Local DPAPI/Keyring

**Decision:** Store RCON credentials locally on the Agent using **Windows DPAPI** (Data Protection API) or OS Keyring. Do NOT store plaintext or encrypted RCON passwords in the Railway PostgreSQL cloud database.

**Rationale:**
- **Blast radius:** If Railway DB is compromised, every customer's physical game server is compromised (attackers get RCON credentials). 
- **Local storage:** DPAPI is machine-bound (decrypt only on that host). Blast radius of a cloud breach = zero.
- **Cloud DB contains:** Server IP, port metadata, connection status only. No credentials.
- **Implementation:** Agent stores RCON password in Windows Credential Manager or Tauri's secure store (platform-abstracted).

**Security benefit:** Aligns with "Local-First, Security by Default" philosophy. The game host's RCON credentials never leave the host.

---

## ADR-002: Agent Distribution — Headless Windows Service

**Decision:** The Local Agent ships as a **separate Windows Service / Console Daemon install**, NOT bundled as a sidecar inside the Tauri MSI.

**Rationale:**
- **Game hosts are headless:** VPS / Dedicated servers run 24/7, no desktop. Administrators don't want Tauri running on production servers.
- **Deployment model:**
  - **Agent:** Installed once on each game host (e.g., `C:\Program Files\Sentinel Agent\`), runs as Windows Service
  - **Client:** Tauri MSI installed on admin's workstation (Windows, Mac, Linux)
- **Communication:** Client talks to Agent via `http://game-host-ip:5001/api` (local trusted network, no HTTPS needed)
- **Lifecycle:** Agent runs independently; client can be closed without affecting server operations.

**Architecture benefit:** Lightweight production footprint on game hosts. Heavy UI/state management stays on admin workstations.

---

## ADR-003: Multi-Tenancy Scope — Single-Org MVP with Stubbed `tenant_id`

**Decision:** Phase 1B implements a **single-organization** (logical single-tenancy). Database schema includes `tenant_id` UUID columns on all tenant-scoped tables, but application logic assumes one org.

**Rationale:**
- **Cost/risk:** Building fully dynamic multi-tenant isolation (per-org billing, cross-org data fences, org invites) in weeks 1-3 is feature creep.
- **Database prep:** Adding `tenant_id` now costs nothing; removing it later is a painful migration. Better to over-provision the schema than under-provision.
- **Phase 2 readiness:** By Phase 1B end, adding multi-org support is a business-logic change, not a database refactor.
- **Implementation:** All queries filter by a single hardcoded `tenant_id` or the logged-in user's org (assume 1:1 for MVP).

**Operational benefit:** Database scales to multi-tenancy without emergency migrations in Phase 2.

---

## ADR-004: Configuration Migration — Clean DB Start + Client-Side Import Form

**Decision:** Phase 1B starts with a **clean PostgreSQL schema**. Phase 1A users do NOT have an automated database-level migration. Instead, the React client provides an "Import from Phase 1A" utility that reads their old `state.json` file and pre-fills the new config creation form.

**Rationale:**
- **Complexity:** Mapping flat Phase 1A JSON → normalized Phase 1B Postgres schema inside EF Core migrations is a distraction in week 1.
- **User flow:** 
  1. User opens Sentinel Desktop client
  2. Sees "Import Phase 1A Config" button on first launch
  3. Selects old `state.json` file
  4. Form fields pre-populate (server name, modlist, etc.)
  5. User confirms, creates new server record in DB
- **Benefit:** Simple, auditable, user-controlled. Old data doesn't corrupt new schema.

**Operational benefit:** Reduces week 1-3 risk surface. Users own the migration path.

---

## Architecture Summary

**Topology:**
```
Admin Workstation
├── Tauri Client (React)
│   ├── connects to Railway Cloud API (auth, audit, registry)
│   └── connects to Local Agent API (RCON, validation)
│
Game Host (VPS / Dedicated Server)
└── Sentinel Agent (Windows Service)
    ├── manages RCON (local, DPAPI password)
    ├── manages Arma process + headless clients
    ├── runs mod validation, log tailing
    └── exposes REST API (port 5001) to Tauri client
```

**Security Boundary:**
- Agent ↔ Client: local network, no HTTPS, DPAPI passwords stay local
- Client ↔ Railway: HTTPS, JWT, audit logging, no server secrets

**Scope:**
- Single organization, stubbed for Phase 2 multi-tenancy
- Clean database, client-side import for legacy configs
- Vertical slice proof-of-concept: login → list servers → check RCON status → audit log

---

**Status:** ✅ LOCKED  
**Unblocks:** Implementer Agent (weeks 1-3)  
**Next Step:** Reconcile source trees, hand off to implementer
