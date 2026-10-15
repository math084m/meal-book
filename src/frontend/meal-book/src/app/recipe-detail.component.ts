import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from './auth.service';
import { Recipe, RecipeApiService } from './recipe-api.service';

@Component({
  standalone: true,
  imports: [DatePipe, DecimalPipe, FormsModule, RouterLink],
  template: `
    @if (recipe(); as item) {
      <a class="back-link" routerLink="/recipes">← All recipes</a>
      <section class="recipe-detail-heading">
        <p class="eyebrow">{{ item.cuisine || 'RECIPE' }} · {{ item.mealType || 'ANY MEAL' }} · {{ item.course || 'ANY COURSE' }}</p>
        <h1>{{ item.name }}</h1>
        <p class="intro">{{ item.description }}</p>
        <div class="detail-facts">
          <span>{{ item.prepTimeMinutes + item.cookTimeMinutes }} min total</span>
          <span>{{ item.prepTimeMinutes }} min prep</span>
          <span>{{ item.cookTimeMinutes }} min cook</span>
          <span>{{ item.servings }} servings</span>
          @if (item.cookCount) { <span>{{ item.cookCount }} cooks</span> }
          @if (item.averageRating) { <span>★ {{ item.averageRating | number:'1.1-1' }}</span> }
        </div>
        <div class="tag-list">
          @for (tag of item.tags; track tag) { <span class="tag">{{ tag }}</span> }
        </div>
        @if (auth.currentUser()?.isAdmin) {
          <div class="admin-actions">
            <a class="button secondary" [routerLink]="['/admin/recipes', item.id, 'edit']">Edit recipe</a>
            <button class="button danger" type="button" (click)="deleteRecipe()">Delete</button>
          </div>
        }
      </section>

      <div class="detail-columns">
        <aside>
          <section class="detail-card">
            <p class="eyebrow">WHAT YOU NEED</p>
            <h2>Ingredients</h2>
            <ul class="ingredient-list">
              @for (ingredient of item.ingredients; track $index) {
                <li><strong>{{ ingredient.quantity }} {{ ingredient.unit }}</strong> {{ ingredient.name }}
                  @if (ingredient.notes) { <span class="muted">— {{ ingredient.notes }}</span> }
                </li>
              }
            </ul>
            @if (item.equipment.length) {
              <h3>Equipment</h3>
              <ul class="plain-list">@for (tool of item.equipment; track tool) { <li>{{ tool }}</li> }</ul>
            }
            @if (item.caloriesPerServing || item.proteinGramsPerServing || item.carbohydratesGramsPerServing || item.fatGramsPerServing) {
              <h3>Nutrition per serving</h3>
              <p class="muted">
                @if (item.caloriesPerServing) { {{ item.caloriesPerServing }} kcal }
                @if (item.proteinGramsPerServing) { · {{ item.proteinGramsPerServing }}g protein }
                @if (item.carbohydratesGramsPerServing) { · {{ item.carbohydratesGramsPerServing }}g carbs }
                @if (item.fatGramsPerServing) { · {{ item.fatGramsPerServing }}g fat }
              </p>
            }
          </section>
        </aside>
        <section class="detail-card instructions">
          <p class="eyebrow">LET'S GET COOKING</p>
          <h2>Method</h2>
          <ol class="steps-list">
            @for (step of item.steps; track $index) {
              <li><span class="step-number">{{ $index + 1 }}</span><p>{{ step }}</p></li>
            }
          </ol>
        </section>
      </div>

      @if (auth.currentUser()) {
        <section class="cook-panel">
          <div><p class="eyebrow">MADE THIS?</p><h2>Log your cook</h2><p class="muted">Your rating helps surface community favorites.</p></div>
          <form (ngSubmit)="logCooked()">
            <label>Rating
              <select name="rating" [(ngModel)]="rating">
                <option [ngValue]="null">Skip rating</option>
                <option [ngValue]="5">5 — loved it</option>
                <option [ngValue]="4">4 — very good</option>
                <option [ngValue]="3">3 — good</option>
                <option [ngValue]="2">2 — needs work</option>
                <option [ngValue]="1">1 — not for me</option>
              </select>
            </label>
            <label>Notes
              <input name="notes" [(ngModel)]="notes" maxlength="1000" placeholder="What would you change next time?" />
            </label>
            <button class="button" type="submit" [disabled]="saving()">I cooked this</button>
          </form>
          @if (message()) { <p class="success-message" role="status">{{ message() }}</p> }
          @if (error()) { <p class="error-message" role="alert">{{ error() }}</p> }
        </section>
      } @else {
        <p class="muted sign-in-note"><a routerLink="/login">Sign in</a> to log a cook and rate this recipe.</p>
      }
      <p class="muted added-date">Added {{ item.addedAt | date:'longDate' }}</p>
    } @else if (error()) {
      <div class="state-card" role="alert">{{ error() }}</div>
    } @else {
      <div class="state-card" role="status">Loading recipe…</div>
    }
  `
})
export class RecipeDetailComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);
  readonly recipe = signal<Recipe | null>(null);
  readonly error = signal('');
  readonly message = signal('');
  readonly saving = signal(false);
  rating: number | null = null;
  notes = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set('Recipe not found.');
      return;
    }
    this.api.getRecipe(id).subscribe({
      next: value => this.recipe.set(value),
      error: () => this.error.set('Recipe could not be found.')
    });
  }

  logCooked(): void {
    const recipe = this.recipe();
    if (!recipe) return;
    this.saving.set(true);
    this.error.set('');
    this.api.logCooked(recipe.id, this.rating, this.notes).subscribe({
      next: () => {
        this.saving.set(false);
        this.message.set('Cook added to your history. Thanks for rating!');
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Could not save cooking entry. Please try again.');
      }
    });
  }

  deleteRecipe(): void {
    const recipe = this.recipe();
    if (!recipe || !window.confirm(`Delete "${recipe.name}"? This cannot be undone.`)) return;
    this.api.deleteRecipe(recipe.id).subscribe({
      next: () => void this.router.navigate(['/recipes']),
      error: () => this.error.set('Recipe could not be deleted.')
    });
  }
}
