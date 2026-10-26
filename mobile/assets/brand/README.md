# Reelix — Brand Kit v1

## Included
- `logos/`: horizontal, vertical, monochrome and isolated mark SVGs.
- `app-icons/`: app icon and adaptive foreground SVGs.
- `navigation-icons/`: independent outline icons using `currentColor`. Set `stroke` through CSS or convert to component props.
- `genre-icons/`: genre icon SVGs with individual accent colors.
- `badges/`: achievement badge SVGs.
- `ui/`: splash-screen and empty-state SVG concepts.
- `docs/design-tokens.json`: palette, typography, spacing and radius tokens.

## Use in this project
- This kit is kept in `mobile/assets/brand/`; use its SVGs as the source assets for the mobile UI.
- The authentication screen renders `logos/logo-horizontal.svg` through the Expo SVG transformer.
- The app theme tokens in `mobile/src/theme/tokens.ts` follow `docs/design-tokens.json`, and Sora is loaded by the root layout.
- PNG copies of the app icon, adaptive foreground and vertical splash logo are generated from the supplied SVGs for Expo platform configuration.

## Brand direction
Dark cinematic base, bright red/pink primary accent, white text and cool-gray secondary surfaces. The generated reference board is a visual direction, while these files are separate editable vector assets.

## Important implementation notes
- SVG assets are editable vectors; use `react-native-svg` or convert them to components for React Native.
- The provided app-icon SVG should be exported to PNG at the platform-required dimensions before shipping to app stores.
- The splash screen is a concept asset. Adjust safe areas, logo sizing and loading indicator to match the actual Expo configuration.
- Typography uses Sora as the brand recommendation; install/package the font and define a fallback in the app.
- Review and refine the logo geometry before public release; this is a first-pass concept kit, not a trademark clearance.
