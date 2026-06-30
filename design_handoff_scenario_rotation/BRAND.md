# STG Brand System

> One-page reference for anyone — human or Claude Code — building an STG surface.
> Keep this file in the repo root. Point Claude Code at it at the start of any UI session:
> _"Follow BRAND.md."_

---

## 1. Names — use these exactly

| Thing | Canonical name | Notes |
|---|---|---|
| The company | **STG — Signal Tactical Group** | "STG" on second reference. Never "Spare Time Gaming" (legacy domain only). |
| Web panel | **Sitrep** | Flagship product. "Sitrep, an STG product." |
| Desktop server tool | **Sentinel Desktop** | Renamed from "Longbow." Retire all "Longbow" naming + logo. |
| The host agent | **Sentinel** | The service that runs on the game-server host. |

- Domain `spare-time-gaming.us` → keep as a **redirect only**. Primary should move to an STG domain.
- Backstory line (allowed): _"Built in our spare time. Self-hosted by design. Built by a veteran."_

---

## 2. Color tokens

One brand accent (sky blue). Everything else is neutral. Status colors signal status **only** — never decoration.

### Brand — sky blue
```
brand-300  #7DD3FC
brand-400  #38BDF8   ← primary accent (the STG blue)
brand-500  #0EA5E9   ← primary action / buttons
brand-600  #0284C7   ← button default (current Sitrep)
brand-700  #0369A1   ← hover
brand-800  #075985   ← active
brand-900  #0C4A6E
```

### Neutrals — deep slate (SteamOS-adjacent)
```
canvas     #05080C   ← app/page background
surface    #0E151D   ← cards, panels
raised     #18222E   ← raised surfaces, inputs
line       #25323F   ← borders, dividers
muted      #8A97A6   ← secondary text
faint      #5B6877   ← labels, captions, disabled
text       #E7EDF3   ← primary text
```
> The current Sitrep app uses Tailwind's `gray` scale (`gray-950 #030712` … `gray-100 #F3F4F6`). The slate values above warm it slightly toward Steam's blue-gray — adopt gradually; both are compatible.

### Semantic — use sparingly
```
online / ok        #34D399
offline / danger   #F87171
admin / warning    #FBBF24
```

---

## 3. Typography

| Role | Family | Weights | Use |
|---|---|---|---|
| Display | **Space Grotesk** | 600 / 700 | Headlines, wordmark, big numbers |
| UI & body | **Inter** | 400–700 | Interface, paragraphs, controls |
| Mono | **JetBrains Mono** | 400 / 500 | Labels, RCON, code, status, IDs |

- Headlines: tight tracking (`letter-spacing: -1px to -2px`), `line-height ~1.03`.
- Mono labels: uppercase, `letter-spacing: 1–3px`, often in `faint` or `brand-400`.

---

## 4. Shape, spacing, components

- **Radius:** buttons `8px` (lg) · cards/panels `16px` (xl) · status pills `9999px` (full).
- **Borders:** `1px solid line (#25323F)`. Active/brand state: `1px solid brand-400` + soft glow `box-shadow: 0 18px 50px -28px rgba(56,189,248,.5)`.
- **Buttons:** primary `bg brand-600`, hover `brand-700`, active `brand-800`, text white. Ghost `bg raised`, text `text`.
- **Status pill:** tinted bg at ~12% of the semantic color + 7px dot + mono/label text in that color.
- **Cards:** `surface` bg, `line` border, `16px` radius, ~30–36px padding.

---

## 5. Iconography & voice

- **Icons:** [lucide](https://lucide.dev) line icons, stroke 1.5. Sitrep mark = `Shield`. (Brand exploration uses an ascending-bars "signal" motif — optional.)
- **Voice:** plain English, jargon translated, calm and confident. De-mystify milsim/server jargon. Short sentences. No hype, no emoji.

---

## 6. Brand architecture

Endorsed house. STG is the visible parent; every product carries the STG signature.

```
STG — Signal Tactical Group   (master brand / endorser)
├── Sitrep            web panel · flagship   → "an STG product"
└── Sentinel Desktop  Windows host tool      → "part of Sitrep · by STG"
```

Always pair a product with its endorsement lockup somewhere on the surface
(header, footer, installer splash): the STG mark + "an STG product".
