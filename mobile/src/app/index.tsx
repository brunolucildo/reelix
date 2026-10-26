import { AuthScreen } from '@/components/auth/auth-screen';
import { SignInForm } from '@/components/auth/sign-in-form';

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
