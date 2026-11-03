import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useState } from 'react';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Switch,
  Text,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import {
  fetchNotificationInbox,
  fetchNotificationPreferences,
  markNotificationRead,
  saveNotificationPreferences,
} from '@/services/api-client';
import { registerCurrentPushDevice } from '@/services/push-registration';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function NotificationsScreen() {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { session } = useAuthSession();
  const { showError } = useToast();
  const [feedback, setFeedback] = useState<string | null>(null);
  const preferences = useQuery({
    queryKey: ['notification-preferences'],
    queryFn: fetchNotificationPreferences,
  });
  const inbox = useInfiniteQuery({
    queryKey: ['notification-inbox'],
    initialPageParam: undefined as string | undefined,
    queryFn: ({ pageParam, signal }) => fetchNotificationInbox(pageParam, signal),
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
  });
  useErrorToast(preferences.error, 'Não foi possível carregar as preferências de notificações.');
  useErrorToast(inbox.error, 'Não foi possível carregar a caixa de entrada.');

  const preferencesMutation = useMutation({
    mutationFn: saveNotificationPreferences,
    onSuccess: (saved) => {
      queryClient.setQueryData(['notification-preferences'], saved);
      setFeedback('Preferências atualizadas.');
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar as preferências.');
    },
  });
  const readMutation = useMutation({
    mutationFn: markNotificationRead,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['notification-inbox'] }),
        queryClient.invalidateQueries({ queryKey: ['notification-unread-count'] }),
      ]);
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível atualizar a notificação.');
    },
  });
  const pushMutation = useMutation({
    mutationFn: async () => {
      if (!session?.user.id) {
        throw new Error('Entre novamente para ativar notificações neste dispositivo.');
      }
      await registerCurrentPushDevice(session.user.id);
    },
    onSuccess: () => setFeedback('Este dispositivo foi registrado para receber notificações push.'),
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível ativar notificações push.');
    },
  });

  if (preferences.isPending || inbox.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Carregando notificações...</Text>
      </View>
    );
  }

  if (preferences.isError || inbox.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>
          Não foi possível carregar as notificações.
        </Text>
        <PrimaryButton
          label="Tentar novamente"
          onPress={() => {
            void preferences.refetch();
            void inbox.refetch();
          }}
        />
      </View>
    );
  }

  const items = inbox.data.pages.flatMap((page) => page.items);

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <View style={styles.container}>
        <View style={styles.topBar}>
          <BackButton />
          <Text style={styles.eyebrow}>CENTRAL DE AVISOS</Text>
        </View>

        <View style={styles.heading}>
          <Text accessibilityRole="header" style={styles.title}>Notificações</Text>
          <Text style={styles.secondaryText}>Gerencie seus avisos e veja as atividades recentes.</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Preferências</Text>
          <PreferenceToggle
            label="Atividades sociais"
            description="Solicitações de amizade e confirmações."
            value={preferences.data.socialEnabled}
            disabled={preferencesMutation.isPending}
            onValueChange={(socialEnabled) => {
              setFeedback(null);
              preferencesMutation.mutate({ ...preferences.data, socialEnabled });
            }}
          />
          <View style={styles.unavailable}>
            <Text style={styles.preferenceTitle}>Episódios e estreias</Text>
            <Text style={styles.preferenceDescription}>
              Avisos de catálogo ficarão disponíveis quando a fonte de atualização estiver configurada.
            </Text>
          </View>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Neste dispositivo</Text>
          <Text style={styles.preferenceDescription}>
            Push exige um build nativo, projectId EAS e credenciais Expo/APNs/FCM configurados.
            Sem isso, sua caixa de entrada continua disponível no app.
          </Text>
          <PrimaryButton
            label="Ativar notificações push"
            loading={pushMutation.isPending}
            onPress={() => {
              setFeedback(null);
              pushMutation.mutate();
            }}
            variant="secondary"
          />
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Caixa de entrada</Text>
          {items.length === 0 ? (
            <Text style={styles.emptyText}>Você ainda não tem notificações.</Text>
          ) : items.map((item) => (
            <Pressable
              key={item.id}
              accessibilityRole="button"
              accessibilityLabel={`${item.title}. ${item.body}. ${item.readAt ? 'Lida' : 'Não lida'}.`}
              accessibilityHint="Abre a área relacionada à notificação."
              onPress={() => {
                if (!item.readAt) {
                  readMutation.mutate(item.id);
                }
                router.push(item.deepLink);
              }}
              style={[styles.notification, !item.readAt && styles.unreadNotification]}
            >
              <View style={styles.notificationCopy}>
                <Text style={styles.notificationTitle}>{item.title}</Text>
                <Text style={styles.preferenceDescription}>{item.body}</Text>
                <Text style={styles.timestamp}>
                  {new Date(item.createdAt).toLocaleString('pt-BR')}
                </Text>
              </View>
              {!item.readAt ? <View accessibilityLabel="Não lida" style={styles.unreadDot} /> : null}
            </Pressable>
          ))}
          {inbox.hasNextPage ? (
            <PrimaryButton
              label="Carregar mais"
              loading={inbox.isFetchingNextPage}
              onPress={() => void inbox.fetchNextPage()}
              variant="secondary"
            />
          ) : null}
        </View>

        {feedback ? <Text accessibilityLiveRegion="polite" style={styles.successText}>{feedback}</Text> : null}
      </View>
    </ScrollView>
  );
}

function PreferenceToggle({
  label,
  description,
  value,
  disabled,
  onValueChange,
}: {
  label: string;
  description: string;
  value: boolean;
  disabled: boolean;
  onValueChange: (value: boolean) => void;
}) {
  return (
    <View style={styles.toggleRow}>
      <View style={styles.toggleCopy}>
        <Text style={styles.preferenceTitle}>{label}</Text>
        <Text style={styles.preferenceDescription}>{description}</Text>
      </View>
      <Switch
        accessibilityLabel={label}
        disabled={disabled}
        onValueChange={onValueChange}
        thumbColor={colors.white}
        trackColor={{ false: colors.surfaceElevated, true: colors.brand }}
        value={value}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    backgroundColor: colors.background,
    paddingHorizontal: spacing.large,
    paddingVertical: spacing.extraLarge,
  },
  container: {
    width: '100%',
    maxWidth: 560,
    alignSelf: 'center',
    gap: spacing.large,
  },
  centered: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: spacing.medium,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
  },
  backButton: {
    minHeight: 44,
    justifyContent: 'center',
    paddingRight: spacing.medium,
  },
  backLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  eyebrow: {
    color: colors.brandSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.extraBold,
    letterSpacing: 1,
  },
  heading: {
    gap: spacing.small,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  section: {
    gap: spacing.medium,
    padding: spacing.medium,
    borderRadius: radii.large,
    backgroundColor: colors.surface,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  toggleRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
  },
  toggleCopy: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  preferenceTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.semiBold,
  },
  preferenceDescription: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  unavailable: {
    gap: spacing.extraSmall,
    paddingTop: spacing.small,
  },
  notification: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
    padding: spacing.medium,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
  },
  unreadNotification: {
    borderColor: colors.brand,
    borderWidth: 1,
  },
  notificationCopy: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  notificationTitle: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  timestamp: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  unreadDot: {
    width: 8,
    height: 8,
    borderRadius: 4,
    backgroundColor: colors.brand,
  },
  emptyText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  successText: {
    color: colors.success,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
});
