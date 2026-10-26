import { useState } from 'react';
import { Redirect } from 'expo-router';

import { AuthScreen } from '@/components/auth/auth-screen';
import { ResetPasswordForm } from '@/components/auth/reset-password-form';
import { useAuthSession } from '@/providers/auth-provider';

export default function ResetPasswordScreen() {
  const { session } = useAuthSession();
  const [completed, setCompleted] = useState(false);

  if (!session) {
    return <Redirect href="/" />;
  }

  return (
    <AuthScreen
      title={completed ? 'Senha atualizada.' : 'Escolha uma nova senha.'}
      subtitle={
        completed
          ? 'Sua senha foi alterada. Sua sessão continua protegida.'
          : 'Use uma senha nova para recuperar o acesso à sua conta.'
      }
    >
      <ResetPasswordForm completed={completed} onCompleted={() => setCompleted(true)} />
    </AuthScreen>
  );
}
