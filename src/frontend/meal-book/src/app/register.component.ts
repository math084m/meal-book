import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-card">
      <p class="eyebrow">MAKE IT YOURS</p>
      <h1>Start your recipe book.</h1>
      <p class="muted">Create a free account to log meals cooked and rate recipes.</p>
      <form (ngSubmit)="submit()">
        <label>Email
          <input name="email" type="email" [(ngModel)]="email" required autocomplete="email" />
        </label>
        <label>Password
          <input name="password" type="password" [(ngModel)]="password" required minlength="10" autocomplete="new-password" />
          <span class="field-hint">At least 10 characters, with uppercase, lowercase, and a number.</span>
        </label>
        @if (error()) { <p class="error-message" role="alert">{{ error() }}</p> }
        <button class="button" type="submit" [disabled]="busy()">Create account</button>
      </form>
      <p class="auth-switch">Already have an account? <a routerLink="/login">Sign in</a></p>
    </section>
  `
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  email = '';
  password = '';
  readonly error = signal('');
  readonly busy = signal(false);

  submit(): void {
    this.busy.set(true);
    this.error.set('');
    this.auth.register(this.email, this.password).subscribe({
      next: () => {
        this.busy.set(false);
        void this.router.navigate(['/login'], { queryParams: { registered: 'true' } });
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Could not create account. Email may already be registered or password may not meet requirements.');
      }
    });
  }
}
