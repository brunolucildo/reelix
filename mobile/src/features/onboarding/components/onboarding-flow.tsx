import { useRouter } from 'expo-router';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import { BackButton } from '@/components/navigation/back-button';
import { PrimaryButton } from '@/components/ui/primary-button';
import type { OnboardingPreferences } from '@/features/onboarding/api/onboarding-api';
import { FavoritesStep } from '@/features/onboarding/components/favorites-step';
import { GenresStep } from '@/features/onboarding/components/genres-step';
import { onboardingStyles as shared } from '@/features/onboarding/components/onboarding-styles';
import { onboardingSteps, useOnboardingFlow } from '@/features/onboarding/hooks/use-onboarding-flow';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  preferences: OnboardingPreferences;
  isEditing: boolean;
};

/** The onboarding wizard: step header and progress, the current step and its actions. */
export function OnboardingFlow({ preferences, isEditing }: Props) {
  const router = useRouter();
  const flow = useOnboardingFlow(preferences, isEditing);
  const { step, safeIndex, nextStep, isBusy, isSaving, selection } = flow;
  const total = onboardingSteps.length;

  return (
    <ScrollView
      contentContainerStyle={styles.content}
      keyboardShouldPersistTaps="handled"
      showsVerticalScrollIndicator={false}
    >
      <View style={styles.container}>
        <View style={styles.topBar}>
          <BackButton onPress={step === 'welcome' && !isEditing ? undefined : flow.goBack} />
          <Text style={styles.stepCount}>
            {isEditing ? 'EDITANDO PREFERÊNCIAS' : `ETAPA ${safeIndex + 1} DE ${total}`}
          </Text>
        </View>

        <View accessibilityRole="progressbar" accessibilityValue={{ min: 0, max: total, now: safeIndex + 1 }} style={styles.progressTrack}>
          <View style={[styles.progressFill, { width: `${((safeIndex + 1) / total) * 100}%` }]} />
        </View>

        {step === 'welcome' ? (
          <View style={styles.intro}>
            <Text style={shared.eyebrow}>SEU REELIX</Text>
            <Text accessibilityRole="header" style={shared.title}>Vamos encontrar seu próximo favorito.</Text>
            <Text style={shared.secondaryText}>Escolha alguns gêneros e títulos para personalizar seu espaço.</Text>
            <View style={shared.rule} />
          </View>
        ) : null}

        {step === 'genres' ? <GenresStep flow={flow} /> : null}
        {flow.isFavoriteStep ? <FavoritesStep flow={flow} /> : null}

        {step === 'review' ? (
          <View style={shared.section}>
            <Text style={shared.eyebrow}>PRONTO</Text>
            <Text accessibilityRole="header" style={shared.title}>Seu ponto de partida.</Text>
            <SummaryRow label="Gêneros" value={selection.movieGenreIds.length + selection.seriesGenreIds.length} />
            <SummaryRow label="Filmes favoritos" value={selection.favoriteMovieIds.length} />
            <SummaryRow label="Séries favoritas" value={selection.favoriteSeriesIds.length} />
            <Text style={shared.secondaryText}>Você pode editar suas escolhas no perfil.</Text>
          </View>
        ) : null}

        {flow.selectionNotice ? (
          <Text accessibilityLiveRegion="polite" style={styles.selectionNotice}>{flow.selectionNotice}</Text>
        ) : null}

        <View style={styles.actions}>
          {step === 'review' ? (
            <PrimaryButton
              label={isEditing ? 'Salvar preferências' : 'Concluir'}
              loading={isBusy}
              onPress={() => void flow.finish()}
            />
          ) : nextStep ? (
            <PrimaryButton
              label={step === 'welcome' ? 'Começar' : 'Continuar'}
              loading={isSaving}
              onPress={() => void flow.continueTo(nextStep)}
            />
          ) : null}
          {step !== 'welcome' && step !== 'review' ? (
            <Pressable
              accessibilityRole="button"
              disabled={isBusy}
              onPress={() => nextStep && void flow.continueTo(nextStep)}
              style={({ pressed }) => [shared.skipButton, pressed ? shared.pressed : null]}
            >
              <Text style={shared.skipLabel}>Pular esta etapa</Text>
            </Pressable>
          ) : null}
          {isEditing ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => router.back()}
              style={({ pressed }) => [shared.skipButton, pressed ? shared.pressed : null]}
            >
              <Text style={shared.skipLabel}>Cancelar edição</Text>
            </Pressable>
          ) : null}
        </View>
      </View>
    </ScrollView>
  );
}

function SummaryRow({ label, value }: { label: string; value: number }) {
  return (
    <View style={styles.summaryRow}>
      <Text style={styles.summaryLabel}>{label}</Text>
      <Text style={styles.summaryValue}>{value}</Text>
    </View>
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
  topBar: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
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
  selectionNotice: {
    color: colors.warning,
    fontSize: typography.bodySmall,
    lineHeight: 21,
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
});
