# SpareTimeGaming — Remote Server Management Guide

This guide covers the full picture: what the Scenario Manager web app does today,
how the end-to-end workflow for switching scenarios and restarting the server works,
and how to extend it for fully automated remote control.

> **Requires Longbow with the May 2026 patches applied.**
> A bug in `ConfigurationManager.cs` caused mod load order to be scrambled on every
> config load after the first one in a session. This made remote scenario switching
> unreliable — the second switch would load mods in the wrong order, breaking
> dependencies and crashing the server on startup. The patched version fixes this so
> every load, regardless of how many switches you've done, produces the exact order
> from the JSON file. Remote management is only reliable with this fix in place.

---

## Architecture Overview

```
[ You — Phone/PC anywhere ]
         |
         | HTTPS (Cloudflare Tunnel, IP hidden)
         v
[ Unraid — scenario-manager Docker container ]
         |
    (1) Reads/writes scenario JSON files from shared folder
    (2) SSH into Windows machine to trigger restarts
         |
         v
[ Windows 11 PC — Longbow + Arma Reforger Server ]
    D:\Longbow\scenarios\Production\   ← scenario JSON files
    D:\Longbow\Longbow.exe             ← server manager GUI
    ArmaReforgerServer.exe             ← game server process
```

The Unraid scenarios folder (`/mnt/user/scenarios`) is mapped to
`D:\Longbow\scenarios\Production\` via a Windows network share (SMB).

---

## Part 1 — What the Web App Does Today

### Login
Navigate to your Cloudflare Tunnel URL (e.g. `https://scenarios.sparetimegaming.com`).
Enter your username and password. Sessions persist until you log out.

### View Scenarios
The dashboard lists every `.json` file in the scenarios folder with:
- Server name
- Map / scenario ID
- Mod count
- Max players
- File size and last modified date
- Active badge (green) on the currently selected scenario

### Upload a Scenario
Drag-and-drop a `.json` file onto the upload zone, or click to browse.
The app validates JSON structure before saving. Only `.json` files are accepted.

### Set Active
Click **Set Active** on any scenario. This marks that scenario in the database
as the current one and highlights it with a green badge.

> **Note:** "Set Active" records your intent in the database. To actually apply
> it to the running server, continue to Part 2 (current manual workflow) or
> Part 3 (automated restart setup).

### Download a Scenario
Click **Download** to save the JSON to your device. You can then load it into
Longbow via **Load Config** on the configuration tab.

### Delete a Scenario
Click **Delete** to permanently remove the file. Active scenarios cannot be
deleted until another is set active.

### Optimize a Scenario
Click **Optimize** to strip cosmetic/visual mods from a scenario and save a
`_optimized` copy. Shows before/after mod count and reduction percentage.
Use this to create a lighter config for console-friendly sessions.

---

## Part 2 — Current Workflow: Switching Scenarios

Until automated restart is configured (Part 3), the workflow is:

```
1. Log into the web app from anywhere
2. Upload your new scenario JSON (if it isn't already there)
3. Click "Set Active" on the scenario you want
4. Click "Download" to get the JSON
5. Open Longbow on the server PC
6. Click "Load Config" and select the downloaded JSON
7. Stop the server (if running)
8. Start the server
```

**When to use this workflow:**
- Scheduled scenario rotations (planned in advance)
- Testing a new scenario during low-population hours
- Recovering from a crash with a different config

---

## Part 3 — Automated Remote Control (Full Setup)

This extends the web app so that clicking **Switch & Restart** in the browser
actually stops the running server, swaps the scenario, and restarts — all
without touching the Windows PC.

### How It Works

```
Browser → Web App → SSH → Windows PowerShell script → Longbow/Arma
```

1. You click "Switch & Restart" in the web app
2. The web app copies the selected scenario JSON to the production folder
3. The web app SSHs into the Windows machine
4. PowerShell kills the Arma server process gracefully (RCON shutdown)
5. PowerShell copies the new scenario to the active config location
6. PowerShell restarts Longbow or the server directly

### Step 1 — Enable OpenSSH Server on Windows

On the Windows 11 PC, open **PowerShell as Administrator**:

```powershell
# Install OpenSSH Server
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

# Start the service
Start-Service sshd

# Set to start automatically
Set-Service -Name sshd -StartupType Automatic

# Allow through Windows Firewall (usually done automatically)
New-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' -DisplayName 'OpenSSH Server (sshd)' `
  -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

Verify it's running:
```powershell
Get-Service sshd
```

### Step 2 — Set Up SSH Key Auth from Unraid to Windows

On your **Unraid server** (SSH into Unraid first):

```bash
# Generate a key pair (no passphrase — needed for automation)
ssh-keygen -t ed25519 -f /root/.ssh/longbow_control -N ""

# Copy the public key
cat /root/.ssh/longbow_control.pub
```

On the **Windows PC**, open PowerShell as Administrator and add the public key:

```powershell
# Create the authorized_keys file for Administrator
$sshDir = "C:\ProgramData\ssh"
New-Item -ItemType Directory -Force -Path $sshDir

# Paste your public key from above between the quotes
$pubKey = "ssh-ed25519 AAAA... your-key-here"
Add-Content -Path "$sshDir\administrators_authorized_keys" -Value $pubKey

# Set correct permissions (critical — SSH will reject wrong permissions)
icacls "$sshDir\administrators_authorized_keys" /inheritance:r
icacls "$sshDir\administrators_authorized_keys" /grant "SYSTEM:R"
icacls "$sshDir\administrators_authorized_keys" /grant "Administrators:R"
```

Test from Unraid:
```bash
ssh -i /root/.ssh/longbow_control vpira@192.168.8.201 "echo connected"
```

You should see `connected` with no password prompt.

### Step 3 — Create the Restart Script on Windows

Save this as `D:\Longbow\restart_with_scenario.ps1`:

```powershell
param(
    [string]$ScenarioFile
)

$LongbowDir    = "D:\Longbow"
$ProductionDir = "$LongbowDir\scenarios\Production"
$ServerExe     = "ArmaReforgerServer.exe"
$LongbowExe    = "Longbow.exe"
$RconPort      = 19999
$RconPassword  = "Longbow"

# --- Graceful shutdown via RCON ---
function Send-RconShutdown {
    try {
        Add-Type -AssemblyName System.Net
        $udp = New-Object System.Net.Sockets.UdpClient
        $udp.Connect("127.0.0.1", $RconPort)
        $udp.Client.ReceiveTimeout = 3000

        function Get-Crc32([byte[]]$data, [int]$offset, [int]$length) {
            $crc = [uint32]0xFFFFFFFF
            for ($i = $offset; $i -lt $offset + $length; $i++) {
                $crc = $crc -bxor $data[$i]
                for ($j = 0; $j -lt 8; $j++) {
                    if ($crc -band 1) { $crc = ($crc -shr 1) -bxor 0xEDB88320 }
                    else              { $crc = $crc -shr 1 }
                }
            }
            return $crc -bxor 0xFFFFFFFF
        }

        function Build-Packet([byte]$type, [byte[]]$payload) {
            $packet = New-Object byte[] (8 + $payload.Length)
            $packet[0] = 0x42; $packet[1] = 0x45
            $packet[6] = 0xFF; $packet[7] = $type
            [Array]::Copy($payload, 0, $packet, 8, $payload.Length)
            $crc = Get-Crc32 $packet 6 ($packet.Length - 6)
            $packet[2] = $crc -band 0xFF
            $packet[3] = ($crc -shr 8) -band 0xFF
            $packet[4] = ($crc -shr 16) -band 0xFF
            $packet[5] = ($crc -shr 24) -band 0xFF
            return $packet
        }

        $pwBytes = [System.Text.Encoding]::UTF8.GetBytes($RconPassword)
        $udp.Send((Build-Packet 0x00 $pwBytes), (Build-Packet 0x00 $pwBytes).Length) | Out-Null
        $udp.Receive([ref](New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0))) | Out-Null

        $cmd = [System.Text.Encoding]::UTF8.GetBytes("#shutdown")
        $payload = @([byte]0x00) + $cmd
        $udp.Send((Build-Packet 0x01 $payload), (Build-Packet 0x01 $payload).Length) | Out-Null
        $udp.Close()
        Write-Output "RCON shutdown sent."
    } catch {
        Write-Output "RCON shutdown failed (server may already be stopped): $_"
    }
}

# 1. Warn players and send RCON shutdown
Write-Output "Sending RCON shutdown..."
Send-RconShutdown

# 2. Wait up to 30 seconds for the server to exit gracefully
$wait = 0
while ((Get-Process -Name $ServerExe -ErrorAction SilentlyContinue) -and $wait -lt 30) {
    Start-Sleep -Seconds 2
    $wait += 2
}

# 3. Force-kill if still running
if (Get-Process -Name $ServerExe -ErrorAction SilentlyContinue) {
    Write-Output "Force-killing server process..."
    Stop-Process -Name $ServerExe -Force
    Start-Sleep -Seconds 2
}

# 4. Copy the selected scenario to the active config
if ($ScenarioFile) {
    $src = "$ProductionDir\$ScenarioFile"
    $dst = "$LongbowDir\server.json"
    if (Test-Path $src) {
        Copy-Item $src $dst -Force
        Write-Output "Active scenario set to: $ScenarioFile"
    } else {
        Write-Output "WARNING: Scenario file not found: $src"
    }
}

# 5. Restart the server via Longbow's scheduled task (or directly)
$task = Get-ScheduledTask -TaskName "ArmaReforgerServer" -ErrorAction SilentlyContinue
if ($task) {
    Start-ScheduledTask -TaskName "ArmaReforgerServer"
    Write-Output "Server restarted via scheduled task."
} else {
    # Fallback: launch server directly (adjust path as needed)
    $armaPath = (Get-ChildItem "D:\steam\steamapps\common" -Recurse -Filter "ArmaReforgerServer.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName
    if ($armaPath) {
        Start-Process $armaPath -ArgumentList "-config `"$dst`"" -WindowStyle Hidden
        Write-Output "Server started directly."
    } else {
        Write-Output "ERROR: Could not find ArmaReforgerServer.exe. Start manually."
    }
}

Write-Output "Done."
```

Test it manually from Windows first:
```powershell
D:\Longbow\restart_with_scenario.ps1 -ScenarioFile "PVE_North_Carolina_v1.2.json"
```

### Step 4 — Add the API Endpoint to the Web App

Add these routes to `app.py` (before the `if __name__ == '__main__':` line):

```python
import subprocess

WINDOWS_HOST = os.environ.get('WINDOWS_HOST', '192.168.8.201')
WINDOWS_USER = os.environ.get('WINDOWS_USER', 'vpira')
SSH_KEY_PATH = os.environ.get('SSH_KEY', '/root/.ssh/longbow_control')
RESTART_SCRIPT = r'D:\Longbow\restart_with_scenario.ps1'

@app.route('/api/server/restart', methods=['POST'])
@login_required
def api_restart():
    """Restart the server, optionally switching to a new scenario first."""
    data = request.get_json() or {}
    scenario = data.get('scenario_name', '')  # optional

    try:
        cmd = [
            'ssh', '-i', SSH_KEY_PATH,
            '-o', 'StrictHostKeyChecking=no',
            '-o', 'ConnectTimeout=10',
            f'{WINDOWS_USER}@{WINDOWS_HOST}',
            f'powershell -NonInteractive -File "{RESTART_SCRIPT}"'
            + (f' -ScenarioFile "{scenario}"' if scenario else '')
        ]

        result = subprocess.run(cmd, capture_output=True, text=True, timeout=60)
        output = result.stdout + result.stderr

        if result.returncode == 0:
            if scenario:
                set_active_scenario(scenario)
            logger.info(f"User {session.get('username')} restarted server"
                        + (f" with scenario {scenario}" if scenario else ""))
            return jsonify({'success': True, 'output': output})
        else:
            logger.error(f"Restart failed: {output}")
            return jsonify({'error': 'Restart command failed', 'output': output}), 500

    except subprocess.TimeoutExpired:
        return jsonify({'error': 'SSH command timed out'}), 504
    except Exception as e:
        logger.error(f"Restart error: {str(e)}")
        return jsonify({'error': str(e)}), 500


@app.route('/api/server/status', methods=['GET'])
@login_required
def api_server_status():
    """Check if the Arma server process is running on the Windows host."""
    try:
        cmd = [
            'ssh', '-i', SSH_KEY_PATH,
            '-o', 'StrictHostKeyChecking=no',
            '-o', 'ConnectTimeout=5',
            f'{WINDOWS_USER}@{WINDOWS_HOST}',
            'powershell -NonInteractive -Command '
            '"(Get-Process ArmaReforgerServer -ErrorAction SilentlyContinue) -ne $null"'
        ]
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=10)
        running = result.stdout.strip().lower() == 'true'
        return jsonify({'running': running})
    except Exception as e:
        return jsonify({'running': False, 'error': str(e)})
```

### Step 5 — Update `docker-compose.yml`

Add the new environment variables:

```yaml
environment:
  - SECRET_KEY=your-secret-key-here
  - SCENARIOS_PATH=/scenarios
  - WINDOWS_HOST=192.168.8.201
  - WINDOWS_USER=vpira
  - SSH_KEY=/root/.ssh/longbow_control

volumes:
  - /mnt/user/scenarios:/scenarios
  - /root/.ssh/longbow_control:/root/.ssh/longbow_control:ro
  - /root/.ssh/longbow_control.pub:/root/.ssh/longbow_control.pub:ro
```

Restart the container:
```bash
docker-compose down && docker-compose up -d
```

---

## Part 4 — API Reference

All endpoints require login (session cookie). Base URL is your Cloudflare Tunnel domain.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/` | Dashboard (HTML) |
| GET | `/api/scenarios` | List all scenarios |
| GET | `/api/scenarios/active` | Get active scenario name |
| POST | `/api/scenarios/set-active` | Mark scenario as active (DB only) |
| POST | `/api/scenarios/upload` | Upload a new scenario JSON |
| GET | `/api/scenarios/download/<file>` | Download a scenario JSON |
| DELETE | `/api/scenarios/delete/<file>` | Delete a scenario |
| POST | `/api/scenarios/optimize/<file>` | Create optimized copy of scenario |
| POST | `/api/server/restart` | Restart server (optional scenario switch) |
| GET | `/api/server/status` | Check if server process is running |
| GET | `/health` | Health check (no auth required) |

### POST `/api/scenarios/set-active`
```json
{ "scenario_name": "PVE_North_Carolina_v1.2.json" }
```

### POST `/api/server/restart`
```json
{ "scenario_name": "PVE_North_Carolina_v1.2.json" }
```
`scenario_name` is optional. If omitted, restarts the server with the current config.
Returns `{ "success": true, "output": "..." }` with the PowerShell script output.

---

## Part 5 — Full Remote Workflow (After Setup)

```
1. Log in at https://scenarios.sparetimegaming.com
2. Upload new scenario JSON (if needed)
3. Click "Switch & Restart" next to the scenario you want
   → Web app SSHs to Windows
   → PowerShell sends RCON shutdown warning
   → Server stops gracefully
   → New scenario is set as active config
   → Server restarts automatically
4. Server is live with the new scenario in ~2-3 minutes
5. Announce in Discord
```

No one needs to be at the Windows PC. The whole thing runs from a phone.

---

## Part 6 — Troubleshooting

### SSH connection refused
```bash
# From Unraid, test SSH manually
ssh -i /root/.ssh/longbow_control -v vpira@192.168.8.201 "echo ok"
```
- Verify `sshd` service is running on Windows: `Get-Service sshd`
- Check Windows Firewall allows port 22
- Confirm `administrators_authorized_keys` permissions are correct (see Step 2)

### Restart script runs but server doesn't come back up
- Check if the scheduled task exists: `Get-ScheduledTask -TaskName "ArmaReforgerServer"`
- If using the scheduled task approach, verify the task is enabled and not disabled
- Check server logs at `D:\Longbow\` for startup errors

### RCON shutdown fails
- Verify RCON is enabled in the active scenario JSON (`rcon.port: 19999`)
- Confirm the server is actually running before sending the command
- The script falls through to force-kill if RCON fails — this is expected behavior

### Web app can't reach Windows
- The Unraid Docker container and Windows must be on the same LAN
- Verify with: `docker exec scenario-manager ping 192.168.8.201`
- Update `WINDOWS_HOST` in `docker-compose.yml` if the IP has changed

### Wrong scenario loaded after restart
- Verify the scenario file is in `/mnt/user/scenarios/` (Unraid) which maps to `D:\Longbow\scenarios\Production\` (Windows)
- The restart script copies the selected file to `D:\Longbow\server.json` — confirm that path matches what Longbow/the scheduled task uses

---

## Security Notes

- The SSH private key is read-only inside the container (`ro` mount)
- PowerShell execution is non-interactive (`-NonInteractive -File`)
- The restart script only reads from the Production folder — WIP files cannot be loaded remotely
- Cloudflare Tunnel hides your home IP from the public internet
- Change `SECRET_KEY` in `docker-compose.yml` to a unique random string before going live
- Use a strong password for all web app user accounts
