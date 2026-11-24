import { StyleSheet } from 'react-native';

import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

/** Section and text styles shared by the blocks of the gamification screen. */
export const gamificationStyles = StyleSheet.create({
  section: {
    gap: spacing.medium,
    paddingTop: spacing.large,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: 22,
    fontFamily: fontFamily.regular,
  },
  caption: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    lineHeight: 18,
    fontFamily: fontFamily.regular,
  },
});
