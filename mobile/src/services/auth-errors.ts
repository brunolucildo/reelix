export function getAuthFailureMessage(error: unknown, fallback: string): string {
  if (
    error instanceof TypeError ||
    (error instanceof Error && /network|fetch|timeout/i.test(`${error.name} ${error.message}`))
  ) {
    return 'Não foi possível conectar ao serviço de autenticação. Verifique sua conexão e tente novamente.';
  }

  if (
    error instanceof Error &&
    'status' in error &&
    typeof error.status === 'number' &&
    error.status === 429
  ) {
    return 'O limite de tentativas foi atingido. Aguarde antes de solicitar outro link.';
  }

  const code = error instanceof Error && 'code' in error && typeof error.code === 'string'
    ? error.code
    : null;

  if (code === 'weak_password') {
    return 'A senha não atende aos requisitos de segurança do projeto. Escolha outra senha e tente novamente.';
  }

  if (code === 'same_password') {
    return 'A nova senha deve ser diferente da senha atual.';
  }

  if (['session_expired', 'session_not_found', 'otp_expired'].includes(code ?? '')) {
    return 'Sua sessão de recuperação expirou. Solicite um novo link para redefinir a senha.';
  }

  return fallback;
}
