import type { PropsWithChildren } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';

import ReelixLogo from '../../../assets/brand/logos/logo-horizontal.svg';
import { colors, fontFamily, spacing } from '@/theme/tokens';

type AuthScreenProps = PropsWithChildren<{
  title: string;
  subtitle: string;
}>;

export function AuthScreen({ title, subtitle, children }: AuthScreenProps) {
  return (
    <ScrollView
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.container}>
        <ReelixLogo accessibilityLabel="Reelix — filmes, séries e amigos" width={320} height={96} />
        <View style={styles.heading}>
          <Text accessibilityRole="header" style={styles.title}>{title}</Text>
          <Text style={styles.subtitle}>{subtitle}</Text>
        </View>
        <View style={styles.form}>{children}</View>
      </View>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    justifyContent: 'center',
    backgroundColor: colors.background,
    paddingHorizontal: spacing.large,
    paddingVertical: spacing.extraLarge,
  },
  container: {
    width: '100%',
    maxWidth: 480,
    alignSelf: 'center',
    gap: 12,
  },
  heading: {
    gap: 8,
    marginTop: 28,
  },
  title: {
    color: colors.textPrimary,
    fontSize: 32,
    fontFamily: fontFamily.extraBold,
    letterSpacing: -1.1,
    lineHeight: 39,
  },
  subtitle: {
    color: colors.textSecondary,
    fontFamily: fontFamily.regular,
    fontSize: 15,
    lineHeight: 23,
  },
  form: {
    gap: 16,
    marginTop: 12,
  },
});
