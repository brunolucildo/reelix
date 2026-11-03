import { useQuery } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';

import FilmIcon from '../../../assets/brand/navigation-icons/films.svg';
import RatingIcon from '../../../assets/brand/navigation-icons/rating.svg';
import { PrimaryButton } from '@/components/auth/primary-button';
import { UserAvatar } from '@/components/user-avatar';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogTitleDetails, type SocialActivityData } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export function SocialActivityCard({ activity, compact = false }: { activity: SocialActivityData; compact?: boolean }) {
  const router = useRouter();
  const contentType = activity.targetType === 'series' ? 'series' : 'movie';
  const title = useQuery({
    queryKey: ['catalog', 'title', activity.provider, contentType, activity.externalId],
    queryFn: ({ signal }) => fetchCatalogTitleDetails(activity.provider, contentType, activity.externalId, signal),
    enabled: !compact && activity.targetType !== 'episode',
    staleTime: 300_000,
  });
  useErrorToast(title.error, 'Não foi possível carregar os detalhes do título.');
  const typeLabel = activity.targetType === 'movie' ? 'Filme' : activity.targetType === 'series' ? 'Série' : 'Episódio';
  const isRating = activity.activityType === 'ratingUpdated';
  const Icon = isRating ? RatingIcon : FilmIcon;
  const openTitle = () => {
    if (activity.targetType === 'episode') return;
    router.push({ pathname: '/title/[provider]/[contentType]/[externalId]', params: { provider: activity.provider, contentType: activity.targetType, externalId: activity.externalId } });
  };

  return <View style={styles.activity}>
    <Pressable accessibilityRole="button" accessibilityLabel={`Ver perfil de ${activity.actorDisplayName}`} onPress={() => router.push({ pathname: '/social-profile/[accountId]', params: { accountId: activity.actorAccountId } })} style={styles.authorRow}>
      <UserAvatar name={activity.actorDisplayName} avatarKey={activity.actorAvatarKey} />
      <View style={styles.copy}>
        <Text style={styles.authorName}>{activity.actorDisplayName}</Text>
        <Text style={styles.secondaryText}>{formatActivityTime(activity.occurredAt)}</Text>
      </View>
      <Icon width={dimensions.iconSize} height={dimensions.iconSize} />
    </Pressable>
    <Text style={styles.action}>{isRating ? 'Compartilhou uma avaliação' : 'Registrou uma visualização'}</Text>
    <Pressable
      accessibilityRole={activity.targetType !== 'episode' ? 'button' : undefined}
      accessibilityLabel={activity.targetType !== 'episode' ? `Abrir ${title.data?.title ?? typeLabel.toLocaleLowerCase('pt-BR')}` : undefined}
      disabled={activity.targetType === 'episode'}
      onPress={openTitle}
      style={({ pressed }) => [styles.publication, pressed && styles.pressed]}
    >
    {compact ? <Text style={styles.secondaryText}>{typeLabel}</Text> : <>
      {title.data?.posterUrl ? <Image accessibilityLabel={`Pôster de ${title.data.title}`} source={{ uri: title.data.posterUrl }} contentFit="cover" style={styles.poster} /> : <View style={[styles.poster, styles.posterFallback]}><Icon width={dimensions.avatarSize} height={dimensions.avatarSize} /></View>}
      <View style={styles.copy}>
        <Text style={styles.secondaryText}>{typeLabel}</Text>
        <Text style={styles.title}>{title.data?.title ?? (activity.targetType === 'episode' ? (isRating ? 'Episódio avaliado' : 'Episódio assistido') : 'Atividade compartilhada')}</Text>
        {title.isFetching ? <ActivityIndicator accessibilityLabel="Carregando título" color={colors.brand} /> : null}
      </View>
    </>}
    </Pressable>
    {title.isError ? <PrimaryButton label="Tentar novamente" onPress={() => void title.refetch()} variant="quiet" /> : null}
  </View>;
}

function formatActivityTime(value: string) {
  const date = new Date(value);
  const minutes = Math.max(0, Math.floor((Date.now() - date.getTime()) / 60_000));
  if (minutes < 1) return 'Agora';
  if (minutes < 60) return `Há ${minutes} min`;
  if (minutes < 1440) return `Há ${Math.floor(minutes / 60)} h`;
  return date.toLocaleDateString('pt-BR', { day: 'numeric', month: 'short', year: 'numeric' });
}

const styles = StyleSheet.create({
  activity: { width: '100%', maxWidth: dimensions.drawerWidth * 2, alignSelf: 'center', gap: spacing.medium, paddingVertical: spacing.large, borderBottomWidth: 1, borderBottomColor: colors.border },
  authorRow: { flexDirection: 'row', alignItems: 'center', gap: spacing.small, minHeight: dimensions.minimumTouchTarget },
  copy: { flex: 1, minWidth: 0, gap: spacing.small },
  avatar: { width: dimensions.minimumTouchTarget, height: dimensions.minimumTouchTarget, borderRadius: radii.pill, backgroundColor: colors.surfaceElevated, alignItems: 'center', justifyContent: 'center' },
  authorName: { color: colors.textPrimary, fontFamily: fontFamily.bold, fontSize: typography.body },
  secondaryText: { color: colors.textSecondary, fontSize: typography.bodySmall, lineHeight: typography.bodySmall + spacing.small, fontFamily: fontFamily.regular },
  action: { color: colors.textPrimary, fontSize: typography.bodySmall, fontFamily: fontFamily.medium },
  publication: { flexDirection: 'row', alignItems: 'center', gap: spacing.medium, minHeight: dimensions.minimumTouchTarget },
  pressed: { opacity: 0.72 },
  poster: { width: dimensions.posterWidth, height: dimensions.posterHeight, borderRadius: radii.small, backgroundColor: colors.surface },
  posterFallback: { alignItems: 'center', justifyContent: 'center' },
  title: { color: colors.textPrimary, fontSize: typography.section, fontFamily: fontFamily.bold },
});