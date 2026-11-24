import { QueryClientProvider } from '@tanstack/react-query';
import {
  Sora_400Regular,
  Sora_500Medium,
  Sora_600SemiBold,
  Sora_700Bold,
  Sora_800ExtraBold,
} from '@expo-google-fonts/sora';
import { useFonts } from 'expo-font';
import { Stack } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import { useEffect } from 'react';
import * as Notifications from 'expo-notifications';

import { SESSION_EXPIRED_MESSAGE } from '@/lib/api-error';
import { createQueryClient } from '@/lib/query/create-query-client';
import {
  notifyUnauthorized,
  setUnauthorizedListener,
} from '@/lib/query/unauthorized-listener';
import { AuthProvider, useAuthSession } from '@/providers/auth-provider';
import { ToastProvider, useToast } from '@/providers/toast-provider';
import { colors, fontFamily, spacing, typography } from '@/theme/tokens';
import { useOpenNotification } from '@/features/notifications/hooks/use-open-notification';

const queryClient = createQueryClient(notifyUnauthorized);

function SessionExpiryListener() {
  const { expireSession } = useAuthSession();
  const { showError } = useToast();

  useEffect(() => {
    setUnauthorizedListener(() => {
      if (expireSession()) {
        showError(SESSION_EXPIRED_MESSAGE);
      }
    });
    return () => setUnauthorizedListener(null);
  }, [expireSession, showError]);

  return null;
}

Notifications.setNotificationHandler({
  handleNotification: async () => ({
    shouldShowBanner: true,
    shouldShowList: true,
    shouldPlaySound: true,
    shouldSetBadge: false,
  }),
});

function RootNavigator() {
  const { session, isLoading, initializationError, retryInitialization } = useAuthSession();
  useOpenNotification(session !== null);

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
        <Pressable
          accessibilityRole="button"
          accessibilityLabel="Tentar novamente"
          onPress={retryInitialization}
          style={styles.retry}
        >
          <Text style={styles.retryText}>Tentar novamente</Text>
        </Pressable>
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
        <Stack.Screen name="lists" />
        <Stack.Screen name="tracking" />
        <Stack.Screen name="search" />
        <Stack.Screen name="social" />
        <Stack.Screen name="notifications" />
        <Stack.Screen name="gamification" />
        <Stack.Screen name="settings" />
        <Stack.Screen name="my-ratings" />
        <Stack.Screen name="social-profile/[accountId]" />
        <Stack.Screen name="title/[provider]/[contentType]/[externalId]" />
        <Stack.Screen name="episode/[provider]/[externalSeriesId]/[seasonNumber]/[episodeId]" />
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
      <ToastProvider>
        <AuthProvider>
          <SessionExpiryListener />
          <RootNavigator />
        </AuthProvider>
      </ToastProvider>
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
    gap: spacing.small,
    padding: 28,
    backgroundColor: colors.background,
  },
  loadingText: {
    color: colors.textSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  errorTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
    textAlign: 'center',
  },
  errorText: {
    color: colors.textSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
    textAlign: 'center',
  },
  retry: {
    minHeight: 44,
    justifyContent: 'center',
    paddingHorizontal: spacing.medium,
  },
  retryText: {
    color: colors.brand,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
});
