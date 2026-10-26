import { AuthScreen } from '@/components/auth/auth-screen';
import { ForgotPasswordForm } from '@/components/auth/forgot-password-form';

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
