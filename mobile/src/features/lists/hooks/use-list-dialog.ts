import { useState } from 'react';

import type { CustomListSummary } from '@/features/lists/api/lists-api';

export type ListDialogState =
  | { kind: 'closed' }
  | { kind: 'form'; list: CustomListSummary | null }
  | { kind: 'menu'; list: CustomListSummary }
  | { kind: 'delete'; list: CustomListSummary };

/** Which dialog of the lists screen is open: create/edit form, list options menu or delete confirmation. */
export function useListDialog() {
  const [state, setState] = useState<ListDialogState>({ kind: 'closed' });

  return {
    state,
    openForm: (list: CustomListSummary | null) => setState({ kind: 'form', list }),
    openMenu: (list: CustomListSummary) => setState({ kind: 'menu', list }),
    openDelete: (list: CustomListSummary) => setState({ kind: 'delete', list }),
    close: () => setState({ kind: 'closed' }),
  };
}

export type ListDialogController = ReturnType<typeof useListDialog>;
