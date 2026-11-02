import { useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';

import { AchievementMedal } from '@/components/gamification/achievement-medal';
import { ModalDialog } from '@/components/modal-dialog';
import type { AchievementProgress } from '@/services/api-client';
import { colors, dimensions, fontFamily, spacing, typography } from '@/theme/tokens';

type Props = {
  achievements: AchievementProgress[];
  compact?: boolean;
};

export function AchievementGrid({ achievements, compact = false }: Props) {
  const [selected, setSelected] = useState<AchievementProgress | null>(null);
  const size = compact ? dimensions.minimumTouchTarget : dimensions.avatarSize;
  return (
    <View style={styles.container}>
      <View style={styles.grid}>
        {achievements.map((achievement) => (
          <Pressable
            key={achievement.code}
            accessibilityRole="button"
            accessibilityLabel={`${achievement.title}, ${achievement.isEarned ? 'conquistada' : 'bloqueada'}. Ver detalhes`}
            onPress={() => setSelected(achievement)}
            style={styles.medal}
          >
            <AchievementMedal
              code={achievement.code}
              title={achievement.title}
              earned={achievement.isEarned}
              size={size}
            />
          </Pressable>
        ))}
      </View>
      <ModalDialog visible={selected !== null} title={selected?.title ?? 'Conquista'} onClose={() => setSelected(null)}>
        {selected ? (
          <>
            <AchievementMedal code={selected.code} title={selected.title} earned={selected.isEarned} size={dimensions.avatarSize} />
            <Text style={styles.text}>{selected.description}</Text>
            <Text style={styles.text}>Critério de desbloqueio: {selected.description}</Text>
            <Text style={styles.text}>Progresso: {selected.currentValue} de {selected.threshold}</Text>
            <Text style={styles.text}>
              Estado: {selected.isEarned ? 'Desbloqueada' : selected.status === 'revoked' ? 'Revogada' : 'Bloqueada'}
            </Text>
            {selected.firstEarnedAt ? (
              <Text style={styles.text}>
                Desbloqueada em {new Date(selected.firstEarnedAt).toLocaleDateString('pt-BR')}
              </Text>
            ) : null}
            {selected.reactivatedAt ? (
              <Text style={styles.text}>
                Reativada em {new Date(selected.reactivatedAt).toLocaleDateString('pt-BR')}
              </Text>
            ) : null}
            {selected.isMetadataIncomplete ? (
              <Text style={styles.text}>Alguns dados do catálogo ainda estão incompletos.</Text>
            ) : null}
          </>
        ) : null}
      </ModalDialog>
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flexShrink: 1 },
  grid: { flexDirection: 'row', flexWrap: 'wrap', gap: spacing.small },
  medal: { minWidth: dimensions.minimumTouchTarget, minHeight: dimensions.minimumTouchTarget, padding: spacing.extraSmall, alignItems: 'center', justifyContent: 'center' },
  text: { color: colors.textPrimary, fontFamily: fontFamily.regular, fontSize: typography.bodySmall },
});
