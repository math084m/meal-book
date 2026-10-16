import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';
import { I18nService } from './i18n.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-card">
      <p class="eyebrow">{{ i18n.t('auth.login.eyebrow') }}</p>
      <h1>{{ i18n.t('auth.login.title') }}</h1>
      <p class="muted">{{ i18n.t('auth.login.text') }}</p>
      <form (ngSubmit)="submit()">
        <label>{{ i18n.t('auth.email') }}
          <input name="email" type="email" [(ngModel)]="email" required autocomplete="email" />
        </label>
        <label>{{ i18n.t('auth.password') }}
          <input name="password" type="password" [(ngModel)]="password" required autocomplete="current-password" />
        </label>
        @if (error()) { <p class="error-message" role="alert">{{ i18n.t('auth.login.failed') }}</p> }
        <button class="button" type="submit" [disabled]="busy()">{{ i18n.t('auth.login.submit') }}</button>
      </form>
      <p class="auth-switch">{{ i18n.t('auth.login.new') }} <a routerLink="/register">{{ i18n.t('auth.login.create') }}</a></p>
    </section>
  `
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  email = '';
  password = '';
  readonly error = signal(false);
  readonly busy = signal(false);

  submit(): void {
    this.busy.set(true);
    this.error.set(false);
    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        this.busy.set(false);
        void this.router.navigate(['/']);
      },
      error: () => {
        this.busy.set(false);
        this.error.set(true);
      }
    });
  }
}
