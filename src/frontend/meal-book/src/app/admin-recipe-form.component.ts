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
          <label class="wide">Source URL <input name="sourceUrl" type="url" [(ngModel)]="model.sourceUrl" maxlength="2048" placeholder="https://example.com/recipe" /></label>
          <label class="publish-toggle">
            <input name="isPublished" type="checkbox" [(ngModel)]="model.isPublished" />
            Publish this recipe to public library
          </label>
        </div>
      </section>

      <section class="form-section">
        <div class="form-section-heading">
          <h2>Ingredients</h2>
          <button class="button secondary small" type="button" (click)="addSection()">Add subsection</button>
        </div>
        @for (section of ingredientSections; track $index) {
          <div class="ingredient-subsection">
            <div class="ingredient-subsection-heading">
              <label>Subsection name <input [name]="'ingredientSection' + $index" [(ngModel)]="section.name" required placeholder="e.g. Ragù" /></label>
              <button class="remove-button" type="button" (click)="removeSection($index)" [disabled]="ingredientSections.length === 1" aria-label="Remove subsection">Remove subsection</button>
            </div>
            @for (ingredient of section.ingredients; track $index) {
              <div class="ingredient-editor">
                <label>Ingredient <input [name]="'ingredientName' + ingredientIndex(section, ingredient)" [(ngModel)]="ingredient.name" required /></label>
                <label>Quantity <input [name]="'ingredientQty' + ingredientIndex(section, ingredient)" type="number" min="0.001" step="0.001" [(ngModel)]="ingredient.quantity" required /></label>
                <div class="ingredient-unit">
                  <label>Unit
                    <select [name]="'ingredientUnit' + ingredientIndex(section, ingredient)" [ngModel]="unitChoice(ingredient.unit)" (ngModelChange)="setUnit(ingredient, $event)" required>
                      <option value="">Select unit</option>
                      @for (unit of units; track unit) { <option [value]="unit">{{ unit }}</option> }
                      <option [value]="otherUnit">Other</option>
                    </select>
                  </label>
                  @if (unitChoice(ingredient.unit) === otherUnit) {
                    <label>Custom unit <input [name]="'ingredientCustomUnit' + ingredientIndex(section, ingredient)" [(ngModel)]="ingredient.unit" required /></label>
                  }
                </div>
                <label>Notes <input [name]="'ingredientNotes' + ingredientIndex(section, ingredient)" [(ngModel)]="ingredient.notes" placeholder="finely chopped" /></label>
                <button class="remove-button" type="button" (click)="removeIngredient(section, $index)" [disabled]="section.ingredients.length === 1" aria-label="Remove ingredient">Remove</button>
              </div>
            }
              <button class="button secondary small subsection-add-ingredient" type="button" (click)="addIngredient(section)">Add ingredient</button>
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
  readonly units = ['g', 'kg', 'mg', 'ml', 'cl', 'dl', 'l', 'tsp', 'tbsp', 'piece', 'clove', 'can', 'package', 'pinch', 'handful', 'slice', 'bunch', 'cup'];
  readonly otherUnit = '__other__';
  ingredientSections: IngredientSection[] = [{ name: '', ingredients: [this.newIngredient()] }];
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
    isPublished: true,
    sourceUrl: '',
    tags: [],
    ingredients: [],
    steps: [''],
    equipment: []
  };

  ngOnInit(): void {
    if (!this.recipeId) return;
    this.api.getRecipe(this.recipeId).subscribe({
      next: recipe => {
        const sections = new Map<string, IngredientSection>();
        for (const ingredient of recipe.ingredients) {
          const name = ingredient.section?.trim() || 'Ingredients';
          const key = name.toLocaleLowerCase();
          const section = sections.get(key) ?? { name, ingredients: [] };
          section.ingredients.push({ ...ingredient });
          sections.set(key, section);
        }
        this.ingredientSections = [...sections.values()];
        if (!this.ingredientSections.length) {
          this.ingredientSections = [{ name: 'Ingredients', ingredients: [this.newIngredient()] }];
        }
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
          isPublished: recipe.isPublished,
          sourceUrl: recipe.sourceUrl ?? '',
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

  addSection(): void {
    this.ingredientSections.push({ name: '', ingredients: [this.newIngredient()] });
  }

  removeSection(index: number): void {
    if (this.ingredientSections.length > 1) this.ingredientSections.splice(index, 1);
  }

  addIngredient(section: IngredientSection): void {
    section.ingredients.push(this.newIngredient());
  }

  removeIngredient(section: IngredientSection, index: number): void {
    if (section.ingredients.length > 1) section.ingredients.splice(index, 1);
  }

  ingredientIndex(section: IngredientSection, ingredient: RecipeIngredient): number {
    const previousSections = this.ingredientSections
      .slice(0, this.ingredientSections.indexOf(section))
      .reduce((count, item) => count + item.ingredients.length, 0);
    return previousSections + section.ingredients.indexOf(ingredient);
  }

  private newIngredient(): RecipeIngredient {
    return { name: '', quantity: 1, unit: '', section: '', notes: '' };
  }

  unitChoice(unit: string): string {
    return this.units.includes(unit) ? unit : unit ? this.otherUnit : '';
  }

  setUnit(ingredient: RecipeIngredient, choice: string): void {
    if (choice === this.otherUnit) {
      if (this.units.includes(ingredient.unit)) ingredient.unit = '';
    } else {
      ingredient.unit = choice;
    }
  }

  removeAt(items: string[], index: number): void {
    items.splice(index, 1);
    if (items === this.model.steps && items.length === 0) {
      this.model.steps.push('');
    }
  }

  save(): void {
    this.error.set('');
    this.model.ingredients = this.ingredientSections.flatMap(section =>
      section.ingredients.map(ingredient => ({ ...ingredient, section: section.name.trim() }))
    );
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

interface IngredientSection {
  name: string;
  ingredients: RecipeIngredient[];
}
