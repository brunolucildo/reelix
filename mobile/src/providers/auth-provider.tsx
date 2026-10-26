import type { Session } from '@supabase/supabase-js';
import {
  createContext,
  type PropsWithChildren,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useQueryClient } from '@tanstack/react-query';

import { supabase } from '@/services/supabase';

type AuthContextValue = {
  session: Session | null;
  isLoading: boolean;
  initializationError: string | null;
  retryInitialization: () => void;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: PropsWithChildren) {
  const queryClient = useQueryClient();
  const [session, setSession] = useState<Session | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [initializationError, setInitializationError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;

    const {
      data: { subscription },
    } = supabase.auth.onAuthStateChange((_event, nextSession) => {
      if (active) {
        setSession(nextSession);
      }
    });

    const restoreSession = async () => {
      try {
        const { data, error } = await supabase.auth.getSession();
        if (error) {
          throw error;
        }
        if (active) {
          setSession(data.session);
        }
      } catch {
        if (active) {
          setInitializationError(
            'Não foi possível restaurar sua sessão. Verifique a conexão ou o armazenamento seguro e tente novamente.',
          );
        }
      } finally {
        if (active) {
          setIsLoading(false);
        }
      }
    };

    void restoreSession();

    return () => {
      active = false;
      subscription.unsubscribe();
    };
  }, [attempt]);

  const retryInitialization = useCallback(() => {
    setInitializationError(null);
    setIsLoading(true);
    setAttempt((current) => current + 1);
  }, []);

  const signOut = useCallback(async () => {
    let signOutError: Error | null = null;
    try {
      const { error } = await supabase.auth.signOut({ scope: 'local' });
      if (error) {
        signOutError = error;
      }
    } catch (error) {
      signOutError = error instanceof Error ? error : new Error('Logout failed.');
    } finally {
      setSession(null);
      queryClient.clear();
    }

    if (signOutError) {
      throw signOutError;
    }
  }, [queryClient]);

  const value = useMemo(
    () => ({
      session,
      isLoading,
      initializationError,
      retryInitialization,
      signOut,
    }),
    [initializationError, isLoading, retryInitialization, session, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuthSession(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuthSession must be used inside AuthProvider.');
  }

  return context;
}
