# Railway.app License API Hosting Strategy

**Date Locked:** 2026-06-30  
**Platform:** Railway.app  
**Cost:** Free tier (~$0), upgrade to paid when needed (~$5-20/month)  
**HTTPS:** Automatic (Let's Encrypt via Railway)  
**Reliability:** 99.9% SLA

---

## Why Railway

| Criteria | Railway | Unraid | AWS |
|----------|---------|--------|-----|
| **Cost** | Free-$20/mo | $0 + electricity | Pay-per-use ($10+) |
| **Uptime** | 99.9% SLA | Home server (???) | 99.99% SLA |
| **HTTPS** | Automatic | Manual (Let's Encrypt) | Manual |
| **Secrets** | Built-in | Painful | Complex |
| **Scaling** | Automatic | Manual | Complex |
| **Ops Burden** | Zero | Medium-High | High |
| **Time to deploy** | 5 min | 30 min | 1 hour |

**Decision:** Railway eliminates infrastructure burden. You focus on building features, not monitoring servers.

---

## Phase 1B Implementation Plan

### Week 1-3: Railway Setup (Parallel with Tauri/React)

1. **Create Railway Account** (free)
   ```
   → railway.app
   → Sign in with GitHub
   → New Project
   ```

2. **Deploy License API Service**
   ```
   Language: Node.js (or Python)
   Repository: Sentinel-Desktop (GitHub)
   Branch: main
   
   Railway auto-detects package.json or requirements.txt
   → Builds on push
   → Deploys automatically
   ```

3. **Configure Environment**
   ```
   Railway dashboard:
   Variables tab
   
   Add secrets:
   - DATABASE_URL (PostgreSQL from Railway)
   - STRIPE_WEBHOOK_SECRET
   - LICENSE_API_KEY (for Tauri app calls)
   - JWT_SECRET
   ```

4. **Database: PostgreSQL (Free on Railway)**
   ```
   Railway dashboard:
   Add service → PostgreSQL
   
   Automatic backup, 1GB storage on free tier
   (upgrade to paid if needed)
   ```

5. **Domain & HTTPS (Automatic)**
   ```
   Railway provides:
   api-sentinel-desktop-prod.railway.app (automatic)
   
   OR use custom domain:
   api.sentinel-desktop.spare-time-gaming.us
   → Point DNS CNAME to Railway
   → HTTPS automatic (Let's Encrypt, Railway manages renewal)
   ```

---

## License API Architecture (Node.js/Express)

### Project Structure

```
Sentinel-Desktop/
├── license-api/
│   ├── package.json
│   ├── Dockerfile (optional, Railway auto-detects)
│   ├── railway.toml (optional config)
│   ├── .env.example
│   ├── src/
│   │   ├── index.js (Express server)
│   │   ├── routes/
│   │   │   ├── licenses.js (POST /v1/licenses/validate)
│   │   │   └── webhooks.js (POST /v1/webhooks/stripe)
│   │   ├── models/
│   │   │   ├── License.js (Prisma schema)
│   │   │   └── Customer.js
│   │   ├── utils/
│   │   │   ├── crypto.js (license key generation/validation)
│   │   │   └── stripe.js (webhook handling)
│   │   └── middleware/
│   │       └── auth.js (API key validation)
│   └── Procfile (Railway reads this for startup)
```

### Core Endpoints

**1. License Validation (Called by Tauri app)**
```javascript
// POST /v1/licenses/validate
{
  "license_key": "STG-PROF-XXXX-XXXX-XXXX",
  "machine_id": "w10-abc123def456",
  "app_version": "1.0.0"
}

Response:
{
  "valid": true,
  "tier": "professional",
  "expires_at": "2027-06-30T23:59:59Z",
  "features": ["multi-server", "audit-logging", "user-roles"],
  "offline_grace_until": "2026-07-07T23:59:59Z"
}
```

**2. Stripe Webhook (Generates license keys on purchase)**
```javascript
// POST /v1/webhooks/stripe
// Stripe webhook → payment_intent.succeeded
// → Generate license key
// → Store in database
// → Email user with key + download link
```

### Minimal Implementation (Phase 1B)

```javascript
// src/index.js
const express = require('express');
const { PrismaClient } = require('@prisma/client');
const stripe = require('stripe')(process.env.STRIPE_KEY);

const app = express();
const prisma = new PrismaClient();

app.use(express.json());

// License validation endpoint
app.post('/v1/licenses/validate', async (req, res) => {
  const { license_key, machine_id } = req.body;
  
  try {
    const license = await prisma.license.findUnique({
      where: { key: license_key }
    });
    
    if (!license || license.expires_at < new Date()) {
      return res.status(401).json({ valid: false });
    }
    
    // Log validation attempt
    await prisma.validationLog.create({
      data: { license_key, machine_id, timestamp: new Date() }
    });
    
    return res.json({
      valid: true,
      tier: license.tier,
      expires_at: license.expires_at,
      features: license.features,
      offline_grace_until: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: 'Validation failed' });
  }
});

// Stripe webhook
app.post('/v1/webhooks/stripe', async (req, res) => {
  const sig = req.headers['stripe-signature'];
  
  try {
    const event = stripe.webhooks.constructEvent(
      req.rawBody,
      sig,
      process.env.STRIPE_WEBHOOK_SECRET
    );
    
    if (event.type === 'payment_intent.succeeded') {
      const { metadata } = event.data.object;
      
      // Generate license key
      const licenseKey = generateLicenseKey(metadata.tier);
      
      // Store in database
      await prisma.license.create({
        data: {
          key: licenseKey,
          tier: metadata.tier,
          customer_id: metadata.customer_id,
          expires_at: new Date(Date.now() + 365 * 24 * 60 * 60 * 1000) // 1 year
        }
      });
      
      // Email user
      await sendEmail(metadata.email, licenseKey, 'download-link');
    }
    
    res.json({ received: true });
  } catch (error) {
    console.error(error);
    res.status(400).send('Webhook error');
  }
});

app.listen(process.env.PORT || 3000, () => {
  console.log('License API running');
});
```

### Prisma Schema (PostgreSQL)

```prisma
// prisma/schema.prisma
datasource db {
  provider = "postgresql"
  url      = env("DATABASE_URL")
}

generator client {
  provider = "prisma-client-js"
}

model License {
  id                 String   @id @default(uuid())
  key                String   @unique
  tier               String   // "trial" | "professional" | "enterprise" | "esports"
  customer_id        String
  customer_email     String
  expires_at         DateTime
  created_at         DateTime @default(now())
  revoked_at         DateTime?
  
  validations        ValidationLog[]
}

model ValidationLog {
  id             String   @id @default(uuid())
  license_id     String
  license        License  @relation(fields: [license_id], references: [id])
  machine_id     String
  app_version    String
  validated_at   DateTime @default(now())
}
```

---

## Deployment Steps

### Step 1: Connect GitHub to Railway

```
1. Go to railway.app
2. New Project → GitHub Repo
3. Select: Sentinel-Desktop
4. Authorize Railway to GitHub
5. Click "Deploy Now"
```

### Step 2: Railway Auto-Detects Configuration

```
Railway reads:
- package.json (npm install)
- Procfile (npm start)
- .env.example (loads into Variables)
- Dockerfile (optional, uses if present)

Auto-builds & deploys on every git push to main
```

### Step 3: Add PostgreSQL Database

```
1. Railway dashboard → Add Service
2. Select PostgreSQL
3. Railway auto-creates DATABASE_URL
4. Available in Variables for license-api service
```

### Step 4: Configure Secrets

```
Railway dashboard → Variables tab

DATABASE_URL=postgres://...  (auto-set by PostgreSQL service)
STRIPE_KEY=sk_live_...
STRIPE_WEBHOOK_SECRET=whsec_...
LICENSE_API_KEY=your-secret-key
JWT_SECRET=your-jwt-secret
```

### Step 5: Get Your Endpoint

```
Railway dashboard → Deployments

Your API lives at:
https://api-sentinel-desktop-prod.railway.app

OR custom domain:
https://api.sentinel-desktop.spare-time-gaming.us
(Point DNS CNAME to Railway, auto-HTTPS)
```

---

## Tauri App Integration

### License Validation Call (from Sentinel Desktop)

```rust
// src-tauri/src/license.rs
#[tauri::command]
pub async fn validate_license(license_key: String, machine_id: String) -> Result<LicenseData, String> {
    let client = reqwest::Client::new();
    let response = client
        .post("https://api.sentinel-desktop.spare-time-gaming.us/v1/licenses/validate")
        .json(&json!({
            "license_key": license_key,
            "machine_id": machine_id,
            "app_version": env!("CARGO_PKG_VERSION")
        }))
        .send()
        .await
        .map_err(|e| format!("Network error: {}", e))?;

    let license: LicenseData = response
        .json()
        .await
        .map_err(|e| format!("Invalid response: {}", e))?;

    if license.valid {
        Ok(license)
    } else {
        Err("Invalid license key".to_string())
    }
}
```

---

## Cost Breakdown

### Free Tier (Phase 1B Launch)
| Resource | Free Limit | Sufficient? |
|----------|-----------|------------|
| **Compute** | 5 GB-hours/month | Yes (light traffic) |
| **PostgreSQL** | 1 GB storage, 10GB bandwidth | Yes (MVP) |
| **Domains** | 1 free Railway domain | Yes (start here) |

**Cost: $0**

### Paid Tier (After Launch)
| Usage Level | Monthly Cost | When? |
|------------|-------------|-------|
| 1-100 paid users | ~$5-10/month | Q4 2026 |
| 100-1000 paid users | ~$20-50/month | 2027 |
| 1000+ paid users | ~$100+/month | 2028+ |

---

## Monitoring & Alerts

### Health Check (Optional, Free)

```javascript
// Add to Express app
app.get('/health', (req, res) => {
  res.json({ status: 'ok', timestamp: new Date() });
});

// Use free service: uptimerobot.com
// Ping /health every 5 min
// Alert to Discord if down
```

### Logging (Built-in to Railway)

```
Railway dashboard → Logs tab
→ See all requests, errors, crashes
→ CPU/memory usage
→ Deployments history
```

---

## Git Workflow for License API

### Phase 1B Development

```bash
# Week 1-3: Develop locally
cd Sentinel-Desktop/license-api
npm install
npm run dev  # Local testing

# Week 4+: Deploy to Railway
git push origin main
# Railway automatically rebuilds & deploys
# Check: railway.app dashboard → Deployments
```

### Database Migrations (Prisma)

```bash
# Create migration
npx prisma migrate dev --name add_license_table

# Commit to git
git add prisma/migrations/
git commit -m "Add license schema"
git push

# Railway auto-runs: npm run migrate (if in Procfile)
```

---

## Disaster Recovery

### Backup Strategy

**Railway handles backups automatically:**
- PostgreSQL snapshots daily
- 7-day retention
- Restore via Railway dashboard if needed

**You handle secrets:**
- Stripe keys in 1Password or similar
- DATABASE_URL can be recreated (it's in Railway)
- License keys are in database (backed up)

---

## Checklist: Phase 1B Infrastructure

**Week 1:**
- [ ] Create Railway account (free)
- [ ] Connect GitHub repository
- [ ] Deploy placeholder Node.js app
- [ ] Add PostgreSQL database
- [ ] Test: `curl https://api.sentinel-desktop.railway.app/health`

**Week 2-3:**
- [ ] Implement license validation endpoint
- [ ] Implement Stripe webhook
- [ ] Add Prisma schema
- [ ] Test locally (npm run dev)
- [ ] Deploy to Railway (git push)

**Week 4+:**
- [ ] Integrate with Tauri app
- [ ] Test end-to-end: purchase → key → validation
- [ ] Set up uptimerobot monitoring (optional)
- [ ] Document API in postman collection

---

## Summary

| Aspect | Railway Solution |
|--------|-----------------|
| **Cost** | Free-$20/month (vs. Unraid $0 + ops burden) |
| **Uptime** | 99.9% SLA (vs. Unraid ???) |
| **HTTPS** | Automatic (vs. Unraid manual Let's Encrypt) |
| **Scaling** | Automatic (vs. Unraid manual) |
| **Ops** | Zero (vs. Unraid monitoring + maintenance) |
| **Time to deploy** | 5 minutes (vs. Unraid 30+ minutes) |
| **When to abandon** | Never — scales with you indefinitely |

**Verdict: Railway is the boring, right choice.** It works. You focus on features. Everything else is ops theater.

---

**Status:** 🔒 LOCKED  
**Implementation:** Weeks 1-4 of Phase 1B  
**Estimated setup time:** 2-4 hours total
