import type { PropsWithChildren } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';

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
        <View style={styles.brandRow}>
          <Text accessibilityLabel="Reelix" style={styles.brandMark}>R</Text>
          <Text style={styles.brandName}>reelix</Text>
        </View>
        <Text style={styles.tagline}>YOUR STORIES. YOUR SCREEN.</Text>
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
  brandRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
  },
  brandMark: {
    width: 38,
    height: 38,
    overflow: 'hidden',
    borderRadius: 11,
    backgroundColor: colors.brand,
    color: colors.white,
    fontSize: 27,
    fontFamily: fontFamily.extraBold,
    lineHeight: 38,
    textAlign: 'center',
  },
  brandName: {
    color: colors.textPrimary,
    fontSize: 28,
    fontFamily: fontFamily.extraBold,
    letterSpacing: -1.3,
  },
  tagline: {
    color: colors.brand,
    fontSize: 10,
    fontFamily: fontFamily.bold,
    letterSpacing: 2.2,
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
