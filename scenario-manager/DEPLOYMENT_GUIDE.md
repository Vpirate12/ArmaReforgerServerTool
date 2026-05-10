# Scenario Manager - Deployment Guide

Complete guide to deploy the Spare Time Gaming Scenario Manager on your Unraid server with Cloudflare Tunnel.

---

## 📋 What This App Does

- **View all scenarios** with details (map, mod count, player count)
- **Upload new scenarios** (JSON files)
- **Mark a scenario as active** (your current scenario)
- **Download any scenario** to load in Longbow
- **Delete scenarios** you no longer need
- **Secure login** with username/password authentication

---

## 🚀 Quick Start (5 Minutes)

### Step 1: Prepare Your Unraid Server

1. On your Unraid server, create a folder for this app:
   ```
   /mnt/user/appdata/scenario-manager/
   ```

2. Create a scenarios folder where your JSON files will be stored:
   ```
   /mnt/user/scenarios/
   ```

3. Copy the scenario manager files into the appdata folder:
   - `app.py`
   - `requirements.txt`
   - `docker-compose.yml`
   - `setup.py`
   - `templates/` folder

### Step 2: Initialize the Database and Create Users

SSH into your Unraid server and run:

```bash
cd /mnt/user/appdata/scenario-manager
docker run -it --rm \
  -v $(pwd):/app \
  python:3.11-slim \
  sh -c "cd /app && pip install werkzeug && python setup.py <username> <password>"
```

Replace:
- `<username>` with the username (e.g., `aaron`)
- `<password>` with a secure password

**Example:**
```bash
python setup.py aaron mypassword123
```

**Create multiple users:**
```bash
python setup.py aaron password1
python setup.py discordowner password2
```

### Step 3: Start the Docker Container

From the app folder:

```bash
docker-compose up -d
```

This starts the app on `http://localhost:5000`

**Verify it's running:**
```bash
docker logs scenario-manager
```

You should see: `WARNING: Running on http://0.0.0.0:5000`

---

## 🔒 Setup Cloudflare Tunnel (Secure Internet Access)

This keeps your IP address hidden and provides a secure domain name.

### Step 1: Create a Cloudflare Account

1. Go to [https://dash.cloudflare.com](https://dash.cloudflare.com)
2. Sign up (free tier is fine)
3. Add your domain (e.g., `example.com`)
4. Update your domain's nameservers to Cloudflare (instructions provided)

**Don't have a domain?**
- Get a free subdomain from [cloudflare.one](https://cloudflare.one) or [noip.com](https://noip.com)

### Step 2: Install Cloudflared on Unraid

SSH into Unraid and install cloudflared:

```bash
# Download cloudflared for ARM (Unraid uses ARM architecture)
curl -L https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-linux-arm64 -o /usr/local/bin/cloudflared

chmod +x /usr/local/bin/cloudflared

cloudflared --version
```

### Step 3: Create Tunnel in Cloudflare

```bash
cloudflared tunnel login
```

This opens a browser to authenticate. Select your domain and authorize.

Create a tunnel named "scenario-manager":

```bash
cloudflared tunnel create scenario-manager
```

You'll see a Tunnel ID. Save this.

### Step 4: Configure the Tunnel

Create a config file: `/root/.cloudflare-warp/config.yml`

```bash
mkdir -p /root/.cloudflare-warp
```

Edit the file with:

```yaml
tunnel: scenario-manager
credentials-file: /root/.cloudflare-warp/TUNNEL_ID.json

ingress:
  - hostname: scenarios.example.com
    service: http://localhost:5000
  - service: http_status:404
```

Replace:
- `TUNNEL_ID` with your tunnel ID (from previous step)
- `scenarios.example.com` with your desired domain/subdomain

### Step 5: Route Traffic to the Tunnel

In Cloudflare dashboard:

1. Go to **DNS** > **Records**
2. Create a CNAME record:
   - Name: `scenarios`
   - Target: `<TUNNEL_ID>.cfargotunnel.com`
   - Proxy status: Proxied

(Or if using a subdomain you created, follow Cloudflare's UI)

### Step 6: Start the Tunnel

```bash
cloudflared tunnel run scenario-manager
```

Test the tunnel is working:

```bash
cloudflared tunnel info scenario-manager
```

### Step 7: Keep Tunnel Running (Systemd Service)

Create systemd service file: `/etc/systemd/system/cloudflare-tunnel.service`

```ini
[Unit]
Description=Cloudflare Tunnel for Scenario Manager
After=network.target

[Service]
Type=simple
User=root
WorkingDirectory=/root
ExecStart=/usr/local/bin/cloudflared tunnel run scenario-manager
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
```

Enable and start:

```bash
systemctl daemon-reload
systemctl enable cloudflare-tunnel.service
systemctl start cloudflare-tunnel.service
```

Check status:

```bash
systemctl status cloudflare-tunnel.service
```

---

## 🌐 Access Your App

Once everything is set up:

1. Go to `https://scenarios.example.com` (your Cloudflare domain)
2. Login with your username/password
3. Manage scenarios!

**The URL is secure and your IP is hidden.**

---

## 📝 Managing Scenarios

### Upload a Scenario
1. Drag and drop a JSON file onto the upload area, or click to browse
2. File is validated and saved

### Set a Scenario as Active
1. Click **Set Active** on any scenario
2. Active scenario is marked with a green badge
3. Download this JSON and load it in Longbow

### Download a Scenario
1. Click **Download** to get the JSON file
2. Load it in Longbow via "Load Config"

### Delete a Scenario
1. Click **Delete**
2. Confirm deletion
3. File is removed from the system

---

## 🔐 Security Notes

- **Change the SECRET_KEY** in `docker-compose.yml` to a random string
- **Use strong passwords** for user accounts
- **Cloudflare Tunnel** encrypts traffic end-to-end
- Users can only access scenarios via login
- All uploads are validated as JSON files

---

## 🛠️ Troubleshooting

### App won't start
```bash
# Check container logs
docker logs scenario-manager

# Restart container
docker-compose restart
```

### Can't access from outside
1. Verify tunnel is running: `systemctl status cloudflare-tunnel.service`
2. Check DNS resolves to tunnel: `nslookup scenarios.example.com`
3. Try accessing locally first: `http://localhost:5000`

### Forgot user password
Delete the user and create a new one:

```bash
sqlite3 /path/to/scenarios.db
DELETE FROM users WHERE username='old_user';
.quit

python setup.py newuser newpassword
```

### Tunnel stops running
```bash
systemctl restart cloudflare-tunnel.service
```

---

## 📦 Docker Commands Reference

```bash
# Start
docker-compose up -d

# Stop
docker-compose down

# View logs
docker logs scenario-manager

# Restart
docker-compose restart

# Remove everything (careful!)
docker-compose down -v
```

---

## 🎯 Workflow Example

1. You think of a new event for The Island
2. You log into `https://scenarios.example.com`
3. Upload the scenario JSON (or create one in Longbow and export)
4. Set it as "Active"
5. Download the active scenario JSON
6. Load it in Longbow via "Load Config"
7. Start the server
8. Announce in Discord!

---

## 📞 Support

If something breaks:
1. Check `docker logs scenario-manager`
2. Check `systemctl status cloudflare-tunnel.service`
3. Verify network connectivity: `ping scenarios.example.com`
4. Restart everything: `docker-compose restart && systemctl restart cloudflare-tunnel.service`

---

**That's it! You now have a secure, cloud-accessible scenario manager on your Unraid server.** 🎉
