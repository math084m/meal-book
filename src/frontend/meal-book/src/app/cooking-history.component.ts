import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CookLogEntryComponent } from './cook-log-entry.component';
import { CookLog, RecipeApiService } from './recipe-api.service';
import { I18nService } from './i18n.service';

@Component({
  standalone: true,
  imports: [CookLogEntryComponent],
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
            <app-cook-log-entry [entry]="entry" (changed)="replace($event)" (deleted)="remove($event)" />
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

  replace(updated: CookLog): void {
    this.entries.update(logs => logs?.map(log => (log.id === updated.id ? updated : log)) ?? null);
  }

  remove(id: string): void {
    this.entries.update(logs => logs?.filter(log => log.id !== id) ?? null);
  }
}
