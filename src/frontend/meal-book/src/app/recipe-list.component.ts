import { Component, HostListener, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { Recipe, RecipeApiService, RecipeSuggestion } from './recipe-api.service';
import { RecipeImagePipe } from './recipe-image.pipe';
import { PlanButtonComponent } from './plan-button.component';
import { FavoriteButtonComponent } from './favorite-button.component';
import { FavoriteService } from './favorite.service';
import { AuthService } from './auth.service';
import { I18nService, TranslationKey } from './i18n.service';
import { CatalogService } from './catalog.service';
import { SelectFieldComponent } from './select-field.component';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe, SelectFieldComponent, RecipeImagePipe, PlanButtonComponent, FavoriteButtonComponent],
  template: `
    <section class="page-heading">
      <p class="eyebrow">{{ i18n.t('list.eyebrow') }}</p>
      <h1>{{ i18n.t('list.title') }}</h1>
      <p class="intro">{{ i18n.t('list.intro') }}</p>
    </section>
    <form class="filter-panel" (ngSubmit)="searchRecipes()">
      <div class="field wide search-field">
        <label for="recipe-search">{{ i18n.t('list.search') }}</label>
        <input id="recipe-search" name="search" autocomplete="off" [(ngModel)]="filters.search"
          (ngModelChange)="onSearchInput($event)" (focus)="suggestionsOpen.set(true)"
          [placeholder]="i18n.t('list.search.placeholder')" />
        @if (suggestionsOpen() && suggestions().length) {
          <ul class="suggestions" role="listbox" [attr.aria-label]="i18n.t('list.suggestions')">
            @for (item of suggestions(); track item.id) {
              <li><a [routerLink]="['/recipes', item.id]">{{ item.name }}</a></li>
            }
          </ul>
        }
      </div>
      <div class="field">
        <label>{{ i18n.t('list.ingredient') }}</label>
        <app-select-field [options]="catalog.ingredientOptions()" [searchable]="true" [(value)]="filters.ingredients" [ariaLabel]="i18n.t('list.ingredient')" />
      </div>
      <div class="field">
        <label>{{ i18n.t('list.tags') }}</label>
        <app-select-field [options]="catalog.options('tag')" [searchable]="true" [(value)]="filters.tags" [ariaLabel]="i18n.t('list.tags')" />
      </div>
      <div class="field">
        <label>{{ i18n.t('list.mealType') }}</label>
        <app-select-field [options]="catalog.options('mealType')" [(value)]="filters.mealTypes" [ariaLabel]="i18n.t('list.mealType')" />
      </div>
      <div class="field">
        <label>{{ i18n.t('list.course') }}</label>
        <app-select-field [options]="catalog.options('course')" [(value)]="filters.courses" [ariaLabel]="i18n.t('list.course')" />
      </div>
      <div class="field">
        <label>{{ i18n.t('list.method') }}</label>
        <app-select-field [options]="catalog.options('cookingMethod')" [(value)]="filters.cookingMethods" [ariaLabel]="i18n.t('list.method')" />
      </div>
      <div class="field">
        <label>{{ i18n.t('list.cuisine') }}</label>
        <app-select-field [options]="catalog.options('cuisine')" [searchable]="true" [(value)]="filters.cuisines" [ariaLabel]="i18n.t('list.cuisine')" />
      </div>
      <div class="field wide">
        <label>{{ i18n.t('list.readyWithin') }}</label>
        <div class="time-chips">
          @for (chip of timeChips; track chip) {
            <button type="button" class="time-chip" [class.active]="timeChip === chip" [attr.aria-pressed]="timeChip === chip" (click)="toggleTime(chip)">
              {{ i18n.t(timeKey(chip)) }}
            </button>
          }
        </div>
      </div>
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
        @if (auth.currentUser()) {
          <button type="button" class="button secondary" [attr.aria-pressed]="favoritesOnly()" (click)="favoritesOnly.set(!favoritesOnly())">
            {{ favoritesOnly() ? '★' : '☆' }} {{ i18n.t('fav.only') }}
          </button>
        }
        <button class="button" type="submit">{{ i18n.t('list.apply') }}</button>
        <button class="button secondary" type="button" (click)="clearFilters()">{{ i18n.t('list.clear') }}</button>
      </div>
    </form>

    <section class="library-section">
      <div class="section-heading">
        <h2>{{ i18n.t('list.heading') }}</h2>
        @if (shown(); as recipeList) { <span class="muted">{{ i18n.t('list.found', { n: recipeList.length }) }}</span> }
      </div>
      @if (error()) {
        <div class="state-card" role="alert">{{ i18n.t('list.error') }}</div>
      } @else {
        @if (shown(); as recipeList) {
        @if (recipeList.length) {
          <div class="recipe-grid">
            @for (recipe of recipeList; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <app-favorite-button class="card-fav" [recipeId]="recipe.id" [compact]="true" />
                <app-plan-button class="card-plan" [recipeId]="recipe.id" [compact]="true" />
                @if (recipe.imageUrl | recipeImage; as src) { <img class="card-image" [src]="src" alt="" loading="lazy" /> }
                <p class="eyebrow">{{ catalog.label('cuisine', recipe.cuisines[0] ?? '') || i18n.t('common.recipeFallback') }} · {{ catalog.label('course', recipe.courses[0] ?? '') || i18n.t('common.anyCourse') }}</p>
                <h3>{{ recipe.name }}</h3>
                <p class="description">{{ recipe.description || i18n.t('list.descriptionFallback') }}</p>
                <p class="recipe-meta">
                  {{ i18n.t('common.minutes', { n: recipe.prepTimeMinutes + recipe.cookTimeMinutes }) }} · {{ i18n.t('common.servings', { n: recipe.servings }) }} ·
                  {{ i18n.t('common.ingredients', { n: recipe.ingredientCount }) }}
                </p>
                <div class="tag-list">
                  @for (tag of recipe.tags; track tag) { <span class="tag">{{ catalog.label('tag', tag) }}</span> }
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
  `,
  styles: `
    .field { display: flex; flex-direction: column; gap: 0.4rem; position: relative; }
    .field > label { color: var(--label); font-size: 0.68rem; font-weight: 500; letter-spacing: 0.16em; text-transform: uppercase; }
    .suggestions {
      position: absolute; z-index: 30; top: 100%; left: 0; right: 0; margin: 4px 0 0; padding: 0.3rem;
      list-style: none; border: 1px solid var(--input-line); border-radius: 0;
      background: var(--paper); box-shadow: 0 14px 34px rgb(0 0 0 / 14%);
    }
    .suggestions a { display: block; padding: 0.45rem 0.6rem; border-radius: 0; color: var(--ink); text-decoration: none; font-size: 0.9rem; }
    .suggestions a:hover { background: var(--green-wash); }
    .time-chips { display: flex; flex-wrap: wrap; gap: 0.4rem; }
    .time-chip {
      padding: 0.4rem 0.9rem; border: 1px solid var(--input-line); border-radius: 0;
      color: var(--ink); background: var(--input-bg); font: inherit; font-size: 0.72rem; letter-spacing: 0.12em; text-transform: uppercase; cursor: pointer;
    }
    .time-chip:hover { border-color: var(--hover-line); }
    .time-chip.active { border-color: var(--green); color: var(--tag-ink); background: var(--green-wash); font-weight: 500; }
  `
})
export class RecipeListComponent {
  private readonly api = inject(RecipeApiService);
  readonly i18n = inject(I18nService);
  readonly catalog = inject(CatalogService);
  readonly auth = inject(AuthService);
  private readonly favorites = inject(FavoriteService);
  readonly favoritesOnly = signal(false);
  readonly recipes = signal<Recipe[] | null>(null);
  readonly shown = computed(() => {
    const list = this.recipes();
    if (!list || !this.favoritesOnly()) return list;
    const ids = this.favorites.ids();
    return list.filter(recipe => ids.has(recipe.id));
  });
  readonly error = signal(false);
  readonly suggestions = signal<RecipeSuggestion[]>([]);
  readonly suggestionsOpen = signal(false);
  readonly timeChips = [15, 30, 60, 120, 240];
  private readonly searchInput = new Subject<string>();
  timeChip: number | null = null;
  maxCalories: number | null = null;
  minRating: number | '' = '';
  filters = this.emptyFilters();

  constructor() {
    this.catalog.load();
    effect(() => {
      this.i18n.lang();
      untracked(() => this.searchRecipes());
    });
    this.searchInput.pipe(
      debounceTime(200),
      distinctUntilChanged(),
      switchMap(text => text.trim().length < 2 ? of([]) : this.api.getSuggestions(text.trim()).pipe(catchError(() => of([])))),
      takeUntilDestroyed()
    ).subscribe(items => this.suggestions.set(items));
  }

  @HostListener('document:click', ['$event'])
  closeSuggestions(event: Event): void {
    if (!(event.target as HTMLElement).closest('.search-field')) this.suggestionsOpen.set(false);
  }

  onSearchInput(text: string): void {
    this.suggestionsOpen.set(true);
    this.searchInput.next(text ?? '');
  }

  timeKey(chip: number): TranslationKey {
    return `list.time.${chip}` as TranslationKey;
  }

  toggleTime(chip: number): void {
    this.timeChip = this.timeChip === chip ? null : chip;
  }

  searchRecipes(): void {
    this.suggestionsOpen.set(false);
    this.error.set(false);
    this.recipes.set(null);
    this.api.getRecipes({
      ...this.filters,
      maxTotalMinutes: this.timeChip !== null && this.timeChip < 240 ? this.timeChip : null,
      minTotalMinutes: this.timeChip === 240 ? 240 : null,
      maxCalories: this.maxCalories,
      minRating: this.minRating === '' ? null : Number(this.minRating)
    }).subscribe({
      next: value => this.recipes.set(value),
      error: () => this.error.set(true)
    });
  }

  clearFilters(): void {
    this.filters = this.emptyFilters();
    this.timeChip = null;
    this.maxCalories = null;
    this.minRating = '';
    this.suggestions.set([]);
    this.searchRecipes();
  }

  private emptyFilters() {
    return {
      search: '',
      ingredients: [] as string[],
      tags: [] as string[],
      mealTypes: [] as string[],
      courses: [] as string[],
      cookingMethods: [] as string[],
      cuisines: [] as string[],
      sort: 'newest'
    };
  }
}
