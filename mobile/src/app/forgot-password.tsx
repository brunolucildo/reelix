import { AuthScreen } from '@/features/auth/components/auth-screen';
import { ForgotPasswordForm } from '@/features/auth/components/forgot-password-form';

export default function ForgotPasswordScreen() {
  return (
    <AuthScreen
      title="Recupere seu acesso."
      subtitle="Enviaremos as instruções para o e-mail informado."
    >
      <ForgotPasswordForm />
    </AuthScreen>
  );
}
