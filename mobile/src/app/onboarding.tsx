import { Image } from 'expo-image';
import { Redirect, useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { PrimaryButton } from '@/components/auth/primary-button';
import {
  completeOnboarding,
  fetchCatalogGenres,
  fetchOnboarding,
  saveOnboardingProgress,
  searchCatalog,
  type CatalogGenre,
  type CatalogTitle,
  type OnboardingPreferences,
  type OnboardingProgress,
} from '@/services/api-client';
import { colors, fontFamily, radii, spacing } from '@/theme/tokens';

const steps: Exclude<OnboardingPreferences['currentStep'], 'completed'>[] = [
  'welcome',
  'genres',
  'favoriteMovies',
  'favoriteSeries',
  'review',
];

export default function OnboardingScreen() {
  const params = useLocalSearchParams<{ edit?: string }>();
  const isEditing = params.edit === '1';
  const preferences = useQuery({ queryKey: ['onboarding'], queryFn: fetchOnboarding });

  if (preferences.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Não foi possível carregar suas preferências.</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void preferences.refetch()} />
      </View>
    );
  }

  if (preferences.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Preparando seu perfil...</Text>
      </View>
    );
  }

  if (preferences.data.isComplete && !isEditing) {
    return <Redirect href="/home" />;
  }

  return <OnboardingFlow preferences={preferences.data} isEditing={isEditing} />;
}

function OnboardingFlow({
  preferences,
  isEditing,
}: {
  preferences: OnboardingPreferences;
  isEditing: boolean;
}) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const initialStep = preferences.currentStep === 'completed'
    ? (isEditing ? 'genres' : 'welcome')
    : preferences.currentStep;
  const [draft, setDraft] = useState<OnboardingProgress>(() => ({
    currentStep: initialStep,
    movieGenreIds: preferences.movieGenreIds,
    seriesGenreIds: preferences.seriesGenreIds,
    favoriteMovieIds: preferences.favoriteMovieIds,
    favoriteSeriesIds: preferences.favoriteSeriesIds,
  }));
  const [step, setStep] = useState<OnboardingProgress['currentStep']>(initialStep);
  const [genreType, setGenreType] = useState<'movie' | 'series'>('movie');
  const [searchText, setSearchText] = useState('');
  const [submittedSearch, setSubmittedSearch] = useState('');
  const [saveError, setSaveError] = useState<string | null>(null);

  const movieGenres = useQuery({
    queryKey: ['catalog', 'genres', 'movie'],
    queryFn: () => fetchCatalogGenres('movie'),
  });
  const seriesGenres = useQuery({
    queryKey: ['catalog', 'genres', 'series'],
    queryFn: () => fetchCatalogGenres('series'),
  });

  const search = useQuery({
    queryKey: ['catalog', 'search', genreType, submittedSearch],
    queryFn: () => searchCatalog(submittedSearch, genreType),
    enabled: submittedSearch.trim().length >= 2,
  });

  const saveMutation = useMutation({
    mutationFn: saveOnboardingProgress,
    onSuccess: (saved) => {
      queryClient.setQueryData(['onboarding'], saved);
      const { isComplete: _isComplete, ...progress } = saved;
      setDraft({
        ...progress,
        currentStep: progress.currentStep === 'completed' ? 'review' : progress.currentStep,
      });
      setSaveError(null);
    },
    onError: (error) => {
      setSaveError(error instanceof Error ? error.message : 'Não foi possível salvar suas escolhas.');
    },
  });

  const completeMutation = useMutation({
    mutationFn: completeOnboarding,
    onSuccess: (saved) => {
      queryClient.setQueryData(['onboarding'], saved);
      router.replace(isEditing ? '/profile' : '/home');
    },
    onError: (error) => {
      setSaveError(error instanceof Error ? error.message : 'Não foi possível concluir o onboarding.');
    },
  });

  const currentIndex = steps.indexOf(step as (typeof steps)[number]);
  const safeIndex = currentIndex < 0 ? 0 : currentIndex;
  const selectedGenres = genreType === 'movie' ? draft.movieGenreIds : draft.seriesGenreIds;
  const genres = genreType === 'movie' ? movieGenres : seriesGenres;
  const isFavoriteStep = step === 'favoriteMovies' || step === 'favoriteSeries';
  const favoriteType = step === 'favoriteSeries' ? 'series' : 'movie';
  const favoriteIds = favoriteType === 'movie' ? draft.favoriteMovieIds : draft.favoriteSeriesIds;
  const selectedGenresTotal = draft.movieGenreIds.length + draft.seriesGenreIds.length;
  const updateDraft = (changes: Partial<OnboardingProgress>) => {
    setDraft((current) => current ? { ...current, ...changes } : current);
    setSaveError(null);
  };

  const continueTo = async (nextStep: (typeof steps)[number]) => {
    const progress = { ...draft, currentStep: nextStep };
    await saveMutation.mutateAsync(progress);
    setStep(nextStep);
  };

  const goBack = () => {
    if (safeIndex > 0) {
      setStep(steps[safeIndex - 1]);
      return;
    }
    router.replace('/home');
  };

  const toggleGenre = (genre: CatalogGenre) => {
    const currentIds = genreType === 'movie' ? draft.movieGenreIds : draft.seriesGenreIds;
    const nextIds = currentIds.includes(genre.id)
      ? currentIds.filter((id) => id !== genre.id)
      : [...currentIds, genre.id];
    updateDraft(genreType === 'movie'
      ? { movieGenreIds: nextIds }
      : { seriesGenreIds: nextIds });
  };

  const toggleFavorite = (title: CatalogTitle) => {
    const nextIds = favoriteIds.includes(title.id)
      ? favoriteIds.filter((id) => id !== title.id)
      : favoriteIds.length < 10
        ? [...favoriteIds, title.id]
        : favoriteIds;
    updateDraft(favoriteType === 'movie'
      ? { favoriteMovieIds: nextIds }
      : { favoriteSeriesIds: nextIds });
  };

  const finish = async () => {
    try {
      await saveMutation.mutateAsync({ ...draft, currentStep: 'review' });
      await completeMutation.mutateAsync();
    } catch {
      return;
    }
  };

  const nextStep = steps[safeIndex + 1];
  const isBusy = saveMutation.isPending || completeMutation.isPending;

  return (
    <ScrollView
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.container}>
        <View style={styles.topBar}>
          <Pressable accessibilityRole="button" onPress={goBack} style={styles.backButton}>
            <Text style={styles.backLabel}>Voltar</Text>
          </Pressable>
          <Text style={styles.stepCount}>{isEditing ? 'PREFERÊNCIAS' : `${safeIndex + 1} / ${steps.length}`}</Text>
        </View>

        <View accessibilityRole="progressbar" accessibilityValue={{ min: 0, max: steps.length, now: safeIndex + 1 }} style={styles.progressTrack}>
          <View style={[styles.progressFill, { width: `${((safeIndex + 1) / steps.length) * 100}%` }]} />
        </View>

        {step === 'welcome' ? (
          <View style={styles.intro}>
            <Text style={styles.eyebrow}>SEU REELIX</Text>
            <Text accessibilityRole="header" style={styles.title}>Vamos encontrar seu próximo favorito.</Text>
            <Text style={styles.secondaryText}>Escolha alguns gêneros e títulos para personalizar seu espaço.</Text>
            <View style={styles.introRule} />
          </View>
        ) : null}

        {step === 'genres' ? (
          <View style={styles.section}>
            <Text style={styles.eyebrow}>PREFERÊNCIAS</Text>
            <Text accessibilityRole="header" style={styles.title}>Que tipo de história te prende?</Text>
            <View style={styles.segmented}>
              <SegmentButton label="Filmes" selected={genreType === 'movie'} onPress={() => setGenreType('movie')} />
              <SegmentButton label="Séries" selected={genreType === 'series'} onPress={() => setGenreType('series')} />
            </View>
            {genres.isPending ? <ActivityIndicator color={colors.brand} /> : null}
            {genres.isError ? (
              <View style={styles.messageBlock}>
                <Text accessibilityRole="alert" style={styles.errorText}>Não foi possível carregar os gêneros.</Text>
                <PrimaryButton label="Tentar novamente" variant="secondary" onPress={() => void genres.refetch()} />
              </View>
            ) : (
              <View style={styles.genreGrid}>
                {genres.data?.map((genre) => (
                  <SelectionChip
                    key={genre.id}
                    label={genre.name}
                    selected={selectedGenres.includes(genre.id)}
                    onPress={() => toggleGenre(genre)}
                  />
                ))}
              </View>
            )}
          </View>
        ) : null}

        {isFavoriteStep ? (
          <View style={styles.section}>
            <Text style={styles.eyebrow}>{favoriteType === 'movie' ? 'FILMES' : 'SÉRIES'}</Text>
            <Text accessibilityRole="header" style={styles.title}>
              {favoriteType === 'movie' ? 'Quais filmes você ama?' : 'Quais séries você acompanha?'}
            </Text>
            <Text style={styles.secondaryText}>Escolha até 10. Esta etapa é opcional.</Text>
            <View style={styles.searchRow}>
              <TextInput
                accessibilityLabel={`Buscar ${favoriteType === 'movie' ? 'filmes' : 'séries'}`}
                autoCapitalize="sentences"
                onChangeText={setSearchText}
                onSubmitEditing={() => setSubmittedSearch(searchText.trim())}
                placeholder="Buscar no catálogo"
                placeholderTextColor={colors.textSecondary}
                returnKeyType="search"
                style={styles.searchInput}
                value={searchText}
              />
              <Pressable
                accessibilityRole="button"
                onPress={() => setSubmittedSearch(searchText.trim())}
                style={styles.searchButton}
              >
                <Text style={styles.searchButtonLabel}>Buscar</Text>
              </Pressable>
            </View>
            {search.isFetching ? <ActivityIndicator color={colors.brand} /> : null}
            {search.isError ? <Text accessibilityRole="alert" style={styles.errorText}>{search.error.message}</Text> : null}
            {search.data?.length === 0 && !search.isFetching ? (
              <Text style={styles.emptyText}>Nenhum título encontrado.</Text>
            ) : null}
            <View style={styles.titleList}>
              {search.data?.map((title) => (
                <TitleResult
                  key={title.id}
                  title={title}
                  selected={favoriteIds.includes(title.id)}
                  onPress={() => toggleFavorite(title)}
                />
              ))}
            </View>
            {favoriteIds.length > 0 ? (
              <Text style={styles.selectedCount}>{favoriteIds.length} de 10 selecionados</Text>
            ) : null}
          </View>
        ) : null}

        {step === 'review' ? (
          <View style={styles.section}>
            <Text style={styles.eyebrow}>PRONTO</Text>
            <Text accessibilityRole="header" style={styles.title}>Seu ponto de partida.</Text>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Gêneros</Text>
              <Text style={styles.summaryValue}>{selectedGenresTotal}</Text>
            </View>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Filmes favoritos</Text>
              <Text style={styles.summaryValue}>{draft.favoriteMovieIds.length}</Text>
            </View>
            <View style={styles.summaryRow}>
              <Text style={styles.summaryLabel}>Séries favoritas</Text>
              <Text style={styles.summaryValue}>{draft.favoriteSeriesIds.length}</Text>
            </View>
            <Text style={styles.secondaryText}>Você pode editar suas escolhas no perfil.</Text>
          </View>
        ) : null}

        {saveError ? <Text accessibilityRole="alert" style={styles.errorText}>{saveError}</Text> : null}

        <View style={styles.actions}>
          {step === 'review' ? (
            <PrimaryButton
              label={isEditing ? 'Salvar preferências' : 'Concluir'}
              loading={isBusy}
              onPress={() => void finish()}
            />
          ) : nextStep ? (
            <PrimaryButton
              label={step === 'welcome' ? 'Começar' : 'Continuar'}
              loading={saveMutation.isPending}
              onPress={() => void continueTo(nextStep)}
            />
          ) : null}
          {step !== 'welcome' && step !== 'review' ? (
            <Pressable
              accessibilityRole="button"
              disabled={isBusy}
              onPress={() => nextStep && void continueTo(nextStep)}
              style={styles.skipButton}
            >
              <Text style={styles.skipLabel}>Pular esta etapa</Text>
            </Pressable>
          ) : null}
          {isEditing ? (
            <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.skipButton}>
              <Text style={styles.skipLabel}>Cancelar edição</Text>
            </Pressable>
          ) : null}
        </View>
      </View>
    </ScrollView>
  );
}

function SegmentButton({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="button"
      accessibilityState={{ selected }}
      onPress={onPress}
      style={[styles.segment, selected ? styles.segmentSelected : null]}
    >
      <Text style={[styles.segmentLabel, selected ? styles.segmentLabelSelected : null]}>{label}</Text>
    </Pressable>
  );
}

function SelectionChip({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="checkbox"
      accessibilityState={{ checked: selected }}
      onPress={onPress}
      style={[styles.chip, selected ? styles.chipSelected : null]}
    >
      <Text style={[styles.chipLabel, selected ? styles.chipLabelSelected : null]}>{label}</Text>
    </Pressable>
  );
}

function TitleResult({ title, selected, onPress }: { title: CatalogTitle; selected: boolean; onPress: () => void }) {
  return (
    <Pressable
      accessibilityRole="checkbox"
      accessibilityState={{ checked: selected }}
      onPress={onPress}
      style={[styles.titleResult, selected ? styles.titleResultSelected : null]}
    >
      {title.posterUrl ? (
        <Image contentFit="cover" source={title.posterUrl} style={styles.poster} />
      ) : (
        <View style={[styles.poster, styles.posterFallback]}>
          <Text style={styles.posterFallbackLabel}>R</Text>
        </View>
      )}
      <View style={styles.titleInfo}>
        <Text numberOfLines={2} style={styles.resultTitle}>{title.title}</Text>
        {title.releaseDate ? <Text style={styles.releaseDate}>{title.releaseDate.slice(0, 4)}</Text> : null}
      </View>
      <Text style={styles.selectionMark}>{selected ? 'Selecionado' : 'Adicionar'}</Text>
    </Pressable>
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
    maxWidth: 620,
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
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  backButton: {
    minHeight: 44,
    justifyContent: 'center',
    paddingRight: spacing.medium,
  },
  backLabel: {
    color: colors.textPrimary,
    fontSize: 14,
    fontFamily: fontFamily.semiBold,
  },
  stepCount: {
    color: colors.textSecondary,
    fontSize: 11,
    fontFamily: fontFamily.bold,
  },
  progressTrack: {
    height: 4,
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    borderRadius: radii.pill,
    backgroundColor: colors.brand,
  },
  intro: {
    gap: 16,
    paddingTop: 24,
    paddingBottom: 8,
  },
  introRule: {
    width: 52,
    height: 4,
    marginTop: 12,
    borderRadius: radii.pill,
    backgroundColor: colors.brand,
  },
  section: {
    gap: 16,
    paddingTop: 12,
  },
  eyebrow: {
    color: colors.brandSecondary,
    fontSize: 11,
    fontFamily: fontFamily.extraBold,
  },
  title: {
    color: colors.textPrimary,
    fontSize: 28,
    lineHeight: 36,
    fontFamily: fontFamily.extraBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: 14,
    lineHeight: 21,
    fontFamily: fontFamily.regular,
  },
  segmented: {
    flexDirection: 'row',
    gap: 8,
    marginTop: 6,
  },
  segment: {
    minHeight: 44,
    minWidth: 104,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: 16,
    backgroundColor: colors.surface,
  },
  segmentSelected: {
    borderColor: colors.brand,
    backgroundColor: '#35131D',
  },
  segmentLabel: {
    color: colors.textSecondary,
    fontSize: 14,
    fontFamily: fontFamily.semiBold,
  },
  segmentLabelSelected: {
    color: colors.textPrimary,
  },
  genreGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: 9,
  },
  chip: {
    minHeight: 42,
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: 15,
    backgroundColor: colors.surface,
  },
  chipSelected: {
    borderColor: colors.brand,
    backgroundColor: '#35131D',
  },
  chipLabel: {
    color: colors.textSecondary,
    fontSize: 13,
    fontFamily: fontFamily.semiBold,
  },
  chipLabelSelected: {
    color: colors.textPrimary,
  },
  searchRow: {
    flexDirection: 'row',
    gap: 8,
  },
  searchInput: {
    flex: 1,
    minHeight: 50,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: 14,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: 15,
    fontFamily: fontFamily.regular,
  },
  searchButton: {
    minHeight: 50,
    minWidth: 82,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    paddingHorizontal: 12,
    backgroundColor: colors.brand,
  },
  searchButtonLabel: {
    color: colors.white,
    fontSize: 13,
    fontFamily: fontFamily.bold,
  },
  titleList: {
    gap: 8,
  },
  titleResult: {
    minHeight: 90,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    padding: 8,
    backgroundColor: colors.surface,
  },
  titleResultSelected: {
    borderColor: colors.brand,
  },
  poster: {
    width: 48,
    height: 70,
    borderRadius: 4,
    backgroundColor: colors.surfaceElevated,
  },
  posterFallback: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  posterFallbackLabel: {
    color: colors.brandSecondary,
    fontSize: 20,
    fontFamily: fontFamily.extraBold,
  },
  titleInfo: {
    flex: 1,
    gap: 5,
  },
  resultTitle: {
    color: colors.textPrimary,
    fontSize: 14,
    fontFamily: fontFamily.semiBold,
  },
  releaseDate: {
    color: colors.textSecondary,
    fontSize: 12,
    fontFamily: fontFamily.regular,
  },
  selectionMark: {
    color: colors.brandSecondary,
    fontSize: 11,
    fontFamily: fontFamily.bold,
  },
  selectedCount: {
    color: colors.textSecondary,
    fontSize: 12,
    fontFamily: fontFamily.medium,
  },
  emptyText: {
    color: colors.textSecondary,
    fontSize: 14,
    fontFamily: fontFamily.regular,
  },
  messageBlock: {
    gap: 12,
  },
  errorText: {
    color: colors.error,
    fontSize: 13,
    lineHeight: 19,
    fontFamily: fontFamily.medium,
  },
  summaryRow: {
    minHeight: 52,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  summaryLabel: {
    color: colors.textSecondary,
    fontSize: 14,
    fontFamily: fontFamily.medium,
  },
  summaryValue: {
    color: colors.textPrimary,
    fontSize: 16,
    fontFamily: fontFamily.bold,
  },
  actions: {
    gap: 8,
    marginTop: 8,
  },
  skipButton: {
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: 12,
  },
  skipLabel: {
    color: colors.textSecondary,
    fontSize: 13,
    fontFamily: fontFamily.semiBold,
  },
});