import { HttpClient } from '@angular/common/http';
import { computed, effect, inject, Injectable, signal, untracked } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { I18nService } from './i18n.service';

export interface PlanEntry {
  recipeId: string;
  recipeName: string;
  imageUrl: string | null;
  servings: number;
  recipeServings: number;
}

export interface ShoppingItem {
  ingredientId: string | null;
  name: string;
  quantity: number;
  unit: string;
  checked: boolean;
  recipes: string[];
}

export interface ShoppingSection {
  slug: string;
  name: string;
  items: ShoppingItem[];
}

export interface MealPlan {
  entries: PlanEntry[];
  sections: ShoppingSection[];
}

export interface PlanSuggestion {
  recipeId: string;
  name: string;
  imageUrl: string | null;
  sharedIngredients: string[];
  newPerishables: number;
  score: number;
}

const apiUrl = new URL('api/', document.baseURI).toString();

@Injectable({ providedIn: 'root' })
export class PlanService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly i18n = inject(I18nService);
  private connection: signalR.HubConnection | null = null;
  private refreshing = false;
  private refreshQueued = false;

  readonly plan = signal<MealPlan | null>(null);
  readonly suggestions = signal<PlanSuggestion[]>([]);
  readonly error = signal(false);
  readonly live = signal(false);

  readonly plannedIds = computed(() => new Set(this.plan()?.entries.map(entry => entry.recipeId) ?? []));
  readonly count = computed(() => this.plan()?.entries.length ?? 0);

  constructor() {
    effect(() => {
      const user = this.auth.currentUser();
      untracked(() => (user ? this.start() : this.stop()));
    });
    effect(() => {
      this.i18n.lang();
      untracked(() => {
        if (this.auth.currentUser()) this.refresh();
      });
    });
  }

  isPlanned(recipeId: string): boolean {
    return this.plannedIds().has(recipeId);
  }

  add(recipeId: string, servings?: number): void {
    this.http.put(`${apiUrl}meal-plan/recipes/${recipeId}`, servings ? { servings } : {}).subscribe({
      next: () => this.refresh()
    });
  }

  remove(recipeId: string): void {
    this.http.delete(`${apiUrl}meal-plan/recipes/${recipeId}`).subscribe({ next: () => this.refresh() });
  }

  setServings(recipeId: string, servings: number): void {
    const value = Math.min(100, Math.max(1, Math.round(servings)));
    this.plan.update(plan =>
      plan ? { ...plan, entries: plan.entries.map(e => (e.recipeId === recipeId ? { ...e, servings: value } : e)) } : plan
    );
    this.http.put(`${apiUrl}meal-plan/recipes/${recipeId}/servings`, { servings: value }).subscribe({
      next: () => this.refresh(),
      error: () => this.refresh()
    });
  }

  clear(): void {
    this.http.delete(`${apiUrl}meal-plan`).subscribe({ next: () => this.refresh() });
  }

  setChecked(item: ShoppingItem, checked: boolean): void {
    if (!item.ingredientId) return;
    this.plan.update(plan =>
      plan
        ? {
            ...plan,
            sections: plan.sections.map(section => ({
              ...section,
              items: section.items.map(i =>
                i.ingredientId === item.ingredientId && i.unit === item.unit ? { ...i, checked } : i
              )
            }))
          }
        : plan
    );
    this.http
      .put(`${apiUrl}meal-plan/checks`, { ingredientId: item.ingredientId, unit: item.unit, checked })
      .subscribe({ next: () => this.refresh(), error: () => this.refresh() });
  }

  uncheckAll(): void {
    this.http.delete(`${apiUrl}meal-plan/checks`).subscribe({ next: () => this.refresh() });
  }

  // Requests that arrive while a refresh is running are merged into one follow-up refresh.
  refresh(): void {
    if (this.refreshing) {
      this.refreshQueued = true;
      return;
    }

    this.refreshing = true;
    const lang = this.i18n.lang();
    this.http.get<MealPlan>(`${apiUrl}meal-plan`, { params: { lang } }).subscribe({
      next: plan => {
        this.plan.set(plan);
        this.error.set(false);
        this.loadSuggestions(lang);
      },
      error: () => {
        this.error.set(true);
        this.finishRefresh();
      }
    });
  }

  private loadSuggestions(lang: string): void {
    this.http.get<PlanSuggestion[]>(`${apiUrl}meal-plan/suggestions`, { params: { lang } }).subscribe({
      next: items => {
        this.suggestions.set(items);
        this.finishRefresh();
      },
      error: () => this.finishRefresh()
    });
  }

  private finishRefresh(): void {
    this.refreshing = false;
    if (this.refreshQueued) {
      this.refreshQueued = false;
      this.refresh();
    }
  }

  private start(): void {
    this.refresh();
    if (this.connection) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiUrl}hubs/mealplan`, { accessTokenFactory: () => this.auth.token() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
    connection.on('planChanged', () => this.refresh());
    connection.onreconnecting(() => this.live.set(false));
    connection.onreconnected(() => {
      this.live.set(true);
      this.refresh();
    });
    connection.onclose(() => this.live.set(false));
    this.connection = connection;
    connection
      .start()
      .then(() => {
        this.live.set(true);
        this.refresh();
      })
      .catch(() => this.live.set(false));
  }

  private stop(): void {
    const connection = this.connection;
    this.connection = null;
    this.live.set(false);
    this.plan.set(null);
    this.suggestions.set([]);
    void connection?.stop();
  }
}
