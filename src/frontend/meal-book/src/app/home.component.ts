import { Component, HostListener, computed, effect, inject, signal, untracked, ChangeDetectionStrategy } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, of, switchMap } from 'rxjs';
import { I18nService } from './i18n.service';
import { Recipe, RecipeApiService, RecipeDashboard, RecipeSuggestion } from './recipe-api.service';
import { RecipeImagePipe } from './recipe-image.pipe';
import { PlanButtonComponent } from './plan-button.component';
import { FavoriteButtonComponent } from './favorite-button.component';
import { CatalogService } from './catalog.service';

@Component({
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe, RecipeImagePipe, PlanButtonComponent, FavoriteButtonComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <section class="home-hero">
      <h1>Meal Book</h1>
      <p class="home-subtitle">
        {{ i18n.t('home.subtitle') }}
        @if (library(); as list) {
          <span class="home-counts">· {{ i18n.t('home.count.recipes', { n: list.length }) }} · {{ i18n.t('home.count.cuisines', { n: cuisineCount() }) }}</span>
        }
      </p>

      <form class="home-search search-field" role="search" (submit)="$event.preventDefault(); submitSearch()">
        <label class="visually-hidden" for="home-search">{{ i18n.t('list.search') }}</label>
        <input id="home-search" name="search" type="search" autocomplete="off" [value]="searchText"
          (input)="onSearchInput($any($event.target).value)" (focus)="suggestionsOpen.set(true)"
          [placeholder]="i18n.t('list.search.placeholder')" />
        <button class="button" type="submit">{{ i18n.t('home.search') }}</button>
        @if (suggestionsOpen() && suggestions().length) {
          <ul class="suggestions" role="listbox" [attr.aria-label]="i18n.t('list.suggestions')">
            @for (item of suggestions(); track item.id) {
              <li><a [routerLink]="['/recipes', item.id]">{{ item.name }}</a></li>
            }
          </ul>
        }
      </form>

      @if (topCuisines().length) {
        <nav class="cuisine-chips" [attr.aria-label]="i18n.t('home.cuisines')">
          @for (cuisine of topCuisines(); track cuisine) {
            <a class="chip" routerLink="/recipes" [queryParams]="{ cuisine }">{{ catalog.label('cuisine', cuisine) }}</a>
          }
          <a class="chip" routerLink="/recipes">{{ i18n.t('home.all') }} →</a>
        </nav>
      }
    </section>

    @if (photos().length) {
      <section class="photo-mosaic" [class.few]="photos().length < 5" [attr.aria-label]="i18n.t('home.photos')">
        @for (recipe of photos(); track recipe.id) {
          <a class="photo-tile" [routerLink]="['/recipes', recipe.id]">
            <img [src]="recipe.imageUrl | recipeImage" alt="" loading="lazy" />
            <span>{{ recipe.name }}</span>
          </a>
        }
      </section>
    }

    @if (error()) {
      <div class="state-card" role="alert">
        <h2>{{ i18n.t('home.unavailable.title') }}</h2>
        <p>{{ i18n.t('home.unavailable.text') }}</p>
      </div>
    } @else {
      @if (dashboard(); as data) {
      <section class="dashboard-section">
        <div class="section-heading">
          <h2>{{ i18n.t('home.recently.title') }}</h2>
        </div>
        @if (data.recentlyCooked.length) {
          <div class="recipe-grid compact">
            @for (item of data.recentlyCooked; track item.cookedAt + item.recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', item.recipe.id]">
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
          <h2>{{ i18n.t('home.added.title') }}</h2>
          <a routerLink="/recipes">{{ i18n.t('home.added.browse') }}</a>
        </div>
        @if (data.recentlyAdded.length) {
          <div class="recipe-grid">
            @for (recipe of data.recentlyAdded; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <app-favorite-button class="card-fav" [recipeId]="recipe.id" [compact]="true" />
                <app-plan-button class="card-plan" [recipeId]="recipe.id" [compact]="true" />
                @if (recipe.imageUrl | recipeImage; as src) { <img class="card-image" [src]="src" alt="" loading="lazy" /> }
                <p class="eyebrow">{{ catalog.label('cuisine', recipe.cuisines[0] ?? '') || i18n.t('common.recipeFallback') }} · {{ catalog.label('course', recipe.courses[0] ?? '') || i18n.t('common.anyCourse') }}</p>
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
          <h2>{{ i18n.t('home.favorites.title') }}</h2>
        </div>
        @if (data.mostCooked.length) {
          <div class="recipe-grid">
            @for (recipe of data.mostCooked; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <p class="eyebrow">                {{ i18n.t('home.favorites.cooks', { n: recipe.cookCount }) }}@if (recipe.averageRating) { · ★ {{ recipe.averageRating | number:'1.1-1' }} }</p>
                <h3>{{ recipe.name }}</h3>
                                <p class="recipe-meta">{{ catalog.label('mealType', recipe.mealTypes[0] ?? '') || i18n.t('home.favorites.anyMeal') }} · {{ catalog.label('cuisine', recipe.cuisines[0] ?? '') || i18n.t('home.favorites.noCuisine') }}</p>
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
  `,
  styles: `
    .home-hero { max-width: 720px; margin: 0 auto; padding: clamp(2.5rem, 6vw, 4.5rem) 0 2rem; text-align: center; }
    .home-hero h1 { margin-bottom: 0.4rem; }
    .home-subtitle { margin: 0 0 1.8rem; color: var(--muted); font-family: var(--serif); font-size: 1.25rem; font-style: italic; }
    .home-counts { font-style: normal; font-family: inherit; font-size: 0.95rem; }
    .home-search { position: relative; display: flex; gap: 0.5rem; max-width: 560px; margin: 0 auto; text-align: left; }
    .home-search input { flex: 1; min-width: 0; min-height: 48px; padding: 0 1rem; border: 1px solid var(--input-line); border-radius: 0; color: var(--ink); background: var(--input-bg); font-size: 1rem; }
    .home-search input:focus { outline: 2px solid var(--green); outline-offset: -1px; }
    .suggestions {
      position: absolute; z-index: 30; top: 100%; left: 0; right: 0; margin: 4px 0 0; padding: 0.3rem;
      list-style: none; border: 1px solid var(--input-line); background: var(--paper); box-shadow: 0 14px 34px rgb(0 0 0 / 14%);
    }
    .suggestions a { display: block; padding: 0.45rem 0.6rem; color: var(--ink); text-decoration: none; }
    .suggestions a:hover, .suggestions a:focus { background: var(--green-wash); }
    .cuisine-chips { display: flex; flex-wrap: wrap; justify-content: center; gap: 0.4rem; margin-top: 1.2rem; }
    .chip {
      padding: 0.4rem 0.9rem; border: 1px solid var(--input-line); color: var(--ink); background: var(--input-bg);
      font-size: 0.72rem; letter-spacing: 0.12em; text-decoration: none; text-transform: uppercase;
    }
    .chip:hover, .chip:focus-visible { border-color: var(--green); color: var(--tag-ink); background: var(--green-wash); }
    .photo-mosaic {
      display: grid; grid-template-columns: repeat(4, 1fr); grid-auto-rows: 170px; gap: 6px; margin: 1rem 0 2.5rem;
    }
    .photo-tile { position: relative; overflow: hidden; display: block; background: var(--line); }
    .photo-tile:first-child { grid-column: span 2; grid-row: span 2; }
    .photo-mosaic.few { grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); grid-auto-rows: 240px; }
    .photo-mosaic.few .photo-tile:first-child { grid-column: auto; grid-row: auto; }
    .photo-tile img { width: 100%; height: 100%; object-fit: cover; display: block; transition: transform 400ms ease; }
    .photo-tile span {
      position: absolute; inset: auto 0 0 0; padding: 1.6rem 0.8rem 0.6rem; color: #fff; font-family: var(--serif); font-size: 1.05rem;
      background: linear-gradient(transparent, rgb(0 0 0 / 60%));
    }
    .photo-tile:hover img, .photo-tile:focus-visible img { transform: scale(1.04); }
    @media (prefers-reduced-motion: reduce) { .photo-tile img { transition: none; } }
    @media (max-width: 720px) {
      .photo-mosaic { grid-template-columns: repeat(2, 1fr); grid-auto-rows: 130px; }
    }
  `
})
export class HomeComponent {
  private readonly api = inject(RecipeApiService);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  readonly catalog = inject(CatalogService);
  readonly dashboard = signal<RecipeDashboard | null>(null);
  readonly library = signal<Recipe[] | null>(null);
  readonly error = signal(false);
  readonly suggestions = signal<RecipeSuggestion[]>([]);
  readonly suggestionsOpen = signal(false);
  private readonly searchInput = new Subject<string>();
  searchText = '';

  readonly photos = computed(() => (this.library() ?? [])
    .filter(recipe => recipe.imageUrl)
    .sort((a, b) => b.cookCount - a.cookCount)
    .slice(0, 5));
  private readonly cuisineCounts = computed(() => {
    const counts = new Map<string, number>();
    for (const recipe of this.library() ?? []) {
      for (const cuisine of recipe.cuisines) counts.set(cuisine, (counts.get(cuisine) ?? 0) + 1);
    }
    return counts;
  });
  readonly cuisineCount = computed(() => this.cuisineCounts().size);
  readonly topCuisines = computed(() =>
    [...this.cuisineCounts()].sort((a, b) => b[1] - a[1]).slice(0, 6).map(([cuisine]) => cuisine));

  constructor() {
    this.catalog.load();
    effect(() => {
      this.i18n.lang();
      untracked(() => this.load());
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
    this.searchText = text ?? '';
    this.suggestionsOpen.set(true);
    this.searchInput.next(text ?? '');
  }

  submitSearch(): void {
    const search = this.searchText.trim();
    void this.router.navigate(['/recipes'], { queryParams: search ? { search } : {} });
  }

  private load(): void {
    this.api.getRecipes().subscribe({
      next: value => this.library.set(value),
      error: () => this.library.set(null)
    });
    this.api.getDashboard().subscribe({
      next: value => {
        this.dashboard.set(value);
        this.error.set(false);
      },
      error: () => this.error.set(true)
    });
  }
}
