import { AuthScreen } from '@/features/auth/components/auth-screen';
import { SignUpForm } from '@/features/auth/components/sign-up-form';

export default function SignUpScreen() {
  return (
    <AuthScreen
      title="Crie sua conta."
      subtitle="Um lugar para tudo o que você assiste."
    >
      <SignUpForm />
    </AuthScreen>
  );
}
