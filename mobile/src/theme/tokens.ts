export const colors = {
  brand: '#FF2D55',
  brandSecondary: '#FF6688',
  brandSurface: '#35131D',
  background: '#080B0F',
  surface: '#1A1A1E',
  surfaceElevated: '#2A2A2E',
  border: '#2A2A2E',
  textPrimary: '#FFFFFF',
  textSecondary: '#B8B8BD',
  error: '#FF6688',
  success: '#9ADDAA',
  warning: '#FBBF24',
  info: '#38BDF8',
  white: '#FFFFFF',
} as const;

export const spacing = {
  extraSmall: 4,
  small: 8,
  medium: 16,
  large: 24,
  extraLarge: 32,
} as const;

export const typography = {
  display: 32,
  title: 28,
  section: 18,
  body: 15,
  bodySmall: 14,
  caption: 12,
  eyebrow: 11,
} as const;

export const radii = {
  extraSmall: 4,
  small: 8,
  medium: 14,
  large: 24,
  pill: 999,
} as const;

export const dimensions = {
  posterWidth: 120,
  posterHeight: 180,
  minimumTouchTarget: 44,
} as const;

export const fontFamily = {
  regular: 'Sora_400Regular',
  medium: 'Sora_500Medium',
  semiBold: 'Sora_600SemiBold',
  bold: 'Sora_700Bold',
  extraBold: 'Sora_800ExtraBold',
} as const;
