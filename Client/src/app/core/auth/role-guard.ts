import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { inject } from '@angular/core';
import { Role } from '../../shared/models/user';

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
