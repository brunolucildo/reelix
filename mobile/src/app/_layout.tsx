import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import {
  Sora_400Regular,
  Sora_500Medium,
  Sora_600SemiBold,
  Sora_700Bold,
  Sora_800ExtraBold,
} from '@expo-google-fonts/sora';
import { useFonts } from 'expo-font';
import { Stack } from 'expo-router';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';

import { AuthProvider, useAuthSession } from '@/providers/auth-provider';
import { colors, fontFamily } from '@/theme/tokens';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
});

function RootNavigator() {
  const { session, isLoading, initializationError, retryInitialization } = useAuthSession();

  if (isLoading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.loadingText}>Carregando sua sessão...</Text>
      </View>
    );
  }

  if (initializationError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.errorTitle}>
          Não foi possível iniciar o Reelix
        </Text>
        <Text style={styles.errorText}>{initializationError}</Text>
        <Text accessibilityRole="button" onPress={retryInitialization} style={styles.retry}>
          Tentar novamente
        </Text>
      </View>
    );
  }

  const isAuthenticated = session !== null;

  return (
    <Stack screenOptions={{ headerShown: false, contentStyle: styles.screen }}>
      <Stack.Protected guard={!isAuthenticated}>
        <Stack.Screen name="index" />
        <Stack.Screen name="sign-up" />
        <Stack.Screen name="forgot-password" />
      </Stack.Protected>
      <Stack.Protected guard={isAuthenticated}>
        <Stack.Screen name="home" />
        <Stack.Screen name="onboarding" />
        <Stack.Screen name="profile" />
        <Stack.Screen name="auth/reset-password" />
      </Stack.Protected>
      <Stack.Screen name="auth/callback" />
    </Stack>
  );
}

export default function RootLayout() {
  const [fontsLoaded, fontError] = useFonts({
    [fontFamily.regular]: Sora_400Regular,
    [fontFamily.medium]: Sora_500Medium,
    [fontFamily.semiBold]: Sora_600SemiBold,
    [fontFamily.bold]: Sora_700Bold,
    [fontFamily.extraBold]: Sora_800ExtraBold,
  });

  if (!fontsLoaded && !fontError) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
      </View>
    );
  }

  if (fontError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="alert" style={styles.errorTitle}>
          Não foi possível carregar a tipografia do Reelix.
        </Text>
      </View>
    );
  }

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RootNavigator />
      </AuthProvider>
    </QueryClientProvider>
  );
}

const styles = StyleSheet.create({
  screen: {
    backgroundColor: colors.background,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 14,
    padding: 28,
    backgroundColor: colors.background,
  },
  loadingText: {
    color: colors.textSecondary,
    fontSize: 15,
    fontFamily: fontFamily.regular,
  },
  errorTitle: {
    color: colors.textPrimary,
    fontSize: 20,
    fontFamily: fontFamily.bold,
    textAlign: 'center',
  },
  errorText: {
    color: colors.textSecondary,
    fontSize: 15,
    fontFamily: fontFamily.regular,
    textAlign: 'center',
  },
  retry: {
    color: colors.brand,
    fontSize: 16,
    fontFamily: fontFamily.bold,
    padding: 12,
  },
});
