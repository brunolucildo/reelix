import { Image } from 'expo-image';
import { Redirect, useLocalSearchParams, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { PrimaryButton } from '@/components/auth/primary-button';
import { BackButton } from '@/components/navigation/back-button';
import { useErrorToast, useToast } from '@/providers/toast-provider';
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
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

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
  useErrorToast(preferences.error, 'Não foi possível carregar suas preferências. Verifique sua conexão e tente novamente.');

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
  const [isSearchFocused, setIsSearchFocused] = useState(false);
  const { showError } = useToast();
  const [selectionNotice, setSelectionNotice] = useState<string | null>(null);

  const isFavoriteStep = step === 'favoriteMovies' || step === 'favoriteSeries';
  const favoriteType = step === 'favoriteSeries' ? 'series' : 'movie';
  const movieGenres = useQuery({
    queryKey: ['catalog', 'genres', 'movie'],
    queryFn: () => fetchCatalogGenres('movie'),
  });
  const seriesGenres = useQuery({
    queryKey: ['catalog', 'genres', 'series'],
    queryFn: () => fetchCatalogGenres('series'),
  });

  const search = useQuery({
    queryKey: ['catalog', 'search', favoriteType, submittedSearch],
    queryFn: () => searchCatalog(submittedSearch, favoriteType),
    enabled: isFavoriteStep && submittedSearch.trim().length >= 2,
  });
  useErrorToast(step === 'genres' ? (genreType === 'movie' ? movieGenres.error : seriesGenres.error) : null, 'Não foi possível carregar os gêneros. Tente novamente.');
  useErrorToast(search.error, search.error?.message ?? 'Não foi possível buscar no catálogo.');

  const saveMutation = useMutation({
    mutationFn: saveOnboardingProgress,
    onSuccess: (saved) => {
      queryClient.setQueryData(['onboarding'], saved);
      const { isComplete: _isComplete, ...progress } = saved;
      setDraft({
        ...progress,
        currentStep: progress.currentStep === 'completed' ? 'review' : progress.currentStep,
      });
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar suas escolhas.');
    },
  });

  const completeMutation = useMutation({
    mutationFn: completeOnboarding,
    onSuccess: (saved) => {
      queryClient.setQueryData(['onboarding'], saved);
      router.replace(isEditing ? '/profile' : '/home');
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível concluir o onboarding.');
    },
  });

  const currentIndex = steps.indexOf(step as (typeof steps)[number]);
  const safeIndex = currentIndex < 0 ? 0 : currentIndex;
  const selectedGenres = genreType === 'movie' ? draft.movieGenreIds : draft.seriesGenreIds;
  const genres = genreType === 'movie' ? movieGenres : seriesGenres;
  const favoriteIds = favoriteType === 'movie' ? draft.favoriteMovieIds : draft.favoriteSeriesIds;
  const selectedGenresTotal = draft.movieGenreIds.length + draft.seriesGenreIds.length;
  const updateDraft = (changes: Partial<OnboardingProgress>) => {
    setDraft((current) => current ? { ...current, ...changes } : current);
    setSelectionNotice(null);
  };

  const continueTo = async (nextStep: (typeof steps)[number]) => {
    const progress = { ...draft, currentStep: nextStep };
    try {
      await saveMutation.mutateAsync(progress);
      setStep(nextStep);
    } catch {
      return;
    }
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
    if (!currentIds.includes(genre.id) && currentIds.length >= 20) {
      setSelectionNotice('Você já selecionou o limite de 20 gêneros para esta categoria.');
      return;
    }
    const nextIds = currentIds.includes(genre.id)
      ? currentIds.filter((id) => id !== genre.id)
      : [...currentIds, genre.id];
    updateDraft(genreType === 'movie'
      ? { movieGenreIds: nextIds }
      : { seriesGenreIds: nextIds });
  };

  const toggleFavorite = (title: CatalogTitle) => {
    if (!favoriteIds.includes(title.id) && favoriteIds.length >= 10) {
      setSelectionNotice(`Você já selecionou o limite de 10 ${favoriteType === 'movie' ? 'filmes' : 'séries'}.`);
      return;
    }
    const nextIds = favoriteIds.includes(title.id)
      ? favoriteIds.filter((id) => id !== title.id)
      : [...favoriteIds, title.id];
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
          <BackButton onPress={step === 'welcome' && !isEditing ? undefined : goBack} />
          <Text style={styles.stepCount}>
            {isEditing ? 'EDITANDO PREFERÊNCIAS' : `ETAPA ${safeIndex + 1} DE ${steps.length}`}
          </Text>
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
            <Text style={styles.selectedCount}>
              {selectedGenres.length} de 20 gêneros selecionados
            </Text>
            {genres.isPending ? <ActivityIndicator color={colors.brand} /> : null}
            {genres.isError ? (
              <View style={styles.messageBlock}>
                <Text style={styles.emptyText}>Gêneros indisponíveis no momento.</Text>
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
                onBlur={() => setIsSearchFocused(false)}
                onChangeText={setSearchText}
                onFocus={() => setIsSearchFocused(true)}
                onSubmitEditing={() => setSubmittedSearch(searchText.trim())}
                placeholder="Buscar no catálogo"
                placeholderTextColor={colors.textSecondary}
                returnKeyType="search"
                style={[styles.searchInput, isSearchFocused ? styles.searchInputFocused : null]}
                value={searchText}
              />
              <Pressable
                accessibilityRole="button"
                disabled={searchText.trim().length < 2 || search.isFetching}
                onPress={() => setSubmittedSearch(searchText.trim())}
                style={({ pressed }) => [
                  styles.searchButton,
                  (searchText.trim().length < 2 || search.isFetching) ? styles.disabled : null,
                  pressed && searchText.trim().length >= 2 && !search.isFetching ? styles.pressed : null,
                ]}
              >
                <Text style={styles.searchButtonLabel}>Buscar</Text>
              </Pressable>
            </View>
            {search.isFetching ? (
              <View accessibilityLiveRegion="polite" style={styles.loadingRow}>
                <ActivityIndicator color={colors.brand} />
                <Text style={styles.emptyText}>Buscando no catálogo...</Text>
              </View>
            ) : null}
            {submittedSearch.trim().length < 2 ? (
              <Text style={styles.emptyText}>Digite ao menos 2 caracteres para buscar no catálogo.</Text>
            ) : null}
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
              <>
                <Text style={styles.selectedCount}>{favoriteIds.length} de 10 selecionados</Text>
                <Pressable
                  accessibilityRole="button"
                  disabled={isBusy}
                  onPress={() => updateDraft(favoriteType === 'movie'
                    ? { favoriteMovieIds: [] }
                    : { favoriteSeriesIds: [] })}
                  style={({ pressed }) => [styles.skipButton, pressed ? styles.pressed : null]}
                >
                  <Text style={styles.skipLabel}>Remover seleções desta etapa</Text>
                </Pressable>
              </>
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

        {selectionNotice ? (
          <Text accessibilityLiveRegion="polite" style={styles.selectionNotice}>{selectionNotice}</Text>
        ) : null}

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
              style={({ pressed }) => [styles.skipButton, pressed ? styles.pressed : null]}
            >
              <Text style={styles.skipLabel}>Pular esta etapa</Text>
            </Pressable>
          ) : null}
          {isEditing ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => router.back()}
              style={({ pressed }) => [styles.skipButton, pressed ? styles.pressed : null]}
            >
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
      style={({ pressed }) => [
        styles.segment,
        selected ? styles.segmentSelected : null,
        pressed ? styles.pressed : null,
      ]}
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
      style={({ pressed }) => [
        styles.chip,
        selected ? styles.chipSelected : null,
        pressed ? styles.pressed : null,
      ]}
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
      style={({ pressed }) => [
        styles.titleResult,
        selected ? styles.titleResultSelected : null,
        pressed ? styles.pressed : null,
      ]}
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
    paddingHorizontal: spacing.medium,
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
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  stepCount: {
    color: colors.textSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.bold,
    letterSpacing: 0.8,
    textAlign: 'right',
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
    gap: spacing.medium,
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
    gap: spacing.medium,
    paddingTop: 12,
  },
  eyebrow: {
    color: colors.brandSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.extraBold,
    letterSpacing: 1,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    lineHeight: 37,
    fontFamily: fontFamily.extraBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: 21,
    fontFamily: fontFamily.regular,
  },
  segmented: {
    flexDirection: 'row',
    gap: spacing.small,
    marginTop: 6,
  },
  segment: {
    flex: 1,
    minHeight: 48,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
  },
  segmentSelected: {
    borderColor: colors.brand,
    backgroundColor: colors.brandSurface,
  },
  segmentLabel: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  segmentLabelSelected: {
    color: colors.textPrimary,
  },
  genreGrid: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    gap: spacing.small,
  },
  chip: {
    minHeight: 44,
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
  },
  chipSelected: {
    borderColor: colors.brand,
    backgroundColor: colors.brandSurface,
  },
  chipLabel: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  chipLabelSelected: {
    color: colors.textPrimary,
  },
  searchRow: {
    flexDirection: 'row',
    gap: spacing.small,
  },
  searchInput: {
    flex: 1,
    minHeight: 52,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    paddingHorizontal: spacing.medium,
    backgroundColor: colors.surface,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  searchInputFocused: {
    borderColor: colors.brandSecondary,
  },
  searchButton: {
    minHeight: 52,
    minWidth: 84,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.small,
    paddingHorizontal: 12,
    backgroundColor: colors.brand,
  },
  disabled: {
    opacity: 0.5,
  },
  pressed: {
    opacity: 0.78,
  },
  searchButtonLabel: {
    color: colors.white,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  titleList: {
    gap: spacing.small,
  },
  titleResult: {
    minHeight: 96,
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
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
    width: 52,
    height: 76,
    borderRadius: radii.extraSmall,
    backgroundColor: colors.surfaceElevated,
  },
  posterFallback: {
    alignItems: 'center',
    justifyContent: 'center',
  },
  posterFallbackLabel: {
    color: colors.brandSecondary,
    fontSize: typography.section,
    fontFamily: fontFamily.extraBold,
  },
  titleInfo: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  resultTitle: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  releaseDate: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
  selectionMark: {
    color: colors.brandSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.bold,
  },
  selectedCount: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.medium,
  },
  emptyText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
  },
  loadingRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.small,
  },
  selectionNotice: {
    color: colors.warning,
    fontSize: typography.bodySmall,
    lineHeight: 21,
    fontFamily: fontFamily.medium,
  },
  messageBlock: {
    gap: spacing.small,
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
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  summaryValue: {
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.bold,
  },
  actions: {
    gap: spacing.small,
    marginTop: 8,
  },
  skipButton: {
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    paddingHorizontal: spacing.small,
  },
  skipLabel: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
});