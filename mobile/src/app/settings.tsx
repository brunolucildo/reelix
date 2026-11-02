import { useRouter } from 'expo-router';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { PrimaryButton } from '@/components/auth/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';

export default function SettingsScreen() {
  const router = useRouter();
  return (
    <SafeAreaView style={styles.screen}>
      <ScrollView contentContainerStyle={styles.content}>
        <BackButton />
        <Text accessibilityRole="header" style={styles.title}>Configurações</Text>
        <View style={styles.section}>
          <PrimaryButton label="Perfil e privacidade" variant="secondary" onPress={() => router.push('/profile')} />
          <PrimaryButton label="Preferências de catálogo" variant="secondary" onPress={() => router.push({ pathname: '/onboarding', params: { edit: '1' } })} />
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background },
  content: { padding: spacing.medium, gap: spacing.large },
  section: { gap: spacing.medium },
  title: { fontFamily: fontFamily.bold, fontSize: typography.title, color: colors.textPrimary },
});