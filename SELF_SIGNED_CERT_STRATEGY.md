# Self-Signed Certificate Strategy: User Communication

**Date Locked:** 2026-06-30  
**Approach:** Self-signed certs (free) + explicit user education (trust)  
**Upgrade Path:** DigiCert EV cert when revenue justifies ($5k+/month)

---

## User-Facing Messaging

### When Windows Shows the SmartScreen Warning

**What the user sees:**
```
⚠️  Windows SmartScreen
    "Windows Defender SmartScreen prevented an unrecognized app from starting."
    [More info] [Run anyway]
```

**What we tell them (before they see the warning):**

#### 1. In README.md (Installation section)
```markdown
## Installation

### Windows SmartScreen Warning (Expected & Safe)

When you first run Sentinel Desktop, Windows may show a security warning:

> "Windows Defender SmartScreen prevented an unrecognized app from starting."

**This is expected and safe.** Here's why:

Sentinel Desktop is a **self-signed application**. This means:
- The installer is digitally signed by the Sentinel Desktop team (not by a paid certificate authority)
- Windows doesn't recognize the signing authority yet, so it shows a warning
- This is **normal for indie software** and does NOT mean the software is malicious
- Thousands of legitimate indie games, dev tools, and admin utilities show this warning

**Is it safe?** Yes. We sign all releases with our self-signed certificate. If the signature is valid, 
the software hasn't been tampered with.

**How to proceed:**
1. Click "More info" to see: "Publisher: Sentinel Desktop"
2. Click "Run anyway" to launch the app

**When will this change?** Once Sentinel Desktop reaches 1000+ paid users, we'll upgrade to a 
certificate authority-signed cert (no more warning).

---

**Note to security-conscious admins:** You can always verify the installer's SHA-256 hash 
against our GitHub releases page before running it.
```

#### 2. In Installer (Pre-Installation Screen)
```
┌────────────────────────────────────────────────────┐
│ Sentinel Desktop Setup Wizard                      │
├────────────────────────────────────────────────────┤
│                                                    │
│ ⚠️  Important: Self-Signed Certificate             │
│                                                    │
│ Sentinel Desktop uses a self-signed certificate.  │
│ This is safe and normal for indie software.       │
│                                                    │
│ When you run the app, Windows may show a warning: │
│ "Unrecognized app prevented from starting"        │
│                                                    │
│ This is expected. Click "Run anyway" to proceed.  │
│                                                    │
│ ☐ I understand (don't show again)                 │
│                                                    │
│ [Back]  [Continue]                                │
└────────────────────────────────────────────────────┘
```

#### 3. In First-Run Wizard (Onboarding)
```
┌────────────────────────────────────────────────────┐
│ Welcome to Sentinel Desktop                       │
├────────────────────────────────────────────────────┤
│                                                    │
│ About the Warning You Just Saw:                   │
│                                                    │
│ Windows showed a security warning. This is        │
│ completely normal because Sentinel Desktop is:    │
│                                                    │
│ ✓ Independently developed (indie software)       │
│ ✓ Digitally signed with our self-signed cert     │
│ ✓ Not yet from a paid certificate authority      │
│                                                    │
│ Your system is safe. We wouldn't ask you to      │
│ run something we didn't trust.                   │
│                                                    │
│ More info: https://docs.sentinel-desktop.dev/    │
│            security/self-signed-certs             │
│                                                    │
│ [I Understand - Continue]                         │
└────────────────────────────────────────────────────┘
```

#### 4. In Help/FAQ (Website + App)
```
Q: Why does Windows show a security warning?
A: Sentinel Desktop uses a self-signed certificate for code signing. This is standard 
   practice for indie software. The warning doesn't mean the software is unsafe — 
   it just means the signing certificate isn't from a paid certificate authority yet.

Q: Is it safe to click "Run anyway"?
A: Yes. We digitally sign all releases. If the signature is valid, the software 
   hasn't been tampered with.

Q: When will you get a "real" certificate?
A: Once we reach enough paying users to justify the cost (~$300/year). In the meantime, 
   we prioritize features over certificate costs.

Q: How can I verify the installer is authentic?
A: Compare the SHA-256 hash of your download against the hash published on our GitHub 
   releases page. Instructions here: [link]
```

---

## Technical Implementation

### 1. Installer (NSIS)

**Add pre-install information page:**
```nsis
; In installer script:
Page custom nsDialogWarning
Page directory
Page instfiles
Page finish

Function nsDialogWarning
  nsDialogs::Create 1018
  Pop $Dialog
  
  ${If} $Dialog == error
    Abort
  ${EndIf}
  
  ${NSD_CreateLabel} 0 0 100% 20u "⚠️  Self-Signed Certificate Notice"
  ${NSD_CreateLabel} 0 25 100% 60u "Sentinel Desktop uses a self-signed certificate. Windows may show a security warning when you run the app. This is normal and safe. Click 'Run anyway' to proceed."
  ${NSD_CreateCheckBox} 0 90 100% 10u "I understand, don't show again" $CheckBoxState
  
  nsDialogs::Show
FunctionEnd
```

### 2. App Launch (Tauri)

**On first run, show modal:**
```rust
// src-tauri/src/main.rs
#[tauri::command]
fn show_cert_warning() -> bool {
  let prefs = load_preferences();
  if !prefs.cert_warning_dismissed {
    tauri::api::dialog::ask(
      &tauri::State::default(),
      "Self-Signed Certificate",
      "Did you see a Windows security warning when launching this app? That's normal and safe. \
       We use self-signed certificates for code signing. Click 'Yes' to continue.",
      |answer| {
        if answer {
          dismiss_cert_warning();
        }
      }
    );
    return true;
  }
  false
}

// On app startup
fn main() {
  tauri::Builder::default()
    .setup(|app| {
      show_cert_warning();
      Ok(())
    })
    .run(tauri::generate_context!())
    .expect("error while running tauri application");
}
```

### 3. README + Docs

**GitHub README:**
```markdown
## Installation

### Windows Users: SmartScreen Warning

When you first install Sentinel Desktop, Windows may show this warning:

> "Windows Defender SmartScreen prevented an unrecognized app from starting."

**This is expected.** Sentinel Desktop is signed with a self-signed certificate 
(standard for indie software). Click **"Run anyway"** to launch.

→ [Learn more about self-signed certificates](SELF_SIGNED_CERT_STRATEGY.md)
```

**Docs site section:**
```
docs/security/self-signed-certs/
├── overview.md (what is it, why we use it)
├── is-it-safe.md (yes, with explanation)
├── how-to-verify.md (SHA-256 hash verification)
└── when-will-it-change.md (timeline for upgrade)
```

---

## Transparency Checklist

Before shipping Phase 1B:

- [ ] README.md explains self-signed cert + SmartScreen warning
- [ ] Installer has pre-install warning page
- [ ] First-run wizard (onboarding) explains warning
- [ ] Help/FAQ section covers certificate questions
- [ ] Website has security documentation
- [ ] SHA-256 hashes published with every release
- [ ] Discord/support has pinned message about warning
- [ ] Release notes mention "self-signed cert (expected)"

---

## User Trust Impact

**Transparent approach (what we're doing):**
- User sees warning
- User reads: "This is normal indie software"
- User thinks: "Okay, makes sense"
- Result: **Trust increases** (honesty > hiding)

**Opaque approach (what NOT to do):**
- User sees warning
- User thinks: "Is this malware???"
- User Googles it, finds: "Self-signed certs are sketchy"
- Result: **Trust decreases** (silence breeds suspicion)

---

## Upgrade Path to Paid Certificate

**When to upgrade:** Once you hit $5k+/month revenue
- Cost: ~$300/year (DigiCert EV certificate)
- Benefit: No SmartScreen warning
- Decision: Community request or business justification

**Upgrade announcement:**
```
Release Notes v2.0.0:

🔒 Code Signing Update

Sentinel Desktop now uses a DigiCert EV code-signing certificate. 
This means Windows no longer shows a security warning when you run the app.

Previous users: Thanks for putting up with the warning during our early days. 
Your support made this upgrade possible.
```

---

## Security Verification (For Paranoid Users)

**How to verify installer authenticity:**

1. Download installer from GitHub: `Sentinel_Desktop_1.0.0.exe`
2. Get SHA-256 from release page: `abc123...`
3. In PowerShell:
   ```powershell
   (Get-FileHash "Sentinel_Desktop_1.0.0.exe" -Algorithm SHA256).Hash
   # Compare output to published hash
   ```
4. If hashes match: **Software is authentic and unmodified**

---

**Status:** 🔒 LOCKED  
**Messaging:** Transparent, user-first, trust-building  
**Implementation:** Weeks 1-3 (installer, README, docs, onboarding)  
**User Impact:** Informed consent, no surprises, genuine transparency
