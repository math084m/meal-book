import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { RecipeApiService, RecipeDashboard } from './recipe-api.service';

@Component({
  standalone: true,
  imports: [RouterLink, DatePipe, DecimalPipe],
  template: `
    <section class="hero">
      <p class="eyebrow">YOUR PERSONAL RECIPE LIBRARY</p>
      <h1>Good food starts<br />with a good note.</h1>
      <p class="intro">
        Save the recipes you love, discover what to cook next, and keep every
        ingredient and instruction close at hand.
      </p>
      <a class="button" routerLink="/recipes">Explore recipes <span aria-hidden="true">→</span></a>
    </section>

    @if (error()) {
      <div class="state-card" role="alert">
        <h2>Recipe library unavailable</h2>
        <p>Could not load recipes. Check API and database, then refresh.</p>
      </div>
    } @else {
      @if (dashboard(); as data) {
      <section class="dashboard-section">
        <div class="section-heading">
          <div><p class="eyebrow">FRESH FROM THE KITCHEN</p><h2>Recently cooked</h2></div>
        </div>
        @if (data.recentlyCooked.length) {
          <div class="recipe-grid compact">
            @for (item of data.recentlyCooked; track item.cookedAt + item.recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', item.recipe.id]">
                <p class="eyebrow">RECENTLY COOKED</p>
                <h3>{{ item.recipe.name }}</h3>
                <p class="recipe-meta">{{ item.cookedAt | date:'mediumDate' }}</p>
              </a>
            }
          </div>
        } @else {
          <p class="muted">Your community’s cooking history will show up here.</p>
        }
      </section>

      <section class="dashboard-section">
        <div class="section-heading">
          <div><p class="eyebrow">NEW IN THE COLLECTION</p><h2>Recently added</h2></div>
          <a routerLink="/recipes">Browse all</a>
        </div>
        @if (data.recentlyAdded.length) {
          <div class="recipe-grid">
            @for (recipe of data.recentlyAdded; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <p class="eyebrow">{{ recipe.cuisine || 'RECIPE' }} · {{ recipe.course || 'ANY COURSE' }}</p>
                <h3>{{ recipe.name }}</h3>
                <p class="description">{{ recipe.description || 'A new recipe to add to your kitchen.' }}</p>
                <p class="recipe-meta">{{ recipe.prepTimeMinutes + recipe.cookTimeMinutes }} min · {{ recipe.servings }} servings</p>
              </a>
            }
          </div>
        } @else {
          <p class="muted">No recipes added yet.</p>
        }
      </section>

      <section class="dashboard-section">
        <div class="section-heading">
          <div><p class="eyebrow">HOUSE FAVORITES</p><h2>Most cooked</h2></div>
        </div>
        @if (data.mostCooked.length) {
          <div class="recipe-grid">
            @for (recipe of data.mostCooked; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <p class="eyebrow">{{ recipe.cookCount }} COOKS@if (recipe.averageRating) { · ★ {{ recipe.averageRating | number:'1.1-1' }} }</p>
                <h3>{{ recipe.name }}</h3>
                <p class="recipe-meta">{{ recipe.mealType || 'Any meal' }} · {{ recipe.cuisine || 'Cuisine not set' }}</p>
              </a>
            }
          </div>
        } @else {
          <p class="muted">Cook and rate recipes to build community favorites.</p>
        }
      </section>
      } @else {
        <div class="state-card" role="status">Loading recipe library…</div>
      }
    }
  `
})
export class HomeComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  readonly dashboard = signal<RecipeDashboard | null>(null);
  readonly error = signal(false);

  ngOnInit(): void {
    this.api.getDashboard().subscribe({
      next: value => this.dashboard.set(value),
      error: () => this.error.set(true)
    });
  }
}
