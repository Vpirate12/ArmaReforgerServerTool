# Sentinel Desktop: Open-Core Business Model & Implementation

**Date:** 2026-06-30  
**Status:** Locked business model for Phase 1B+

---

## Strategy Overview

Sentinel Desktop uses an **Open-Core** business model (proven by GitLab, PostHog, HashiCorp, Plausible):

- **Public Repository:** Phase 1A validator (free, open-source, GPL-3.0)
- **Private Repository:** Phase 1B/2 proprietary code (development only, never shipped as source)
- **Paywall Enforcement:** Distribution level (compiled binaries), not source code level
- **License Gating:** Lightweight local-first validation with offline grace period

---

## Repository Architecture

```
┌─────────────────────────────────────────────────┐
│ ArmaReforgerServerTool (Public)                 │
│ • Phase 1A Validator code (GPL-3.0)             │
│ • README, ROADMAP, contributing guidelines     │
│ • Installer for free tier (no license needed)  │
│ • Community issues & PRs welcome                │
└─────────────────┬───────────────────────────────┘
                  │ (upstream remote: "public")
                  │ (Merge Phase 1A bug fixes)
                  ▼
┌─────────────────────────────────────────────────┐
│ Sentinel-Desktop (Private)                      │
│ • All of Phase 1A (copied from public)         │
│ • Phase 1B proprietary code (CLOSED SOURCE)    │
│ • Phase 2 proprietary code (CLOSED SOURCE)     │
│ • CI/CD pipeline (GitHub Actions)              │
│ • License key validation logic                 │
│ • Team members + bot access only               │
│ • Development branch only (never shipped)      │
└─────────────────────────────────────────────────┘
                  │
                  │ (CI/CD builds signed binaries)
                  ▼
┌─────────────────────────────────────────────────┐
│ Distribution (Stripe / Tebex / GitHub Releases)│
│ • Free installer: Phase 1A only (signed .exe) │
│ • Premium installer: Phase 1A + 1B (key gate) │
│ • Enterprise installer: Phase 1A + 1B + 1B.1  │
│ • Esports installer: All phases (key gate)    │
└─────────────────────────────────────────────────┘
```

---

## Phase 1A Distribution (Free Tier)

**What's Free:**
- Mod Validator (4-pass algorithm, dependency detection)
- Auto-fix (Kahn's topological sort, dependency resolution)
- Check Mods button, progress feedback
- Config save/load
- Steam Workshop integration (real mod metadata)

**License:** GPL-3.0 (forces competitors to open-source any derivative work)

**Distribution:** GitHub Releases or direct download from website (no license key required)

**Code Location:**
- Public: `/ArmaReforgerServerTool` (source code, fully auditable)
- Private: `/Sentinel-Desktop/src/phase1a/*` (copy of public for integration testing)

---

## Phase 1B/2 Distribution (Paid Tiers)

**What's Premium:**
- Multi-server dashboard (Professional tier, $249/yr)
- Sentinel Link telemetry (Enterprise tier, custom)
- Tournament mode & esports features (Esports tier, custom)

**License:** Proprietary / Closed Source (never disclosed to customers)

**Distribution:** 
- Only compiled `.exe` / `.msi` binaries (no source code)
- Signed with Windows code-signing certificate (prevents tampering)
- Delivered via Stripe checkout → link to secure bucket

**Code Location:**
- Private only: `/Sentinel-Desktop/src/phase1b/*`, `/Sentinel-Desktop/src/phase2/*`
- Never committed to public repo
- Development branch only (git history remains private)

---

## Licensing & Paywall Enforcement

### License Key Validation Flow

**1. Purchase → License Key Generation**
```
User visits: https://store.spare-time-gaming.us/sentinel-desktop
Selects tier: "Professional ($249/yr)" 
Pays via Stripe
    ↓
Backend generates: STG-PROF-XXXX-XXXX-XXXX
Tied to: Discord/Steam ID
Expiration: +1 year
    ↓
User receives: License key + download link for signed installer
```

**2. First Launch → Validation**
```
User installs: Sentinel_Desktop_Professional_1.0.0.exe
First launch → Modal: "Enter license key"
User pastes: STG-PROF-XXXX-XXXX-XXXX
    ↓
Tauri app makes secure HTTPS POST:
POST https://api.spare-time-gaming.us/v1/licenses/validate
{
  "license_key": "STG-PROF-XXXX-XXXX-XXXX",
  "machine_id": "W10-SERIAL-ABC123DEF456",  // Windows hardware ID
  "app_version": "1.0.0"
}
    ↓
API response:
{
  "valid": true,
  "tier": "professional",
  "expires_at": "2027-06-30T23:59:59Z",
  "features": ["multi-server", "audit-logging", "user-roles"],
  "offline_grace_until": "2026-07-07T23:59:59Z"
}
```

**3. Feature Gating (Runtime)**
```csharp
// In Tauri/React app
public class LicenseGate {
  public static bool CanAccessFeature(string featureName, License license) {
    if (license.IsExpired && DateTime.UtcNow > license.OfflineGraceExpire) {
      ShowModal("License expired. Renew at spare-time-gaming.us");
      return false;
    }
    
    return license.Features.Contains(featureName);
  }
}

// Usage in React:
if (LicenseGate.CanAccessFeature("multi-server", license)) {
  return <MultiServerDashboard />;
} else {
  return <LockedFeatureUpsell tier="professional" />;
}
```

**4. Offline Grace Period (Crucial for Trust)**
```
If API unreachable:
  • Check local encrypted cache (validity)
  • If cache valid + within "offline_grace_until" timestamp
    → Allow all licensed features for 7 days
  • If cache expired
    → Show "License check failed. Try again when online."
    
This prevents fury when your API is down or user is offline.
```

### License Key Format & Expiration

**Key Format:**
```
STG-{TIER}-XXXX-XXXX-XXXX
├─ STG: Product identifier
├─ TIER: TRIAL | FREE | PROF | ENT | ESPORTS
└─ XXXX: Cryptographic checksum (prevent tampering)
```

**Tier Capabilities:**
| Tier | Price | Features | Offline Grace |
|------|-------|----------|----------------|
| FREE | Free | Phase 1A validator | ∞ (no expiration) |
| TRIAL | Free | Phase 1B preview (14 days) | 7 days |
| PROF | $249/yr | Phase 1B (multi-server, audit, roles) | 7 days |
| ENT | Custom | Phase 1B + Sentinel Link telemetry | 7 days |
| ESPORTS | Custom | All features (Phase 1A + 1B + 2) | 7 days |

---

## Code Organization: Keeping Proprietary Code Safe

### What's in Public Repo (`ArmaReforgerServerTool`)

```
ArmaReforgerServerTool/
├── src/
│   ├── Phase1A/
│   │   ├── ModValidationService.cs (4-pass algorithm)
│   │   ├── SteamWorkshopMetadataProvider.cs
│   │   ├── ValidationError.cs
│   │   └── ... (all Phase 1A code)
│   └── (NO Phase 1B or 2 code)
├── Longbow.Tests/ (unit tests)
├── README.md (features, build instructions)
├── ROADMAP.md (strategic direction — okay to public)
├── LICENSE (GPL-3.0)
└── .gitignore (excludes private keys, etc.)
```

### What's in Private Repo (`Sentinel-Desktop`)

```
Sentinel-Desktop/
├── src/
│   ├── Phase1A/ (copy of public for integration)
│   ├── Phase1B/ (multi-server, audit, roles — PROPRIETARY)
│   ├── Phase2/ (tournament, esports — PROPRIETARY)
│   └── Licensing/ (license key validation — PROPRIETARY)
├── .github/workflows/
│   ├── build-and-sign.yml (CI/CD: build → sign → upload)
│   └── deploy-release.yml (upload to S3, notify Stripe webhook)
├── ROADMAP.md (yes, same file in both repos)
├── ROADMAP_RESEARCH.md (okay to have in private)
├── .gitignore (excludes signing certs, API keys, etc.)
└── (CLOSED: No LICENSE file, no public contributors)
```

### Critical: Secrets Management

**Never commit to any repo:**
- Windows code-signing certificate private key (`.pfx`)
- Stripe API keys, webhook secrets
- License validation server API keys
- Machine ID seed (for deterministic hardware hashing)

**Store in GitHub Secrets:**
```
CODESIGN_CERT_BASE64: (base64-encoded .pfx)
CODESIGN_CERT_PASSWORD: (encrypted)
LICENSE_API_KEY: (encrypted)
STRIPE_WEBHOOK_SECRET: (encrypted)
```

**Used in CI/CD only:**
```yaml
# .github/workflows/build-and-sign.yml
- name: Download signing certificate
  env:
    CERT: ${{ secrets.CODESIGN_CERT_BASE64 }}
  run: |
    echo "$CERT" | base64 -d > cert.pfx
    # Use cert to sign .exe
    signtool sign /f cert.pfx /p "${{ secrets.CODESIGN_CERT_PASSWORD }}" setup.exe
    rm cert.pfx  # Clean up
```

---

## Git Sync Workflow (Upstream Management)

### When You Fix a Bug in Phase 1A (Public Repo)

1. **Bug reported in public repo** → Create issue
2. **Fix in public repo** → Commit to main, push
3. **Update private repo** → Pull upstream:
   ```bash
   cd Sentinel-Desktop
   git fetch public
   git merge public/main
   # (handles merge conflicts if any)
   git push private main
   ```
4. **CI/CD rebuilds** → New signed installer created with fix

### When You Add a Phase 1B Feature (Private Repo)

1. **Feature development** → Commit to private/main
2. **No syncing needed** → Phase 1B code never touches public repo
3. **CI/CD builds** → Signed installer includes Phase 1B
4. **User buys** → Gets new installer with new feature

### Important: Don't Expose Private Commits in Public

**Never do this:**
```bash
git push public :feature-branch  # ❌ WRONG
git rebase public/main private/main  # ❌ Could expose history
```

**Always do this:**
```bash
git merge public/main private/main  # ✅ Clean merge, no history leak
# (Private commits stay in private history)
```

---

## Distribution & Payment Integration

### Free Tier Distribution

**GitHub Releases:**
```
https://github.com/Vpirate12/ArmaReforgerServerTool/releases/tag/v1.0.0

Downloads:
- Sentinel_Desktop_Free_1.0.0.exe (signed, ~80 MB)
- Sentinel_Desktop_Free_1.0.0.msi (signed, ~80 MB)
- Checksums.txt (SHA-256)
```

No license key required. Users run installer directly.

### Paid Tier Distribution

**Stripe Checkout → Secure Download:**
```
User: Clicks "Buy Professional" on spare-time-gaming.us
  ↓
Stripe Checkout (enter payment info)
  ↓
Payment successful → Server generates unique download link
  ↓
Link valid for 7 days, includes:
- License key (embedded in download page)
- Signed installer (.exe)
- License key again (copy-paste during install)
  ↓
User downloads & installs, pastes key on first launch
```

**Implementation (Node.js + Stripe):**
```javascript
app.post("/v1/checkout/success", async (req, res) => {
  const session = await stripe.checkout.sessions.retrieve(req.body.session_id);
  const customerId = session.customer;
  
  // Generate license key
  const licenseKey = generateLicenseKey("PROF", customerId);
  
  // Generate download link (valid 7 days)
  const downloadLink = createSecureLink(licenseKey, "7d");
  
  // Email user
  await sendEmail(session.customer_email, {
    subject: "Your Sentinel Desktop Professional License",
    body: `Download: ${downloadLink}\nLicense Key: ${licenseKey}`
  });
  
  res.json({ success: true, downloadUrl: downloadLink });
});
```

---

## Legal & Protection

### GPL-3.0 Protections (Public Repo)

**Why GPL-3.0 on Phase 1A?**
- Competitor A copies Phase 1A validator into their closed-source tool → **Violation of GPL-3.0**
- GPL-3.0 forces them to: Open-source their entire tool **or** don't use your code
- Your Phase 1B/2 code is proprietary, not affected by GPL-3.0

**How Dual-Licensing Works:**
- You are the copyright holder of Phase 1A
- You release it under GPL-3.0 to the public
- You simultaneously copy the same code into your proprietary Phase 1B repo
- Your proprietary code is not constrained by GPL-3.0 because you own the copyright
- Anyone who wants to relicense Phase 1A (e.g., for commercial use) must negotiate with you

### Terms of Service (For Paid Tiers)

**Must include in EULA:**
```
By purchasing a license, you agree to:
1. License is non-transferable (tied to license key)
2. You may not decompile, reverse-engineer, or extract proprietary code
3. License expires on date specified
4. Refunds: 14-day money-back guarantee
5. We reserve the right to revoke licenses for Terms of Service violations
```

---

## Security Considerations

### Code Signing (Windows)

**Why:** Prevents trojanized versions of your software

**Cost:** ~$200/year (DigiCert, Sectigo, etc.)

**Process:**
```bash
signtool sign /f cert.pfx /p password /t http://timestamp.authority.com setup.exe
```

**User verification:**
- Right-click .exe → Properties → Digital Signatures → "Verified: Sentinel Desktop"

### Machine ID Hashing (Prevent License Key Sharing)

**Simple approach:**
```csharp
public static string GetMachineId() {
  var hardwareId = $"{GetCpuId()}-{GetMotherboardId()}-{GetHardDriveSerial()}";
  using (var hash = SHA256.Create()) {
    return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(hardwareId)));
  }
}
```

**Benefit:** One license key tied to one machine (prevents mass redistribution)

### HTTPS-Only License Validation

**Never** send license keys over HTTP. Always:
```csharp
const string LICENSE_API_URL = "https://api.spare-time-gaming.us/v1/licenses/validate";
// Certificate pinning (prevent MITM):
handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => {
  var expectedThumbprint = "ABC123...";
  return cert.Thumbprint == expectedThumbprint;
};
```

---

## Summary: Open-Core Checklist

✅ **Public Repo:** Phase 1A code, GPL-3.0, free installer, community issues welcome  
✅ **Private Repo:** Phase 1B/2 code, proprietary, team-only, CI/CD only  
✅ **Paywall:** Enforced at distribution level (compiled binaries + license keys)  
✅ **License Validation:** Local-first, offline grace period, secure HTTPS callback  
✅ **Code Signing:** Windows cert, signed .exe, user verification  
✅ **Machine ID Hashing:** Prevents mass license sharing  
✅ **Git Sync:** Upstream remotes, no private history leaks  
✅ **Secrets:** GitHub Secrets only, never committed  

---

**Status:** 🔒 LOCKED  
**Implementation:** Phase 1B development (CI/CD setup in weeks 1-3)  
**Ship Date:** Q3 2026
