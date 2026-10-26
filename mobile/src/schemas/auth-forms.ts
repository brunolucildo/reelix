import { z } from 'zod';

const email = z.string().trim().email('Informe um e-mail válido.');

export const emailSchema = z.object({ email });
export type EmailValues = z.infer<typeof emailSchema>;

export const signInSchema = z.object({
  email,
  password: z.string().min(1, 'Informe sua senha.'),
});
export type SignInValues = z.infer<typeof signInSchema>;

export const signUpSchema = z.object({
  email,
  password: z.string().min(8, 'A senha deve ter pelo menos 8 caracteres.'),
});
export type SignUpValues = z.infer<typeof signUpSchema>;

export const resetPasswordSchema = z
  .object({
    password: z.string().min(8, 'A senha deve ter pelo menos 8 caracteres.'),
    confirmPassword: z.string().min(1, 'Confirme a nova senha.'),
  })
  .refine((values) => values.password === values.confirmPassword, {
    path: ['confirmPassword'],
    message: 'As senhas não conferem.',
  });
export type ResetPasswordValues = z.infer<typeof resetPasswordSchema>;
