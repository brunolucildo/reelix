import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Animated, Pressable, StyleSheet, Text, View } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';

import { colors, fontFamily, radii, spacing, typography } from '@/theme/tokens';

type ToastTone = 'error' | 'success';

type Toast = {
  id: number;
  message: string;
  tone: ToastTone;
};

type ToastContextValue = {
  toast: Toast | null;
  showToast: (message: string, tone?: ToastTone) => void;
  showError: (message: string) => void;
  dismissToast: () => void;
};

const TOAST_DURATION_MS = 5000;

const ToastContext = createContext<ToastContextValue | null>(null);

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<Toast | null>(null);
  const nextId = useRef(0);

  const dismissToast = useCallback(() => setToast(null), []);
  const showToast = useCallback((message: string, tone: ToastTone = 'error') => {
    nextId.current += 1;
    setToast({ id: nextId.current, message, tone });
  }, []);
  const showError = useCallback((message: string) => showToast(message, 'error'), [showToast]);

  const value = useMemo(() => ({ toast, showToast, showError, dismissToast }), [toast, dismissToast, showError, showToast]);

  return (
    <ToastContext.Provider value={value}>
      <View style={styles.root}>
        {children}
        <ToastViewport />
      </View>
    </ToastContext.Provider>
  );
}

export function ToastViewport() {
  const { toast, dismissToast } = useToast();
  return toast ? <ToastView key={toast.id} toast={toast} onDismiss={dismissToast} /> : null;
}

function ToastView({ toast, onDismiss }: { toast: Toast; onDismiss: () => void }) {
  const insets = useSafeAreaInsets();
  const [opacity] = useState(() => new Animated.Value(0));

  useEffect(() => {
    Animated.timing(opacity, { toValue: 1, duration: 180, useNativeDriver: false }).start();
    const timeout = setTimeout(onDismiss, TOAST_DURATION_MS);
    return () => clearTimeout(timeout);
  }, [onDismiss, opacity]);

  const isError = toast.tone === 'error';

  return (
    <View pointerEvents="box-none" style={[styles.viewport, { top: insets.top + spacing.small }]}>
      <Animated.View
        accessibilityLiveRegion={isError ? 'assertive' : 'polite'}
        accessibilityRole={isError ? 'alert' : 'text'}
        style={[styles.toast, isError ? styles.toastError : styles.toastSuccess, { opacity }]}
      >
        <View style={[styles.indicator, isError ? styles.indicatorError : styles.indicatorSuccess]} />
        <View style={styles.content}>
          <Text style={[styles.title, isError ? styles.titleError : styles.titleSuccess]}>
            {isError ? 'Algo deu errado' : 'Tudo certo'}
          </Text>
          <Text style={styles.message}>{toast.message}</Text>
        </View>
        <Pressable
          accessibilityLabel="Fechar aviso"
          accessibilityRole="button"
          hitSlop={8}
          onPress={onDismiss}
          style={({ pressed }) => [styles.close, pressed ? styles.closePressed : null]}
        >
          <Text style={styles.closeLabel}>×</Text>
        </Pressable>
      </Animated.View>
    </View>
  );
}

export function useToast() {
  const context = useContext(ToastContext);
  if (!context) {
    throw new Error('useToast must be used inside ToastProvider.');
  }

  return context;
}

export function useErrorToast(error: unknown, message: string) {
  const { showError } = useToast();

  useEffect(() => {
    if (error) {
      showError(message);
    }
  }, [error, message, showError]);
}

const styles = StyleSheet.create({
  root: {
    flex: 1,
  },
  viewport: {
    position: 'absolute',
    left: spacing.medium,
    right: spacing.medium,
    alignItems: 'center',
    zIndex: 1000,
    elevation: 1000,
  },
  toast: {
    width: '100%',
    maxWidth: 480,
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: spacing.small,
    padding: spacing.medium,
    borderWidth: 1,
    borderRadius: radii.medium,
    backgroundColor: colors.surfaceElevated,
    shadowColor: '#000000',
    shadowOpacity: 0.4,
    shadowRadius: 18,
    shadowOffset: { width: 0, height: 10 },
  },
  toastError: {
    borderColor: colors.brand,
  },
  toastSuccess: {
    borderColor: colors.success,
  },
  indicator: {
    width: 4,
    alignSelf: 'stretch',
    borderRadius: radii.pill,
  },
  indicatorError: {
    backgroundColor: colors.brand,
  },
  indicatorSuccess: {
    backgroundColor: colors.success,
  },
  content: {
    flex: 1,
    gap: spacing.extraSmall,
  },
  title: {
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.bold,
  },
  titleError: {
    color: colors.brandSecondary,
  },
  titleSuccess: {
    color: colors.success,
  },
  message: {
    color: colors.textPrimary,
    fontSize: typography.bodySmall,
    fontFamily: fontFamily.regular,
    lineHeight: 20,
  },
  close: {
    width: 44,
    height: 44,
    marginTop: -spacing.small,
    marginRight: -spacing.small,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radii.pill,
  },
  closePressed: {
    backgroundColor: colors.surface,
  },
  closeLabel: {
    color: colors.textSecondary,
    fontSize: 24,
    fontFamily: fontFamily.regular,
    lineHeight: 28,
  },
});
