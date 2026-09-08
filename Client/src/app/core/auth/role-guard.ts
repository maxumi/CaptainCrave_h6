import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { inject } from '@angular/core';
import { Role } from '../../shared/models/user';

/**
 * A guard that checks if the user has the required role before allowing access to a route.
 * @param route The route that is being accessed.
 * @returns A boolean indicating whether the route can be activated or a UrlTree to redirect to.
 */
export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const allowedRoles = route.data['roles'] as Role[];

  if (!authService.isLoggedIn()) {
    return router.parseUrl('/login');
  }

  if (authService.hasRole(allowedRoles)) {
    return true;
  }

  return router.parseUrl('/');
};
