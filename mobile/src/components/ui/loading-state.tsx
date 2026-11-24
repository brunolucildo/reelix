import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

export function LoadingState({ label }: { label: string }) {
  return (
    <View accessibilityLiveRegion="polite" style={styles.loading}>
      <ActivityIndicator accessibilityLabel={label} color={colors.brand} />
      <Text style={styles.description}>{label}</Text>
    </View>
  );
}

/** Retry affordance shown next to the error toast when a request fails. */
export function RetryState({ onRetry }: { onRetry: () => void }) {
  return <PrimaryButton label="Tentar novamente" onPress={onRetry} variant="secondary" />;
}

const styles = StyleSheet.create({
  loading: {
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.small,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  description: {
    color: colors.textSecondary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
  },
});
