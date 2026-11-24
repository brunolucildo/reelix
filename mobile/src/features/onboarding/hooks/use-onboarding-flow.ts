import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'expo-router';
import { useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';

import type { CatalogGenre, CatalogTitle } from '@/features/catalog/api/catalog-api';
import {
  completeOnboarding,
  saveOnboardingProgress,
  type OnboardingPreferences,
  type OnboardingProgress,
} from '@/features/onboarding/api/onboarding-api';
import {
  useOnboardingGenres,
  useOnboardingTitleSearch,
} from '@/features/onboarding/hooks/use-onboarding-catalog';
import { onboardingQueryKeys } from '@/features/onboarding/query-keys';
import {
  MAX_FAVORITES,
  MAX_GENRES,
  onboardingSelectionSchema,
  toSelection,
  type OnboardingSelection,
} from '@/features/onboarding/schemas';
import { useErrorToast, useToast } from '@/providers/toast-provider';

export const onboardingSteps: Exclude<OnboardingPreferences['currentStep'], 'completed'>[] = [
  'welcome',
  'genres',
  'favoriteMovies',
  'favoriteSeries',
  'review',
];

/**
 * State machine of the onboarding wizard: current step, selections (React Hook Form), the genre
 * and title queries, and the save/complete mutations.
 */
export function useOnboardingFlow(preferences: OnboardingPreferences, isEditing: boolean) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const initialStep = preferences.currentStep === 'completed'
    ? (isEditing ? 'genres' : 'welcome')
    : preferences.currentStep;
  const form = useForm<OnboardingSelection>({
    resolver: zodResolver(onboardingSelectionSchema),
    defaultValues: toSelection(preferences),
  });
  const draft = useWatch({ control: form.control });
  const [step, setStep] = useState<OnboardingProgress['currentStep']>(initialStep);
  const [genreType, setGenreType] = useState<'movie' | 'series'>('movie');
  const [searchText, setSearchText] = useState('');
  const [submittedSearch, setSubmittedSearch] = useState('');
  const [selectionNotice, setSelectionNotice] = useState<string | null>(null);

  const isFavoriteStep = step === 'favoriteMovies' || step === 'favoriteSeries';
  const favoriteType = step === 'favoriteSeries' ? 'series' : 'movie';
  const movieGenres = useOnboardingGenres('movie');
  const seriesGenres = useOnboardingGenres('series');
  const search = useOnboardingTitleSearch(favoriteType, submittedSearch, isFavoriteStep);
  useErrorToast(
    step === 'genres' ? (genreType === 'movie' ? movieGenres.error : seriesGenres.error) : null,
    'Não foi possível carregar os gêneros. Tente novamente.',
  );

  const saveMutation = useMutation({
    mutationFn: saveOnboardingProgress,
    onSuccess: (saved) => {
      queryClient.setQueryData(onboardingQueryKeys.preferences, saved);
      form.reset(toSelection(saved));
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar suas escolhas.');
    },
  });
  const completeMutation = useMutation({
    mutationFn: completeOnboarding,
    onSuccess: (saved) => {
      queryClient.setQueryData(onboardingQueryKeys.preferences, saved);
      router.replace(isEditing ? '/profile' : '/home');
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível concluir o onboarding.');
    },
  });

  const selection: OnboardingSelection = {
    movieGenreIds: draft.movieGenreIds ?? [],
    seriesGenreIds: draft.seriesGenreIds ?? [],
    favoriteMovieIds: draft.favoriteMovieIds ?? [],
    favoriteSeriesIds: draft.favoriteSeriesIds ?? [],
  };
  const currentIndex = onboardingSteps.indexOf(step as (typeof onboardingSteps)[number]);
  const safeIndex = currentIndex < 0 ? 0 : currentIndex;
  const selectedGenres = genreType === 'movie' ? selection.movieGenreIds : selection.seriesGenreIds;
  const favoriteIds = favoriteType === 'movie' ? selection.favoriteMovieIds : selection.favoriteSeriesIds;

  const updateSelection = (changes: Partial<OnboardingSelection>) => {
    form.reset({ ...selection, ...changes });
    setSelectionNotice(null);
  };
  const progress = (currentStep: OnboardingProgress['currentStep']): OnboardingProgress =>
    ({ ...selection, currentStep });

  const continueTo = async (nextStep: (typeof onboardingSteps)[number]) => {
    try {
      await saveMutation.mutateAsync(progress(nextStep));
      setStep(nextStep);
    } catch {
      return;
    }
  };

  const goBack = () => {
    if (safeIndex > 0) {
      setStep(onboardingSteps[safeIndex - 1]);
      return;
    }
    router.replace('/home');
  };

  const toggleGenre = (genre: CatalogGenre) => {
    if (!selectedGenres.includes(genre.id) && selectedGenres.length >= MAX_GENRES) {
      setSelectionNotice(`Você já selecionou o limite de ${MAX_GENRES} gêneros para esta categoria.`);
      return;
    }
    const nextIds = selectedGenres.includes(genre.id)
      ? selectedGenres.filter((id) => id !== genre.id)
      : [...selectedGenres, genre.id];
    updateSelection(genreType === 'movie' ? { movieGenreIds: nextIds } : { seriesGenreIds: nextIds });
  };

  const toggleFavorite = (title: CatalogTitle) => {
    if (!favoriteIds.includes(title.id) && favoriteIds.length >= MAX_FAVORITES) {
      setSelectionNotice(`Você já selecionou o limite de ${MAX_FAVORITES} ${favoriteType === 'movie' ? 'filmes' : 'séries'}.`);
      return;
    }
    const nextIds = favoriteIds.includes(title.id)
      ? favoriteIds.filter((id) => id !== title.id)
      : [...favoriteIds, title.id];
    updateSelection(favoriteType === 'movie' ? { favoriteMovieIds: nextIds } : { favoriteSeriesIds: nextIds });
  };

  const finish = async () => {
    try {
      await saveMutation.mutateAsync(progress('review'));
      await completeMutation.mutateAsync();
    } catch {
      return;
    }
  };

  return {
    step,
    safeIndex,
    nextStep: onboardingSteps[safeIndex + 1],
    isFavoriteStep,
    favoriteType,
    genreType,
    setGenreType,
    genres: genreType === 'movie' ? movieGenres : seriesGenres,
    selection,
    selectedGenres,
    favoriteIds,
    search,
    searchText,
    setSearchText,
    submittedSearch,
    submitSearch: () => setSubmittedSearch(searchText.trim()),
    selectionNotice,
    clearFavorites: () => updateSelection(favoriteType === 'movie'
      ? { favoriteMovieIds: [] }
      : { favoriteSeriesIds: [] }),
    toggleGenre,
    toggleFavorite,
    continueTo,
    goBack,
    finish,
    isSaving: saveMutation.isPending,
    isBusy: saveMutation.isPending || completeMutation.isPending,
  };
}

export type OnboardingFlowState = ReturnType<typeof useOnboardingFlow>;
