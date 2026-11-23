type UnauthorizedListener = () => void;

let listener: UnauthorizedListener | null = null;

/** Registers the single handler (mounted under the auth and toast providers) for global 401s. */
export function setUnauthorizedListener(next: UnauthorizedListener | null): void {
  listener = next;
}

export function notifyUnauthorized(): void {
  listener?.();
}
