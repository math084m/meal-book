import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { I18nService, TranslationKey } from './i18n.service';
import { CookLog, RecipeApiService } from './recipe-api.service';
import { StarRatingComponent } from './star-rating.component';

@Component({
  selector: 'app-cook-log-entry',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, StarRatingComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <article class="history-entry cook-log-entry">
      @if (editing()) {
        <form class="cook-log-edit" (ngSubmit)="save()">
          <p class="eyebrow">{{ entry().cookedAt | date:'longDate' }}</p>
          <div class="field">
            <span class="rating-label" aria-hidden="true">{{ i18n.t('detail.rating') }}</span>
            <app-star-rating [(value)]="rating" />
          </div>
          <label>{{ i18n.t('detail.notes') }}
            <textarea name="notes" rows="3" maxlength="1000" [(ngModel)]="notes" [placeholder]="i18n.t('detail.notes.placeholder')"></textarea>
          </label>
          @if (error()) { <p class="error-message" role="alert">{{ i18n.t(error()!) }}</p> }
          <div class="admin-actions">
            <button class="button secondary" type="button" [disabled]="busy()" (click)="cancel()">{{ i18n.t('common.cancel') }}</button>
            <button class="button" type="submit" [disabled]="busy()">@if (busy()) { <span class="spinner" aria-hidden="true"></span> }{{ i18n.t('common.save') }}</button>
          </div>
        </form>
      } @else {
        <div>
          <p class="eyebrow">{{ entry().cookedAt | date:'longDate' }}</p>
          @if (showRecipe()) {
            <h2><a [routerLink]="['/recipes', entry().recipeId]">{{ entry().recipeName }}</a></h2>
          }
          @if (entry().notes) { <p class="muted cook-log-notes">{{ entry().notes }}</p> }
          @if (confirmingDelete()) {
            <div class="inline-confirm" role="alertdialog" [attr.aria-label]="i18n.t('cookLog.deleteConfirm')">
              <span>{{ i18n.t('cookLog.deleteConfirm') }}</span>
              <button class="button secondary" type="button" [disabled]="busy()" (click)="confirmingDelete.set(false)">{{ i18n.t('common.cancel') }}</button>
              <button class="button danger" type="button" [disabled]="busy()" (click)="remove()">{{ i18n.t('common.delete') }}</button>
            </div>
          } @else {
            <div class="entry-actions">
              <button class="link-button" type="button" (click)="startEdit()">{{ i18n.t('common.edit') }}</button>
              <button class="link-button danger-link" type="button" (click)="confirmingDelete.set(true)">{{ i18n.t('common.delete') }}</button>
            </div>
          }
          @if (error()) { <p class="error-message" role="alert">{{ i18n.t(error()!) }}</p> }
        </div>
        @if (entry().rating) { <app-star-rating class="rating" [value]="entry().rating ?? null" [readonly]="true" /> }
      }
    </article>
  `
})
export class CookLogEntryComponent {
  private readonly api = inject(RecipeApiService);
  readonly i18n = inject(I18nService);
  readonly entry = input.required<CookLog>();
  readonly showRecipe = input(true);
  readonly changed = output<CookLog>();
  readonly deleted = output<string>();
  readonly editing = signal(false);
  readonly confirmingDelete = signal(false);
  readonly busy = signal(false);
  readonly error = signal<TranslationKey | null>(null);
  rating: number | null = null;
  notes = '';

  startEdit(): void {
    this.rating = this.entry().rating ?? null;
    this.notes = this.entry().notes ?? '';
    this.error.set(null);
    this.editing.set(true);
  }

  cancel(): void {
    this.editing.set(false);
    this.error.set(null);
  }

  save(): void {
    this.busy.set(true);
    this.error.set(null);
    this.api.updateCookLog(this.entry().id, this.rating, this.notes).subscribe({
      next: updated => {
        this.busy.set(false);
        this.editing.set(false);
        this.changed.emit(updated);
      },
      error: () => {
        this.busy.set(false);
        this.error.set('cookLog.saveFailed');
      }
    });
  }

  remove(): void {
    this.busy.set(true);
    this.error.set(null);
    this.api.deleteCookLog(this.entry().id).subscribe({
      next: () => {
        this.busy.set(false);
        this.deleted.emit(this.entry().id);
      },
      error: () => {
        this.busy.set(false);
        this.confirmingDelete.set(false);
        this.error.set('cookLog.deleteFailed');
      }
    });
  }
}
