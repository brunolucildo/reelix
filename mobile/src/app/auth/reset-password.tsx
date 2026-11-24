import { useState } from 'react';

import { AuthScreen } from '@/features/auth/components/auth-screen';
import { ResetPasswordForm } from '@/features/auth/components/reset-password-form';

export default function ResetPasswordScreen() {
  const [completed, setCompleted] = useState(false);

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
