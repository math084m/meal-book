import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CookLog, RecipeApiService } from './recipe-api.service';
import { I18nService } from './i18n.service';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <section class="page-heading">
      <p class="eyebrow">{{ i18n.t('history.eyebrow') }}</p>
      <h1>{{ i18n.t('history.title') }}</h1>
      <p class="intro">{{ i18n.t('history.intro') }}</p>
    </section>
    @if (error()) {
      <div class="state-card" role="alert">{{ i18n.t('history.failed') }}</div>
    } @else {
      @if (entries(); as logs) {
      @if (logs.length) {
        <div class="history-list">
          @for (entry of logs; track entry.id) {
            <article class="history-entry">
              <div>
                <p class="eyebrow">{{ entry.cookedAt | date:'longDate' }}</p>
                <h2><a [routerLink]="['/recipes', entry.recipeId]">{{ entry.recipeName }}</a></h2>
                @if (entry.notes) { <p class="muted">{{ entry.notes }}</p> }
              </div>
              @if (entry.rating) { <span class="rating">★ {{ entry.rating }} / 5</span> }
            </article>
          }
        </div>
      } @else {
        <div class="state-card">{{ i18n.t('history.empty') }}</div>
      }
    } @else {
      <div class="state-card" role="status">{{ i18n.t('history.loading') }}</div>
    }
    }
  `
})
export class CookingHistoryComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  readonly i18n = inject(I18nService);
  readonly entries = signal<CookLog[] | null>(null);
  readonly error = signal(false);

  ngOnInit(): void {
    this.api.getCookHistory().subscribe({
      next: logs => this.entries.set(logs),
      error: () => this.error.set(true)
    });
  }
}
