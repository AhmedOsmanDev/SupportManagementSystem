import { HttpErrorResponse } from '@angular/common/http';

export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return error instanceof Error ? error.message : fallback;
  }

  const body = error.error as Record<string, unknown> | string | null;
  if (typeof body === 'string' && body.trim()) return body;
  if (body && typeof body === 'object') {
    const detail = body['detail'] ?? body['message'] ?? body['title'];
    if (detail) return String(detail);
    const errors = body['errors'] as Record<string, string[]> | undefined;
    if (errors) return Object.values(errors).flat().join(' ');
  }
  if (error.status === 0) return 'The API is unavailable. Check that the backend is running.';
  return fallback;
}
