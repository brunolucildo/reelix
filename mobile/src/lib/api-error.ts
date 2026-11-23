export const SESSION_EXPIRED_MESSAGE = 'Sua sessão expirou. Entre novamente para continuar.';
export const NETWORK_ERROR_MESSAGE = 'Não foi possível conectar à API. Verifique a rede e tente novamente.';
export const CONTRACT_ERROR_MESSAGE = 'A API retornou dados em formato inesperado.';
export const REQUEST_FAILED_MESSAGE = 'Não foi possível concluir a solicitação. Tente novamente.';

/** Non-2xx response from the API. `message` is the server `detail`, then `title`, then a safe fallback. */
export class ApiError extends Error {
  readonly status: number;
  readonly title: string | null;
  readonly detail: string | null;

  constructor(status: number, title: string | null = null, detail: string | null = null) {
    super(detail ?? title ?? (status === 401 ? SESSION_EXPIRED_MESSAGE : REQUEST_FAILED_MESSAGE));
    Object.setPrototypeOf(this, new.target.prototype);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
  }
}

/** The request never reached the API (offline, DNS, TLS, refused connection). */
export class ApiNetworkError extends Error {
  constructor() {
    super(NETWORK_ERROR_MESSAGE);
    Object.setPrototypeOf(this, new.target.prototype);
    this.name = 'ApiNetworkError';
  }
}

/** The API answered 2xx but the body does not match the zod schema of the endpoint. */
export class ApiContractError extends Error {
  constructor() {
    super(CONTRACT_ERROR_MESSAGE);
    Object.setPrototypeOf(this, new.target.prototype);
    this.name = 'ApiContractError';
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export function isUnauthorizedError(error: unknown): error is ApiError {
  return error instanceof ApiError && error.status === 401;
}

/**
 * Translates any API failure into a pt-BR message for the toast. HTTP status codes and
 * other technical details never reach the user; `fallback` covers every other API error.
 */
export function toUserMessage(error: unknown, fallback: string): string {
  if (isUnauthorizedError(error)) {
    return SESSION_EXPIRED_MESSAGE;
  }
  if (error instanceof ApiNetworkError || error instanceof ApiContractError) {
    return error.message;
  }
  return fallback;
}
