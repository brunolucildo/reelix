export function getAuthFailureMessage(error: unknown, fallback: string): string {
  if (
    error instanceof TypeError ||
    (error instanceof Error && /network|fetch|timeout/i.test(`${error.name} ${error.message}`))
  ) {
    return 'Não foi possível conectar ao serviço de autenticação. Verifique sua conexão e tente novamente.';
  }

  return fallback;
}
