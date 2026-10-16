import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { I18nService } from './i18n.service';

export type TermKind = 'mealType' | 'course' | 'cookingMethod' | 'cuisine' | 'tag';

export interface Term {
  kind: TermKind;
  slug: string;
  nameEn: string;
  nameDa: string;
}

export interface CatalogIngredient {
  id: string;
  nameEn: string;
  nameDa: string;
  shopSectionSlug: string;
}

export interface ShopSection {
  slug: string;
  nameEn: string;
  nameDa: string;
  position: number;
  perishable: boolean;
}

export interface SelectOption {
  value: string;
  label: string;
}

const apiUrl = new URL('api/', document.baseURI).toString();

@Injectable({ providedIn: 'root' })
export class CatalogService {
  private readonly http = inject(HttpClient);
  private readonly i18n = inject(I18nService);
  private loaded = false;

  readonly terms = signal<Term[]>([]);
  readonly ingredients = signal<CatalogIngredient[]>([]);
  readonly shopSections = signal<ShopSection[]>([]);

  readonly shopSectionOptions = computed<SelectOption[]>(() =>
    this.shopSections().map(section => ({ value: section.slug, label: this.name(section) }))
  );
  readonly ingredientOptions = computed<SelectOption[]>(() =>
    this.ingredients()
      .map(item => ({ value: item.id, label: this.name(item) }))
      .sort((a, b) => a.label.localeCompare(b.label, this.i18n.lang()))
  );

  load(): void {
    if (this.loaded) return;
    this.loaded = true;
    this.http.get<Term[]>(`${apiUrl}terms`).subscribe({
      next: terms => this.terms.set(terms),
      error: () => (this.loaded = false)
    });
    this.refreshIngredients();
    this.http.get<ShopSection[]>(`${apiUrl}shop-sections`).subscribe({
      next: sections => this.shopSections.set(sections)
    });
  }

  refreshIngredients(): void {
    this.http.get<CatalogIngredient[]>(`${apiUrl}ingredients`).subscribe({
      next: items => this.ingredients.set(items)
    });
  }

  options(kind: TermKind): SelectOption[] {
    return this.terms()
      .filter(term => term.kind === kind)
      .map(term => ({ value: term.slug, label: this.name(term) }));
  }

  label(kind: TermKind, slug: string): string {
    const term = this.terms().find(item => item.kind === kind && item.slug === slug);
    return term ? this.name(term) : slug;
  }

  labels(kind: TermKind, slugs: readonly string[]): string[] {
    return slugs.map(slug => this.label(kind, slug));
  }

  sectionLabel(slug: string): string {
    const section = this.shopSections().find(item => item.slug === slug);
    return section ? this.name(section) : slug;
  }

  createIngredient(nameEn: string, nameDa: string, shopSectionSlug: string): Observable<CatalogIngredient> {
    return this.http
      .post<CatalogIngredient>(`${apiUrl}ingredients`, { nameEn, nameDa, shopSectionSlug })
      .pipe(tap(() => this.refreshIngredients()));
  }

  updateIngredient(id: string, nameEn: string, nameDa: string, shopSectionSlug: string): Observable<CatalogIngredient> {
    return this.http
      .put<CatalogIngredient>(`${apiUrl}ingredients/${id}`, { nameEn, nameDa, shopSectionSlug })
      .pipe(tap(() => this.refreshIngredients()));
  }

  deleteIngredient(id: string): Observable<void> {
    return this.http.delete<void>(`${apiUrl}ingredients/${id}`).pipe(tap(() => this.refreshIngredients()));
  }

  private name(item: { nameEn: string; nameDa: string }): string {
    return this.i18n.lang() === 'da' && item.nameDa ? item.nameDa : item.nameEn;
  }
}
