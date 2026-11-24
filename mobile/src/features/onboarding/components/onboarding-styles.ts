import { StyleSheet } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Text, layout and button styles shared by the steps of the onboarding wizard. */
export const onboardingStyles = StyleSheet.create({
  section: {
    gap: spacing.medium,
    paddingTop: 12,
  },
  eyebrow: {
    color: colors.brandSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.extraBold,
    letterSpacing: 1,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    lineHeight: 37,
    fontFamily: fontFamily.extraBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: 21,
    fontFamily: fontFamily.regular,
  },
  selectedCount: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  emptyText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  skipButton: {
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  skipLabel: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  pressed: {
    opacity: 0.78,
  },
  rule: {
    width: 52,
    height: 4,
    marginTop: 12,
    borderRadius: radii.pill,
    backgroundColor: colors.brand,
  },
});
