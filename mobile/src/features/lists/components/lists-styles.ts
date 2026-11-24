import { StyleSheet } from 'react-native';

import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Styles shared by the sections of the lists screen. */
export const listsStyles = StyleSheet.create({
  section: {
    gap: spacing.medium,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  subtitle: {
    color: colors.textSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  muted: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  titleCopy: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  horizontalCards: {
    gap: spacing.small,
    alignItems: 'flex-start',
    paddingBottom: spacing.small,
  },
  unavailableCard: {
    width: dimensions.minimumTouchTarget * 3 + spacing.small * 4,
    minHeight: dimensions.posterHeight,
    gap: spacing.small,
    padding: spacing.small,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
});
