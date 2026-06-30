# Longbow Phase 1B+ — Private Repository

**Repository:** https://github.com/Vpirate12/Sentinel-Desktop (Private)  
**Status:** Setting up Phase 1B development  
**Date:** 2026-06-30

---

## Repository Strategy

**Two-repo split (as of 2026-06-30):**

| Repo | Visibility | Content | Use |
|------|-----------|---------|-----|
| **ArmaReforgerServerTool** | Public | Phase 1A MVP only (validator + auto-fix) | Open source, community use |
| **Sentinel-Desktop** | Private | All phases (1A + 1B + 2), strategic docs, internal work | Core team development |

**Why?**
- Public: Showcase the free tier (validator) to users/community
- Private: Protect Phase 1B+ strategy, monetization tiers, Sentinel Link concept

---

## What's in This Private Repo

**All Phase 1A code** (copied from public repo on 2026-06-30):
- Mod validator with Steam Workshop integration
- Auto-fix logic
- UI (Check Mods button, Start button gating)
- Tests (13 unit tests)
- Dark theme + Sitrep branding

**Plus — Strategic Docs (removed from public):**
- `ROADMAP.md` — Phase 1A → 1B → 2 product roadmap
- `DEVOPS_WORKFLOW.md` — Subagent coordination (architect → implementer → reviewer → auditor → tester → QA → docs → deploy)
- `.claude/agents/*.md` — Internal subagent definitions
- `.claude/hooks/*.sh` — Internal automation
- `PRODUCT_POLICY.md` — Vision, philosophy, monetization tiers
- Sentinel Link technical brief (Phase 1B/2 feature)

---

## Next Steps for Phase 1B

### 1. Analyze Monetization Tiers
**Aaron's task:** Decide which Phase 1A features are actually free vs. premium.

Current assumption:
- **Free:** Basic validator (no auto-fix)
- **Premium:** Validator + auto-fix + save/load + crash recovery
- **Enterprise:** Phase 1B (multi-server, audit logging, user/role management)

→ Review PRODUCT_POLICY.md and finalize tier structure.

### 2. Sentinel Link Spike
**Biggest unknown:** What can the Enfusion scripting API actually do?

See ROADMAP.md "Sentinel Link: Server Mod Roadmap" for 4 open questions:
1. Position read, kill/chat hooks, mute, teleport, makeGM, HTTP/WebSocket, file IO — which are available?
2. Server-only mod assumption — will clients need to download it?
3. Stable player identity across reconnects?
4. Which transport mechanism works (NDJSON file, local socket, HTTP)?

→ Time-box a spike proving Phase 1 telemetry + command round-trip end-to-end.

### 3. Architecture Shift Planning
Phase 1B requires:
- Web-based UI (React/TypeScript) — currently WinForms
- .NET backend (API for multi-server)
- Database (audit logging, user management)

→ Design session with architect agent.

---

## Session Protocol (Updated)

**This private repo is the PRIMARY development repo going forward.**

```
At session START:
  1. Read HANDOFF_PRIVATE.md (this file) — understand current state
  2. Read ROADMAP.md — know what's next
  3. Pull latest from https://github.com/Vpirate12/Sentinel-Desktop

At session END:
  1. Commit all changes
  2. Push to private repo: git push private main
  3. (Optionally) sync Phase 1A-only changes back to public repo
  4. Update this HANDOFF_PRIVATE.md with next steps
  5. Disconnect only after push succeeds
```

**Memory:** Updated to point to `Sentinel-Desktop` private repo.

---

## File Structure

```
Sentinel-Desktop/
├── src/                           # Phase 1A code (from public repo)
├── Longbow.Tests/                 # Unit tests
├── bin/Release/net8.0-windows/    # Built exe
├── ROADMAP.md                     # Product roadmap (Phase 1A → 1B → 2)
├── DEVOPS_WORKFLOW.md             # Subagent coordination
├── PRODUCT_POLICY.md              # Vision, philosophy, monetization
├── HANDOFF_PRIVATE.md             # This file (session handoff)
├── .claude/
│   ├── agents/                    # Internal subagent defs (architect, implementer, etc.)
│   ├── hooks/                     # Internal automation
│   └── settings.json              # Project settings
├── STG_COMPANION_MOD.md           # Sentinel Link technical brief (Phase 1B concept)
└── docs/
    └── design_handoff_scenario_rotation/  # Phase 1B.1 UI designs
```

---

## What Happened on 2026-06-30

1. Created private repo `Sentinel-Desktop`
2. Pushed all Phase 1A code + strategic docs
3. Cleaned public repo (removed ROADMAP, DEVOPS_WORKFLOW, internal .claude/)
4. Updated public README to Phase 1A MVP only
5. Repo split complete ✅

---

## Team Access

**Private repo:** https://github.com/Vpirate12/Sentinel-Desktop  
**Public repo:** https://github.com/Vpirate12/ArmaReforgerServerTool (Phase 1A only)

Only core team has access to private repo.

---

## Questions for Aaron

1. **Monetization:** Which Phase 1A features are free? Which premium?
2. **Sentinel Link spike:** When can this happen? (Needs Enfusion API testing)
3. **Phase 1B architecture:** Web-based (React + .NET backend) or keep WinForms?
4. **Timeline:** When does Phase 1B start? After monetization analysis?

---

**Private repo established:** 2026-06-30  
**All work is now safe in GitHub** (hotspot reliability achieved)

Next session: Start with ROADMAP.md → decide monetization → plan Phase 1B.
