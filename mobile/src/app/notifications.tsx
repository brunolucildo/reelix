import { ActivityIndicator, ScrollView, StyleSheet, Text, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { ToggleRow } from '@/components/ui/toggle-row';
import { NotificationInbox } from '@/features/notifications/components/notification-inbox';
import { useNotifications } from '@/features/notifications/hooks/use-notifications';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function NotificationsScreen() {
  const {
    preferences, inbox, items, savePreferences, markRead, enablePush, feedback, clearFeedback,
  } = useNotifications();

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
          <ToggleRow
            variant="plain"
            label="Atividades sociais"
            description="Solicitações de amizade e confirmações."
            value={preferences.data.socialEnabled}
            disabled={savePreferences.isPending}
            onValueChange={(socialEnabled) => {
              clearFeedback();
              savePreferences.mutate({ ...preferences.data, socialEnabled });
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
            loading={enablePush.isPending}
            onPress={() => {
              clearFeedback();
              enablePush.mutate();
            }}
            variant="secondary"
          />
        </View>

        <NotificationInbox
          items={items}
          hasNextPage={Boolean(inbox.hasNextPage)}
          isFetchingNextPage={inbox.isFetchingNextPage}
          onLoadMore={() => void inbox.fetchNextPage()}
          onRead={(id) => markRead.mutate(id)}
        />

        {feedback ? <Text accessibilityLiveRegion="polite" style={styles.successText}>{feedback}</Text> : null}
      </View>
    </ScrollView>
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
  successText: {
    color: colors.success,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
});
