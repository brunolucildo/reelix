import { zodResolver } from '@hookform/resolvers/zod';
import { Link, useRouter } from 'expo-router';
import { useState } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import TmdbLogo from '@/assets/brand/logos/tmdb-logo-blue.svg';
import { BackButton } from '@/components/navigation/back-button';
import { PrimaryButton } from '@/components/ui/primary-button';
import { UserAvatar } from '@/components/ui/user-avatar';
import type { ProfileData } from '@/features/profile/api/profile-api';
import { AvatarPicker } from '@/features/profile/components/avatar-picker';
import { PrivacyToggles } from '@/features/profile/components/privacy-toggles';
import { ProfileAchievementsSection } from '@/features/profile/components/profile-achievements-section';
import { ProfileInfoFields } from '@/features/profile/components/profile-info-fields';
import { useSaveProfile } from '@/features/profile/hooks/use-save-profile';
import {
  profileFormSchema,
  toProfileFormValues,
  type ProfileFormValues,
} from '@/features/profile/schemas';
import { getInitials } from '@/lib/format';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

/** Profile form: avatar, achievements, name and bio, privacy switches, preferences and credits. */
export function ProfileEditor({ initialProfile }: { initialProfile: ProfileData }) {
  const router = useRouter();
  const save = useSaveProfile();
  const [feedback, setFeedback] = useState<string | null>(null);
  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: toProfileFormValues(initialProfile),
  });
  const displayName = useWatch({ control: form.control, name: 'displayName' });
  const avatarKey = useWatch({ control: form.control, name: 'avatarKey' });
  const avatarName = displayName || getInitials(displayName, 'R');
  const clearFeedback = () => setFeedback(null);
  const { errors } = form.formState;

  const submit = form.handleSubmit((values) => {
    setFeedback(null);
    save.mutate({
      ...values,
      displayName: values.displayName.trim() || null,
      biography: values.biography.trim() || null,
    }, {
      onSuccess: (saved) => {
        form.reset(toProfileFormValues(saved));
        setFeedback('Perfil atualizado.');
      },
    });
  });

  return (
    <ScrollView contentContainerStyle={styles.content} keyboardShouldPersistTaps="handled">
      <View style={styles.container}>
        <View style={styles.topBar}>
          <BackButton />
          <Text style={styles.eyebrow}>CONTA REELIX</Text>
        </View>

        <View style={styles.heading}>
          <UserAvatar name={avatarName} avatarKey={avatarKey} size={dimensions.avatarSize} />
          <View style={styles.headingText}>
            <Text accessibilityRole="header" style={styles.title}>Seu perfil</Text>
            <Text style={styles.secondaryText}>Sua identidade no Reelix.</Text>
          </View>
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Seu avatar</Text>
          <AvatarPicker
            name={avatarName}
            selectedKey={avatarKey}
            disabled={save.isPending}
            onSelect={(key) => {
              form.setValue('avatarKey', key);
              clearFeedback();
            }}
          />
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Conquistas</Text>
          <ProfileAchievementsSection />
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Informações</Text>
          <ProfileInfoFields
            control={form.control}
            onChange={clearFeedback}
            error={errors.displayName?.message ?? errors.biography?.message}
          />
        </View>

        <View style={styles.formSection}>
          <Text style={styles.sectionTitle}>Privacidade</Text>
          <PrivacyToggles control={form.control} onChange={clearFeedback} />
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
        <PrimaryButton label="Salvar alterações" loading={save.isPending} onPress={() => void submit()} />
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
    flexDirection: 'row',
    alignItems: 'center',
    gap: spacing.medium,
    paddingVertical: 8,
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
