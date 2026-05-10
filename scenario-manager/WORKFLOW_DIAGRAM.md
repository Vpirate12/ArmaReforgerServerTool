# Scenario Optimization Workflow

## The Complete Flow

```
USER UPLOADS SCENARIO
         ↓
  webapp receives JSON
         ↓
   [Can now click "Optimize"]
         ↓
  Backend analyzes JSON:
  - Identifies cosmetic mods (clothing, textures, skins, etc.)
  - Removes them from scenario
  - Saves as: {name}_optimized.json
         ↓
  webapp displays results:
  - Mods removed: 31
  - Reduction: 14.7%
  - New file: available to select/download
         ↓
  USER CAN NOW:
  ├─ Set as active (replace current server scenario)
  ├─ Download optimized version
  └─ Delete (keep original or optimized)
```

## User Experience

### Before (Manual)
```
1. Edit JSON in text editor (error-prone)
2. Manually identify cosmetic mods
3. Remove them from mod list
4. Save new file
5. Test on server
6. Hope it works
```

### After (Automated)
```
1. Upload JSON to webapp
2. Click "Optimize"
3. Get instant results
4. One click to activate
5. Done
```

## File Flow

```
D:\Longbow\
├── scenarios/
│   ├── Production/          ← Finalized scenarios
│   │   ├── PVE_NC_Lite.json (211 mods)
│   │   └── PVE_Arland_RHS.json (220 mods)
│   │
│   └── [webapp scenarios folder]
│       ├── PVE_NC_Lite.json (211 mods) [original]
│       │       ↓ [optimize button clicked]
│       │       ↓ [cosmetic mods removed]
│       └── PVE_NC_Lite_optimized.json (180 mods) [optimized]
│
└── scenario-manager/
    ├── app.py              ← NEW: /api/scenarios/optimize endpoint
    └── templates/
        └── dashboard.html  ← NEW: Optimize button & results UI
```

## Database Tracking

```sql
optimizations table:
┌─────────────────────────────────────────────────────────┐
│ id │ original_file      │ optimized_file              │
├─────────────────────────────────────────────────────────┤
│ 1  │ PVE_NC_Lite.json   │ PVE_NC_Lite_optimized.json  │
│ 2  │ PVE_Arland_RHS.json│ PVE_Arland_RHS_optimized.json
│ 3  │ Custom_v1.json     │ Custom_v1_optimized.json    │
├─────────────────────────────────────────────────────────┤
│ original_mod_count │ optimized_mod_count │ reduction_% │
├─────────────────────────────────────────────────────────┤
│ 211                │ 180                 │ 14.7       │
│ 220                │ 190                 │ 13.6       │
│ 145                │ 135                 │ 6.9        │
└─────────────────────────────────────────────────────────┘
```

## API Endpoints Summary

### Old Endpoints (Still Work)
- `POST /api/scenarios/upload` - Upload scenario
- `GET /api/scenarios` - List all scenarios
- `POST /api/scenarios/set-active` - Set as active
- `GET /api/scenarios/download/{filename}` - Download
- `DELETE /api/scenarios/delete/{filename}` - Delete

### NEW Endpoint
- `POST /api/scenarios/optimize/{filename}` ← **HERE!**
  - Input: Any uploaded scenario JSON
  - Output: Optimized JSON + stats
  - Removes cosmetic mods automatically

## Optimization Logic

```python
cosmetic_keywords = [
    'apparel', 'clothing', 'uniform',     # Clothing/appearance
    'gear', 'patch', 'visual',            # Visual elements
    'skin', 'camo', 'paint', 'texture',   # Textures
    'deco', 'prop', 'sign', 'object',     # Decorative
    'decal', 'retexture', 'enhancement'   # Enhancement/retexture
]

for each mod in scenario:
    if any(keyword in mod_name.lower()):
        mark for removal
    else:
        keep it

result = {
    mods: [kept mods only],
    removed: [list of removed mod IDs],
    reduction_percent: calculation
}
```

## Deployment Architecture

```
┌─────────────────────────────────────────────────┐
│          Unraid Server (192.168.8.124)         │
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌─────────────────────────────────────────┐   │
│  │ Docker Container: scenario-manager:5000 │   │
│  ├─────────────────────────────────────────┤   │
│  │                                         │   │
│  │  Flask App (app.py)                     │   │
│  │  ├─ /login - Authentication             │   │
│  │  ├─ /dashboard - Web UI                 │   │
│  │  ├─ /api/scenarios/* - List/upload      │   │
│  │  ├─ /api/scenarios/set-active - Switch  │   │
│  │  └─ /api/scenarios/optimize/* ← NEW!    │   │
│  │                                         │   │
│  │  Database: scenarios.db (SQLite)        │   │
│  │  ├─ users                              │   │
│  │  ├─ active_scenario                    │   │
│  │  └─ optimizations ← NEW!                │   │
│  │                                         │   │
│  └─────────────────────────────────────────┘   │
│         ↓ (reads/writes)                      │
│  ┌─────────────────────────────────────────┐   │
│  │  Shared Volume: /scenarios/             │   │
│  │  (points to D:\Longbow/scenarios/)      │   │
│  │                                         │   │
│  │  ├─ PVE_NC_Lite.json                   │   │
│  │  ├─ PVE_NC_Lite_optimized.json         │   │
│  │  ├─ PVE_Arland_RHS.json                │   │
│  │  └─ [other scenarios]                  │   │
│  │                                         │   │
│  └─────────────────────────────────────────┘   │
│                                                 │
└─────────────────────────────────────────────────┘
         ↑
    User Browser
 (access via webapp)
```

## Success Criteria

✅ Upload JSON to webapp
✅ Click "Optimize" button
✅ Get optimized JSON with removed cosmetic mods
✅ Can select optimized version as active
✅ Can download optimized version
✅ Original stays intact
✅ Tracks optimization history in database

---

## Performance

- **Upload**: <1 second
- **Optimization**: <1 second (just JSON parsing & filtering)
- **Display**: Instant refresh
- **Database**: Lightweight SQLite, no performance impact
