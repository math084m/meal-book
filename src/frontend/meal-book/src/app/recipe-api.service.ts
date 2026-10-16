import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface RecipeIngredient {
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
  mealType: string;
  course: string;
  cookingMethod: string;
  cuisine: string;
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
}

export interface RecipeSearch {
  search?: string;
  maxTotalMinutes?: number | null;
  mealType?: string;
  course?: string;
  cookingMethod?: string;
  cuisine?: string;
  ingredient?: string;
  tags?: string;
  maxCalories?: number | null;
  minRating?: number | null;
  sort?: string;
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
  name: string;
  description: string;
  servings: number;
  prepTimeMinutes: number;
  cookTimeMinutes: number;
  mealType: string;
  course: string;
  cookingMethod: string;
  cuisine: string;
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

const apiUrl = new URL('api/', document.baseURI).toString();

@Injectable({ providedIn: 'root' })
export class RecipeApiService {
  private readonly http = inject(HttpClient);

  getRecipes(search: RecipeSearch = {}): Observable<Recipe[]> {
    return this.http.get<Recipe[]>(`${apiUrl}recipes`, { params: this.toParams(search) });
  }

  getRecipe(id: string): Observable<Recipe> {
    return this.http.get<Recipe>(`${apiUrl}recipes/${id}`);
  }

  getDrafts(): Observable<Recipe[]> {
    return this.http.get<Recipe[]>(`${apiUrl}recipes/admin/drafts`);
  }

  getDashboard(): Observable<RecipeDashboard> {
    return this.http.get<RecipeDashboard>(`${apiUrl}recipes/dashboard`);
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

  private toParams(search: RecipeSearch): Record<string, string> {
    return Object.fromEntries(
      Object.entries(search)
        .filter((entry): entry is [string, string | number] =>
          entry[1] !== undefined && entry[1] !== null && entry[1] !== '')
        .map(([key, value]) => [key, String(value)])
    );
  }
}
