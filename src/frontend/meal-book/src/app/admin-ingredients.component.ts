import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CatalogIngredient, CatalogService } from './catalog.service';
import { I18nService, TranslationKey } from './i18n.service';

@Component({
  standalone: true,
  imports: [FormsModule],
  template: `
    <section class="page-heading">
      <p class="eyebrow">{{ i18n.t('ingredients.eyebrow') }}</p>
      <h1>{{ i18n.t('ingredients.title') }}</h1>
      <p class="intro">{{ i18n.t('ingredients.intro') }}</p>
    </section>

    <form class="filter-panel" (ngSubmit)="add()">
      <label>{{ i18n.t('ingredients.nameEn') }}
        <input name="nameEn" [(ngModel)]="nameEn" required maxlength="200" />
      </label>
      <label>{{ i18n.t('ingredients.nameDa') }}
        <input name="nameDa" [(ngModel)]="nameDa" required maxlength="200" />
      </label>
      <label>{{ i18n.t('ingredients.section') }}
        <select name="section" [(ngModel)]="section">
          @for (option of catalog.shopSectionOptions(); track option.value) {
            <option [value]="option.value">{{ option.label }}</option>
          }
        </select>
      </label>
      <div class="filter-actions">
        <button class="button" type="submit">{{ i18n.t('ingredients.add') }}</button>
      </div>
    </form>
    @if (error()) { <p class="error-message" role="alert">{{ i18n.t(error()!) }}</p> }

    <section class="library-section">
      <div class="section-heading">
        <h2>{{ i18n.t('ingredients.title') }}</h2>
        <span class="muted">{{ i18n.t('ingredients.count', { n: visible().length }) }}</span>
      </div>
      <input class="ingredient-filter" name="filter" [ngModel]="filter()" (ngModelChange)="filter.set($event)"
        [placeholder]="i18n.t('ingredients.filter')" [attr.aria-label]="i18n.t('ingredients.filter')" />
      <ul class="ingredient-admin-list">
        @for (item of visible(); track item.id) {
          <li>
            @if (editingId() === item.id) {
              <input name="editEn" [(ngModel)]="editEn" [attr.aria-label]="i18n.t('ingredients.nameEn')" />
              <input name="editDa" [(ngModel)]="editDa" [attr.aria-label]="i18n.t('ingredients.nameDa')" />
              <select name="editSection" [(ngModel)]="editSection" [attr.aria-label]="i18n.t('ingredients.section')">
                @for (option of catalog.shopSectionOptions(); track option.value) {
                  <option [value]="option.value">{{ option.label }}</option>
                }
              </select>
              <button class="button small" type="button" (click)="saveEdit(item)">{{ i18n.t('ingredients.save') }}</button>
              <button class="button secondary small" type="button" (click)="editingId.set(null)">{{ i18n.t('common.cancel') }}</button>
            } @else {
              <span class="name">{{ item.nameEn }}</span>
              <span class="muted name">{{ item.nameDa }}</span>
              <span class="section-tag">{{ catalog.sectionLabel(item.shopSectionSlug) }}</span>
              <button class="button secondary small" type="button" (click)="startEdit(item)">{{ i18n.t('detail.edit') }}</button>
              <button class="button danger small" type="button" (click)="remove(item)">{{ i18n.t('detail.delete') }}</button>
            }
          </li>
        }
      </ul>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: `
    .ingredient-filter { margin-bottom: 1rem; }
    .ingredient-admin-list { margin: 0; padding: 0; list-style: none; }
    .ingredient-admin-list li {
      display: grid; grid-template-columns: 1fr 1fr 11rem auto auto; gap: 0.6rem; align-items: center;
      padding: 0.5rem 0; border-bottom: 1px solid var(--line);
    }
    .section-tag { font-size: 0.85rem; color: var(--muted, inherit); }
  `
})
export class AdminIngredientsComponent {
  readonly i18n = inject(I18nService);
  readonly catalog = inject(CatalogService);

  readonly error = signal<TranslationKey | null>(null);
  readonly filter = signal('');
  readonly editingId = signal<string | null>(null);
  nameEn = '';
  nameDa = '';
  section = 'dry_goods';
  editEn = '';
  editDa = '';
  editSection = 'dry_goods';

  readonly visible = computed(() => {
    const query = this.filter().trim().toLocaleLowerCase();
    return [...this.catalog.ingredients()]
      .filter(item => !query || item.nameEn.toLocaleLowerCase().includes(query) || item.nameDa.toLocaleLowerCase().includes(query))
      .sort((a, b) => a.nameEn.localeCompare(b.nameEn));
  });

  constructor() {
    this.catalog.load();
  }

  add(): void {
    this.error.set(null);
    this.catalog.createIngredient(this.nameEn.trim(), this.nameDa.trim(), this.section).subscribe({
      next: () => {
        this.nameEn = '';
        this.nameDa = '';
      },
      error: (e: HttpErrorResponse) => this.fail(e)
    });
  }

  startEdit(item: CatalogIngredient): void {
    this.editingId.set(item.id);
    this.editEn = item.nameEn;
    this.editDa = item.nameDa;
    this.editSection = item.shopSectionSlug;
  }

  saveEdit(item: CatalogIngredient): void {
    this.error.set(null);
    this.catalog.updateIngredient(item.id, this.editEn.trim(), this.editDa.trim(), this.editSection).subscribe({
      next: () => this.editingId.set(null),
      error: (e: HttpErrorResponse) => this.fail(e)
    });
  }

  remove(item: CatalogIngredient): void {
    if (!confirm(this.i18n.t('ingredients.confirmDelete'))) return;
    this.error.set(null);
    this.catalog.deleteIngredient(item.id).subscribe({ error: (e: HttpErrorResponse) => this.fail(e, 'ingredients.inUse') });
  }

  private fail(e: HttpErrorResponse, conflict: TranslationKey = 'ingredients.exists'): void {
    this.error.set(e.status === 409 ? conflict : 'ingredients.failed');
  }
}
