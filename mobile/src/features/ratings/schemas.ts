import { z } from 'zod';

export const COMMENT_MAX_LENGTH = 2000;

/** Comment form; the publish button stays disabled while the text is blank, so there is no inline message. */
export const commentFormSchema = z.object({
  content: z
    .string()
    .max(COMMENT_MAX_LENGTH)
    .refine((value) => value.trim().length > 0),
  containsSpoiler: z.boolean(),
  audience: z.enum(['private', 'public']),
});

export type CommentFormValues = z.infer<typeof commentFormSchema>;
