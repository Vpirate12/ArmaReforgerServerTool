# Longbow – Contribution Notes

These changes were developed and tested against the main branch as of early May 2026.
All changes build cleanly (0 errors, pre-existing warnings only) on .NET 8 / WinForms.

---

## 1. Bug Fix — `AdvancedServerParameterEnumerated.cs`

**Problem:** The `ParameterValue` getter cast `parameterValue.SelectedItem` to `object` (via
the base type) and then attempted to use it as a `string` downstream, throwing an
`InvalidCastException` at runtime whenever an enumerated parameter was read back after
save/load.

**Fix:** Cast directly to `string` in both the getter and the `SelectedItem` convenience
property; use `Items.IndexOf(value)` in the setter instead of a manual loop.

```csharp
// Before
public override object ParameterValue
{
    get => parameterValue.SelectedItem; // returns object — crashes on downstream cast
    set { /* manual loop */ }
}

// After
public override object ParameterValue
{
    get => (string) parameterValue.SelectedItem;
    set
    {
        int index = parameterValue.Items.IndexOf(value);
        if (index >= 0)
            parameterValue.SelectedIndex = index;
    }
}

public string SelectedItem
{
    get => (string) parameterValue.SelectedItem;
}
```

---

## 2. Memory Leak Fixes — `ProcessManager.cs`

### 2a. `BackgroundWorker` promoted to field

The `BackgroundWorker` was created as a local variable inside `StartStopServer()`, meaning
every server start leaked a new worker. It is now a nullable field (`m_worker`) that is
disposed and re-created on each start.

```csharp
// Field added
private BackgroundWorker? m_worker;

// On each server start
m_worker?.Dispose();
m_worker = new BackgroundWorker { WorkerReportsProgress = true };
// ... configure and run m_worker ...
```

### 2b. `m_steamCmdUpdateProcess` disposed after use

The SteamCMD process was never disposed after `WaitForExit()`. Added `Dispose()` in the
worker's `DoWork` handler after the update step completes.

```csharp
m_steamCmdUpdateProcess.WaitForExit();
m_steamCmdUpdateProcess.Dispose();
```

### 2c. `m_serverProcess` disposed in stop path

`Kill()` without `Dispose()` left the process handle open. Both the normal stop path and
the exception handler now call `Dispose()` immediately after `Kill()`.

```csharp
m_serverProcess.Kill();
m_serverProcess.Dispose();
```

### 2d. `Dispose()` method added to `ProcessManager`

`ProcessManager` now implements a `Dispose()` method that covers all owned resources.

```csharp
public void Dispose()
{
    m_timerCancellationTokenSource.Dispose();
    m_intervalRestartCts?.Cancel();
    m_intervalRestartCts?.Dispose();
    m_worker?.Dispose();
    m_serverProcess.Dispose();
    m_steamCmdUpdateProcess.Dispose();
}
```

---

## 3. Event Handler / BindingSource Cleanup — `Forms/Main.cs`

**Problem:** `OnFormClosing` did not unsubscribe event handlers or dispose `BindingSource`
objects, leaving `ProcessManager` rooted by the form after close.

**Fix:** All five event handlers are unsubscribed, both `BindingSource` instances are
disposed, and `ProcessManager.GetInstance().Dispose()` is called in `OnFormClosing`.

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    // Unsubscribe all event handlers
    ProcessManager.GetInstance().UpdateSteamCmdLogEvent  -= HandleSteamCmdLogEvent;
    ProcessManager.GetInstance().UpdateGuiControlsEvent  -= HandleGuiControlsEvent;
    ProcessManager.GetInstance().UpdateServerStatusEvent -= HandleServerStatusEvent;
    m_serverStatusParser.UpdateServerStatus             -= HandleServerStatusEvent;
    // ... additional handler(s) ...

    // Dispose status refresh timer
    m_statusRefreshTimer?.Stop();
    m_statusRefreshTimer?.Dispose();

    // Dispose binding sources
    m_availableModsBindingSource.Dispose();
    m_enabledModsBindingSource.Dispose();

    ProcessManager.GetInstance().Dispose();
    base.OnFormClosing(e);
}
```

---

## 4. New Feature — Interval Restart with RCON Player Notifications

### Overview

Adds a configurable timer that gracefully restarts the server every N hours (1–24),
sending countdown warnings to all connected players via BattlEye RCON before shutdown.
Useful for clearing any accumulated memory or state issues without surprising players.

### 4a. New file: `Managers/RconManager.cs`

Singleton BattlEye UDP RCON client. Reads RCON configuration (address, port, password)
from the loaded server configuration. Silently no-ops when RCON is not enabled.

**Protocol:** `BE(2 bytes) + CRC32(4 bytes, little-endian) + 0xFF + type + payload`
- Login packet: type `0x00`, payload = password bytes
- Command packet: type `0x01`, payload = sequence byte + command string
- CRC32 polynomial: `0xEDB88320` (reflected/reversed variant)
- `say -1 <message>` broadcasts to all players

```csharp
public async Task SendBroadcastAsync(string message)
{
    // reads config, connects to 127.0.0.1 when address is 0.0.0.0,
    // authenticates, sends "say -1 <message>"
}
```

### 4b. `ProcessManager.cs` — interval restart methods

```csharp
public void ConfigureIntervalRestartTask(int intervalHours)
// Cancels any existing task, starts a new RunIntervalRestartAsync loop.

public void CancelIntervalRestartTask()
// Cancels and nulls m_intervalRestartCts.

private async Task RunIntervalRestartAsync(TimeSpan interval, CancellationToken ct)
// Waits (interval - 10 min), then sends RCON warnings:
//   T-10 min: "Server restarting in 10 minutes."  → wait 5 min
//   T-5  min: "Server restarting in 5 minutes."   → wait 4 min
//   T-1  min: "Server restarting in 1 minute."    → wait 1 min
//   Restart: StopServer() → delay → StartServer()
```

### 4c. `Forms/Main.cs` — UI control

A new `AdvancedServerParameterNumeric` control is added to the Advanced panel after
`autoRestartOnCrash`:

| Property | Value |
|---|---|
| Key | `intervalRestartHours` |
| Friendly name | Interval Restart (hours) |
| Range | 1 – 24 |
| Default | 4 (disabled) |
| Description | Restart every N hours with RCON warnings at 10, 5, and 1 minute(s) |

`StartServerBtnPressed` configures or cancels the task independently of the daily
restart timer when the server is toggled on/off.

### 4d. `Models/SavedState.cs` — persisted setting

```csharp
public static readonly AdvancedSetting DEFAULT_INTERVAL_RESTART_HOURS =
    new("intervalRestartHours", 4, false);
```

Added to `GetDefaultAdvancedSettings()` so the value survives restarts via `state.json`.

### Requirements

- RCON must be enabled in the server JSON configuration (`rcon.enabled = true`).
- The RCON port and password must match what is configured in the server JSON.
- If RCON is not enabled, countdown warnings are skipped but the restart still fires.

---

## 5. UI Improvement — 500 ms Status Tab Refresh

**Problem:** The Status tab only updated on events pushed from `ProcessManager`. If no
event fired, the displayed FPS / memory / player count could be stale.

**Fix:** A `System.Timers.Timer` polls `HandleServerStatusEvent` every 500 ms while the
server is online. The timer is disabled when the server stops and disposed on form close.

```csharp
// Field
private System.Timers.Timer m_statusRefreshTimer;
private const int STATUS_REFRESH_INTERVAL_MS = 500;

// Constructor
m_statusRefreshTimer = new System.Timers.Timer(STATUS_REFRESH_INTERVAL_MS);
m_statusRefreshTimer.Elapsed += (s, e) => HandleServerStatusEvent(this, new());
m_statusRefreshTimer.AutoReset = true;
m_statusRefreshTimer.Enabled = false;

// Enable when server comes online (inside HandleServerStatusEvent)
if (m_statusRefreshTimer != null && !m_statusRefreshTimer.Enabled)
    m_statusRefreshTimer.Enabled = true;

// Disable when server goes offline
if (m_statusRefreshTimer != null)
    m_statusRefreshTimer.Enabled = false;

// Dispose in OnFormClosing
m_statusRefreshTimer?.Stop();
m_statusRefreshTimer?.Dispose();
```

---

## 6. UI Bug Fix — Status Tab Data Persistence Issue

**Problem:** The 500ms status tab refresh timer was calling `HandleServerStatusEvent(this, new())` with an empty `ServerStatusEventArgs` object. Since the empty args have `ServerOnline = false` by default, the handler would immediately clear all status fields (server address, players, RCON address, etc.), causing them to blink with values and then reset to "Server is offline." every 500ms.

**Root Cause:** The timer was passing empty args instead of the current/last known status, so the UI refresh would perpetually clear the fields.

**Fix:** 

1. Added public getter to `ServerStatusParser.cs`:
```csharp
public ServerStatusEventArgs GetCurrentStatus()
{
    return m_serverStatus;
}
```

2. Updated timer callback in `Forms/Main.cs`:
```csharp
// Before
m_statusRefreshTimer.Elapsed += (s, e) => HandleServerStatusEvent(this, new());

// After
m_statusRefreshTimer.Elapsed += (s, e) => HandleServerStatusEvent(this, m_serverStatusParser.GetCurrentStatus());
```

Now the timer passes the LAST KNOWN status instead of empty args, so fields persist and update smoothly without blinking or clearing.

---

---

## 7. Bug Fix — Mod Load Order Scrambled on Second Config Load — `Managers/ConfigurationManager.cs`

**Problem:** Loading a saved config a second time within the same session produced a
scrambled mod load order in the Enabled Mods list. Mods appeared in a different order than
the JSON file, causing mod dependency errors at server startup.

**Root Cause:** `PopulateServerConfiguration()` (and `ImportModsList()`) first attempted to
move all enabled mods back to the available list using an index-based loop:

```csharp
// BUG — shrinking the list while incrementing i skips every other item
for (int i = 0; i < m_enabledMods.Count; i++)
{
    MoveMod(m_enabledMods[i], m_enabledMods, m_availableMods);
}
```

With N mods, this loop only moves mods at even indices (0, 2, 4…). Mods at odd indices
(1, 3, 5…) remain in `m_enabledMods`. When the new config then re-populates the list,
`Contains()` skips those already-present mods, and the rest are appended at the end,
producing a completely scrambled order.

The bug was silent on the *first* load of a session (when `m_enabledMods` starts empty)
but triggered reliably on every subsequent load.

**Fix:** Snapshot the list to an array, clear it, then restore — all in one safe pass.

```csharp
// Before (buggy — skips every other mod)
for (int i = 0; i < m_enabledMods.Count; i++)
{
    MoveMod(m_enabledMods[i], m_enabledMods, m_availableMods);
}

// After (correct — snapshots first, clears atomically)
Mod[] previouslyEnabled = m_enabledMods.ToArray();
m_enabledMods.Clear();
foreach (Mod mod in previouslyEnabled)
{
    if (!m_availableMods.Contains(mod))
        m_availableMods.Add(mod);
}
```

The same fix was applied to `ImportModsList()`, which contained an identical copy of
the buggy loop.

**Impact beyond the UI:** This fix is what makes automated remote scenario switching
reliable. Without it, any tooling (web apps, scripts) that switches scenarios by
loading a new config into Longbow more than once per session will produce a
scrambled mod order on the second switch and every one after — causing server
startup failures due to broken mod dependencies.

---

## Upstream Overlap Analysis (as of May 2026)

After PR #242 was opened, a comparison against upstream `main` (commit `a6b1ee3`) was done
to check for any overlap with the author's concurrent work on status fetching
(`711187a Implemented ServerStatusParser`, `cc7d15d Progress on status fetching`).

### What the author independently implemented

The author added the `ServerStatusParser` class and the `ServerStatusEventArgs` model as
their own new feature. These are the same classes our status fixes build on — but they were
added to upstream *after* our branch was cut, not taken from our code.

### Our additions that are still absent from upstream

Every fix in this file remains absent from upstream `main`:

| Change | Status in upstream |
|---|---|
| `ConfigurationManager` mod load order bug (`ToArray()` fix) | ❌ Upstream still has the buggy index loop |
| `ProcessManager` `BackgroundWorker` promoted to field | ❌ Upstream still uses local variable |
| `ProcessManager` `m_steamCmdUpdateProcess.Dispose()` | ❌ Missing |
| `ProcessManager` `m_serverProcess.Dispose()` in stop paths | ❌ Missing |
| `ProcessManager.Dispose()` method | ❌ Not present |
| `Forms/Main.cs` event handler unsubscription in `OnFormClosing` | ❌ Not present |
| `Forms/Main.cs` 500 ms status refresh timer | ❌ Not present |
| Timer passes last-known status (prevents field flicker) | ❌ Not present |
| `ServerStatusParser.GetCurrentStatus()` public getter | ❌ Not present |
| `Managers/RconManager.cs` BattlEye RCON client | ❌ Not present |
| Interval restart feature | ❌ Not present |

### One conflict to note for the PR

`AdvancedServerParameterEnumerated.cs` diverged from both the original and our fix.
The author changed the `ParameterValue` getter to `get => parameterValue`, which returns
the entire `ComboBox` control. Our fix returns `(string) parameterValue.SelectedItem`,
which is the semantically correct value. The author's version would throw
`InvalidCastException` anywhere the result is treated as a string — they appear to have
worked around it by calling `SelectedItem` directly at every call site instead. Our version
fixes the root cause; their version works around it. Both approaches are valid but they will
conflict on merge — the reviewer should pick one.

---

## Files Changed

| File | Type |
|---|---|
| `Components/AdvancedServerParameterEnumerated.cs` | Bug fix |
| `Managers/ConfigurationManager.cs` | Bug fix — mod load order preserved on repeated loads |
| `Managers/ProcessManager.cs` | Memory leak fixes + interval restart |
| `Managers/RconManager.cs` | **New file** — BattlEye RCON client |
| `Forms/Main.cs` | Event cleanup + interval restart UI + status timer + UI persistence fix |
| `Models/SavedState.cs` | New persisted setting for interval restart |
| `Utils/ServerStatusParser.cs` | Added public getter for current status (UI persistence fix) |

---

## Testing

1. **ComboBox fix** — open Advanced settings, change any enumerated parameter (e.g. freeze
   check mode), save, close, reopen — value should persist without exception.

2. **Memory leaks** — start and stop the server several times; Task Manager should show
   stable handle count for `Longbow.exe` between cycles.

3. **Interval restart** — enable RCON in the server JSON, check "Interval Restart (hours)",
   set to 1 hour, start the server. At T-10 / T-5 / T-1 minutes before the hour, connected
   players receive in-game chat warnings. Server stops and restarts automatically.

4. **Status tab** — start the server, switch to Status tab. FPS and memory readings should
   update smoothly roughly twice per second rather than only when new log lines arrive.

5. **Mod load order** — load any saved config, note the mod order. Load the same config a
   second time without restarting the app. The order should be identical both times and
   match the JSON file exactly.
