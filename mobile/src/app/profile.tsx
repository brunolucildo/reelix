import { Link, useRouter } from 'expo-router';
import { useState } from 'react';
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Switch, Text, TextInput, View } from 'react-native';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { PrimaryButton } from '@/components/auth/primary-button';
import { fetchProfile, saveProfile, type ProfileData } from '@/services/api-client';
import { colors, fontFamily, radii, spacing } from '@/theme/tokens';
import TmdbLogo from '../../assets/brand/logos/tmdb-logo-blue.svg';

type ProfileDraft = Omit<ProfileData, 'avatarUrl' | 'displayName' | 'biography'> & {
  displayName: string;
  biography: string;
};

export default function ProfileScreen() {
  const profile = useQuery({ queryKey: ['profile'], queryFn: fetchProfile });

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
  const [draft, setDraft] = useState<ProfileDraft>(() => ({
    displayName: initialProfile.displayName ?? '',
    biography: initialProfile.biography ?? '',
    profileIsPublic: initialProfile.profileIsPublic,
    favoritesArePublic: initialProfile.favoritesArePublic,
    watchHistoryIsPublic: initialProfile.watchHistoryIsPublic,
  }));
  const [feedback, setFeedback] = useState<string | null>(null);

  const saveMutation = useMutation({
    mutationFn: saveProfile,
    onSuccess: (saved) => {
      queryClient.setQueryData(['profile'], saved);
      setDraft({
        displayName: saved.displayName ?? '',
        biography: saved.biography ?? '',
        profileIsPublic: saved.profileIsPublic,
        favoritesArePublic: saved.favoritesArePublic,
        watchHistoryIsPublic: saved.watchHistoryIsPublic,
      });
      setFeedback('Perfil atualizado.');
    },
    onError: (error) => {
      setFeedback(error instanceof Error ? error.message : 'Não foi possível salvar o perfil.');
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
          <Pressable accessibilityRole="button" onPress={() => router.back()} style={styles.backButton}>
            <Text style={styles.backLabel}>Voltar</Text>
          </Pressable>
          <Text style={styles.eyebrow}>CONTA REELIX</Text>
        </View>

        <View style={styles.heading}>
          <View accessibilityLabel="Avatar padrão" style={styles.avatar}>
            <Text style={styles.avatarInitial}>{initials}</Text>
          </View>
          <View style={styles.headingText}>
            <Text accessibilityRole="header" style={styles.title}>Seu perfil</Text>
            <Text style={styles.secondaryText}>Sua identidade no Reelix.</Text>
          </View>
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Informações</Text>
          <View style={styles.field}>
            <Text style={styles.label}>Nome de exibição</Text>
            <TextInput
              accessibilityLabel="Nome de exibição"
              autoCapitalize="words"
              maxLength={50}
              onChangeText={(displayName) => update({ displayName })}
              placeholder="Como você quer aparecer"
              placeholderTextColor={colors.textSecondary}
              style={styles.input}
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
              onChangeText={(biography) => update({ biography })}
              placeholder="Uma frase sobre você"
              placeholderTextColor={colors.textSecondary}
              style={[styles.input, styles.biographyInput]}
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
            value={draft.profileIsPublic}
            onValueChange={(profileIsPublic) => update({ profileIsPublic })}
          />
          <PrivacyToggle
            label="Favoritos visíveis"
            value={draft.favoritesArePublic}
            onValueChange={(favoritesArePublic) => update({ favoritesArePublic })}
          />
          <PrivacyToggle
            label="Histórico visível"
            value={draft.watchHistoryIsPublic}
            onValueChange={(watchHistoryIsPublic) => update({ watchHistoryIsPublic })}
          />
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Preferências</Text>
          <Pressable
            accessibilityRole="button"
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
          <Text accessibilityRole={saveMutation.isError ? 'alert' : 'text'} style={saveMutation.isError ? styles.errorText : styles.successText}>
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
  value,
  onValueChange,
}: {
  label: string;
  value: boolean;
  onValueChange: (value: boolean) => void;
}) {
  return (
    <View style={styles.toggleRow}>
      <Text style={styles.toggleLabel}>{label}</Text>
      <Switch
        accessibilityLabel={label}
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
    fontSize: 14,
    fontFamily: fontFamily.semiBold,
  },
  eyebrow: {
    color: colors.brandSecondary,
    fontSize: 11,
    fontFamily: fontFamily.extraBold,
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
    fontSize: 27,
    fontFamily: fontFamily.extraBold,
  },
  headingText: {
    flex: 1,
    gap: 5,
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
  formSection: {
    gap: 14,
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  creditsSection: {
    gap: 12,
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
    fontSize: 11,
    lineHeight: 16,
    fontFamily: fontFamily.regular,
  },
  sectionTitle: {
    color: colors.textPrimary,
    fontSize: 16,
    fontFamily: fontFamily.bold,
  },
  field: {
    gap: 7,
  },
  label: {
    color: colors.textPrimary,
    fontSize: 13,
    fontFamily: fontFamily.semiBold,
  },
  input: {
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
  biographyInput: {
    minHeight: 104,
    paddingTop: 12,
  },
  counter: {
    alignSelf: 'flex-end',
    color: colors.textSecondary,
    fontSize: 11,
    fontFamily: fontFamily.regular,
  },
  toggleRow: {
    minHeight: 52,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  toggleLabel: {
    color: colors.textPrimary,
    fontSize: 14,
    fontFamily: fontFamily.medium,
  },
  preferenceButton: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 14,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.surface,
  },
  preferenceLabel: {
    color: colors.textPrimary,
    fontSize: 13,
    fontFamily: fontFamily.medium,
  },
  preferenceAction: {
    color: colors.brandSecondary,
    fontSize: 13,
    fontFamily: fontFamily.bold,
  },
  errorText: {
    color: colors.error,
    fontSize: 13,
    lineHeight: 19,
    fontFamily: fontFamily.medium,
  },
  successText: {
    color: colors.success,
    fontSize: 13,
    fontFamily: fontFamily.medium,
  },
});