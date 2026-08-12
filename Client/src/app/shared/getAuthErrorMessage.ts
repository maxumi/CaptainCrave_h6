import { HttpErrorResponse } from "@angular/common/http";

export function getAuthErrorMessage(error: unknown, fallbackErrorMessage: string): string {
  if (error instanceof HttpErrorResponse) {
    const apiMessage = error.error?.message;

    if (typeof apiMessage === 'string' && apiMessage.trim().length > 0) {
      return apiMessage;
    }
  }

  return fallbackErrorMessage;
}