import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiUrl } from './recipe-api.service';

export interface PantryItem {
  ingredientId: string;
  haveFraction: number | null;
}

export interface Pantry {
  useForShopping: boolean;
  items: PantryItem[];
}

@Injectable({ providedIn: 'root' })
export class PantryService {
  private readonly http = inject(HttpClient);

  get(): Observable<Pantry> {
    return this.http.get<Pantry>(`${apiUrl}pantry`);
  }

  setUseForShopping(useForShopping: boolean): Observable<void> {
    return this.http.put<void>(`${apiUrl}pantry/settings`, { useForShopping });
  }

  add(ingredientId: string): Observable<void> {
    return this.http.put<void>(`${apiUrl}pantry/ingredients/${ingredientId}`, {});
  }

  remove(ingredientId: string): Observable<void> {
    return this.http.delete<void>(`${apiUrl}pantry/ingredients/${ingredientId}`);
  }

  setHaveFraction(ingredientId: string, haveFraction: number | null): Observable<void> {
    return this.http.put<void>(`${apiUrl}pantry/ingredients/${ingredientId}/shopping`, { haveFraction });
  }
}
