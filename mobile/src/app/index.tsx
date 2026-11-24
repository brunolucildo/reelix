import { AuthScreen } from '@/features/auth/components/auth-screen';
import { SignInForm } from '@/features/auth/components/sign-in-form';

export default function SignInScreen() {
  return (
    <AuthScreen
      title="Suas histórias começam aqui."
      subtitle="Entre para acompanhar tudo o que você assiste."
    >
      <SignInForm />
    </AuthScreen>
  );
}
