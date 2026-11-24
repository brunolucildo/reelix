import { zodResolver } from '@hookform/resolvers/zod';
import { Controller, useForm, useWatch } from 'react-hook-form';
import { StyleSheet, Text, TextInput, View } from 'react-native';

import { PrimaryButton } from '@/components/ui/primary-button';
import { AudienceSelector } from '@/features/ratings/components/audience-selector';
import { reviewsStyles as shared } from '@/features/ratings/components/reviews-styles';
import { SpoilerToggle } from '@/features/ratings/components/spoiler-toggle';
import {
  COMMENT_MAX_LENGTH,
  commentFormSchema,
  type CommentFormValues,
} from '@/features/ratings/schemas';
import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

const EMPTY_VALUES: CommentFormValues = { content: '', containsSpoiler: false, audience: 'private' };

type Props = {
  /** Set while editing an existing comment; `values` are its current content, spoiler flag and audience. */
  editing: { id: string; values: CommentFormValues } | null;
  isSaving: boolean;
  onSave: (commentId: string | undefined, values: CommentFormValues, onSuccess: () => void) => void;
  /** Leaves edit mode (after saving or cancelling). */
  onFinish: () => void;
};

/** Write or edit a comment: text, spoiler flag and audience. */
export function CommentForm({ editing, isSaving, onSave, onFinish }: Props) {
  const form = useForm<CommentFormValues>({
    resolver: zodResolver(commentFormSchema),
    defaultValues: editing?.values ?? EMPTY_VALUES,
  });
  const content = useWatch({ control: form.control, name: 'content' });
  const finish = () => {
    form.reset(EMPTY_VALUES);
    onFinish();
  };
  const submit = form.handleSubmit((values) => onSave(editing?.id, values, finish));

  return (
    <View style={shared.card}>
      <Text style={shared.subheading}>{editing ? 'Editar comentário' : 'Escrever comentário'}</Text>
      <Controller
        control={form.control}
        name="content"
        render={({ field }) => (
          <TextInput
            accessibilityLabel="Texto do comentário"
            accessibilityHint="Máximo de 2.000 caracteres"
            editable={!isSaving}
            maxLength={COMMENT_MAX_LENGTH}
            multiline
            onBlur={field.onBlur}
            onChangeText={field.onChange}
            placeholder="Compartilhe sua opinião"
            placeholderTextColor={colors.textSecondary}
            style={styles.input}
            textAlignVertical="top"
            value={field.value}
          />
        )}
      />
      <Text style={styles.characterCount}>{content.length}/2.000</Text>
      <Controller
        control={form.control}
        name="containsSpoiler"
        render={({ field }) => (
          <SpoilerToggle
            selected={field.value}
            label={field.value ? 'Este comentário contém spoiler' : 'Marcar como spoiler'}
            onPress={() => field.onChange(!field.value)}
          />
        )}
      />
      <Controller
        control={form.control}
        name="audience"
        render={({ field }) => (
          <AudienceSelector audience={field.value} onChange={field.onChange} disabled={isSaving} />
        )}
      />
      <PrimaryButton
        label={editing ? 'Salvar alterações' : 'Publicar comentário'}
        loading={isSaving}
        disabled={content.trim().length === 0}
        onPress={() => void submit()}
      />
      {editing ? (
        <PrimaryButton label="Cancelar edição" variant="secondary" disabled={isSaving} onPress={finish} />
      ) : null}
    </View>
  );
}

const styles = StyleSheet.create({
  input: {
    minHeight: 130,
    padding: spacing.medium,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radii.small,
    backgroundColor: colors.background,
    color: colors.textPrimary,
    fontSize: typography.body,
    fontFamily: fontFamily.regular,
  },
  characterCount: {
    alignSelf: 'flex-end',
    color: colors.textSecondary,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
});
