import { useQuery } from '@tanstack/react-query';
import { Image } from 'expo-image';
import { Redirect, useLocalSearchParams, useRouter } from 'expo-router';
import {
  ActivityIndicator,
  Pressable,
  ScrollView,
  StyleSheet,
  Text,
  View,
} from 'react-native';

import { PrimaryButton } from '@/components/auth/primary-button';
import { useAuthSession } from '@/providers/auth-provider';
import { useErrorToast } from '@/providers/toast-provider';
import { fetchCatalogTitleDetails } from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

export default function CatalogTitleScreen() {
  const router = useRouter();
  const { session } = useAuthSession();
  const params = useLocalSearchParams<{
    provider?: string;
    contentType?: string;
    externalId?: string;
  }>();
  const provider = isCatalogProvider(params.provider) ? params.provider : null;
  const contentType = isCatalogContentType(params.contentType) ? params.contentType : null;
  const externalId = typeof params.externalId === 'string' ? params.externalId : null;
  const validParams = provider && contentType && externalId
    ? { provider, contentType, externalId }
    : null;

  const details = useQuery({
    queryKey: ['catalog', 'title', provider, contentType, externalId],
    queryFn: ({ signal }) => {
      if (!validParams) {
        throw new Error('O endereço deste título é inválido.');
      }

      return fetchCatalogTitleDetails(
        validParams.provider,
        validParams.contentType,
        validParams.externalId,
        signal,
      );
    },
    enabled: validParams !== null,
    staleTime: 300_000,
  });
  useErrorToast(details.error, 'Não foi possível carregar os detalhes deste título. Tente novamente.');

  if (!session) {
    return <Redirect href="/" />;
  }

  if (!validParams) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="alert" style={styles.title}>
          O endereço deste título é inválido.
        </Text>
        <PrimaryButton label="Voltar" onPress={() => router.back()} />
      </View>
    );
  }

  if (details.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Carregando detalhes...</Text>
      </View>
    );
  }

  if (details.isError || !details.data) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>
          Não foi possível abrir este título.
        </Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void details.refetch()} />
        <PrimaryButton label="Voltar" onPress={() => router.back()} variant="secondary" />
      </View>
    );
  }

  const title = details.data;
  const releaseDate = title.releaseDate
    ? new Intl.DateTimeFormat('pt-BR', { dateStyle: 'long' })
      .format(new Date(`${title.releaseDate}T00:00:00`))
    : null;

  return (
    <ScrollView contentContainerStyle={styles.content}>
      <Pressable
        accessibilityRole="button"
        onPress={() => router.back()}
        style={styles.backButton}
      >
        <Text style={styles.backText}>Voltar</Text>
      </Pressable>
      {title.backdropUrl ? (
        <Image
          contentFit="cover"
          source={{ uri: title.backdropUrl }}
          style={styles.backdrop}
          transition={150}
        />
      ) : null}
      <Text accessibilityRole="header" style={styles.title}>{title.title}</Text>
      {title.originalTitle && title.originalTitle !== title.title ? (
        <Text style={styles.secondaryText}>{title.originalTitle}</Text>
      ) : null}
      <View style={styles.metadata}>
        <Text style={styles.metadataText}>
          {title.contentType === 'movie' ? 'Filme' : 'Série'}
        </Text>
        {releaseDate ? <Text style={styles.metadataText}>{releaseDate}</Text> : null}
        {title.runtimeMinutes ? (
          <Text style={styles.metadataText}>{title.runtimeMinutes} min</Text>
        ) : null}
      </View>
      {title.genres.length > 0 ? (
        <Text style={styles.secondaryText}>{title.genres.join(' · ')}</Text>
      ) : null}
      {title.overview ? (
        <View style={styles.overview}>
          <Text style={styles.sectionTitle}>Sinopse</Text>
          <Text style={styles.body}>{title.overview}</Text>
        </View>
      ) : null}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  content: {
    flexGrow: 1,
    gap: spacing.medium,
    padding: spacing.medium,
    paddingTop: spacing.large,
    backgroundColor: colors.background,
  },
  centered: {
    flex: 1,
    alignItems: 'center',
    justifyContent: 'center',
    gap: spacing.medium,
    padding: spacing.large,
    backgroundColor: colors.background,
  },
  backButton: {
    minHeight: dimensions.minimumTouchTarget,
    alignSelf: 'flex-start',
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  backText: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  backdrop: {
    width: '100%',
    aspectRatio: 16 / 9,
    borderRadius: radii.large,
    backgroundColor: colors.surface,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    fontFamily: fontFamily.bold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  metadata: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  metadataText: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  overview: {
    gap: spacing.small,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  body: {
    color: colors.textSecondary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
    lineHeight: typography.body * 1.6,
  },
});

function isCatalogProvider(value: unknown): value is 'tmdb' | 'omdb' {
  return value === 'tmdb' || value === 'omdb';
}

function isCatalogContentType(value: unknown): value is 'movie' | 'series' {
  return value === 'movie' || value === 'series';
}
