import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';

import CheckIcon from '@/assets/brand/navigation-icons/check.svg';
import { avatarOptions, UserAvatar } from '@/components/ui/user-avatar';
import { colors, dimensions, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type Props = {
  /** Name shown on the "initials" option and in the avatar labels. */
  name: string;
  selectedKey: string;
  disabled: boolean;
  onSelect: (avatarKey: string) => void;
};

const options = [{ key: 'initials', label: 'Iniciais', category: 'Seu nome' }, ...avatarOptions];

/** Horizontal radio list of the brand avatars plus the "initials" option. */
export function AvatarPicker({ name, selectedKey, disabled, onSelect }: Props) {
  return (
    <ScrollView horizontal showsHorizontalScrollIndicator={false} accessibilityRole="radiogroup" contentContainerStyle={styles.choices}>
      {options.map((option) => <Pressable key={option.key} accessibilityRole="radio" accessibilityLabel={`${option.label}, ${option.category}`} accessibilityState={{ selected: selectedKey === option.key }} disabled={disabled} onPress={() => onSelect(option.key)} style={[styles.choice, selectedKey === option.key && styles.choiceSelected]}>
        <UserAvatar name={name} avatarKey={option.key} size={dimensions.avatarSize} />
        <View style={styles.choiceText}>
          <Text numberOfLines={2} style={styles.choiceLabel}>{option.label}</Text>
          <Text numberOfLines={2} style={styles.choiceCategory}>{option.category}</Text>
        </View>
        <View style={styles.choiceIndicator}>{selectedKey === option.key ? <CheckIcon width={dimensions.iconSize} height={dimensions.iconSize} /> : null}</View>
      </Pressable>)}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  choices: { flexDirection: 'row', gap: spacing.small, paddingBottom: spacing.small },
  choice: { width: dimensions.posterWidth, height: dimensions.posterHeight, gap: spacing.small, padding: spacing.small, borderWidth: 2, borderColor: colors.border, borderRadius: radii.small, alignItems: 'center', justifyContent: 'center' },
  choiceSelected: { borderColor: colors.brand, backgroundColor: colors.brandSurface },
  choiceText: { width: '100%', gap: spacing.extraSmall },
  choiceIndicator: { position: 'absolute', top: spacing.extraSmall, right: spacing.extraSmall, width: dimensions.iconSize, height: dimensions.iconSize },
  choiceLabel: { color: colors.textPrimary, fontFamily: fontFamily.medium, fontSize: typography.bodySmall, lineHeight: typography.bodySmall + spacing.extraSmall, minHeight: (typography.bodySmall + spacing.extraSmall) * 2, textAlign: 'center' },
  choiceCategory: { color: colors.textSecondary, fontFamily: fontFamily.regular, fontSize: typography.caption, lineHeight: typography.caption + spacing.extraSmall, minHeight: (typography.caption + spacing.extraSmall) * 2, textAlign: 'center' },
});
