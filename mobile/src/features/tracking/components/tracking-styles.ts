import { StyleSheet } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Text and card styles shared by the sections of the tracking screen. */
export const trackingStyles = StyleSheet.create({
  section: {
    gap: spacing.medium,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontFamily: fontFamily.bold,
    fontSize: typography.section,
  },
  description: {
    color: colors.textSecondary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
  validation: {
    color: colors.error,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
  card: {
    gap: spacing.small,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
  },
  cardTitle: {
    color: colors.textPrimary,
    fontFamily: fontFamily.semiBold,
    fontSize: typography.body,
  },
  watchedCount: {
    color: colors.success,
    fontFamily: fontFamily.medium,
    fontSize: typography.body,
  },
});
