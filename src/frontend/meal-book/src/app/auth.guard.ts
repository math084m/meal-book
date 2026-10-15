import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.checkAdmin().pipe(map(isAdmin => isAdmin ? true : router.parseUrl('/login')));
};

export const userGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.checkAuthenticated().pipe(map(isAuthenticated =>
    isAuthenticated ? true : router.parseUrl('/login')
  ));
};
