import { z } from 'zod';

import { parseLocalDateTimeInput } from '@/lib/format';

export const WATCHED_AT_INVALID_MESSAGE = 'Informe uma data e hora válidas.';

/** Assisted "watched at" input: local "DD/MM/AAAA HH:mm". */
export const watchedAtFormSchema = z.object({
  watchedAt: z.string().refine((value) => parseLocalDateTimeInput(value) !== null, {
    message: WATCHED_AT_INVALID_MESSAGE,
  }),
});

export type WatchedAtFormValues = z.infer<typeof watchedAtFormSchema>;
