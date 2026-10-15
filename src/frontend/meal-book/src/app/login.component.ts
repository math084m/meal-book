import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-card">
      <p class="eyebrow">WELCOME BACK</p>
      <h1>Sign in to your kitchen.</h1>
      <p class="muted">Save your cooking history and keep track of recipes you love.</p>
      <form (ngSubmit)="submit()">
        <label>Email
          <input name="email" type="email" [(ngModel)]="email" required autocomplete="email" />
        </label>
        <label>Password
          <input name="password" type="password" [(ngModel)]="password" required autocomplete="current-password" />
        </label>
        @if (error()) { <p class="error-message" role="alert">{{ error() }}</p> }
        <button class="button" type="submit" [disabled]="busy()">Sign in</button>
      </form>
      <p class="auth-switch">New to Meal Book? <a routerLink="/register">Create an account</a></p>
    </section>
  `
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  email = '';
  password = '';
  readonly error = signal('');
  readonly busy = signal(false);

  submit(): void {
    this.busy.set(true);
    this.error.set('');
    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        this.busy.set(false);
        void this.router.navigate(['/']);
      },
      error: () => {
        this.busy.set(false);
        this.error.set('Sign-in failed. Check your email and password.');
      }
    });
  }
}
