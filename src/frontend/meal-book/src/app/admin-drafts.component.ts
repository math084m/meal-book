import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { Recipe, RecipeApiService } from './recipe-api.service';
import { I18nService, TranslationKey } from './i18n.service';

@Component({
  standalone: true,
  imports: [RouterLink],
  template: `
    <section class="page-heading">
      <p class="eyebrow">{{ i18n.t('drafts.eyebrow') }}</p>
      <h1>{{ i18n.t('drafts.title') }}</h1>
      <p class="intro">{{ i18n.t('drafts.intro') }}</p>
    </section>
    @if (error()) {
      <div class="state-card" role="alert">{{ i18n.t(error()!) }}</div>
    }
    @if (drafts(); as list) {
      @if (list.length) {
        <div class="recipe-grid draft-grid">
          @for (recipe of list; track recipe.id) {
            <article class="recipe-card">
              <p class="eyebrow">{{ i18n.t('drafts.needsReview') }}</p>
              <h2>{{ recipe.name }}</h2>
              <p class="description">{{ recipe.description || i18n.t('drafts.noDescription') }}</p>
              <p class="recipe-meta">{{ i18n.t('drafts.counts', { ingredients: recipe.ingredients.length, steps: recipe.steps.length, minutes: recipe.prepTimeMinutes + recipe.cookTimeMinutes }) }}</p>
              @if (recipe.sourceUrl) {
                <a class="source-link" [href]="recipe.sourceUrl" target="_blank" rel="noopener noreferrer">{{ i18n.t('drafts.source') }}</a>
              }
              <div class="draft-actions">
                <a class="button" [routerLink]="['/admin/recipes', recipe.id, 'edit']">{{ i18n.t('drafts.review') }}</a>
                <button type="button" class="button" [disabled]="busyId() === recipe.id" (click)="publish(recipe)">{{ i18n.t('drafts.publish') }}</button>
                <button type="button" class="button danger" [disabled]="busyId() === recipe.id" (click)="discard(recipe)">{{ i18n.t('drafts.discard') }}</button>
              </div>
            </article>
          }
        </div>
      } @else {
        <div class="state-card">{{ i18n.t('drafts.empty') }}</div>
      }
    } @else if (!error()) {
      <div class="state-card" role="status">{{ i18n.t('drafts.loading') }}</div>
    }
  `
})
export class AdminDraftsComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  readonly i18n = inject(I18nService);
  readonly drafts = signal<Recipe[] | null>(null);
  readonly error = signal<TranslationKey | null>(null);
  readonly busyId = signal('');

  publish(recipe: Recipe): void {
    this.act(recipe, this.api.publishRecipe(recipe.id), 'drafts.publishFailed');
  }

  discard(recipe: Recipe): void {
    if (!confirm(this.i18n.t('drafts.discardConfirm', { name: recipe.name }))) {
      return;
    }
    this.act(recipe, this.api.deleteRecipe(recipe.id), 'drafts.discardFailed');
  }

  private act(recipe: Recipe, request: Observable<unknown>, failure: TranslationKey): void {
    this.busyId.set(recipe.id);
    this.error.set(null);
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
      error: () => this.error.set('drafts.loadFailed')
    });
  }
}
