import { z } from 'zod';

export const LIST_NAME_MESSAGE = 'Informe um nome com até 80 caracteres.';
export const LIST_DESCRIPTION_MESSAGE = 'A descrição deve ter no máximo 500 caracteres.';

export const listFormSchema = z.object({
  name: z.string().trim().min(1, LIST_NAME_MESSAGE).max(80, LIST_NAME_MESSAGE),
  description: z.string().trim().max(500, LIST_DESCRIPTION_MESSAGE),
});

export type ListFormValues = z.infer<typeof listFormSchema>;
