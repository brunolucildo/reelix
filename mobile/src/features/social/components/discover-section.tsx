import { useRouter } from 'expo-router';
import { useState } from 'react';
import { Pressable, Text, TextInput, View } from 'react-native';

import type { SocialUser } from '@/features/social/api/social-api';
import { SocialAvatar } from '@/features/social/components/social-avatar';
import { socialStyles as styles } from '@/features/social/components/social-styles';
import type { SocialMutations } from '@/features/social/hooks/use-social-mutations';
import { colors } from '@/theme/tokens';

type Props = {
  searchText: string;
  onSearchTextChange: (text: string) => void;
  results: SocialUser[] | undefined;
  mutations: SocialMutations;
};

/** "Descobrir": search people by name and send, cancel or answer friend requests inline. */
export function DiscoverSection({ searchText, onSearchTextChange, results, mutations }: Props) {
  const router = useRouter();
  const [focused, setFocused] = useState(false);
  const { busy, cancel, respond, request } = mutations;
  const openProfile = (accountId: string) =>
    router.push({ pathname: '/social-profile/[accountId]', params: { accountId } });

  return (
    <View style={styles.section}>
      <Text style={styles.sectionTitle}>Encontrar pessoas</Text>
      <TextInput
        accessibilityLabel="Buscar pessoas pelo nome"
        autoCapitalize="words"
        onChangeText={onSearchTextChange}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
        placeholder="Buscar por nome"
        placeholderTextColor={colors.textSecondary}
        style={[styles.input, focused && styles.inputFocused]}
        value={searchText}
      />
      {searchText.trim().length < 2 ? <Text style={styles.secondaryText}>Digite pelo menos 2 caracteres.</Text> : null}
      {results?.length === 0 ? (
        <Text style={styles.secondaryText}>Nenhuma pessoa encontrada.</Text>
      ) : null}
      {results?.map((user) => (
        <View key={user.accountId} style={styles.listRow}>
          <SocialAvatar name={user.displayName} uri={user.avatarUrl} />
          <Pressable accessibilityRole="button" accessibilityLabel={`Ver perfil de ${user.displayName}`} onPress={() => openProfile(user.accountId)} style={[styles.personDetails, styles.friendLink]}>
            <Text style={styles.rowTitle}>{user.displayName}</Text>
            {user.relationshipStatus === 'accepted' ? <Text style={styles.secondaryText}>Amigos</Text> : user.relationshipStatus === 'pending' ? <Text style={styles.secondaryText}>{user.requestSentByMe ? 'Solicitação enviada' : 'Quer ser seu amigo'}</Text> : null}
          </Pressable>
          {user.relationshipStatus === 'accepted' ? (
            <Pressable
              accessibilityRole="button"
              onPress={() => openProfile(user.accountId)}
              style={styles.actionButton}
            >
              <Text style={styles.actionText}>Ver perfil</Text>
            </Pressable>
          ) : user.relationshipStatus === 'pending' && user.relationshipId ? (
            user.requestSentByMe ? (
              <Pressable
                accessibilityRole="button"
                disabled={busy}
                onPress={() => cancel.mutate(user.relationshipId!)}
                style={styles.actionButton}
              >
                <Text style={styles.actionText}>Cancelar pedido</Text>
              </Pressable>
            ) : (
              <View style={styles.inlineActions}>
                <Pressable
                  accessibilityRole="button"
                  disabled={busy}
                  onPress={() => respond.mutate({ id: user.relationshipId!, response: 'accept' })}
                  style={styles.actionButton}
                >
                  <Text style={styles.actionText}>Aceitar</Text>
                </Pressable>
                <Pressable
                  accessibilityRole="button"
                  disabled={busy}
                  onPress={() => respond.mutate({ id: user.relationshipId!, response: 'decline' })}
                  style={styles.actionButton}
                >
                  <Text style={styles.actionText}>Recusar</Text>
                </Pressable>
              </View>
            )
          ) : (
            <>
              {user.relationshipStatus === 'declined' ? (
                <Text style={styles.secondaryText}>Solicitação recusada anteriormente</Text>
              ) : user.relationshipStatus === 'cancelled' ? (
                <Text style={styles.secondaryText}>Solicitação cancelada</Text>
              ) : user.relationshipStatus === 'removed' ? (
                <Text style={styles.secondaryText}>Amizade removida</Text>
              ) : null}
              <Pressable
                accessibilityRole="button"
                disabled={busy}
                onPress={() => request.mutate(user.accountId)}
                style={styles.actionButton}
              >
                <Text style={styles.actionText}>
                  {user.relationshipStatus === 'declined' || user.relationshipStatus === 'cancelled'
                    ? 'Enviar nova solicitação'
                    : 'Adicionar'}
                </Text>
              </Pressable>
            </>
          )}
        </View>
      ))}
    </View>
  );
}
