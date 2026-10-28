# Reelix brand assets

The identity follows the Reelix visual reference in [`docs/visual_identity.png`](../../../docs/visual_identity.png): a film-strip ribbon forming an `R`, a clean lowercase wordmark, and a dark cinematic palette. All illustrations are individual, editable SVG vectors; no SVG embeds raster images.

## Brand system

| Token | Value | Use |
| --- | --- | --- |
| Reelix red | `#FF2D55` | Primary mark, selected states, calls to action |
| Reelix pink | `#FF6688` | Secondary accents and highlights |
| Background | `#0F0F10` | Main dark canvas |
| Surface | `#1A1A1E` | Cards and contained surfaces |
| Raised surface | `#2A2A2E` | Borders and elevated details |
| Text | `#FFFFFF` | Primary content and monochrome-on-dark logo |
| Neutral | `#E5E7EB` | Secondary icon and neutral artwork |
| Muted text | `#B8B8BD` | Supporting copy |

Sora is used for the wordmark and app typography. The app loads `Sora_700Bold` and `Sora_600SemiBold` for the SVG wordmarks; PNG exports are rendered with those same bundled font files.

## Asset catalog

### Logos — `logos/`

| File | Format and dimensions | Intended use |
| --- | --- | --- |
| `logo-horizontal.svg` / `.png` | SVG; PNG 800 × 240 | Full-color horizontal lockup on dark surfaces; PNG has transparency |
| `logo-vertical.svg` / `.png` | SVG; PNG 520 × 342 | Compact/splash lockup; PNG has transparency |
| `symbol.svg` / `.png` | SVG; PNG 512 × 512 | Isolated Reelix film-ribbon mark; PNG has transparency |
| `logo-white.svg` / `.png` | SVG; PNG 800 × 240 | Single-color white lockup for dark surfaces |
| `logo-black.svg` / `.png` | SVG; PNG 800 × 240 | Single-color dark lockup for light surfaces |

### App icons — `app-icons/`

| File | Format and dimensions | Intended use |
| --- | --- | --- |
| `app-icon.svg` / `.png` | SVG; PNG 1024 × 1024 | Dark-background store and iOS app icon |
| `app-icon-light.svg` / `.png` | SVG; PNG 1024 × 1024 | Alternate red-background app icon |
| `adaptive-icon-foreground.svg` / `.png` | SVG; PNG 432 × 432 | Transparent Android adaptive foreground |
| `adaptive-icon-monochrome.svg` / `.png` | SVG; PNG 432 × 432 | Transparent Android themed-icon mask |
| `favicon.svg` / `.png` | SVG; PNG 48 × 48 | Web favicon |

The app icon includes its own background. Adaptive foreground and monochrome images are transparent and centered within the Android safe area. Do not use the store icon as the adaptive foreground.

Unused Expo starter icons, splash artwork, and duplicate tab PNGs under `assets/images/` were removed after confirming that no app code or configuration referenced them. The `brand/` assets above are now the canonical identity files.

### Functional and supporting artwork

- `navigation-icons/`: 24 × 24 individually named navigation, playback, tracking, social, and utility icons. Their explicit red, pink, or neutral strokes are suitable for direct use as SVG components.
- `genre-icons/`: 48 × 48 individual genre illustrations with a shared stroke weight and the two brand accents.
- `badges/`: 96 × 108 individual achievement medallions with consistent borders, ribbons, and distinct center symbols.
- `ui/empty-state.svg` and `ui/splash-screen.svg`: editable standalone UI illustration concepts, not app-screen replacements.
- `docs/palette.svg` and `docs/design-tokens.json`: visual palette and machine-readable tokens.
- `brand-preview.html`: gallery preview that loads the individual assets from this folder; it is not a sprite or replacement for the source files.

SVGs use ordinary paths and shapes, avoid filters and embedded images, and can be imported as components through the Expo SVG transformer and `react-native-svg` already configured in this app. Use each SVG file individually; PNG exports are provided where a platform image asset is required or useful.

## Regenerating PNG exports

The checked-in SVG files are the editable source of truth. From `mobile/`, run:

```powershell
npm run brand:generate
```

This exports the app icons, adaptive icons, favicon, and logo PNGs at the dimensions in the catalog using headless Chrome or Edge. On a machine with a non-standard browser location, set `CHROME_PATH` to its executable before running the command. No extra npm package is required.

The Expo app configuration points to the dark app icon, Android foreground and monochrome assets, the small web favicon, and the vertical splash logo. The authentication screen imports `logo-horizontal.svg` directly.
