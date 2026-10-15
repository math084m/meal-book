import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Recipe, RecipeApiService } from './recipe-api.service';

@Component({
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="page-heading">
      <p class="eyebrow">ADMIN REVIEW</p>
      <h1>Recipe drafts.</h1>
      <p class="intro">Imported recipes stay private until you check details and publish them.</p>
    </section>
    @if (error()) {
      <div class="state-card" role="alert">{{ error() }}</div>
    } @else {
      @if (drafts(); as list) {
        @if (list.length) {
          <div class="recipe-grid draft-grid">
            @for (recipe of list; track recipe.id) {
              <article class="recipe-card">
                <p class="eyebrow">NEEDS REVIEW</p>
                <h2>{{ recipe.name }}</h2>
                <p class="description">{{ recipe.description || 'No description provided.' }}</p>
                <p class="recipe-meta">{{ recipe.ingredients.length }} ingredients · {{ recipe.steps.length }} steps · {{ recipe.prepTimeMinutes + recipe.cookTimeMinutes }} min</p>
                @if (recipe.sourceUrl) {
                  <a class="source-link" [href]="recipe.sourceUrl" target="_blank" rel="noopener noreferrer">Open source recipe ↗</a>
                }
                <a class="button" [routerLink]="['/admin/recipes', recipe.id, 'edit']">Review recipe</a>
              </article>
            }
          </div>
        } @else {
          <div class="state-card">No drafts need review.</div>
        }
      } @else {
        <div class="state-card" role="status">Loading drafts…</div>
      }
    }
  `
})
export class AdminDraftsComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  readonly drafts = signal<Recipe[] | null>(null);
  readonly error = signal('');

  ngOnInit(): void {
    this.api.getDrafts().subscribe({
      next: value => this.drafts.set(value),
      error: () => this.error.set('Could not load drafts. Check your admin sign-in.')
    });
  }
}
