import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CookLog, RecipeApiService } from './recipe-api.service';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <section class="page-heading">
      <p class="eyebrow">YOUR KITCHEN JOURNAL</p>
      <h1>Cooking history.</h1>
      <p class="intro">Meals you have made, all in one place.</p>
    </section>
    @if (error()) {
      <div class="state-card" role="alert">{{ error() }}</div>
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
        <div class="state-card">No cooking history yet. Log a recipe after you make it.</div>
      }
    } @else {
      <div class="state-card" role="status">Loading your history…</div>
    }
    }
  `
})
export class CookingHistoryComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  readonly entries = signal<CookLog[] | null>(null);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.getCookHistory().subscribe({
      next: logs => this.entries.set(logs),
      error: () => this.error.set('Could not load cooking history. Please sign in again.')
    });
  }
}
