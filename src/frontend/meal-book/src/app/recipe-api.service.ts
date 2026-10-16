import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { I18nService } from './i18n.service';

export interface RecipeIngredient {
  ingredientId?: string | null;
  name: string;
  quantity: number;
  unit: string;
  section?: string | null;
  notes?: string | null;
}

export interface Recipe {
  id: string;
  name: string;
  description?: string | null;
  servings: number;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  mealTypes: string[];
  courses: string[];
  cookingMethods: string[];
  cuisines: string[];
  caloriesPerServing?: number | null;
  proteinGramsPerServing?: number | null;
  carbohydratesGramsPerServing?: number | null;
  fatGramsPerServing?: number | null;
  addedAt: string;
  isPublished: boolean;
  sourceUrl?: string | null;
  cookCount: number;
  recentCookCount: number;
  averageRating?: number | null;
  tags: string[];
  ingredients: RecipeIngredient[];
  steps: string[];
  equipment: string[];
  language: string;
  displayLanguage: string;
  isTranslated: boolean;
  imageUrl?: string | null;
}

export interface RecipeSearch {
  search?: string;
  maxTotalMinutes?: number | null;
  minTotalMinutes?: number | null;
  mealTypes?: string[];
  courses?: string[];
  cookingMethods?: string[];
  cuisines?: string[];
  ingredients?: string[];
  tags?: string[];
  maxCalories?: number | null;
  minRating?: number | null;
  sort?: string;
  lang?: string;
}

export interface RecipeSuggestion {
  id: string;
  name: string;
}

export interface RecipeDashboard {
  recentlyAdded: Recipe[];
  mostCooked: Recipe[];
  recentlyCooked: { email: string; recipe: Recipe; cookedAt: string }[];
}

export interface CookLog {
  id: string;
  recipeId: string;
  recipeName: string;
  cookedAt: string;
  rating?: number | null;
  notes?: string | null;
}

export interface RecipeWrite {
  language: string;
  name: string;
  description: string;
  servings: number;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  mealTypes: string[];
  courses: string[];
  cookingMethods: string[];
  cuisines: string[];
  caloriesPerServing: number | null;
  proteinGramsPerServing: number | null;
  carbohydratesGramsPerServing: number | null;
  fatGramsPerServing: number | null;
  isPublished: boolean;
  sourceUrl: string;
  tags: string[];
  ingredients: RecipeIngredient[];
  steps: string[];
  equipment: string[];
}

export const apiUrl = new URL('api/', document.baseURI).toString();

@Injectable({ providedIn: 'root' })
export class RecipeApiService {
  private readonly http = inject(HttpClient);

  private readonly i18n = inject(I18nService);

  getRecipes(search: RecipeSearch = {}): Observable<Recipe[]> {
    return this.http.get<Recipe[]>(`${apiUrl}recipes`, {
      params: this.toParams({ ...search, lang: this.i18n.lang() })
    });
  }

  getSuggestions(query: string): Observable<RecipeSuggestion[]> {
    return this.http.get<RecipeSuggestion[]>(`${apiUrl}recipes/suggestions`, {
      params: { q: query, lang: this.i18n.lang() }
    });
  }

  // original = true skips translation, so the editor always works on the source text.
  getRecipe(id: string, original = false): Observable<Recipe> {
    return this.http.get<Recipe>(`${apiUrl}recipes/${id}`, {
      params: original ? {} : { lang: this.i18n.lang() }
    });
  }

  getDrafts(): Observable<Recipe[]> {
    return this.http.get<Recipe[]>(`${apiUrl}recipes/admin/drafts`, {
      params: { lang: this.i18n.lang() }
    });
  }

  getDashboard(): Observable<RecipeDashboard> {
    return this.http.get<RecipeDashboard>(`${apiUrl}recipes/dashboard`, {
      params: { lang: this.i18n.lang() }
    });
  }

  logCooked(id: string, rating: number | null, notes: string): Observable<CookLog> {
    return this.http.post<CookLog>(`${apiUrl}recipes/${id}/cooked`, { rating, notes });
  }

  getCookHistory(): Observable<CookLog[]> {
    return this.http.get<CookLog[]>(`${apiUrl}recipes/cooking-history`);
  }

  createRecipe(recipe: RecipeWrite): Observable<Recipe> {
    return this.http.post<Recipe>(`${apiUrl}recipes`, recipe);
  }

  updateRecipe(id: string, recipe: RecipeWrite): Observable<Recipe> {
    return this.http.put<Recipe>(`${apiUrl}recipes/${id}`, recipe);
  }

  publishRecipe(id: string): Observable<Recipe> {
    return this.http.post<Recipe>(`${apiUrl}recipes/${id}/publish`, {});
  }

  deleteRecipe(id: string): Observable<void> {
    return this.http.delete<void>(`${apiUrl}recipes/${id}`);
  }

  uploadImage(id: string, file: Blob): Observable<void> {
    const form = new FormData();
    form.append('file', file, 'cover.jpg');
    return this.http.post<void>(`${apiUrl}recipes/${id}/image`, form);
  }

  deleteImage(id: string): Observable<void> {
    return this.http.delete<void>(`${apiUrl}recipes/${id}/image`);
  }

  private toParams(search: RecipeSearch): HttpParams {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(search)) {
      if (value === undefined || value === null || value === '') continue;
      if (Array.isArray(value)) {
        for (const item of value) params = params.append(key, String(item));
      } else {
        params = params.set(key, String(value));
      }
    }
    return params;
  }
}
