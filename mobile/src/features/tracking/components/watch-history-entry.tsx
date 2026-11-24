import { Image } from 'expo-image';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import type { CatalogTitle } from '@/features/catalog/api/catalog-api';
import type { TrackingHistoryEntry } from '@/features/tracking/api/tracking-api';
import { trackingStyles as shared } from '@/features/tracking/components/tracking-styles';
import { WatchedAtEditor } from '@/features/tracking/components/watched-at-editor';
import { formatDateTimeMedium } from '@/lib/format';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  entry: TrackingHistoryEntry;
  title: CatalogTitle | undefined;
  isEditing: boolean;
  isSaving: boolean;
  onEdit: () => void;
  onCancelEdit: () => void;
  onSubmitWatchedAt: (watchedAtIso: string) => string | null;
  onRemove: () => void;
};

export function WatchHistoryEntry({
  entry,
  title,
  isEditing,
  isSaving,
  onEdit,
  onCancelEdit,
  onSubmitWatchedAt,
  onRemove,
}: Props) {
  const router = useRouter();
  const watchedAtLabel = formatDateTimeMedium(new Date(entry.watchedAt));

  return (
    <View style={shared.card}>
      {title ? (
        <Pressable
          accessibilityRole="button"
          accessibilityLabel={`Abrir detalhes de ${title.title}`}
          onPress={() => router.push(
            `/title/${title.provider}/${title.contentType}/${encodeURIComponent(title.externalId)}`,
          )}
          style={styles.title}
        >
          {title.posterUrl ? (
            <Image contentFit="cover" source={{ uri: title.posterUrl }} style={styles.poster} />
          ) : null}
          <View style={styles.titleCopy}>
            <Text style={shared.cardTitle}>{title.title}</Text>
            <Text style={shared.description}>
              {entry.type === 'movie' ? 'Filme' : 'Série · episódio'}
              {title.releaseDate ? ` · ${title.releaseDate.slice(0, 4)}` : ''}
            </Text>
          </View>
        </Pressable>
      ) : (
        <Text style={shared.cardTitle}>
          {entry.type === 'movie' ? 'Filme assistido' : 'Episódio assistido'}
        </Text>
      )}
      <Text style={shared.description}>{watchedAtLabel}</Text>
      {isEditing ? (
        <WatchedAtEditor
          watchedAt={entry.watchedAt}
          disabled={isSaving}
          onSubmit={onSubmitWatchedAt}
          onCancel={onCancelEdit}
        />
      ) : (
        <View style={styles.actions}>
          <Pressable
            accessibilityRole="button"
            accessibilityLabel={`Corrigir data e hora de ${title?.title ?? 'este registro'}`}
            disabled={isSaving}
            onPress={onEdit}
            style={styles.iconButton}
          >
            <Text style={styles.icon}>✎</Text>
          </Pressable>
          {entry.type === 'movie' || entry.seriesId ? (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel={`Desmarcar ${entry.type === 'movie' ? 'filme' : 'episódio'} assistido em ${watchedAtLabel}`}
              disabled={isSaving}
              onPress={onRemove}
              style={styles.iconButton}
            >
              <Text style={styles.icon}>✓</Text>
            </Pressable>
          ) : null}
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  title: {
    minHeight: 96,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
  },
  titleCopy: { flex: 1, gap: spacing.extraSmall },
  poster: {
    width: 64,
    height: 96,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
  },
  actions: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  iconButton: {
    minWidth: 48,
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surfaceElevated,
  },
  icon: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.semiBold,
  },
});
