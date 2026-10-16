import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, effect, inject, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';
import { I18nService, TranslationKey } from './i18n.service';
import { Recipe, RecipeApiService } from './recipe-api.service';
import { CatalogService } from './catalog.service';
import { formatAmount } from './servings-scaling';

@Component({
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink],
  template: `
    @if (recipe(); as item) {
      <a class="back-link" routerLink="/recipes">{{ i18n.t('common.backToRecipes') }}</a>
      <section class="recipe-detail-heading">
        <p class="eyebrow">{{ catalog.label('cuisine', item.cuisines[0] ?? '') || i18n.t('common.recipeFallback') }} · {{ catalog.label('mealType', item.mealTypes[0] ?? '') || i18n.t('common.anyMeal') }} · {{ catalog.label('course', item.courses[0] ?? '') || i18n.t('common.anyCourse') }}</p>
        <h1>{{ item.name }}</h1>
        @if (item.isTranslated) {
          <p class="translation-badge">{{ i18n.t('detail.translated', { language: i18n.languageName(item.language) }) }}</p>
        }
        <p class="intro">{{ item.description }}</p>
        <div class="detail-facts">
          <span>{{ i18n.t('detail.minTotal', { n: item.prepTimeMinutes + item.cookTimeMinutes }) }}</span>
          <span>{{ i18n.t('detail.minPrep', { n: item.prepTimeMinutes }) }}</span>
          <span>{{ i18n.t('detail.minCook', { n: item.cookTimeMinutes }) }}</span>
          <span>{{ i18n.t('common.servings', { n: item.servings }) }}</span>
          @if (item.cookCount) { <span>{{ i18n.t('common.cooks', { n: item.cookCount }) }}</span> }
          @if (item.averageRating) { <span>★ {{ item.averageRating | number:'1.1-1' }}</span> }
        </div>
        <div class="tag-list">
          @for (slug of item.mealTypes; track slug) { <span class="tag">{{ catalog.label('mealType', slug) }}</span> }
          @for (slug of item.courses; track slug) { <span class="tag">{{ catalog.label('course', slug) }}</span> }
          @for (slug of item.cookingMethods; track slug) { <span class="tag">{{ catalog.label('cookingMethod', slug) }}</span> }
          @for (slug of item.cuisines; track slug) { <span class="tag">{{ catalog.label('cuisine', slug) }}</span> }
          @for (tag of item.tags; track tag) { <span class="tag">{{ catalog.label('tag', tag) }}</span> }
        </div>
        @if (auth.currentUser()?.isAdmin) {
          <div class="admin-actions">
            <a class="button secondary" [routerLink]="['/admin/recipes', item.id, 'edit']">{{ i18n.t('detail.edit') }}</a>
            <button class="button danger" type="button" (click)="deleteRecipe()">{{ i18n.t('detail.delete') }}</button>
          </div>
        }
      </section>

      <div class="detail-columns">
        <aside>
          <section class="detail-card">
            <p class="eyebrow">{{ i18n.t('detail.needs') }}</p>
            <h2>{{ i18n.t('detail.ingredients') }}</h2>
            <div class="servings-scaler">
              <span class="servings-label">{{ i18n.t('detail.servings') }}</span>
              <button type="button" class="button secondary" [attr.aria-label]="i18n.t('detail.servings.decrease')" [disabled]="servings() <= 1" (click)="changeServings(-1)">−</button>
              <input type="number" min="1" max="100" step="1" [value]="servings()" [attr.aria-label]="i18n.t('detail.servings')" (change)="setServings($any($event.target))" />
              <button type="button" class="button secondary" [attr.aria-label]="i18n.t('detail.servings.increase')" [disabled]="servings() >= 100" (click)="changeServings(1)">+</button>
              @if (servings() !== item.servings) {
                <button type="button" class="link-button" (click)="servings.set(item.servings)">{{ i18n.t('detail.servings.reset', { n: item.servings }) }}</button>
              }
            </div>
            @for (group of ingredientGroups(item.ingredients); track group.key) {
              @if (group.section) { <h3 class="ingredient-section-title">{{ group.section }}</h3> }
              <ul class="ingredient-list">
                @for (ingredient of group.ingredients; track $index) {
                  <li><strong>{{ amount(ingredient, item.servings) }}</strong> {{ ingredient.name }}
                    @if (ingredient.notes) { <span class="muted">— {{ ingredient.notes }}</span> }
                  </li>
                }
              </ul>
            }
            @if (item.equipment.length) {
              <h3>{{ i18n.t('detail.equipment') }}</h3>
              <ul class="plain-list">@for (tool of item.equipment; track tool) { <li>{{ tool }}</li> }</ul>
            }
            @if (item.caloriesPerServing || item.proteinGramsPerServing || item.carbohydratesGramsPerServing || item.fatGramsPerServing) {
              <h3>{{ i18n.t('detail.nutrition') }}</h3>
              <p class="muted">
                @if (item.caloriesPerServing) { {{ i18n.t('detail.kcal', { n: item.caloriesPerServing }) }} }
                @if (item.proteinGramsPerServing) { · {{ i18n.t('detail.protein', { n: item.proteinGramsPerServing }) }} }
                @if (item.carbohydratesGramsPerServing) { · {{ i18n.t('detail.carbs', { n: item.carbohydratesGramsPerServing }) }} }
                @if (item.fatGramsPerServing) { · {{ i18n.t('detail.fat', { n: item.fatGramsPerServing }) }} }
              </p>
            }
          </section>
        </aside>
        <section class="detail-card instructions">
          <p class="eyebrow">{{ i18n.t('detail.cooking') }}</p>
          <h2>{{ i18n.t('detail.method') }}</h2>
          <ol class="steps-list">
            @for (step of item.steps; track $index) {
              <li><span class="step-number">{{ $index + 1 }}</span><p>{{ step }}</p></li>
            }
          </ol>
        </section>
      </div>

      @if (auth.currentUser()) {
        <section class="cook-panel">
          <div><p class="eyebrow">{{ i18n.t('detail.madeThis') }}</p><h2>{{ i18n.t('detail.logCook') }}</h2><p class="muted">{{ i18n.t('detail.logCookHelp') }}</p></div>
          <form (ngSubmit)="logCooked()">
            <label>{{ i18n.t('detail.rating') }}
              <select name="rating" [(ngModel)]="rating">
                <option [ngValue]="null">{{ i18n.t('detail.rating.skip') }}</option>
                <option [ngValue]="5">{{ i18n.t('detail.rating.5') }}</option>
                <option [ngValue]="4">{{ i18n.t('detail.rating.4') }}</option>
                <option [ngValue]="3">{{ i18n.t('detail.rating.3') }}</option>
                <option [ngValue]="2">{{ i18n.t('detail.rating.2') }}</option>
                <option [ngValue]="1">{{ i18n.t('detail.rating.1') }}</option>
              </select>
            </label>
            <label>{{ i18n.t('detail.notes') }}
              <input name="notes" [(ngModel)]="notes" maxlength="1000" [placeholder]="i18n.t('detail.notes.placeholder')" />
            </label>
            <button class="button" type="submit" [disabled]="saving()">{{ i18n.t('detail.cooked') }}</button>
          </form>
          @if (message()) { <p class="success-message" role="status">{{ i18n.t(message()!) }}</p> }
          @if (error()) { <p class="error-message" role="alert">{{ i18n.t(error()!) }}</p> }
        </section>
      } @else {
        <p class="muted sign-in-note"><a routerLink="/login">{{ i18n.t('detail.signInLink') }}</a>{{ i18n.t('detail.signInNote.after') }}</p>
      }
      <p class="muted added-date">{{ i18n.t('detail.added', { date: (item.addedAt | date:'longDate') ?? '' }) }}</p>
    } @else if (error()) {
      <div class="state-card" role="alert">{{ i18n.t(error()!) }}</div>
    } @else {
      <div class="state-card" role="status">{{ i18n.t('detail.loading') }}</div>
    }
  `
})
export class RecipeDetailComponent {
  private readonly api = inject(RecipeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly catalog = inject(CatalogService);
  readonly recipe = signal<Recipe | null>(null);
  readonly error = signal<TranslationKey | null>(null);
  readonly message = signal<TranslationKey | null>(null);
  readonly saving = signal(false);
  readonly servings = signal(1);
  rating: number | null = null;
  notes = '';

  constructor() {
    effect(() => {
      this.i18n.lang();
      untracked(() => this.load());
    });
  }

  ingredientGroups(ingredients: Recipe['ingredients']): { key: string; section: string; ingredients: Recipe['ingredients'] }[] {
    const groups = new Map<string, { key: string; section: string; ingredients: Recipe['ingredients'] }>();
    for (const ingredient of ingredients) {
      const section = ingredient.section?.trim() || this.i18n.t('detail.sectionDefault');
      const key = section.toLocaleLowerCase();
      const group = groups.get(key) ?? { key, section, ingredients: [] };
      group.ingredients.push(ingredient);
      groups.set(key, group);
    }
    return [...groups.values()];
  }

  amount(ingredient: Recipe['ingredients'][number], baseServings: number): string {
    return formatAmount(
      ingredient.quantity,
      ingredient.unit,
      this.servings() / baseServings,
      this.i18n.lang() === 'da' ? 'da' : 'en');
  }

  changeServings(delta: number): void {
    this.servings.update(value => Math.min(100, Math.max(1, value + delta)));
  }

  setServings(input: HTMLInputElement): void {
    const value = Math.round(Number(input.value));
    if (Number.isFinite(value) && value >= 1) {
      this.servings.set(Math.min(100, value));
    }

    input.value = String(this.servings());
  }

  private load(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('detail.notFound');
      return;
    }
    this.api.getRecipe(id).subscribe({
      next: value => {
        if (this.recipe()?.id !== value.id) {
          this.servings.set(value.servings);
        }
        this.recipe.set(value);
        this.error.set(null);
      },
      error: () => this.error.set('detail.notFoundShort')
    });
  }

  logCooked(): void {
    const recipe = this.recipe();
    if (!recipe) return;
    this.saving.set(true);
    this.error.set(null);
    this.api.logCooked(recipe.id, this.rating, this.notes).subscribe({
      next: () => {
        this.saving.set(false);
        this.message.set('detail.cookSaved');
      },
      error: () => {
        this.saving.set(false);
        this.error.set('detail.cookFailed');
      }
    });
  }

  deleteRecipe(): void {
    const recipe = this.recipe();
    if (!recipe || !window.confirm(this.i18n.t('detail.deleteConfirm', { name: recipe.name }))) return;
    this.api.deleteRecipe(recipe.id).subscribe({
      next: () => void this.router.navigate(['/recipes']),
      error: () => this.error.set('detail.deleteFailed')
    });
  }
}
