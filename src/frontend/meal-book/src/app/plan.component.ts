import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CatalogService } from './catalog.service';
import { I18nService } from './i18n.service';
import { MealPlan, PlanService, ShoppingItem } from './plan.service';
import { RecipeImagePipe } from './recipe-image.pipe';
import { shoppingAmountText, remainingShare, shoppingSourcesText } from './shopping-amounts';

@Component({
  selector: 'app-plan',
  standalone: true,
  imports: [FormsModule, RouterLink, RecipeImagePipe],
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
        <a routerLink="/plan/week" class="button secondary small">{{ i18n.t('week.open') }}</a>
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
              <span class="plan-actions">
                @if (hasItems(data)) {
                  <button type="button" class="link-button" (click)="copyList(data)">{{ copied() ? i18n.t('plan.copied') : i18n.t('plan.copy') }}</button>
                  @if (canShare) {
                    <button type="button" class="link-button" (click)="shareList(data)">{{ i18n.t('plan.share') }}</button>
                  }
                  <button type="button" class="link-button" (click)="plan.uncheckAll()">{{ i18n.t('plan.uncheckAll') }}</button>
                }
              </span>
            </div>

            <form class="custom-form" (submit)="addCustom($event)">
              <input class="custom-name" name="name" [(ngModel)]="customName" [placeholder]="i18n.t('plan.custom.name')" [attr.aria-label]="i18n.t('plan.custom.name')" maxlength="120" required />
              <input class="custom-qty" name="qty" type="number" min="0" step="any" [(ngModel)]="customQuantity" [placeholder]="i18n.t('plan.custom.qty')" [attr.aria-label]="i18n.t('plan.custom.qty')" />
              <input class="custom-unit" name="unit" list="plan-units" [(ngModel)]="customUnit" [placeholder]="i18n.t('plan.custom.unit')" [attr.aria-label]="i18n.t('plan.custom.unit')" maxlength="40" />
              <datalist id="plan-units">
                @for (unit of units; track unit) { <option [value]="unit"></option> }
              </datalist>
              <select class="custom-section" name="section" [(ngModel)]="customSection" [attr.aria-label]="i18n.t('ingredients.section')">
                @for (option of catalog.shopSectionOptions(); track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
              <button type="submit" class="button small" [disabled]="!customName.trim()">{{ i18n.t('plan.custom.add') }}</button>
            </form>
            @if (customError()) { <p class="muted" role="alert">{{ i18n.t('plan.custom.error') }}</p> }

            @if (data.sections.length) {
              @for (section of data.sections; track section.slug) {
                <h3 class="shop-section">{{ section.name }}</h3>
                <ul class="shop-list">
                  @for (item of section.items; track item.customId ?? (item.ingredientId + '|' + item.name + '|' + item.unit)) {
                    <li class="shop-item" [class.done]="item.checked" [title]="item.recipes.length ? i18n.t('plan.shopping.for', { recipes: item.recipes.join(', ') }) : ''">
                      <label class="shop-main">
                        <input type="checkbox" [checked]="item.checked" [disabled]="!item.ingredientId && !item.customId" (change)="toggle(item, $event)" />
                        <span class="shop-text">
                          <span class="shop-name">{{ item.name }}</span>
                          @if (item.customId ? item.quantity > 0 || item.unit : true) {
                            <span class="shop-qty">{{ amount(item) }}@if (item.have > 0) { · {{ remaining(item) }} }</span>
                          }
                          @if ((item.sources?.length ?? 0) > 1) {
                            <span class="shop-sources">{{ sources(item) }}</span>
                          }
                        </span>
                      </label>
                      @if (item.ingredientId) {
                        <select class="have-select" [attr.aria-label]="i18n.t('plan.have.aria', { name: item.name })" [title]="i18n.t('plan.have')" (change)="setHave(item, $event)">
                          @for (option of haveOptions; track option.value) {
                            <option [value]="option.value" [selected]="option.value === haveFraction(item)">{{ option.value === '0' ? i18n.t('plan.have.none') : i18n.t('plan.have.share', { share: option.label }) }}</option>
                          }
                        </select>
                      }
                      @if (item.customId) {
                        <button type="button" class="link-button" [attr.aria-label]="i18n.t('plan.custom.removeAria', { name: item.name })" (click)="plan.removeCustom(item)">✕</button>
                      }
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
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: `
    .plan-columns { display: flex; flex-direction: column; gap: 1.2rem; }
    .plan-columns > div { display: flex; flex-direction: column; gap: 1.2rem; }
    .plan-actions { display: inline-flex; gap: 1rem; flex-wrap: wrap; justify-content: flex-end; }
    .custom-form { display: flex; flex-wrap: wrap; gap: 0.5rem; margin: 0.8rem 0 0.4rem; }
    .custom-form input, .custom-form select { width: auto; min-height: 38px; }
    .custom-name { flex: 1 1 11rem; }
    .custom-qty { width: 5rem !important; }
    .custom-unit { width: 6.5rem !important; }
    .custom-section { flex: 0 1 12rem; }
    .plan-head { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; }
    .plan-list, .shop-list { list-style: none; margin: 0; padding: 0; }
    .plan-list li { display: flex; align-items: center; gap: 0.8rem; padding: 0.6rem 0; border-top: 1px solid var(--line); }
    .plan-thumb { width: 3.2rem; height: 2.2rem; object-fit: cover; border-radius: 0; }
    .plan-name { flex: 1; min-width: 0; }
    .plan-stepper { display: inline-flex; align-items: center; gap: 0.4rem; }
    .small-text { font-size: 0.85rem; }
    .shop-section { margin: 1.2rem 0 0.3rem; font-size: 1rem; color: var(--green); }
    .shop-list li { display: flex; align-items: center; gap: 0.8rem; padding: 0.5rem 0; border-top: 1px solid var(--line); }
    .shop-main { flex: 1; min-width: 0; flex-direction: row; align-items: center; gap: 0.8rem; cursor: pointer; font-size: 0.95rem; font-weight: 400; color: var(--ink); }
    .shop-main input[type='checkbox'] { flex: none; width: 1.25rem; min-height: 0; height: 1.25rem; padding: 0; accent-color: var(--green); cursor: pointer; }
    .shop-text { display: flex; flex-direction: column; min-width: 0; }
    .shop-name { font-weight: 600; }
    .shop-qty { font-size: 0.85rem; color: var(--muted); }
    .shop-sources { font-size: 0.75rem; color: var(--muted); font-style: italic; }
    .shop-list li.done .shop-name { text-decoration: line-through; color: var(--muted); }
    .have-select { flex: none; width: auto; min-height: 0; padding: 0.25rem 0.4rem; font-size: 0.82rem; color: var(--muted); }
    .plan-head .link-button { background: none; border: 0; padding: 0; color: var(--green); font-size: 0.85rem; text-decoration: underline; cursor: pointer; }
    .live-pill { margin-left: 0.5rem; padding: 0.1rem 0.5rem; border: 1px solid var(--line); border-radius: 0; background: transparent; color: var(--tag-ink); font-size: 0.66rem; letter-spacing: 0.14em; text-transform: uppercase; }
    .live-pill.off { background: transparent; border: 1px solid var(--line); color: var(--muted); }
  `
})
export class PlanComponent {
  readonly plan = inject(PlanService);
  readonly i18n = inject(I18nService);

  readonly catalog = inject(CatalogService);
  readonly units = ['g', 'kg', 'ml', 'cl', 'dl', 'l', 'piece', 'pack', 'can', 'bottle', 'bag', 'roll', 'box'];
  readonly canShare = typeof navigator !== 'undefined' && typeof navigator.share === 'function';
  readonly copied = signal(false);
  readonly customError = signal(false);
  customName = '';
  customQuantity: number | null = null;
  customUnit = '';
  customSection = 'nonfood';

  amount(item: ShoppingItem): string {
    return item.customId ? this.customAmount(item) : shoppingAmountText(item, this.i18n.lang());
  }

  sources(item: ShoppingItem): string {
    return shoppingSourcesText(item.sources, this.i18n.lang());
  }

  private customAmount(item: ShoppingItem): string {
    return [item.quantity > 0 ? String(Number(item.quantity.toFixed(3))) : '', item.unit].filter(Boolean).join(' ');
  }

  hasItems(data: MealPlan): boolean {
    return data.sections.some(section => section.items.length > 0);
  }

  addCustom(event: Event): void {
    event.preventDefault();
    const name = this.customName.trim();
    if (!name) return;
    this.plan.addCustom(name, Math.max(0, Number(this.customQuantity) || 0), this.customUnit.trim(), this.customSection).subscribe({
      next: () => {
        this.customName = '';
        this.customQuantity = null;
        this.customError.set(false);
      },
      error: () => this.customError.set(true)
    });
  }

  // Plain text, one section per block, only what is still left to buy.
  listText(data: MealPlan): string {
    const blocks = data.sections
      .map(section => {
        const lines = section.items
          .filter(item => !item.checked && item.quantity - item.have > 0 || (!item.checked && item.customId))
          .map(item => {
            const left = item.customId
              ? this.customAmount(item)
              : shoppingAmountText(item, this.i18n.lang(), remainingShare(item));
            return `- ${left ? left + ' ' : ''}${item.name}`;
          });
        return lines.length ? `${section.name}\n${lines.join('\n')}` : '';
      })
      .filter(Boolean);
    return `${this.i18n.t('plan.shopping')}\n\n${blocks.join('\n\n')}`;
  }

  async copyList(data: MealPlan): Promise<void> {
    await navigator.clipboard.writeText(this.listText(data));
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 2000);
  }

  async shareList(data: MealPlan): Promise<void> {
    try {
      await navigator.share({ title: this.i18n.t('plan.shopping'), text: this.listText(data) });
    } catch {
      // The user dismissed the share sheet.
    }
  }

  readonly haveOptions = [
    { value: '0', label: '0' },
    { value: '0.25', label: '¼' },
    { value: '0.5', label: '½' },
    { value: '0.75', label: '¾' },
    { value: '1', label: '1/1' }
  ];

  // The share of the needed amount already at home, snapped to the nearest option.
  haveFraction(item: ShoppingItem): string {
    const ratio = item.quantity > 0 ? item.have / item.quantity : 0;
    return this.haveOptions.reduce((best, option) =>
      Math.abs(Number(option.value) - ratio) < Math.abs(Number(best.value) - ratio) ? option : best
    ).value;
  }

  setHave(item: ShoppingItem, event: Event): void {
    const fraction = Number((event.target as HTMLSelectElement).value);
    this.plan.setHave(item, item.quantity * fraction);
  }

  remaining(item: ShoppingItem): string {
    const share = remainingShare(item);
    return share <= 0
      ? this.i18n.t('plan.have.enough')
      : this.i18n.t('plan.have.buy', { amount: shoppingAmountText(item, this.i18n.lang(), share) });
  }

  toggle(item: ShoppingItem, event: Event): void {
    this.plan.setChecked(item, (event.target as HTMLInputElement).checked);
  }

  clearPlan(): void {
    if (confirm(this.i18n.t('plan.clear.confirm'))) this.plan.clear();
  }
}
