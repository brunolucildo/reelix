import { Link, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Switch, Text, TextInput, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { PrimaryButton } from '@/components/auth/primary-button';
import { AchievementGrid } from '@/components/gamification/achievement-grid';
import { BackButton } from '@/components/navigation/back-button';
import { avatarOptions, UserAvatar } from '@/components/user-avatar';
import { useErrorToast, useToast } from '@/providers/toast-provider';
import {
  fetchGamificationDashboard,
  fetchProfile,
  saveProfile,
  type ProfileData,
} from '@/services/api-client';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';
import TmdbLogo from '../../assets/brand/logos/tmdb-logo-blue.svg';
import CheckIcon from '../../assets/brand/navigation-icons/check.svg';

type ProfileDraft = Omit<ProfileData, 'avatarUrl' | 'avatarKey' | 'displayName' | 'biography'> & {
  displayName: string;
  biography: string;
  avatarKey: string;
};

export default function ProfileScreen() {
  const profile = useQuery({ queryKey: ['profile'], queryFn: fetchProfile });
  useErrorToast(profile.error, 'Não foi possível carregar o perfil. Verifique sua conexão e tente novamente.');

  if (profile.isError) {
    return (
      <View style={styles.centered}>
        <Text accessibilityRole="header" style={styles.title}>Não foi possível carregar o perfil.</Text>
        <PrimaryButton label="Tentar novamente" onPress={() => void profile.refetch()} />
      </View>
    );
  }

  if (profile.isPending) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color={colors.brand} size="large" />
        <Text style={styles.secondaryText}>Carregando perfil...</Text>
      </View>
    );
  }

  return <ProfileEditor initialProfile={profile.data} />;
}

function ProfileEditor({ initialProfile }: { initialProfile: ProfileData }) {
  const router = useRouter();
  const queryClient = useQueryClient();
  const { showError } = useToast();
  const gamification = useQuery({
    queryKey: ['gamification', 'dashboard'],
    queryFn: ({ signal }) => fetchGamificationDashboard(signal),
  });
  useErrorToast(gamification.error, 'Não foi possível carregar suas conquistas.');
  const [draft, setDraft] = useState<ProfileDraft>(() => ({
    displayName: initialProfile.displayName ?? '',
    biography: initialProfile.biography ?? '',
    avatarKey: initialProfile.avatarKey ?? 'initials',
    profileIsPublic: initialProfile.profileIsPublic,
    favoritesArePublic: initialProfile.favoritesArePublic,
    watchHistoryIsPublic: initialProfile.watchHistoryIsPublic,
    isDiscoverable: initialProfile.isDiscoverable,
    watchHistorySharedWithFriends: initialProfile.watchHistorySharedWithFriends,
    favoritesSharedWithFriends: initialProfile.favoritesSharedWithFriends,
    ratingsSharedWithFriends: initialProfile.ratingsSharedWithFriends,
  }));
  const [feedback, setFeedback] = useState<string | null>(null);
  const [focusedField, setFocusedField] = useState<'displayName' | 'biography' | null>(null);

  const saveMutation = useMutation({
    mutationFn: saveProfile,
    onSuccess: (saved) => {
      queryClient.setQueryData(['profile'], saved);
      setDraft({
        displayName: saved.displayName ?? '',
        biography: saved.biography ?? '',
        avatarKey: saved.avatarKey ?? 'initials',
        profileIsPublic: saved.profileIsPublic,
        favoritesArePublic: saved.favoritesArePublic,
        watchHistoryIsPublic: saved.watchHistoryIsPublic,
        isDiscoverable: saved.isDiscoverable,
        watchHistorySharedWithFriends: saved.watchHistorySharedWithFriends,
        favoritesSharedWithFriends: saved.favoritesSharedWithFriends,
        ratingsSharedWithFriends: saved.ratingsSharedWithFriends,
      });
      setFeedback('Perfil atualizado.');
      void queryClient.invalidateQueries({ queryKey: ['social'] });
      void queryClient.invalidateQueries({ queryKey: ['reviews'] });
    },
    onError: (error) => {
      showError(error instanceof Error ? error.message : 'Não foi possível salvar o perfil.');
    },
  });

  const initials = draft.displayName.trim().slice(0, 1).toLocaleUpperCase('pt-BR') || 'R';
  const update = (changes: Partial<ProfileDraft>) => {
    setDraft((current) => current ? { ...current, ...changes } : current);
    setFeedback(null);
  };

  return (
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <View style={styles.container}>
        <View style={styles.topBar}>
          <BackButton />
          <Text style={styles.eyebrow}>CONTA REELIX</Text>
        </View>

        <View style={styles.heading}>
          <UserAvatar name={draft.displayName || initials} avatarKey={draft.avatarKey} size={dimensions.avatarSize} />
          <View style={styles.headingText}>
            <Text accessibilityRole="header" style={styles.title}>Seu perfil</Text>
            <Text style={styles.secondaryText}>Sua identidade no Reelix.</Text>
          </View>
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Seu avatar</Text>
          <ScrollView horizontal showsHorizontalScrollIndicator={false} accessibilityRole="radiogroup" contentContainerStyle={styles.avatarChoices}>
            {[{ key: 'initials', label: 'Iniciais', category: 'Seu nome' }, ...avatarOptions].map((option) => <Pressable key={option.key} accessibilityRole="radio" accessibilityLabel={`${option.label}, ${option.category}`} accessibilityState={{ selected: draft.avatarKey === option.key }} disabled={saveMutation.isPending} onPress={() => update({ avatarKey: option.key })} style={[styles.avatarChoice, draft.avatarKey === option.key && styles.avatarChoiceSelected]}>
              <UserAvatar name={draft.displayName || initials} avatarKey={option.key} size={dimensions.avatarSize} />
              <View style={styles.avatarChoiceText}>
                <Text numberOfLines={2} style={styles.avatarChoiceLabel}>{option.label}</Text>
                <Text numberOfLines={2} style={styles.avatarChoiceCategory}>{option.category}</Text>
              </View>
              <View style={styles.avatarChoiceIndicator}>{draft.avatarKey === option.key ? <CheckIcon width={dimensions.iconSize} height={dimensions.iconSize} /> : null}</View>
            </Pressable>)}
          </ScrollView>
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Conquistas</Text>
          {gamification.isPending ? (
            <ActivityIndicator color={colors.brand} />
          ) : gamification.isError ? (
            <PrimaryButton
              label="Tentar carregar"
              onPress={() => void gamification.refetch()}
              variant="secondary"
            />
          ) : gamification.data.achievements.length === 0 ? (
            <Text style={styles.toggleDescription}>Suas conquistas aparecerão aqui.</Text>
          ) : (
            <AchievementGrid achievements={gamification.data.achievements} />
          )}
          <Pressable
            accessibilityRole="button"
            onPress={() => router.push('/gamification')}
            style={styles.preferenceButton}
          >
            <Text style={styles.preferenceLabel}>Ver progresso e estatísticas</Text>
            <Text style={styles.preferenceAction}>Abrir</Text>
          </Pressable>
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Informações</Text>
          <View style={styles.field}>
            <Text style={styles.label}>Nome de exibição</Text>
            <TextInput
              accessibilityLabel="Nome de exibição"
              autoCapitalize="words"
              maxLength={50}
              onBlur={() => setFocusedField(null)}
              onChangeText={(displayName) => update({ displayName })}
              onFocus={() => setFocusedField('displayName')}
              placeholder="Como você quer aparecer"
              placeholderTextColor={colors.textSecondary}
              style={[styles.input, focusedField === 'displayName' ? styles.inputFocused : null]}
              value={draft.displayName}
            />
            <Text style={styles.counter}>{draft.displayName.length}/50</Text>
          </View>
          <View style={styles.field}>
            <Text style={styles.label}>Biografia</Text>
            <TextInput
              accessibilityLabel="Biografia"
              maxLength={280}
              multiline
              onBlur={() => setFocusedField(null)}
              onChangeText={(biography) => update({ biography })}
              onFocus={() => setFocusedField('biography')}
              placeholder="Uma frase sobre você"
              placeholderTextColor={colors.textSecondary}
              style={[
                styles.input,
                styles.biographyInput,
                focusedField === 'biography' ? styles.inputFocused : null,
              ]}
              textAlignVertical="top"
              value={draft.biography}
            />
            <Text style={styles.counter}>{draft.biography.length}/280</Text>
          </View>
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Privacidade</Text>
          <PrivacyToggle
            label="Perfil visível"
            description="Seus dados básicos podem aparecer quando alguém consulta seu perfil."
            value={draft.profileIsPublic}
            onValueChange={(profileIsPublic) => update({ profileIsPublic })}
          />
          <PrivacyToggle
            label="Permitir descoberta"
            description="Permite encontrar seu nome na busca de pessoas, mesmo com o perfil privado."
            value={draft.isDiscoverable}
            onValueChange={(isDiscoverable) => update({ isDiscoverable })}
          />
          <PrivacyToggle
            label="Favoritos visíveis"
            description="Exibe seus títulos favoritos no perfil público."
            value={draft.favoritesArePublic}
            onValueChange={(favoritesArePublic) => update({ favoritesArePublic })}
          />
          <PrivacyToggle
            label="Histórico visível"
            description="A configuração será aplicada quando o histórico público estiver disponível."
            value={draft.watchHistoryIsPublic}
            onValueChange={(watchHistoryIsPublic) => update({ watchHistoryIsPublic })}
          />
          <PrivacyToggle
            label="Compartilhar histórico com amigos"
            description="Compartilha apenas atividades registradas depois de ativar esta opção."
            value={draft.watchHistorySharedWithFriends}
            onValueChange={(watchHistorySharedWithFriends) => update({ watchHistorySharedWithFriends })}
          />
          <PrivacyToggle
            label="Compartilhar favoritos com amigos"
            description="Permite que seus amigos vejam os títulos favoritos."
            value={draft.favoritesSharedWithFriends}
            onValueChange={(favoritesSharedWithFriends) => update({ favoritesSharedWithFriends })}
          />
          <PrivacyToggle
            label="Compartilhar avaliações com amigos"
            description="Compartilha somente avaliações criadas ou atualizadas após ativar esta opção."
            value={draft.ratingsSharedWithFriends}
            onValueChange={(ratingsSharedWithFriends) => update({ ratingsSharedWithFriends })}
          />
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Preferências</Text>
          <Pressable
            accessibilityRole="button"
            accessibilityHint="Abre a edição de gêneros, filmes e séries favoritos"
            onPress={() => router.push({ pathname: '/onboarding', params: { edit: '1' } })}
            style={styles.preferenceButton}
          >
            <Text style={styles.preferenceLabel}>Gêneros e títulos favoritos</Text>
            <Text style={styles.preferenceAction}>Editar</Text>
          </Pressable>
        </View>

        <View style={styles.creditsSection}>
          <Text style={styles.sectionTitle}>Créditos</Text>
          <Link href="https://www.themoviedb.org" asChild>
            <Pressable accessibilityRole="link" style={styles.tmdbLink}>
              <TmdbLogo accessibilityLabel="The Movie Database (TMDB)" width={150} height={38} />
            </Pressable>
          </Link>
          <Text style={styles.creditNotice}>
            This product uses the TMDB API but is not endorsed or certified by TMDB.
          </Text>
        </View>

        {feedback ? (
          <Text accessibilityLiveRegion="polite" style={styles.successText}>
            {feedback}
          </Text>
        ) : null}
        <PrimaryButton
          label="Salvar alterações"
          loading={saveMutation.isPending}
          onPress={() => {
            setFeedback(null);
            saveMutation.mutate({
              ...draft,
              displayName: draft.displayName.trim() || null,
              biography: draft.biography.trim() || null,
            });
          }}
        />
      </View>
    </ScrollView>
  );
}

function PrivacyToggle({
  label,
  description,
  value,
  onValueChange,
}: {
  label: string;
  description: string;
  value: boolean;
  onValueChange: (value: boolean) => void;
}) {
  return (
    <View style={styles.toggleRow}>
      <View style={styles.toggleCopy}>
        <Text style={styles.toggleLabel}>{label}</Text>
        <Text style={styles.toggleDescription}>{description}</Text>
      </View>
      <Switch
        accessibilityLabel={label}
        accessibilityHint={description}
        onValueChange={onValueChange}
        thumbColor={colors.white}
        trackColor={{ false: colors.surfaceElevated, true: colors.brand }}
        value={value}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  avatarChoices: { flexDirection: 'row', gap: spacing.small, paddingBottom: spacing.small },
  avatarChoice: { width: dimensions.posterWidth, height: dimensions.posterHeight, gap: spacing.small, padding: spacing.small, borderWidth: 2, borderColor: colors.border, borderRadius: radii.small, alignItems: 'center', justifyContent: 'center' },
  avatarChoiceSelected: { borderColor: colors.brand, backgroundColor: colors.brandSurface },
  avatarChoiceText: { width: '100%', gap: spacing.extraSmall },
  avatarChoiceIndicator: { position: 'absolute', top: spacing.extraSmall, right: spacing.extraSmall, width: dimensions.iconSize, height: dimensions.iconSize },
  avatarChoiceLabel: { color: colors.textPrimary, fontFamily: fontFamily.medium, fontSize: typography.bodySmall, lineHeight: typography.bodySmall + spacing.extraSmall, minHeight: (typography.bodySmall + spacing.extraSmall) * 2, textAlign: 'center' },
  avatarChoiceCategory: { color: colors.textSecondary, fontFamily: fontFamily.regular, fontSize: typography.caption, lineHeight: typography.caption + spacing.extraSmall, minHeight: (typography.caption + spacing.extraSmall) * 2, textAlign: 'center' },
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
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
    paddingVertical: 8,
  },
  avatar: {
    width: 68,
    height: 68,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
    backgroundColor: colors.surfaceElevated,
    borderWidth: 1,
    borderColor: colors.brand,
  },
  avatarInitial: {
    color: colors.brandSecondary,
    fontSize: typography.title,
    fontFamily: fontFamily.extraBold,
  },
  headingText: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  title: {
    color: colors.textPrimary,
    fontSize: typography.title,
    lineHeight: 36,
    fontFamily: fontFamily.extraBold,
  },
  secondaryText: {
    color: colors.textSecondary,
    fontSize: typography.bodySmall,
    lineHeight: 21,
    fontFamily: fontFamily.regular,
  },
  formSection: {
    gap: spacing.medium,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  creditsSection: {
    gap: spacing.small,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  tmdbLink: {
    alignSelf: 'flex-start',
    minHeight: 44,
    justifyContent: 'center',
  },
  creditNotice: {
    color: colors.textSecondary,
    fontSize: typography.eyebrow,
    lineHeight: 16,
    fontFamily: fontFamily.regular,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: typography.section,
    fontFamily: fontFamily.bold,
  },
  field: {
    gap: spacing.small,
  },
  label: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.semiBold,
  },
  input: {
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
  inputFocused: {
    borderColor: colors.brandSecondary,
  },
  biographyInput: {
    minHeight: 104,
    paddingTop: 12,
  },
  counter: {
    alignSelf: 'flex-end',
    color: colors.textSecondary,
    fontSize: typography.eyebrow,
    fontFamily: fontFamily.regular,
  },
  toggleRow: {
    minHeight: 72,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: spacing.medium,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  toggleCopy: {
    flex: 1,
    gap: spacing.extraSmall,
    paddingVertical: spacing.small,
  },
  toggleLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  toggleDescription: {
    color: colors.textSecondary,
    fontSize: typography.caption,
    lineHeight: 17,
    fontFamily: fontFamily.regular,
  },
  preferenceButton: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
  },
  preferenceLabel: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
  preferenceAction: {
    color: colors.brandSecondary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  successText: {
    color: colors.success,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.medium,
  },
});