# Quick Setup for Unraid Deployment

## Files in This Package

```
scenario-manager/
├── app.py                    # Flask backend
├── requirements.txt          # Python dependencies
├── docker-compose.yml        # Docker config
├── setup.py                  # User account creator
├── .env.example              # Environment template
├── README.md                 # Project info
├── DEPLOYMENT_GUIDE.md       # Full deployment guide
├── QUICK_SETUP.md            # This file
└── templates/
    ├── login.html            # Login page
    └── dashboard.html        # Main interface
```

## Quick Deploy (5 Minutes)

### 1. SFTP Files to Unraid

Upload all files to: `/mnt/user/appdata/scenario-manager/`

Use SFTP client (WinSCP, Cyberduck, etc):
- Host: `192.168.8.124`
- Username: `root`
- Password: Your Unraid admin password
- Target folder: `/mnt/user/appdata/scenario-manager/`

### 2. Create User Account

SSH into Unraid:
```bash
ssh root@192.168.8.124
```

Navigate to app folder:
```bash
cd /mnt/user/appdata/scenario-manager
```

Create your admin account:
```bash
docker run -it --rm \
  -v $(pwd):/app \
  python:3.11-slim \
  sh -c "cd /app && pip install werkzeug && python setup.py admin yourpassword123"
```

Replace `yourpassword123` with a real password.

### 3. Start the App

Still in the same folder:
```bash
docker-compose up -d
```

Wait 10 seconds for it to start.

### 4. Verify It's Running

```bash
docker logs scenario-manager
```

You should see: `WARNING: Running on http://0.0.0.0:5000`

### 5. Test Locally

Open your browser on any computer on your network:
```
http://192.168.8.124:5000
```

Login with:
- Username: `admin`
- Password: `yourpassword123`

You should see the dashboard with empty scenarios.

## Next Steps

Once you verify it works locally, follow `DEPLOYMENT_GUIDE.md` for Cloudflare Tunnel setup (secure external access).

## Troubleshooting

**Can't connect?**
```bash
# Check if container is running
docker ps | grep scenario-manager

# View logs
docker logs scenario-manager

# Restart
docker-compose restart
```

**Forgot your password?**
```bash
# Delete and recreate
docker run -it --rm \
  -v $(pwd):/app \
  python:3.11-slim \
  sh -c "cd /app && pip install werkzeug && python setup.py admin newpassword"
```

---

That's it! Once local access works, proceed to Cloudflare Tunnel.
