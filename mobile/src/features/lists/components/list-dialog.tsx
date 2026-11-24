import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { StyleSheet, Text } from 'react-native';

import { ModalDialog } from '@/components/ui/modal-dialog';
import { PrimaryButton } from '@/components/ui/primary-button';
import type { CustomListSummary } from '@/features/lists/api/lists-api';
import { ListInput } from '@/features/lists/components/list-input';
import { listsStyles } from '@/features/lists/components/lists-styles';
import type { ListDialogController } from '@/features/lists/hooks/use-list-dialog';
import type { ListMutations } from '@/features/lists/hooks/use-list-mutations';
import { listFormSchema, type ListFormValues } from '@/features/lists/schemas';
import { colors, fontFamily, typography } from '@/theme/tokens';

type Props = {
  dialog: ListDialogController;
  mutations: ListMutations;
  onListSaved: (listId: string) => void;
  onListDeleted: () => void;
};

/** Create/edit form, list options menu and delete confirmation, all inside one modal. */
export function ListDialog({ dialog, mutations, onListSaved, onListDeleted }: Props) {
  const { state } = dialog;
  const [busy, setBusy] = useState(false);

  const title = state.kind === 'delete'
    ? 'Excluir lista?'
    : state.kind === 'menu'
      ? state.list.name
      : state.kind === 'form' && state.list
        ? 'Editar lista'
        : 'Criar lista';

  const save = async (editing: CustomListSummary | null, values: ListFormValues) => {
    setBusy(true);
    const saved = await mutations.saveList(editing, values.name, values.description);
    setBusy(false);
    if (saved) {
      dialog.close();
      onListSaved(saved.id);
    }
  };

  const confirmDelete = async (list: CustomListSummary) => {
    setBusy(true);
    const deleted = await mutations.deleteList(list.id);
    setBusy(false);
    if (deleted !== null) {
      dialog.close();
      onListDeleted();
    }
  };

  return (
    <ModalDialog visible={state.kind !== 'closed'} title={title} busy={busy} onClose={dialog.close}>
      {state.kind === 'form' ? (
        <ListForm
          key={state.list?.id ?? 'new'}
          editing={state.list}
          busy={busy}
          onSubmit={(values) => save(state.list, values)}
          onCancel={dialog.close}
        />
      ) : state.kind === 'menu' ? (
        <>
          <PrimaryButton label="Editar lista" variant="secondary" onPress={() => dialog.openForm(state.list)} />
          <PrimaryButton label="Excluir lista" variant="secondary" onPress={() => dialog.openDelete(state.list)} />
        </>
      ) : state.kind === 'delete' ? (
        <>
          <Text style={listsStyles.subtitle}>Excluir a lista &quot;{state.list.name}&quot;?</Text>
          <Text style={listsStyles.muted}>
            Apenas a lista e seus vínculos serão removidos. Filmes e séries do catálogo, Favoritos e Watchlist não serão alterados.
          </Text>
          <PrimaryButton label="Confirmar exclusão" loading={busy} onPress={() => void confirmDelete(state.list)} />
          <PrimaryButton label="Cancelar" disabled={busy} variant="secondary" onPress={dialog.close} />
        </>
      ) : null}
    </ModalDialog>
  );
}

type FormProps = {
  editing: CustomListSummary | null;
  busy: boolean;
  onSubmit: (values: ListFormValues) => Promise<void>;
  onCancel: () => void;
};

function ListForm({ editing, busy, onSubmit, onCancel }: FormProps) {
  const form = useForm<ListFormValues>({
    resolver: zodResolver(listFormSchema),
    defaultValues: { name: editing?.name ?? '', description: editing?.description ?? '' },
  });
  const { errors } = form.formState;
  const message = errors.name?.message ?? errors.description?.message;

  return (
    <>
      <Controller
        control={form.control}
        name="name"
        render={({ field }) => (
          <ListInput
            accessibilityLabel="Nome da lista"
            editable={!busy}
            onBlur={field.onBlur}
            onChangeText={field.onChange}
            placeholder="Nome"
            value={field.value}
          />
        )}
      />
      <Controller
        control={form.control}
        name="description"
        render={({ field }) => (
          <ListInput
            accessibilityLabel="Descrição da lista"
            editable={!busy}
            multiline
            onBlur={field.onBlur}
            onChangeText={field.onChange}
            placeholder="Descrição (opcional)"
            value={field.value ?? ''}
          />
        )}
      />
      {message ? <Text accessibilityRole="alert" style={styles.validation}>{message}</Text> : null}
      <PrimaryButton
        label={editing ? 'Salvar alterações' : 'Criar lista'}
        loading={busy}
        onPress={() => void form.handleSubmit(onSubmit)()}
      />
      <PrimaryButton label="Cancelar" disabled={busy} variant="secondary" onPress={onCancel} />
    </>
  );
}

const styles = StyleSheet.create({
  validation: {
    color: colors.warning,
    fontSize: typography.caption,
    fontFamily: fontFamily.regular,
  },
});
