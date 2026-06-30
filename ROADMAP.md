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

## Phase 1B: Org Admin (Professional)

**Target:** Gaming orgs, clans, small hosting providers (1–50 servers)

**Features:**
- Multi-server dashboard (manage 50+ servers from one UI)
- Enforce standard modlist across all servers
- Push config updates to all servers at once
- User/role management (who can change mods)
- **Audit logging** (who changed what, when)
- Mod version tracking per server
- Performance monitoring (player count, crashes, health)
- Scheduled restarts/updates
- In-game admin panel (kick, ban, chat)
- Player whitelist/blacklist management
- Server-specific override configs

**Architecture shift:** Web-based UI (React/TypeScript) + .NET backend

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
- **Tournament mode** (lock configs during matches)
- **Rapid config switching** (<30 seconds between matches)
- **Live admin controls** (pause, restart, kick from live panel)
- **Spectator management** (observer-only accounts)
- **Match recording** (auto-record for review/broadcast)
- **Statistics tracking** (kills, objectives, player performance)
- **Cheat detection** (flag unauthorized mod use)
- **Leaderboards/rankings** (track competitive results)
- **Integrated RCON** (full server control)
- **Live bracket management** (auto-update tournament bracket)
- **Player stats export** (CSV/JSON for commentators)

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
| In-game admin | ❌ | ✅ | ✅ |
| Tournament mode | ❌ | ❌ | ✅ |
| Live controls | ❌ | ❌ | ✅ |
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

**Started:** March 2026 (Aaron began learning to code)  
**Phase 1A Shipped:** June 29, 2026  
**Next:** Gather user feedback, plan Phase 1B
