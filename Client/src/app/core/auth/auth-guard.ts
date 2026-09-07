import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { inject } from '@angular/core';

/**
 * A guard that checks if the user is authenticated before allowing access to a route.
 * @param route The route that is being accessed.
 * @param state The current router state.
 * @returns A boolean indicating whether the route can be activated or a UrlTree to redirect to.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.user()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.user()) {
    return true;
  }

  return router.createUrlTree(['/']);
};