import { StyleSheet } from 'react-native';

import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Row, button and text styles shared by the sections of the social screen. */
export const socialStyles = StyleSheet.create({
  container: {
    width: '100%',
    maxWidth: dimensions.drawerWidth * 2,
    alignSelf: 'center',
    gap: spacing.large,
  },
  heading: {
    gap: spacing.small,
  },
  section: {
    gap: spacing.small,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: spacing.medium,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: typography.bodySmall + spacing.small,
    fontFamily: fontFamily.regular,
  },
  listRow: {
    minHeight: dimensions.avatarSize,
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.small,
    paddingVertical: spacing.small,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  personDetails: { flex: 1, minWidth: 0, gap: spacing.small },
  rowTitle: {
    flexShrink: 1,
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  actionButton: {
    minHeight: dimensions.minimumTouchTarget,
    minWidth: dimensions.minimumTouchTarget,
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  actionText: {
    color: colors.brandSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.semiBold,
  },
  inlineActions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.extraSmall,
  },
  friendLink: {
    flex: 1,
    minHeight: dimensions.minimumTouchTarget,
    justifyContent: 'center',
  },
  navigationTabActive: { borderBottomWidth: 2, borderBottomColor: colors.brand },
  emptyState: { gap: spacing.medium, alignItems: 'flex-start', paddingVertical: spacing.extraLarge },
  input: {
    minHeight: dimensions.minimumTouchTarget + spacing.small,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  inputFocused: { borderColor: colors.brandSecondary },
});
