import { HttpClient } from '@angular/common/http';
import { effect, inject, Injectable, signal, untracked } from '@angular/core';
import { AuthService } from './auth.service';

const apiUrl = new URL('api/', document.baseURI).toString();

@Injectable({ providedIn: 'root' })
export class FavoriteService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  readonly ids = signal<ReadonlySet<string>>(new Set());

  constructor() {
    effect(() => {
      const user = this.auth.currentUser();
      untracked(() => (user ? this.load() : this.ids.set(new Set())));
    });
  }

  isFavorite(recipeId: string): boolean {
    return this.ids().has(recipeId);
  }

  toggle(recipeId: string): void {
    const adding = !this.isFavorite(recipeId);
    this.setLocal(recipeId, adding);
    const request = adding
      ? this.http.put(`${apiUrl}favorites/${recipeId}`, null)
      : this.http.delete(`${apiUrl}favorites/${recipeId}`);
    request.subscribe({ error: () => this.load() });
  }

  private setLocal(recipeId: string, present: boolean): void {
    const next = new Set(this.ids());
    if (present) next.add(recipeId);
    else next.delete(recipeId);
    this.ids.set(next);
  }

  private load(): void {
    this.http.get<string[]>(`${apiUrl}favorites`).subscribe({
      next: ids => this.ids.set(new Set(ids)),
      error: () => undefined
    });
  }
}
