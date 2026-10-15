import { HttpClient, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { Router } from '@angular/router';

export interface CurrentUser {
  id: string;
  email: string;
  isAdmin: boolean;
}

interface IdentityTokenResponse {
  accessToken: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = new URL('api/auth/', document.baseURI).toString();
  private readonly identityUrl = new URL('api/identity/', document.baseURI).toString();

  readonly currentUser = signal<CurrentUser | null>(null);

  initialize(): void {
    if (this.token()) {
      this.refreshUser().subscribe({ error: () => this.logout(false) });
    }
  }

  register(email: string, password: string): Observable<unknown> {
    return this.http.post(`${this.apiUrl}register`, { email, password });
  }

  login(email: string, password: string): Observable<CurrentUser> {
    return this.http
      .post<IdentityTokenResponse>(`${this.identityUrl}login?useCookies=false`, { email, password })
      .pipe(
        tap(response => localStorage.setItem('mealbook-access-token', response.accessToken)),
        switchMap(() => this.refreshUser())
      );
  }

  refreshUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${this.apiUrl}me`).pipe(
      tap(user => this.currentUser.set(user))
    );
  }

  checkAdmin(): Observable<boolean> {
    if (this.currentUser()) {
      return of(this.currentUser()?.isAdmin ?? false);
    }
    if (!this.token()) {
      return of(false);
    }
    return this.refreshUser().pipe(
      map(user => user.isAdmin),
      catchError(() => {
        this.logout(false);
        return of(false);
      })
    );
  }

  checkAuthenticated(): Observable<boolean> {
    if (this.currentUser()) {
      return of(true);
    }
    if (!this.token()) {
      return of(false);
    }
    return this.refreshUser().pipe(
      map(() => true),
      catchError(() => {
        this.logout(false);
        return of(false);
      })
    );
  }

  logout(navigate = true): void {
    localStorage.removeItem('mealbook-access-token');
    this.currentUser.set(null);
    if (navigate) {
      void this.router.navigate(['/']);
    }
  }

  token(): string | null {
    return localStorage.getItem('mealbook-access-token');
  }
}

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).token();
  if (!token || !request.url.startsWith(new URL('api/', document.baseURI).toString())) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
