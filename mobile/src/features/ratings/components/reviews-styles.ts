import { StyleSheet } from 'react-native';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Styles shared by the cards of the ratings and comments section. */
export const reviewsStyles = StyleSheet.create({
  card: {
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.medium,
    backgroundColor: colors.surface,
  },
  subheading: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
    lineHeight: 21,
  },
  statusBlock: {
    gap: spacing.small,
  },
});
