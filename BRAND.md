# STG Brand System

> One-page reference for anyone — human or Claude Code — building an STG surface.
> Point Claude Code at it at the start of any UI session: "Follow BRAND.md."

## 1. Names — use these exactly
| Thing | Canonical name | Notes |
|---|---|---|
| The company | **STG — Signal Tactical Group** | "STG" on second reference. Never "Spare Time Gaming" (legacy domain only). |
| Web panel | **Sitrep** | Flagship product. "Sitrep, an STG product." |
| Desktop server tool | **Sentinel Desktop** | Renamed from "Longbow." Retire all "Longbow" naming + logo. |
| The host agent | **Sentinel** | The service that runs on the game-server host. |

- Domain `spare-time-gaming.us` → keep as a **redirect only**. Primary should move to an STG domain.
- Backstory line (allowed): "Built in our spare time. Self-hosted by design. Built by a veteran."

## 2. Color tokens
One brand accent (sky blue). Everything else neutral. Status colors signal status **only**.

### Brand — sky blue
brand-300 #7DD3FC · brand-400 #38BDF8 (primary accent) · brand-500 #0EA5E9 (primary action)
brand-600 #0284C7 (button default) · brand-700 #0369A1 (hover) · brand-800 #075985 (active) · brand-900 #0C4A6E

### Neutrals — deep slate (SteamOS-adjacent)
canvas #05080C · surface #0E151D · raised #18222E · line #25323F · muted #8A97A6 · faint #5B6877 · text #E7EDF3
> Current app uses Tailwind gray; warm slightly toward these slate values. Drop the #FF8C00 orange accent.

### Semantic — use sparingly
online/ok #34D399 · offline/danger #F87171 · admin/warning #FBBF24

## 3. Typography
- Display: **Space Grotesk** (600/700) — headlines, wordmark, big numbers. Tight tracking (-1 to -2px).
- UI & body: **Inter** (400–700) — interface, paragraphs, controls.
- Mono: **JetBrains Mono** (400/500) — labels, RCON, code, status. Uppercase labels, +1–3px tracking.

## 4. Shape, spacing, components
- Radius: buttons 8px · cards/panels 16px · status pills 9999px.
- Borders: 1px solid line (#25323F). Brand/active: 1px brand-400 + glow box-shadow: 0 18px 50px -28px rgba(56,189,248,.5).
- Buttons: primary bg brand-600, hover 700, active 800, white text. Ghost: bg raised, text.
- Status pill: ~12% tint of the semantic color + 7px dot + mono label in that color.
- Cards: surface bg, line border, 16px radius, ~30–36px padding.

## 5. Iconography & voice
- Icons: lucide line icons, stroke 1.5. Sitrep mark = Shield.
- Voice: plain English, jargon translated, calm and confident. Short sentences. No hype, no emoji.

## 6. Brand architecture — endorsed house
STG — Signal Tactical Group (master brand / endorser)
├── Sitrep            web panel · flagship   → "an STG product"
└── Sentinel Desktop  Windows host tool      → "part of Sitrep · by STG"
Always pair a product with its STG endorsement lockup (header, footer, installer splash).
