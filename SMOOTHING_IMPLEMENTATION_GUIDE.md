# Smoothing Implementation Guide: Phase 1B Quick Wins

**Goal:** Detailed specifications for the 5 quick-win smoothing solutions that ship with Phase 1B MVP. These are UX/workflow improvements that reduce friction without adding complexity.

---

## 1. Onboarding Wizard

**Type:** Progressive Disclosure, Templates  
**Effort:** Low (React components, C# defaults)  
**Impact:** High (removes first-run friction, guides new admins)  
**Integration Week:** 4-5

### Purpose
Guide new admins through the 3 critical setup steps:
1. Connect to first RCON server
2. Select default admin roles
3. Confirm log paths

Instead of a blank dashboard, admins see a friendly, step-by-step flow.

### User Experience Flow

1. **First Launch**
   - Detect: User has no servers configured
   - Trigger: Show modal dialog "Welcome to Sentinel Desktop"
   - CTA: "Let's set up your first server (2 min)"

2. **Step 1: RCON Connection**
   - Fields: Server IP (text), Port (number), RCON Password (password field)
   - Validation: "Test Connection" button
     - Attempt RCON `#help` command
     - If success → "✅ Connected!" (green)
     - If fail → "❌ Connection failed. Check IP/port/password." (red)
   - Next button (enabled only after successful test)

3. **Step 2: Admin Roles**
   - Question: "Who will use this tool?"
   - Radio options:
     - "Just me (solo admin)" → Create role "Admin" with all permissions
     - "My team (multiple admins)" → Create roles "Owner" (all), "Moderator" (kick/ban/config), "Viewer" (audit log only)
   - Checkboxes: "Assign myself as..." (default: Owner)
   - Next button

4. **Step 3: Log Paths (Optional)**
   - Explain: "Where should Sentinel Desktop look for server logs?"
   - "Auto-detect" button: Scan common paths (`C:\Arma Reforger Server\logs`, etc.)
   - Manual path: File browser button
   - This helps Crash Log Widget find errors
   - Skip button: "I'll configure this later"

5. **Summary**
   - Display: "You're ready to go!"
   - Show: Server name, IP, port (read-only)
   - Show: Selected roles (read-only)
   - Button: "Start Managing"
   - Redirect: Main dashboard

### Technical Details

**React Component Tree:**
```
<OnboardingModal>
  <Step1RCONConnection>
    <form>
      <input name="ip" />
      <input name="port" />
      <input name="password" type="password" />
      <button onClick={testConnection}>Test Connection</button>
      <ConnectionStatus /> {/* green/red indicator */}
    </form>
  </Step1RCONConnection>
  
  <Step2AdminRoles>
    <RadioGroup options={["solo", "team"]} />
    <RoleAssignment />
  </Step2AdminRoles>
  
  <Step3LogPaths>
    <button onClick={autoDetect}>Auto-detect</button>
    <FileBrowser />
  </Step3LogPaths>
  
  <StepNavigation prev next skip /> {/* step buttons */}
</OnboardingModal>
```

**C# Backend Endpoints:**
- `POST /api/servers/test-rcon` — Test RCON connection
  - Input: `{ ip: "...", port: 2302, password: "..." }`
  - Output: `{ success: true/false, error?: string }`

- `POST /api/wizard/complete` — Save wizard results
  - Input: `{ server: {...}, roles: [...], logPaths: {...} }`
  - Output: Redirect to dashboard

**Conditional Display:**
```csharp
// In OnboardingModal component
if (!user.HasAnyServers && !localStorage.GetItem("onboarding_dismissed")) {
  return <OnboardingWizard />;
}
// Else show normal dashboard
```

### Testing Checklist
- [ ] Wizard appears on first launch with no servers
- [ ] RCON test connection succeeds/fails correctly
- [ ] Next button disabled until test passes
- [ ] Roles created with correct permissions
- [ ] Log paths auto-detected correctly
- [ ] Summary review shows correct values
- [ ] Completing wizard creates server + roles + redirects to dashboard
- [ ] Dismissing wizard (close button) hides for rest of session

---

## 2. Crash Log Summary Widget

**Type:** Context, Visibility  
**Effort:** Low (React component, C# log parsing)  
**Impact:** High (most frequent support issue — servers crashing)  
**Integration Week:** 7-8

### Purpose
Auto-surface the last known server error on the dashboard, so admins see crashes immediately instead of hunting through logs.

### User Experience

**Dashboard Widget (Card):**
```
┌─────────────────────────────────┐
│ 🔴 Last Server Error            │
├─────────────────────────────────┤
│ Server: My Test Server          │
│ Time:   2 minutes ago           │
│ Error:  DLL not found           │
│                                 │
│ Likely cause: Missing mod .dll  │
│                                 │
│ [Restart Server] [View Full Log]│
└─────────────────────────────────┘
```

**Behavior:**
- Widget appears only when server has crashed (status = offline + error log exists)
- Auto-refreshes when new crashes detected
- "Restart Server" button → send RCON `#restart` command
- "View Full Log" link → open log file in text editor or open log viewer modal
- Auto-dismiss after server comes back online (green status)

### Technical Details

**React Component:**
```jsx
<CrashLogWidget serverStatus={status} logPath={logPath}>
  {status.status === "offline" && status.lastError ? (
    <>
      <div className="error-banner">
        <Icon name="alert-circle" color="red" />
        <span>Last Server Error</span>
      </div>
      <div className="error-details">
        <p>Server: {status.name}</p>
        <p>Time: {formatTime(status.lastErrorTime)}</p>
        <p>Error: {status.lastError.message}</p>
        <p className="hint">{status.lastError.hint}</p>
      </div>
      <div className="actions">
        <Button onClick={restartServer}>Restart Server</Button>
        <Button onClick={viewLog} variant="secondary">View Full Log</Button>
      </div>
    </>
  ) : null}
</CrashLogWidget>
```

**C# Backend:**

1. **Log Monitoring Service** (runs in background):
   - Watch server log file for changes (FileSystemWatcher)
   - On change: parse last N lines for error patterns
   - Error patterns:
     - `Error:` (generic)
     - `Exception:` (C# exception)
     - `Fatal error:` (engine crash)
     - `DLL not found:` (missing dependency)
     - `Script error:` (mod scripting issue)

2. **Parse & Store:**
   ```csharp
   public class ServerError {
     public int ServerId { get; set; }
     public string Message { get; set; }
     public string Hint { get; set; } // "Missing mod .dll" — helpful context
     public DateTime OccurredAt { get; set; }
     public bool Resolved { get; set; } // true once server comes back online
   }
   ```

3. **Endpoint:**
   - `GET /api/servers/{id}/last-error` → Return latest error + hint

**Hint Generation Logic:**
```csharp
public static string GenerateHint(string errorMessage) {
  if (errorMessage.Contains("DLL")) return "Missing mod .dll or .exe";
  if (errorMessage.Contains("Script error")) return "Mod scripting issue";
  if (errorMessage.Contains("OutOfMemory")) return "Server out of memory (too many players/AI)";
  return "Check server logs for details";
}
```

### Testing Checklist
- [ ] Widget hidden when server is online
- [ ] Widget shown when server is offline + error exists
- [ ] Last error message displayed correctly
- [ ] Hint/suggestion helpful
- [ ] Restart button sends RCON #restart
- [ ] View Full Log opens log file
- [ ] Widget dismisses when server comes back online
- [ ] Multiple crashes tracked (oldest not overwritten)

---

## 3. Live Kick Diagnostic Card

**Type:** Visibility, Context, Automation  
**Effort:** Low (React component, C# log listener)  
**Impact:** High (signature kicks are the #1 support ticket driver)  
**Integration Week:** 7-8

### Purpose
Auto-detect mod signature kick events from server logs and display actionable diagnostics on the dashboard.

### User Experience

**Dashboard Widget (Auto-appears when kicks detected):**
```
┌─────────────────────────────────────┐
│ ⚠️  Mod Signature Kick               │
├─────────────────────────────────────┤
│ Player: John_Doe                    │
│ Mod:    CBA_A3                      │
│ Reason: Version mismatch            │
│                                     │
│ 💡 Solution:                        │
│ Ask player to verify CBA_A3         │
│ in Steam Workshop (right-click →    │
│ Properties → Local Files → Verify)  │
│                                     │
│ [Copy Message] [Post to Discord]    │
│ [Dismiss]                           │
└─────────────────────────────────────┘
```

**Behavior:**
- Card appears within 1 second of kick event in log
- Shows player name + mod name + brief reason
- Provides copy-paste-ready message for Discord
- Auto-dismisses after 5 minutes or when resolved
- Accumulate: if 5+ kicks in 5 min → show "Mod Update Wave" alert instead

### Technical Details

**Regex Patterns (C# log parser):**
```csharp
// Arma Reforger signature kick patterns:
@"Player (?<player>[\w\-]+).*kicked.*addon.*signature.*(?<mod>[a-zA-Z0-9_\-]+)"
@"Invalid addon (?<mod>[a-zA-Z0-9_\-]+).*version (?<version>\d+\.\d+)"
@"Workshop ID \d+ \((?<mod>[a-zA-Z0-9_\-]+)\) mismatch"
```

**React Component:**
```jsx
<KickDiagnosticCard kick={kickEvent} onDismiss={onDismiss}>
  <div className="kick-summary">
    <Icon name="alert-triangle" color="warning" />
    <h3>Mod Signature Kick</h3>
  </div>
  <div className="kick-details">
    <p>Player: <strong>{kick.playerName}</strong></p>
    <p>Mod: <strong>{kick.modName}</strong></p>
    <p>Reason: {kick.reason}</p>
  </div>
  <div className="solution">
    <h4>💡 Solution:</h4>
    <p>{generateSolution(kick.modName)}</p>
  </div>
  <div className="actions">
    <Button onClick={copyToClipboard}>Copy Message</Button>
    <Button onClick={postToDiscord} variant="secondary">Post to Discord</Button>
    <Button onClick={dismiss} variant="ghost">Dismiss</Button>
  </div>
</KickDiagnosticCard>
```

**C# Log Listener:**
```csharp
public class KickDiagnosticService {
  private FileSystemWatcher _logWatcher;
  
  public void StartMonitoring(string logPath) {
    _logWatcher = new FileSystemWatcher(Path.GetDirectoryName(logPath));
    _logWatcher.Changed += OnLogChanged;
  }
  
  private void OnLogChanged(object sender, FileSystemEventArgs e) {
    var newLines = GetNewLogLines(e.FullPath);
    foreach (var line in newLines) {
      var kick = ParseKickEvent(line);
      if (kick != null) {
        _kickQueue.Enqueue(kick);
        // Broadcast to React via WebSocket or HTTP polling
      }
    }
  }
  
  private KickEvent ParseKickEvent(string logLine) {
    // Match against regex patterns
    // Return { playerName, modName, reason }
  }
}
```

**Solution Generator:**
```csharp
public static string GenerateSolution(string modName) {
  return $@"Ask player to verify {modName} in Steam Workshop:
1. Right-click {modName} in Steam Library
2. Properties → Local Files
3. Click 'Verify integrity of application files'
4. Restart game";
}
```

**Discord Integration (Optional):**
- `POST /api/discord/send` — Send message to configured webhook
- Message template: Kick event + player + solution

### Testing Checklist
- [ ] Regex patterns match real kick log lines
- [ ] Player name extracted correctly
- [ ] Mod name extracted correctly
- [ ] Solution text helpful
- [ ] Card appears within 1 second of kick
- [ ] Copy-to-clipboard works
- [ ] Discord post works (if webhook configured)
- [ ] Card dismisses after 5 min or on click
- [ ] Kick wave detection (5+ in 5 min) triggers alert

---

## 4. Ban/Unban Confirmation Dialog

**Type:** Guardrail  
**Effort:** Very Low (React modal)  
**Impact:** High (prevents #1 accidental ban disaster)  
**Integration Week:** 9-10

### Purpose
Require explicit confirmation before any ban/unban action, preventing accidental bans.

### User Experience

**Before Ban/Unban:**
```
User clicks ban player button
  ↓
Modal appears:
┌──────────────────────────────────┐
│ Confirm Ban                      │
├──────────────────────────────────┤
│ You are about to ban:            │
│ Player: John_Doe (SteamID: ...) │
│ Server: My Server (+ 3 others)  │
│ Duration: Permanent             │
│                                  │
│ Reason (optional):               │
│ [text field] ("Griefing")       │
│                                  │
│ [Cancel]  [Confirm Ban]         │
└──────────────────────────────────┘
```

**After Confirm:**
- Ban applied across selected servers
- Audit log: `{ actor: current_user, action: "BanApplied", target: player, reason: "Griefing" }`
- Toast: "✅ Player banned across 4 servers"

### Technical Details

**React Component:**
```jsx
const BanConfirmDialog = ({ player, servers, onConfirm, onCancel }) => {
  const [reason, setReason] = useState("");
  
  return (
    <Modal>
      <h2>Confirm Ban</h2>
      <p>You are about to ban:</p>
      <p><strong>{player.name}</strong> (SteamID: {player.steamId})</p>
      <p><strong>Servers:</strong> {servers.map(s => s.name).join(", ")}</p>
      
      <textarea 
        placeholder="Reason for ban (optional)" 
        value={reason} 
        onChange={e => setReason(e.target.value)} 
      />
      
      <div className="actions">
        <Button onClick={onCancel}>Cancel</Button>
        <Button onClick={() => onConfirm(reason)} variant="danger">Confirm Ban</Button>
      </div>
    </Modal>
  );
};
```

**C# Endpoint:**
- `POST /api/servers/{id}/players/{steamid}/ban` — Apply ban
  - Requires confirmation token (passed in form state)
  - Logs action via AuditService

### Testing Checklist
- [ ] Dialog appears before ban
- [ ] Dialog shows player name, SteamID, servers affected
- [ ] Reason field optional
- [ ] Cancel closes dialog without banning
- [ ] Confirm sends RCON ban command + logs action
- [ ] Same dialog for unban

---

## 5. Bulk SteamID Paste for Whitelist

**Type:** Shortcut  
**Effort:** Very Low (React input logic)  
**Impact:** High (reduces tedium of whitelisting 20-player clan)  
**Integration Week:** 9-10

### Purpose
Allow admins to paste multiple SteamIDs at once (comma or newline separated) instead of entering one by one.

### User Experience

**UI Change:**
```
Before:
[ SteamID input field ] [Add]
(must click Add for each of 20 players)

After:
[ Bulk paste here - SteamIDs separated by comma or newline ]
[Parse] 
→ Shows: "Ready to add 20 SteamIDs. Select servers:" 
[ ] Server 1
[ ] Server 2
[ ] Server 3
[Add to Whitelist]
```

### Technical Details

**React Component:**
```jsx
const BulkWhitelistInput = ({ onAddMultiple }) => {
  const [input, setInput] = useState("");
  const [parsed, setParsed] = useState([]);
  const [selectedServers, setSelectedServers] = useState([]);
  
  const handleParse = () => {
    // Split by comma or newline
    const steamIds = input
      .split(/[,\n]/)
      .map(s => s.trim())
      .filter(s => /^\d{17}$/.test(s)); // Validate SteamID format
    
    setParsed(steamIds);
  };
  
  const handleAdd = () => {
    onAddMultiple(parsed, selectedServers);
    setInput("");
    setParsed([]);
  };
  
  return (
    <div>
      <textarea
        placeholder="Paste SteamIDs (comma or newline separated)"
        value={input}
        onChange={e => setInput(e.target.value)}
      />
      <Button onClick={handleParse}>Parse</Button>
      
      {parsed.length > 0 && (
        <>
          <p>Ready to add {parsed.length} SteamIDs. Select servers:</p>
          <ServerChecklist 
            servers={servers} 
            onChange={setSelectedServers} 
          />
          <Button onClick={handleAdd}>Add to Whitelist</Button>
        </>
      )}
    </div>
  );
};
```

**SteamID Validation:**
```csharp
public static bool IsValidSteamId(string steamId) {
  return Regex.IsMatch(steamId, @"^\d{17}$");
}
```

**C# Endpoint:**
- `POST /api/servers/bulk-whitelist` — Add multiple SteamIDs
  - Input: `{ steamIds: ["...", "..."], serverIds: [1, 2] }`
  - Output: `{ success: true, added: 20, failed: 0 }`

### Testing Checklist
- [ ] Pasting comma-separated SteamIDs parses correctly
- [ ] Pasting newline-separated SteamIDs parses correctly
- [ ] Invalid SteamIDs rejected during parse
- [ ] Server selection checkboxes work
- [ ] Add button sends request + logs action
- [ ] Toast shows "Added 20 SteamIDs to 3 servers"
- [ ] Duplicates (already whitelisted) handled gracefully

---

## Summary: Quick Win Integration

| # | Smoothing | Week | Effort | UI Component | Backend Endpoint | Testing Points |
|---|-----------|------|--------|--------------|------------------|-----------------|
| 1 | Onboarding Wizard | 4-5 | Low | `<OnboardingModal>` | `POST /api/wizard/complete` | 7 |
| 2 | Crash Log Widget | 7-8 | Low | `<CrashLogWidget>` | `GET /api/servers/{id}/last-error` | 8 |
| 3 | Kick Diagnostic | 7-8 | Low | `<KickDiagnosticCard>` | `POST /api/discord/send` (optional) | 8 |
| 4 | Ban Confirmation | 9-10 | Very Low | `<BanConfirmDialog>` | `POST /api/servers/{id}/players/{steamid}/ban` | 6 |
| 5 | Bulk SteamID Paste | 9-10 | Very Low | `<BulkWhitelistInput>` | `POST /api/servers/bulk-whitelist` | 7 |

**Total Implementation Effort:** ~40-50 hours (embedded in Phase 1B timeline, not additive)

**Expected ROI:** Massive. These 5 solutions address the top 5 admin pain points and immediately signal that Sentinel Desktop "gets it."

---

**Status:** 🔒 SPECIFICATIONS LOCKED  
**Next Step:** Implement during Phase 1B weeks 4-5, 7-8, 9-10
