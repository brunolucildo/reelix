import { useMutation, useQueryClient } from '@tanstack/react-query';

import {
  cancelFriendRequest,
  removeFriend,
  respondToFriendRequest,
  sendFriendRequest,
} from '@/features/social/api/social-api';
import { socialQueryKeys } from '@/features/social/query-keys';
import { useErrorToast } from '@/providers/toast-provider';

/** Friend request actions; each one refreshes every social list and drops cached friend profiles. */
export function useSocialMutations() {
  const queryClient = useQueryClient();
  const refreshSocial = async () => {
    queryClient.removeQueries({ queryKey: socialQueryKeys.friendProfileAll });
    await queryClient.invalidateQueries({ queryKey: socialQueryKeys.all });
  };

  const request = useMutation({ mutationFn: sendFriendRequest, onSuccess: refreshSocial });
  const respond = useMutation({
    mutationFn: ({ id, response }: { id: string; response: 'accept' | 'decline' }) =>
      respondToFriendRequest(id, response),
    onSuccess: refreshSocial,
  });
  const cancel = useMutation({ mutationFn: cancelFriendRequest, onSuccess: refreshSocial });
  const remove = useMutation({ mutationFn: removeFriend, onSuccess: refreshSocial });

  useErrorToast(request.error, 'Não foi possível enviar a solicitação de amizade.');
  useErrorToast(respond.error, 'Não foi possível responder à solicitação.');
  useErrorToast(cancel.error, 'Não foi possível cancelar a solicitação.');
  useErrorToast(remove.error, 'Não foi possível remover a amizade.');

  return {
    request,
    respond,
    cancel,
    remove,
    busy: request.isPending || respond.isPending || cancel.isPending || remove.isPending,
  };
}

export type SocialMutations = ReturnType<typeof useSocialMutations>;
