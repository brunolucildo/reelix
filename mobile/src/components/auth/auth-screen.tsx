import type { PropsWithChildren } from 'react';
import { usePathname } from 'expo-router';
import { ScrollView, StyleSheet, Text, View } from 'react-native';

import ReelixLogo from '../../../assets/brand/logos/logo-horizontal.svg';
import { BackButton } from '@/components/navigation/back-button';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type AuthScreenProps = PropsWithChildren<{
  title: string;
  subtitle: string;
}>;

export function AuthScreen({ title, subtitle, children }: AuthScreenProps) {
  const pathname = usePathname();
  return (
    <ScrollView
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.container}>
        {pathname !== '/' ? <BackButton /> : null}
        <View style={styles.brandBlock}>
          <ReelixLogo
            accessibilityLabel="Reelix — filmes e séries"
            style={styles.logo}
            width="100%"
            height={88}
          />
          <View style={styles.brandRule} />
        </View>
        <View style={styles.heading}>
          <Text accessibilityRole="header" style={styles.title}>{title}</Text>
          <Text style={styles.subtitle}>{subtitle}</Text>
        </View>
        <View style={styles.formCard}>
          <View style={styles.form}>{children}</View>
        </View>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    justifyContent: 'center',
    backgroundColor: colors.background,
    paddingHorizontal: spacing.medium,
    paddingVertical: spacing.extraLarge,
  },
  container: {
    width: '100%',
    maxWidth: 440,
    alignSelf: 'center',
    gap: spacing.large,
  },
  brandBlock: {
    alignItems: 'center',
    gap: spacing.medium,
  },
  logo: {
    width: '100%',
    maxWidth: 280,
    height: 88,
    alignSelf: 'center',
  },
  brandRule: {
    width: 40,
    height: 3,
    borderRadius: radii.pill,
    backgroundColor: colors.brand,
  },
  heading: {
    gap: spacing.small,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.display,
    fontFamily: fontFamily.extraBold,
    letterSpacing: -0.8,
    lineHeight: 40,
  },
  subtitle: {
    color: colors.textSecondary,
    fontFamily: fontFamily.regular,
    fontSize: typography.body,
    lineHeight: 24,
  },
  formCard: {
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.large,
    backgroundColor: colors.surface,
  },
  form: {
    gap: spacing.medium,
  },
});
