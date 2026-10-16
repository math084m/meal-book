import { Component, effect, inject, signal, untracked } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { I18nService } from './i18n.service';
import { RecipeApiService, RecipeDashboard } from './recipe-api.service';

@Component({
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe],
  template: `
    <section class="hero">
      <p class="eyebrow">{{ i18n.t('home.eyebrow') }}</p>
      <h1>{{ i18n.t('home.title1') }}<br />{{ i18n.t('home.title2') }}</h1>
      <p class="intro">{{ i18n.t('home.intro') }}</p>
      <a class="button" routerLink="/recipes">{{ i18n.t('home.explore') }} <span aria-hidden="true">→</span></a>
    </section>

    @if (error()) {
      <div class="state-card" role="alert">
        <h2>{{ i18n.t('home.unavailable.title') }}</h2>
        <p>{{ i18n.t('home.unavailable.text') }}</p>
      </div>
    } @else {
      @if (dashboard(); as data) {
      <section class="dashboard-section">
        <div class="section-heading">
          <div><p class="eyebrow">{{ i18n.t('home.recently.eyebrow') }}</p><h2>{{ i18n.t('home.recently.title') }}</h2></div>
        </div>
        @if (data.recentlyCooked.length) {
          <div class="recipe-grid compact">
            @for (item of data.recentlyCooked; track item.cookedAt + item.recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', item.recipe.id]">
                <p class="eyebrow">{{ i18n.t('home.recently.card') }}</p>
                <h3>{{ item.recipe.name }}</h3>
                <p class="recipe-meta">{{ item.cookedAt | date:'mediumDate' }}</p>
              </a>
            }
          </div>
        } @else {
          <p class="muted">{{ i18n.t('home.recently.empty') }}</p>
        }
      </section>

      <section class="dashboard-section">
        <div class="section-heading">
          <div><p class="eyebrow">{{ i18n.t('home.added.eyebrow') }}</p><h2>{{ i18n.t('home.added.title') }}</h2></div>
          <a routerLink="/recipes">{{ i18n.t('home.added.browse') }}</a>
        </div>
        @if (data.recentlyAdded.length) {
          <div class="recipe-grid">
            @for (recipe of data.recentlyAdded; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <p class="eyebrow">{{ recipe.cuisine || i18n.t('common.recipeFallback') }} · {{ recipe.course || i18n.t('common.anyCourse') }}</p>
                <h3>{{ recipe.name }}</h3>
                <p class="description">{{ recipe.description || i18n.t('home.added.descriptionFallback') }}</p>
                <p class="recipe-meta">{{ i18n.t('common.minutes', { n: recipe.prepTimeMinutes + recipe.cookTimeMinutes }) }} · {{ i18n.t('common.servings', { n: recipe.servings }) }}</p>
              </a>
            }
          </div>
        } @else {
          <p class="muted">{{ i18n.t('home.added.empty') }}</p>
        }
      </section>

      <section class="dashboard-section">
        <div class="section-heading">
          <div><p class="eyebrow">{{ i18n.t('home.favorites.eyebrow') }}</p><h2>{{ i18n.t('home.favorites.title') }}</h2></div>
        </div>
        @if (data.mostCooked.length) {
          <div class="recipe-grid">
            @for (recipe of data.mostCooked; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <p class="eyebrow">                {{ i18n.t('home.favorites.cooks', { n: recipe.cookCount }) }}@if (recipe.averageRating) { · ★ {{ recipe.averageRating | number:'1.1-1' }} }</p>
                <h3>{{ recipe.name }}</h3>
                                <p class="recipe-meta">{{ recipe.mealType || i18n.t('home.favorites.anyMeal') }} · {{ recipe.cuisine || i18n.t('home.favorites.noCuisine') }}</p>
              </a>
            }
          </div>
        } @else {
                          <p class="muted">{{ i18n.t('home.favorites.empty') }}</p>
        }
      </section>
      } @else {
                        <div class="state-card" role="status">{{ i18n.t('home.loading') }}</div>
      }
    }
  `
})
export class HomeComponent {
  private readonly api = inject(RecipeApiService);
  readonly i18n = inject(I18nService);
  readonly dashboard = signal<RecipeDashboard | null>(null);
  readonly error = signal(false);

  constructor() {
    effect(() => {
      this.i18n.lang();
      untracked(() => this.load());
    });
  }

  private load(): void {
    this.api.getDashboard().subscribe({
      next: value => {
        this.dashboard.set(value);
        this.error.set(false);
      },
      error: () => this.error.set(true)
    });
  }
}
