import { Component, OnInit, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Recipe, RecipeApiService } from './recipe-api.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, DecimalPipe],
  template: `
    <section class="page-heading">
      <p class="eyebrow">THE COLLECTION</p>
      <h1>Find your next favorite.</h1>
      <p class="intro">Search by ingredients, time, cuisine, course, cooking method, nutrition, and tags.</p>
    </section>
    <form class="filter-panel" (ngSubmit)="searchRecipes()">
      <label class="wide">Search recipes
        <input name="search" [(ngModel)]="filters.search" placeholder="Recipe name or description" />
      </label>
      <label>Ingredient
        <input name="ingredient" [(ngModel)]="filters.ingredient" placeholder="e.g. tomato" />
      </label>
      <label>Tags
        <input name="tags" [(ngModel)]="filters.tags" placeholder="quick, vegetarian" />
      </label>
      <label>Meal type
        <input name="mealType" [(ngModel)]="filters.mealType" placeholder="Dinner" />
      </label>
      <label>Course
        <input name="course" [(ngModel)]="filters.course" placeholder="Main" />
      </label>
      <label>Cooking method
        <input name="cookingMethod" [(ngModel)]="filters.cookingMethod" placeholder="Oven" />
      </label>
      <label>Cuisine / region
        <input name="cuisine" [(ngModel)]="filters.cuisine" placeholder="Italian" />
      </label>
      <label>Ready within (minutes)
        <input name="maxTotalMinutes" type="number" min="0" [(ngModel)]="maxTotalMinutes" />
      </label>
      <label>Max calories / serving
        <input name="maxCalories" type="number" min="0" [(ngModel)]="maxCalories" />
      </label>
      <label>Minimum rating
        <select name="minRating" [(ngModel)]="minRating">
          <option value="">Any rating</option>
          <option value="3">3+ stars</option>
          <option value="4">4+ stars</option>
          <option value="4.5">4.5+ stars</option>
        </select>
      </label>
      <label>Sort by
        <select name="sort" [(ngModel)]="filters.sort">
          <option value="newest">Recently added</option>
          <option value="popular">Most cooked</option>
          <option value="trending">Trending (last 30 days)</option>
          <option value="rating">Top rated</option>
          <option value="quickest">Quickest</option>
          <option value="name">Name</option>
        </select>
      </label>
      <div class="filter-actions">
        <button class="button" type="submit">Apply filters</button>
        <button class="button secondary" type="button" (click)="clearFilters()">Clear all</button>
      </div>
    </form>

    <section class="library-section">
      <div class="section-heading">
        <h2>Recipes</h2>
        @if (recipes(); as recipeList) { <span class="muted">{{ recipeList.length }} found</span> }
      </div>
      @if (error()) {
        <div class="state-card" role="alert">Could not load recipes. Please try again.</div>
      } @else {
        @if (recipes(); as recipeList) {
        @if (recipeList.length) {
          <div class="recipe-grid">
            @for (recipe of recipeList; track recipe.id) {
              <a class="recipe-card" [routerLink]="['/recipes', recipe.id]">
                <p class="eyebrow">{{ recipe.cuisine || 'RECIPE' }} · {{ recipe.course || 'ANY COURSE' }}</p>
                <h3>{{ recipe.name }}</h3>
                <p class="description">{{ recipe.description || 'A recipe worth keeping.' }}</p>
                <p class="recipe-meta">
                  {{ recipe.prepTimeMinutes + recipe.cookTimeMinutes }} min · {{ recipe.servings }} servings ·
                  {{ recipe.ingredients.length }} ingredients
                </p>
                <div class="tag-list">
                  @for (tag of recipe.tags; track tag) { <span class="tag">{{ tag }}</span> }
                </div>
                @if (recipe.averageRating) {
                  <p class="rating">★ {{ recipe.averageRating | number:'1.1-1' }} <span>({{ recipe.cookCount }} cooks)</span></p>
                }
              </a>
            }
          </div>
        } @else {
          <div class="state-card">No recipes match these filters. Try broadening your search.</div>
        }
      } @else {
        <div class="state-card" role="status">Loading recipes…</div>
      }
      }
    </section>
  `
})
export class RecipeListComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  readonly recipes = signal<Recipe[] | null>(null);
  readonly error = signal(false);
  maxTotalMinutes: number | null = null;
  maxCalories: number | null = null;
  minRating: number | '' = '';
  filters = {
    search: '',
    ingredient: '',
    tags: '',
    mealType: '',
    course: '',
    cookingMethod: '',
    cuisine: '',
    sort: 'newest'
  };

  ngOnInit(): void {
    this.searchRecipes();
  }

  searchRecipes(): void {
    this.error.set(false);
    this.recipes.set(null);
    this.api.getRecipes({
      ...this.filters,
      maxTotalMinutes: this.maxTotalMinutes,
      maxCalories: this.maxCalories,
      minRating: this.minRating === '' ? null : Number(this.minRating)
    }).subscribe({
      next: value => this.recipes.set(value),
      error: () => this.error.set(true)
    });
  }

  clearFilters(): void {
    this.filters = {
      search: '',
      ingredient: '',
      tags: '',
      mealType: '',
      course: '',
      cookingMethod: '',
      cuisine: '',
      sort: 'newest'
    };
    this.maxTotalMinutes = null;
    this.maxCalories = null;
    this.minRating = '';
    this.searchRecipes();
  }
}
