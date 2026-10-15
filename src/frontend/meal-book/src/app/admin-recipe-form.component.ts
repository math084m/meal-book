import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RecipeApiService, RecipeIngredient, RecipeWrite } from './recipe-api.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <a class="back-link" routerLink="/recipes">← All recipes</a>
    <section class="page-heading">
      <p class="eyebrow">ADMIN RECIPE EDITOR</p>
      <h1>{{ recipeId ? 'Edit recipe' : 'Add a recipe' }}</h1>
      <p class="intro">Keep ingredients, method, metadata, and equipment together.</p>
    </section>
    <form class="editor-form" (ngSubmit)="save()">
      <section class="form-section">
        <h2>Recipe details</h2>
        <div class="form-grid">
          <label class="wide">Recipe name <input name="name" [(ngModel)]="model.name" required maxlength="200" /></label>
          <label class="wide">Description <textarea name="description" [(ngModel)]="model.description" rows="3" maxlength="2000"></textarea></label>
          <label>Servings <input name="servings" type="number" min="1" max="100" [(ngModel)]="model.servings" required /></label>
          <label>Prep time (minutes) <input name="prepTime" type="number" min="0" [(ngModel)]="model.prepTimeMinutes" /></label>
          <label>Cook time (minutes) <input name="cookTime" type="number" min="0" [(ngModel)]="model.cookTimeMinutes" /></label>
          <label>Meal type <input name="mealType" [(ngModel)]="model.mealType" placeholder="Dinner" /></label>
          <label>Course <input name="course" [(ngModel)]="model.course" placeholder="Main" /></label>
          <label>Cooking method <input name="method" [(ngModel)]="model.cookingMethod" placeholder="Oven baked" /></label>
          <label>Cuisine / region <input name="cuisine" [(ngModel)]="model.cuisine" placeholder="Danish" /></label>
          <label>Calories per serving <input name="calories" type="number" min="0" [(ngModel)]="model.caloriesPerServing" /></label>
          <label>Protein (g) <input name="protein" type="number" min="0" [(ngModel)]="model.proteinGramsPerServing" /></label>
          <label>Carbohydrates (g) <input name="carbs" type="number" min="0" [(ngModel)]="model.carbohydratesGramsPerServing" /></label>
          <label>Fat (g) <input name="fat" type="number" min="0" [(ngModel)]="model.fatGramsPerServing" /></label>
          <label class="wide">Tags <input name="tags" [(ngModel)]="tagText" placeholder="quick, vegetarian, family favorite" /></label>
        </div>
      </section>

      <section class="form-section">
        <div class="form-section-heading"><h2>Ingredients</h2><button class="button secondary small" type="button" (click)="addIngredient()">Add ingredient</button></div>
        @for (ingredient of model.ingredients; track $index) {
          <div class="ingredient-editor">
            <label>Ingredient <input [name]="'ingredientName' + $index" [(ngModel)]="ingredient.name" required /></label>
            <label>Quantity <input [name]="'ingredientQty' + $index" type="number" min="0.001" step="0.001" [(ngModel)]="ingredient.quantity" required /></label>
            <label>Unit <input [name]="'ingredientUnit' + $index" [(ngModel)]="ingredient.unit" required /></label>
            <label>Notes <input [name]="'ingredientNotes' + $index" [(ngModel)]="ingredient.notes" placeholder="finely chopped" /></label>
            <button class="remove-button" type="button" (click)="removeAt(model.ingredients, $index)" aria-label="Remove ingredient">Remove</button>
          </div>
        }
      </section>

      <section class="form-section">
        <div class="form-section-heading"><h2>Step-by-step method</h2><button class="button secondary small" type="button" (click)="model.steps.push('')">Add step</button></div>
        @for (step of model.steps; track $index) {
          <div class="step-editor">
            <span class="step-number">{{ $index + 1 }}</span>
            <textarea [name]="'step' + $index" [(ngModel)]="model.steps[$index]" rows="2" required></textarea>
            <button class="remove-button" type="button" (click)="removeAt(model.steps, $index)" aria-label="Remove step">Remove</button>
          </div>
        }
      </section>

      <section class="form-section">
        <div class="form-section-heading"><h2>Equipment</h2><button class="button secondary small" type="button" (click)="model.equipment.push('')">Add equipment</button></div>
        @for (item of model.equipment; track $index) {
          <div class="equipment-editor">
            <input [name]="'equipment' + $index" [(ngModel)]="model.equipment[$index]" placeholder="Saucepan" />
            <button class="remove-button" type="button" (click)="removeAt(model.equipment, $index)" aria-label="Remove equipment">Remove</button>
          </div>
        }
      </section>
      @if (error()) { <p class="error-message" role="alert">{{ error() }}</p> }
      <div class="form-actions">
        <a class="button secondary" routerLink="/recipes">Cancel</a>
        <button class="button" type="submit" [disabled]="saving()">{{ saving() ? 'Saving…' : 'Save recipe' }}</button>
      </div>
    </form>
  `
})
export class AdminRecipeFormComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly recipeId = this.route.snapshot.paramMap.get('id');
  tagText = '';
  model: RecipeWrite = {
    name: '',
    description: '',
    servings: 4,
    prepTimeMinutes: 0,
    cookTimeMinutes: 0,
    mealType: '',
    course: '',
    cookingMethod: '',
    cuisine: '',
    caloriesPerServing: null,
    proteinGramsPerServing: null,
    carbohydratesGramsPerServing: null,
    fatGramsPerServing: null,
    tags: [],
    ingredients: [{ name: '', quantity: 1, unit: '', notes: '' }],
    steps: [''],
    equipment: []
  };

  ngOnInit(): void {
    if (!this.recipeId) return;
    this.api.getRecipe(this.recipeId).subscribe({
      next: recipe => {
        this.model = {
          name: recipe.name,
          description: recipe.description ?? '',
          servings: recipe.servings,
          prepTimeMinutes: recipe.prepTimeMinutes,
          cookTimeMinutes: recipe.cookTimeMinutes,
          mealType: recipe.mealType,
          course: recipe.course,
          cookingMethod: recipe.cookingMethod,
          cuisine: recipe.cuisine,
          caloriesPerServing: recipe.caloriesPerServing ?? null,
          proteinGramsPerServing: recipe.proteinGramsPerServing ?? null,
          carbohydratesGramsPerServing: recipe.carbohydratesGramsPerServing ?? null,
          fatGramsPerServing: recipe.fatGramsPerServing ?? null,
          tags: recipe.tags,
          ingredients: recipe.ingredients.map(item => ({ ...item })),
          steps: [...recipe.steps],
          equipment: [...recipe.equipment]
        };
        this.tagText = recipe.tags.join(', ');
      },
      error: () => this.error.set('Could not load recipe for editing.')
    });
  }

  addIngredient(): void {
    this.model.ingredients.push({ name: '', quantity: 1, unit: '', notes: '' });
  }

  removeAt(items: string[] | RecipeIngredient[], index: number): void {
    items.splice(index, 1);
    if (items === this.model.ingredients && items.length === 0) {
      this.addIngredient();
    } else if (items === this.model.steps && items.length === 0) {
      this.model.steps.push('');
    }
  }

  save(): void {
    this.error.set('');
    this.saving.set(true);
    this.model.tags = this.tagText.split(',').map(tag => tag.trim()).filter(Boolean);
    const request = this.recipeId
      ? this.api.updateRecipe(this.recipeId, this.model)
      : this.api.createRecipe(this.model);
    request.subscribe({
      next: recipe => {
        this.saving.set(false);
        void this.router.navigate(['/recipes', recipe.id]);
      },
      error: () => {
        this.saving.set(false);
        this.error.set('Could not save recipe. Check required fields and try again.');
      }
    });
  }
}
