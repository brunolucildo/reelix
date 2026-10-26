import { AuthScreen } from '@/components/auth/auth-screen';
import { SignUpForm } from '@/components/auth/sign-up-form';

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
