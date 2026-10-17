import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';
import { I18nService } from './i18n.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <section class="auth-card">
      <p class="eyebrow">{{ i18n.t('auth.register.eyebrow') }}</p>
      <h1>{{ i18n.t('auth.register.title') }}</h1>
      <p class="muted">{{ i18n.t('auth.register.text') }}</p>
      <form (ngSubmit)="submit()">
        <label>{{ i18n.t('auth.email') }}
          <input name="email" type="email" [(ngModel)]="email" required autocomplete="email" />
        </label>
        <label>{{ i18n.t('auth.password') }}
          <input name="password" type="password" [(ngModel)]="password" required minlength="10" autocomplete="new-password" />
          <span class="field-hint">{{ i18n.t('auth.register.hint') }}</span>
        </label>
        @if (error()) { <p class="error-message" role="alert">{{ i18n.t('auth.register.failed') }}</p> }
        <button class="button" type="submit" [disabled]="busy()">@if (busy()) { <span class="spinner" aria-hidden="true"></span> }{{ i18n.t('auth.register.submit') }}</button>
      </form>
      <p class="auth-switch">{{ i18n.t('auth.register.existing') }} <a routerLink="/login">{{ i18n.t('nav.signIn') }}</a></p>
    </section>
  `
})
export class RegisterComponent {
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
    this.auth.register(this.email, this.password).subscribe({
      next: () => {
        this.busy.set(false);
        void this.router.navigate(['/login'], { queryParams: { registered: 'true' } });
      },
      error: () => {
        this.busy.set(false);
        this.error.set(true);
      }
    });
  }
}
