import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from './auth.service';
import { displayNameError } from './display-name';
import { I18nService, TranslationKey } from './i18n.service';
import { PantryComponent } from './pantry.component';

@Component({
  standalone: true,
  imports: [FormsModule, PantryComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <section class="auth-card">
      <p class="eyebrow">{{ i18n.t('profile.eyebrow') }}</p>
      <h1>{{ i18n.t('profile.title') }}</h1>
      <p class="muted">{{ auth.currentUser()?.email }}</p>
      <form (ngSubmit)="save()">
        <label>{{ i18n.t('auth.displayName') }}
          <input name="displayName" [(ngModel)]="displayName" required minlength="2" maxlength="30" autocomplete="nickname" />
          <span class="field-hint">{{ i18n.t('auth.displayName.hint') }}</span>
        </label>
        @if (message()) { <p class="success-message" role="status">{{ i18n.t(message()!) }}</p> }
        @if (error()) { <p class="error-message" role="alert">{{ i18n.t(error()!) }}</p> }
        <button class="button" type="submit" [disabled]="busy()">@if (busy()) { <span class="spinner" aria-hidden="true"></span> }{{ i18n.t('common.save') }}</button>
      </form>
    </section>
    <app-pantry />
  `
})
export class ProfileComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly busy = signal(false);
  readonly message = signal<TranslationKey | null>(null);
  readonly error = signal<TranslationKey | null>(null);
  displayName = '';

  ngOnInit(): void {
    this.displayName = this.auth.currentUser()?.displayName ?? '';
  }

  save(): void {
    this.busy.set(true);
    this.message.set(null);
    this.error.set(null);
    this.auth.updateProfile(this.displayName).subscribe({
      next: user => {
        this.busy.set(false);
        this.displayName = user.displayName ?? '';
        this.message.set('profile.saved');
      },
      error: (response: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(displayNameError(response) ?? 'profile.failed');
      }
    });
  }
}
