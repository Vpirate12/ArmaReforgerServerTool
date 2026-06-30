# Feature Request — Scheduled Scenario Rotation

**Type:** New Feature  
**Depends on:** Bug fix in `ConfigurationManager.cs` (mod load order scramble on repeated
config loads). Without that fix, each rotation cycle after the first will produce a
scrambled mod list and likely crash the server on startup.

---

## Overview

Add a **Scenario Rotation** section to the Advanced panel that lets the server
automatically cycle through a list of scenarios on a scheduled interval. The user
builds the rotation list by picking from the existing Scenario Selector (the same
browser already used for the main scenario picker), sets a duration per slot in hours,
and enables it with a single checkbox. When the server is running with rotation enabled,
it counts down, warns players via RCON, swaps the scenario, and restarts automatically.

---

## User-Facing Behaviour

```
Advanced Panel
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
☑  Scenario Rotation
   ┌─────────────────────────────────────┬──────┐
   │ Scenario                            │  Hrs │
   ├─────────────────────────────────────┼──────┤
   │ North Carolina - Conflict PvE       │   6  │
   │ Conflict - Everon                   │   4  │
   │ Arland PVE                          │   8  │
   └─────────────────────────────────────┴──────┘
   [ Add ]  [ Remove ]  [ ▲ ]  [ ▼ ]
   (hint: uses same scenario browser as Select Scenario)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

- **Add** — opens a compact version of the existing `ScenarioSelector` form.
  Selecting a scenario adds it to the bottom of the rotation list with a default
  duration of 4 hours.
- **Remove** — removes the selected row.
- **▲ / ▼** — reorder slots.
- **Hrs column** — inline editable numeric spinner (1–24).
- The rotation list and the enabled state persist in `state.json`.
- Rotation starts from slot 0 when the server starts. If the server is stopped
  and restarted it begins again from slot 0.
- RCON countdown warnings (10 min / 5 min / 1 min) are sent before each switch,
  reusing the existing `RconManager.SendBroadcastAsync()` infrastructure.
- If RCON is not enabled, the countdown warnings are skipped but the rotation
  still fires.
- The rotation is independent of Interval Restart. Both can be active at once,
  though in practice only one makes sense.

---

## Files to Add

### `Models/ScenarioRotationEntry.cs` (new)

```csharp
namespace Longbow.Models
{
  internal class ScenarioRotationEntry
  {
    public string ScenarioName { get; set; }  // friendly display name
    public string ScenarioPath { get; set; }  // scenarioId path e.g. {GUID}Missions/foo.conf
    public int    DurationHours { get; set; } = 4;

    public ScenarioRotationEntry() { }

    public ScenarioRotationEntry(string name, string path, int hours = 4)
    {
      ScenarioName  = name;
      ScenarioPath  = path;
      DurationHours = hours;
    }
  }
}
```

---

## Files to Modify

### 1. `Models/SavedState.cs`

Add a new property for the rotation list:

```csharp
// Alongside the existing advancedSettings dictionary:
public List<ScenarioRotationEntry> scenarioRotation { get; set; } = new();
```

Add a static default constant:

```csharp
public static readonly bool DEFAULT_SCENARIO_ROTATION_ENABLED = false;
```

### 2. `Utils/JsonUtils.cs` — `SavedStateConverter`

**Important:** The current `default:` case in `SavedStateConverter.Read()` throws
`JsonException` on unknown properties. Change it to `reader.Skip()` so that older
`state.json` files (without `scenarioRotation`) load without error, and future
unknown fields are tolerated gracefully:

```csharp
// Before
default:
    throw new JsonException($"Unexpected property: {propertyName}");

// After
default:
    reader.Skip();
    break;
```

Add the new `scenarioRotation` case to the `Read()` switch and the `Write()` method:

```csharp
// In Read() switch
case "scenarioRotation":
    props.scenarioRotation = JsonSerializer.Deserialize<List<ScenarioRotationEntry>>(
        ref reader, options) ?? new();
    break;

// In Write()
writer.WritePropertyName("scenarioRotation");
JsonSerializer.Serialize(writer, value.scenarioRotation, options);
```

### 3. `Managers/ProcessManager.cs`

Add a field alongside `m_intervalRestartCts`:

```csharp
private CancellationTokenSource? m_rotationCts;
```

Add public methods:

```csharp
public void ConfigureRotationTask(List<ScenarioRotationEntry> entries)
{
    m_rotationCts?.Cancel();
    m_rotationCts?.Dispose();
    m_rotationCts = new CancellationTokenSource();
    _ = RunRotationAsync(entries, m_rotationCts.Token);
    Log.Information("ProcessManager - Scenario rotation configured with {count} slot(s).", entries.Count);
    OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: Scenario rotation active — " +
        $"{entries.Count} scenario(s) queued.{Environment.NewLine}"));
}

public void CancelRotationTask()
{
    if (m_rotationCts == null) return;
    m_rotationCts.Cancel();
    m_rotationCts.Dispose();
    m_rotationCts = null;
    Log.Information("ProcessManager - Scenario rotation cancelled.");
    OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: Scenario rotation cancelled.{Environment.NewLine}"));
}

private static readonly (string Message, TimeSpan Wait)[] s_rotationWarnings =
{
    ("Server changing scenario in 10 minutes.", TimeSpan.FromMinutes(5)),
    ("Server changing scenario in 5 minutes.",  TimeSpan.FromMinutes(4)),
    ("Server changing scenario in 1 minute.",   TimeSpan.FromMinutes(1)),
};

private async Task RunRotationAsync(List<ScenarioRotationEntry> entries, CancellationToken ct)
{
    if (entries.Count == 0) return;
    int index = 0;

    while (!ct.IsCancellationRequested)
    {
        ScenarioRotationEntry current = entries[index];
        TimeSpan slotDuration = TimeSpan.FromHours(current.DurationHours);
        TimeSpan warningLeadTime = TimeSpan.FromMinutes(10);

        // Wait out the slot minus the warning lead-in
        TimeSpan initialWait = slotDuration - warningLeadTime;
        if (initialWait > TimeSpan.Zero)
        {
            try { await Task.Delay(initialWait, ct); }
            catch (OperationCanceledException) { return; }
        }

        // Countdown warnings
        foreach (var (msg, wait) in s_rotationWarnings)
        {
            if (ct.IsCancellationRequested) return;
            await RconManager.GetInstance().SendBroadcastAsync(msg);
            try { await Task.Delay(wait, ct); }
            catch (OperationCanceledException) { return; }
        }

        if (ct.IsCancellationRequested) return;

        // Advance to the next slot
        index = (index + 1) % entries.Count;
        ScenarioRotationEntry next = entries[index];

        Log.Information("ProcessManager - Rotation switching to scenario: {name}", next.ScenarioName);
        OnUpdateSteamCmdLogEvent(new($"{Utilities.GetTimestamp()}: Rotation — switching to " +
            $"\"{next.ScenarioName}\"...{Environment.NewLine}"));

        // Apply the next scenario and restart (triggered via event so Main.cs
        // can call PopulateServerConfiguration + StartStopServer on the UI thread)
        ScenarioRotationSwitchEventArgs switchArgs = new()
        {
            ScenarioName = next.ScenarioName,
            ScenarioPath = next.ScenarioPath
        };
        OnScenarioRotationSwitchEvent(switchArgs);
    }
}
```

Add the switch event so `Main.cs` can handle the config swap on the correct thread:

```csharp
internal class ScenarioRotationSwitchEventArgs : EventArgs
{
    public string ScenarioName { get; set; }
    public string ScenarioPath { get; set; }
}

public delegate void ScenarioRotationSwitchEventHandler(object sender, ScenarioRotationSwitchEventArgs e);
public event ScenarioRotationSwitchEventHandler ScenarioRotationSwitchEvent;

private void OnScenarioRotationSwitchEvent(ScenarioRotationSwitchEventArgs e)
    => ScenarioRotationSwitchEvent?.Invoke(this, e);
```

Extend `Dispose()` to cover `m_rotationCts`:

```csharp
public void Dispose()
{
    m_timerCancellationTokenSource.Dispose();
    m_intervalRestartCts?.Cancel();
    m_intervalRestartCts?.Dispose();
    m_rotationCts?.Cancel();     // ← add
    m_rotationCts?.Dispose();    // ← add
    m_worker?.Dispose();
    m_serverProcess.Dispose();
    m_steamCmdUpdateProcess.Dispose();
}
```

### 4. `Forms/Main.cs`

**a) Add a helper to the Scenario Selector so it can be opened in "add to rotation" mode**

The simplest approach is a new public constructor overload on `ScenarioSelector` that
accepts a callback instead of applying the scenario directly:

```csharp
// In ScenarioSelector.cs — new constructor
public ScenarioSelector(Action<Scenario> onSelected)
{
    InitializeComponent();
    // hide the "currently selected" label, change button text to "Add to Rotation"
    currentlySelectedLbl.Visible = false;
    selectScenarioBtn.Text = "Add to Rotation";
    m_onSelectedCallback = onSelected;
    m_getScenariosRequested = true;
    m_getScenariosThread = new(new ThreadStart(DoGetScenarios));
    m_getScenariosThread.Start();
}

// Modify SelectScenarioButtonClicked to call callback if present
private void SelectScenarioButtonClicked(object sender, EventArgs e)
{
    if (m_onSelectedCallback != null)
    {
        if (scenarioList.SelectedItem is Scenario s)
            m_onSelectedCallback(s);
        this.Close();
        return;
    }
    // ... existing logic unchanged ...
}
```

**b) Add the Scenario Rotation control in `CreateAdvancedServerParameterControls()`**

Add after the `intervalRestart` block:

```csharp
// --- Scenario Rotation ---
GroupBox rotationGroup = new()
{
    Text = "Scenario Rotation",
    AutoSize = true,
    Dock = DockStyle.Top,
    Padding = new Padding(8)
};

CheckBox rotationEnabled = new()
{
    Text = "Enable Scenario Rotation",
    Checked = savedState.scenarioRotation.Count > 0 && savedState.scenarioRotationEnabled,
    AutoSize = true
};

ListView rotationList = new()
{
    View = View.Details,
    FullRowSelect = true,
    Height = 120,
    Dock = DockStyle.Top
};
rotationList.Columns.Add("Scenario", 280);
rotationList.Columns.Add("Hours", 50);

foreach (var entry in savedState.scenarioRotation)
{
    var item = new ListViewItem(entry.ScenarioName);
    item.SubItems.Add(entry.DurationHours.ToString());
    item.Tag = entry;
    rotationList.Items.Add(item);
}

Button addBtn    = new() { Text = "Add",    Width = 60 };
Button removeBtn = new() { Text = "Remove", Width = 60 };
Button upBtn     = new() { Text = "▲",      Width = 36 };
Button downBtn   = new() { Text = "▼",      Width = 36 };

addBtn.Click += (s, e) =>
{
    ScenarioSelector picker = new ScenarioSelector((scenario) =>
    {
        var entry = new ScenarioRotationEntry(scenario.Name, scenario.Path, 4);
        var item  = new ListViewItem(entry.ScenarioName);
        item.SubItems.Add(entry.DurationHours.ToString());
        item.Tag = entry;
        rotationList.Items.Add(item);
    });
    picker.ShowDialog();
};

removeBtn.Click += (s, e) =>
{
    if (rotationList.SelectedItems.Count > 0)
        rotationList.Items.Remove(rotationList.SelectedItems[0]);
};

upBtn.Click += (s, e) =>
{
    if (rotationList.SelectedIndices.Count == 0) return;
    int i = rotationList.SelectedIndices[0];
    if (i == 0) return;
    var item = rotationList.Items[i];
    rotationList.Items.RemoveAt(i);
    rotationList.Items.Insert(i - 1, item);
    rotationList.Items[i - 1].Selected = true;
};

downBtn.Click += (s, e) =>
{
    if (rotationList.SelectedIndices.Count == 0) return;
    int i = rotationList.SelectedIndices[0];
    if (i == rotationList.Items.Count - 1) return;
    var item = rotationList.Items[i];
    rotationList.Items.RemoveAt(i);
    rotationList.Items.Insert(i + 1, item);
    rotationList.Items[i + 1].Selected = true;
};

// inline edit duration on double-click
rotationList.DoubleClick += (s, e) =>
{
    if (rotationList.SelectedItems.Count == 0) return;
    var item  = rotationList.SelectedItems[0];
    var entry = (ScenarioRotationEntry)item.Tag;
    string input = Microsoft.VisualBasic.Interaction.InputBox(
        "Duration in hours (1–24):", "Edit Duration", entry.DurationHours.ToString());
    if (int.TryParse(input, out int h) && h >= 1 && h <= 24)
    {
        entry.DurationHours = h;
        item.SubItems[1].Text = h.ToString();
    }
};

// store reference so StartServerBtnPressed can read it
m_rotationEnabledCheckBox = rotationEnabled;
m_rotationListView        = rotationList;

// layout buttons
FlowLayoutPanel btnRow = new() { AutoSize = true };
btnRow.Controls.AddRange(new Control[] { addBtn, removeBtn, upBtn, downBtn });

rotationGroup.Controls.Add(btnRow);
rotationGroup.Controls.Add(rotationList);
rotationGroup.Controls.Add(rotationEnabled);
advancedParametersPanel.Controls.Add(rotationGroup);
```

**c) Wire up in `StartServerBtnPressed()`**

```csharp
// After the intervalRestart block:
List<ScenarioRotationEntry> rotationEntries = m_rotationListView.Items
    .Cast<ListViewItem>()
    .Select(i => (ScenarioRotationEntry)i.Tag)
    .ToList();

bool rotationActive = m_rotationEnabledCheckBox.Checked && rotationEntries.Count > 0;
if (rotationActive)
{
    if (isStarting)
        ProcessManager.GetInstance().ConfigureRotationTask(rotationEntries);
    else
        ProcessManager.GetInstance().CancelRotationTask();
}
else if (!isStarting)
{
    ProcessManager.GetInstance().CancelRotationTask();
}
```

**d) Handle the scenario switch event in `Main.cs`**

Subscribe in the constructor (alongside the other event subscriptions):

```csharp
ProcessManager.GetInstance().ScenarioRotationSwitchEvent += HandleScenarioRotationSwitch;
```

Unsubscribe in `OnFormClosing`:

```csharp
ProcessManager.GetInstance().ScenarioRotationSwitchEvent -= HandleScenarioRotationSwitch;
```

Handler — runs on UI thread via `Invoke`, stops the server, loads the new scenario config,
and starts the server again:

```csharp
private void HandleScenarioRotationSwitch(object sender, ScenarioRotationSwitchEventArgs e)
{
    this.Invoke((MethodInvoker)(() =>
    {
        // Apply the new scenarioId to the loaded config
        ConfigurationManager.GetInstance()
            .GetServerConfiguration().root.game.scenarioId = e.ScenarioPath;

        // Stop → restart (reuses the existing toggle logic)
        ProcessManager.GetInstance().StartStopServer(triggeredByAutoRestart: true);
        _ = Task.Delay(ToolPropertiesManager.GetInstance()
                .GetToolProperties().autoRestartTime_ms)
            .ContinueWith(_ => ProcessManager.GetInstance()
                .StartStopServer(triggeredByAutoRestart: true));
    }));
}
```

**e) Persist rotation list in `SavedState` on save**

Wherever the existing save/load of `state.json` happens, add the rotation list:

```csharp
// When building the SavedState to write:
savedState.scenarioRotation = m_rotationListView.Items
    .Cast<ListViewItem>()
    .Select(i => (ScenarioRotationEntry)i.Tag)
    .ToList();
savedState.scenarioRotationEnabled = m_rotationEnabledCheckBox.Checked;
```

---

## Interaction with Interval Restart

Scenario rotation and interval restart are independent. If both are enabled, interval
restart will fire inside a rotation slot and restart the server without advancing the
scenario. Only the rotation timer advances the scenario. In practice most operators
will use one or the other.

---

## Forward-Compatibility Note

The `SavedStateConverter` currently throws `JsonException` on any unrecognized property
(the `default:` case). This means any future additions to `state.json` will break
existing builds loading old files. Changing `default: throw` to `default: reader.Skip()`
(described above) is low-risk and prevents this entire class of issue going forward.

---

## Summary of Files Changed

| File | Change |
|---|---|
| `Models/ScenarioRotationEntry.cs` | **New** — rotation slot model (name, path, hours) |
| `Models/SavedState.cs` | Add `scenarioRotation` list + `scenarioRotationEnabled` flag |
| `Utils/JsonUtils.cs` | Add rotation serialization; fix `default: throw` → `reader.Skip()` |
| `Managers/ProcessManager.cs` | Add `ConfigureRotationTask`, `CancelRotationTask`, `RunRotationAsync`, switch event |
| `Forms/ScenarioSelector.cs` | Add callback constructor for "add to rotation" mode |
| `Forms/Main.cs` | Rotation UI control, start/stop wiring, switch event handler, save/load |
