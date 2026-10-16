import { Component, effect, inject, signal, untracked } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Recipe, RecipeApiService } from './recipe-api.service';
import { I18nService } from './i18n.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe],
  template: `
    <section class="page-heading">
      <p class="eyebrow">{{ i18n.t('list.eyebrow') }}</p>
      <h1>{{ i18n.t('list.title') }}</h1>
      <p class="intro">{{ i18n.t('list.intro') }}</p>
    </section>
    <form class="filter-panel" (ngSubmit)="searchRecipes()">
      <label class="wide">{{ i18n.t('list.search') }}
        <input name="search" [(ngModel)]="filters.search" [placeholder]="i18n.t('list.search.placeholder')" />
      </label>
      <label>{{ i18n.t('list.ingredient') }}
        <input name="ingredient" [(ngModel)]="filters.ingredient" [placeholder]="i18n.t('list.ingredient.placeholder')" />
      </label>
      <label>{{ i18n.t('list.tags') }}
        <input name="tags" [(ngModel)]="filters.tags" [placeholder]="i18n.t('list.tags.placeholder')" />
      </label>
      <label>{{ i18n.t('list.mealType') }}
        <input name="mealType" [(ngModel)]="filters.mealType" [placeholder]="i18n.t('list.mealType.placeholder')" />
      </label>
      <label>{{ i18n.t('list.course') }}
        <input name="course" [(ngModel)]="filters.course" [placeholder]="i18n.t('list.course.placeholder')" />
      </label>
      <label>{{ i18n.t('list.method') }}
        <input name="cookingMethod" [(ngModel)]="filters.cookingMethod" [placeholder]="i18n.t('list.method.placeholder')" />
      </label>
      <label>{{ i18n.t('list.cuisine') }}
        <input name="cuisine" [(ngModel)]="filters.cuisine" [placeholder]="i18n.t('list.cuisine.placeholder')" />
      </label>
      <label>{{ i18n.t('list.readyWithin') }}
        <input name="maxTotalMinutes" type="number" min="0" [(ngModel)]="maxTotalMinutes" />
      </label>
      <label>{{ i18n.t('list.maxCalories') }}
        <input name="maxCalories" type="number" min="0" [(ngModel)]="maxCalories" />
      </label>
      <label>{{ i18n.t('list.minRating') }}
        <select name="minRating" [(ngModel)]="minRating">
          <option value="">{{ i18n.t('list.rating.any') }}</option>
          <option value="3">{{ i18n.t('list.rating.3') }}</option>
          <option value="4">{{ i18n.t('list.rating.4') }}</option>
          <option value="4.5">{{ i18n.t('list.rating.45') }}</option>
        </select>
      </label>
      <label>{{ i18n.t('list.sort') }}
        <select name="sort" [(ngModel)]="filters.sort">
          <option value="newest">{{ i18n.t('list.sort.newest') }}</option>
          <option value="popular">{{ i18n.t('list.sort.popular') }}</option>
          <option value="trending">{{ i18n.t('list.sort.trending') }}</option>
          <option value="rating">{{ i18n.t('list.sort.rating') }}</option>
          <option value="quickest">{{ i18n.t('list.sort.quickest') }}</option>
          <option value="name">{{ i18n.t('list.sort.name') }}</option>
        </select>
      </label>
      <div class="filter-actions">
        <button class="button" type="submit">{{ i18n.t('list.apply') }}</button>
        <button class="button secondary" type="button" (click)="clearFilters()">{{ i18n.t('list.clear') }}</button>
      </div>
    </form>

    <section class="library-section">
      <div class="section-heading">
        <h2>{{ i18n.t('list.heading') }}</h2>
        @if (recipes(); as recipeList) { <span class="muted">{{ i18n.t('list.found', { n: recipeList.length }) }}</span> }
      </div>
      @if (error()) {
        <div class="state-card" role="alert">{{ i18n.t('list.error') }}</div>
      } @else {
        @if (recipes(); as recipeList) {
        @if (recipeList.length) {
          <div class="recipe-grid">
            @for (recipe of recipeList; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <p class="eyebrow">{{ recipe.cuisine || i18n.t('common.recipeFallback') }} · {{ recipe.course || i18n.t('common.anyCourse') }}</p>
                <h3>{{ recipe.name }}</h3>
                <p class="description">{{ recipe.description || i18n.t('list.descriptionFallback') }}</p>
                <p class="recipe-meta">
                  {{ i18n.t('common.minutes', { n: recipe.prepTimeMinutes + recipe.cookTimeMinutes }) }} · {{ i18n.t('common.servings', { n: recipe.servings }) }} ·
                  {{ i18n.t('common.ingredients', { n: recipe.ingredients.length }) }}
                </p>
                <div class="tag-list">
                  @for (tag of recipe.tags; track tag) { <span class="tag">{{ tag }}</span> }
                </div>
                @if (recipe.averageRating) {
                  <p class="rating">★ {{ recipe.averageRating | number:'1.1-1' }} <span>{{ i18n.t('list.ratingCooks', { n: recipe.cookCount }) }}</span></p>
                }
              </a>
            }
          </div>
        } @else {
          <div class="state-card">{{ i18n.t('list.empty') }}</div>
        }
      } @else {
        <div class="state-card" role="status">{{ i18n.t('list.loading') }}</div>
      }
      }
    </section>
  `
})
export class RecipeListComponent {
  private readonly api = inject(RecipeApiService);
  readonly i18n = inject(I18nService);
  readonly recipes = signal<Recipe[] | null>(null);
  readonly error = signal(false);
  maxTotalMinutes: number | null = null;
  maxCalories: number | null = null;
  minRating: number | '' = '';
  filters = {
    search: '',
    ingredient: '',
    tags: '',
    mealType: '',
    course: '',
    cookingMethod: '',
    cuisine: '',
    sort: 'newest'
  };

  constructor() {
    effect(() => {
      this.i18n.lang();
      untracked(() => this.searchRecipes());
    });
  }

  searchRecipes(): void {
    this.error.set(false);
    this.recipes.set(null);
    this.api.getRecipes({
      ...this.filters,
      maxTotalMinutes: this.maxTotalMinutes,
      maxCalories: this.maxCalories,
      minRating: this.minRating === '' ? null : Number(this.minRating)
    }).subscribe({
      next: value => this.recipes.set(value),
      error: () => this.error.set(true)
    });
  }

  clearFilters(): void {
    this.filters = {
      search: '',
      ingredient: '',
      tags: '',
      mealType: '',
      course: '',
      cookingMethod: '',
      cuisine: '',
      sort: 'newest'
    };
    this.maxTotalMinutes = null;
    this.maxCalories = null;
    this.minRating = '';
    this.searchRecipes();
  }
}
