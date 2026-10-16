import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { I18nService } from './i18n.service';
import { PlanService, ShoppingItem } from './plan.service';
import { RecipeImagePipe } from './recipe-image.pipe';
import { formatTotal } from './servings-scaling';

@Component({
  selector: 'app-plan',
  standalone: true,
  imports: [RouterLink, RecipeImagePipe],
  template: `
    <section class="plan-page">
      <div class="section-heading">
        <div>
          <p class="eyebrow">{{ i18n.t('plan.eyebrow') }}</p>
          <h1>{{ i18n.t('plan.title') }}</h1>
          <p class="muted">{{ i18n.t('plan.intro') }}
            <span class="live-pill" [class.off]="!plan.live()">{{ plan.live() ? i18n.t('plan.live') : i18n.t('plan.reconnecting') }}</span>
          </p>
        </div>
      </div>

      @if (plan.plan(); as data) {
        <div class="plan-columns">
          <div>
            <section class="detail-card">
              <div class="plan-head">
                <h2>{{ i18n.t('plan.recipes') }}</h2>
                @if (data.entries.length) {
                  <button type="button" class="link-button" (click)="clearPlan()">{{ i18n.t('plan.clear') }}</button>
                }
              </div>
              @if (data.entries.length) {
                <ul class="plan-list">
                  @for (entry of data.entries; track entry.recipeId) {
                    <li>
                      @if (entry.imageUrl | recipeImage; as src) { <img class="plan-thumb" [src]="src" alt="" /> }
                      <a [routerLink]="['/recipes', entry.recipeId]" class="plan-name">{{ entry.recipeName }}</a>
                      <span class="plan-stepper" role="group" [attr.aria-label]="i18n.t('plan.servings.aria', { name: entry.recipeName })">
                        <button type="button" class="button secondary small" [disabled]="entry.servings <= 1" (click)="plan.setServings(entry.recipeId, entry.servings - 1)">−</button>
                        <span>{{ entry.servings }}</span>
                        <button type="button" class="button secondary small" [disabled]="entry.servings >= 100" (click)="plan.setServings(entry.recipeId, entry.servings + 1)">+</button>
                      </span>
                      <button type="button" class="link-button" [attr.aria-label]="i18n.t('plan.removeAria', { name: entry.recipeName })" (click)="plan.remove(entry.recipeId)">✕</button>
                    </li>
                  }
                </ul>
              } @else {
                <p class="muted">{{ i18n.t('plan.empty') }}</p>
              }
            </section>

            @if (plan.suggestions().length) {
              <section class="detail-card">
                <h2>{{ i18n.t('plan.suggestions') }}</h2>
                <p class="muted">{{ i18n.t('plan.suggestions.intro') }}</p>
                <ul class="plan-list">
                  @for (s of plan.suggestions(); track s.recipeId) {
                    <li>
                      @if (s.imageUrl | recipeImage; as src) { <img class="plan-thumb" [src]="src" alt="" /> }
                      <div class="plan-name">
                        <a [routerLink]="['/recipes', s.recipeId]">{{ s.name }}</a>
                        <div class="muted small-text">
                          {{ i18n.t('plan.suggestions.shares', { items: s.sharedIngredients.join(', ') }) }}
                          @if (s.newPerishables) { · {{ i18n.t('plan.suggestions.extra', { n: s.newPerishables }) }} }
                        </div>
                      </div>
                      <button type="button" class="button secondary small" (click)="plan.add(s.recipeId)">{{ i18n.t('plan.add') }}</button>
                    </li>
                  }
                </ul>
              </section>
            }
          </div>

          <section class="detail-card">
            <div class="plan-head">
              <h2>{{ i18n.t('plan.shopping') }}</h2>
              @if (data.sections.length) {
                <button type="button" class="link-button" (click)="plan.uncheckAll()">{{ i18n.t('plan.uncheckAll') }}</button>
              }
            </div>
            @if (data.sections.length) {
              @for (section of data.sections; track section.slug) {
                <h3 class="shop-section">{{ section.name }}</h3>
                <ul class="shop-list">
                  @for (item of section.items; track item.ingredientId + '|' + item.name + '|' + item.unit) {
                    <li [class.done]="item.checked" [title]="i18n.t('plan.shopping.for', { recipes: item.recipes.join(', ') })">
                      <label>
                        <input type="checkbox" [checked]="item.checked" [disabled]="!item.ingredientId" (change)="toggle(item, $event)" />
                        <span class="shop-qty">{{ amount(item) }}</span>
                        <span>{{ item.name }}</span>
                      </label>
                    </li>
                  }
                </ul>
              }
            } @else {
              <p class="muted">{{ i18n.t('plan.shopping.empty') }}</p>
            }
          </section>
        </div>
      } @else if (plan.error()) {
        <div class="state-card" role="alert">{{ i18n.t('plan.loadError') }}</div>
      } @else {
        <div class="state-card" role="status">{{ i18n.t('plan.loading') }}</div>
      }
    </section>
  `,
  styles: `
    .plan-columns { display: grid; grid-template-columns: 1fr 1fr; gap: 1.2rem; align-items: start; }
    @media (max-width: 800px) { .plan-columns { grid-template-columns: 1fr; } }
    .plan-columns > div { display: flex; flex-direction: column; gap: 1.2rem; }
    .plan-head { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; }
    .plan-list, .shop-list { list-style: none; margin: 0; padding: 0; }
    .plan-list li { display: flex; align-items: center; gap: 0.8rem; padding: 0.6rem 0; border-top: 1px solid var(--line); }
    .plan-thumb { width: 3.2rem; height: 2.2rem; object-fit: cover; border-radius: 6px; }
    .plan-name { flex: 1; min-width: 0; }
    .plan-stepper { display: inline-flex; align-items: center; gap: 0.4rem; }
    .small-text { font-size: 0.85rem; }
    .shop-section { margin: 1.2rem 0 0.3rem; font-size: 1rem; color: var(--green); }
    .shop-list li { padding: 0.3rem 0; border-top: 1px solid var(--line); }
    .shop-list label { display: flex; align-items: center; gap: 0.6rem; cursor: pointer; }
    .shop-qty { font-weight: 650; min-width: 4.5rem; }
    .shop-list li.done span { text-decoration: line-through; color: var(--muted); }
    .live-pill { margin-left: 0.5rem; padding: 0.1rem 0.5rem; border-radius: 999px; background: var(--green-wash); color: var(--tag-ink); font-size: 0.78rem; }
    .live-pill.off { background: transparent; border: 1px solid var(--line); color: var(--muted); }
  `
})
export class PlanComponent {
  readonly plan = inject(PlanService);
  readonly i18n = inject(I18nService);

  amount(item: ShoppingItem): string {
    return formatTotal(item.quantity, item.unit, this.i18n.lang());
  }

  toggle(item: ShoppingItem, event: Event): void {
    this.plan.setChecked(item, (event.target as HTMLInputElement).checked);
  }

  clearPlan(): void {
    if (confirm(this.i18n.t('plan.clear.confirm'))) this.plan.clear();
  }
}
