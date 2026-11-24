import { type Control, Controller } from 'react-hook-form';

import { ToggleRow } from '@/components/ui/toggle-row';
import type { ProfileFormValues } from '@/features/profile/schemas';

type ToggleField = Exclude<keyof ProfileFormValues, 'displayName' | 'biography' | 'avatarKey'>;

const toggles: { name: ToggleField; label: string; description: string }[] = [
  { name: 'profileIsPublic', label: 'Perfil visível', description: 'Seus dados básicos podem aparecer quando alguém consulta seu perfil.' },
  { name: 'isDiscoverable', label: 'Permitir descoberta', description: 'Permite encontrar seu nome na busca de pessoas, mesmo com o perfil privado.' },
  { name: 'favoritesArePublic', label: 'Favoritos visíveis', description: 'Exibe seus títulos favoritos no perfil público.' },
  { name: 'watchHistoryIsPublic', label: 'Histórico visível', description: 'A configuração será aplicada quando o histórico público estiver disponível.' },
  { name: 'watchHistorySharedWithFriends', label: 'Compartilhar histórico com amigos', description: 'Compartilha apenas atividades registradas depois de ativar esta opção.' },
  { name: 'favoritesSharedWithFriends', label: 'Compartilhar favoritos com amigos', description: 'Permite que seus amigos vejam os títulos favoritos.' },
  { name: 'ratingsSharedWithFriends', label: 'Compartilhar avaliações com amigos', description: 'Compartilha somente avaliações criadas ou atualizadas após ativar esta opção.' },
];

type Props = {
  control: Control<ProfileFormValues>;
  /** Called after any toggle changes (clears the "Perfil atualizado" line). */
  onChange: () => void;
};

/** The seven privacy and sharing switches of the profile form. */
export function PrivacyToggles({ control, onChange }: Props) {
  return (
    <>
      {toggles.map(({ name, label, description }) => (
        <Controller
          key={name}
          control={control}
          name={name}
          render={({ field }) => (
            <ToggleRow
              variant="privacy"
              label={label}
              description={description}
              accessibilityHint={description}
              value={field.value}
              onValueChange={(value) => {
                field.onChange(value);
                onChange();
              }}
            />
          )}
        />
      ))}
    </>
  );
}
