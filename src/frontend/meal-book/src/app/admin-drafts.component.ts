import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
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
    }
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
              <div class="draft-actions">
                <a class="button" [routerLink]="['/admin/recipes', recipe.id, 'edit']">Review recipe</a>
                <button type="button" class="button" [disabled]="busyId() === recipe.id" (click)="publish(recipe)">Publish</button>
                <button type="button" class="button danger" [disabled]="busyId() === recipe.id" (click)="discard(recipe)">Discard</button>
              </div>
            </article>
          }
        </div>
      } @else {
        <div class="state-card">No drafts need review.</div>
      }
    } @else if (!error()) {
      <div class="state-card" role="status">Loading drafts…</div>
    }
  `
})
export class AdminDraftsComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  readonly drafts = signal<Recipe[] | null>(null);
  readonly error = signal('');
  readonly busyId = signal('');

  publish(recipe: Recipe): void {
    this.act(recipe, this.api.publishRecipe(recipe.id), 'Could not publish the recipe.');
  }

  discard(recipe: Recipe): void {
    if (!confirm(`Discard draft "${recipe.name}"? This cannot be undone.`)) {
      return;
    }
    this.act(recipe, this.api.deleteRecipe(recipe.id), 'Could not discard the draft.');
  }

  private act(recipe: Recipe, request: Observable<unknown>, failure: string): void {
    this.busyId.set(recipe.id);
    this.error.set('');
    request.subscribe({
      next: () => {
        this.drafts.update(list => list?.filter(item => item.id !== recipe.id) ?? null);
        this.busyId.set('');
      },
      error: () => {
        this.error.set(failure);
        this.busyId.set('');
      }
    });
  }

  ngOnInit(): void {
    this.api.getDrafts().subscribe({
      next: value => this.drafts.set(value),
      error: () => this.error.set('Could not load drafts. Check your admin sign-in.')
    });
  }
}
