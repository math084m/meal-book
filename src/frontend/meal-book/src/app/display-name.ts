import { HttpErrorResponse } from '@angular/common/http';
import { TranslationKey } from './i18n.service';

// Maps the server's display-name validation errors to translated messages.
export function displayNameError(response: HttpErrorResponse): TranslationKey | null {
  const errors = (response.error as { errors?: Record<string, unknown> } | null)?.errors;
  if (!errors) return null;
  if ('displayNameTaken' in errors) return 'auth.displayName.taken';
  if ('displayName' in errors) return 'auth.displayName.invalid';
  return null;
}
