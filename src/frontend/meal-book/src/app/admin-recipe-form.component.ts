import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RecipeApiService, RecipeIngredient, RecipeWrite } from './recipe-api.service';
import { I18nService, TranslationKey, languages } from './i18n.service';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <a class="back-link" routerLink="/recipes">{{ i18n.t('common.backToRecipes') }}</a>
    <section class="page-heading">
      <p class="eyebrow">{{ i18n.t('form.eyebrow') }}</p>
      <h1>{{ recipeId ? i18n.t('form.titleEdit') : i18n.t('form.titleNew') }}</h1>
      <p class="intro">{{ i18n.t('form.intro') }}</p>
    </section>
    <form class="editor-form" (ngSubmit)="save()">
      <section class="form-section">
        <h2>{{ i18n.t('form.details') }}</h2>
        <div class="form-grid">
          <label>{{ i18n.t('form.language') }}
            <select name="language" [(ngModel)]="model.language">
              @for (item of languages; track item.code) { <option [value]="item.code">{{ item.label }}</option> }
            </select>
            <span class="field-hint">{{ i18n.t('form.languageHint') }}</span>
          </label>
          <label class="wide">{{ i18n.t('form.name') }} <input name="name" [(ngModel)]="model.name" required maxlength="200" /></label>
          <label class="wide">{{ i18n.t('form.description') }} <textarea name="description" [(ngModel)]="model.description" rows="3" maxlength="2000"></textarea></label>
          <label>{{ i18n.t('form.servings') }} <input name="servings" type="number" min="1" max="100" [(ngModel)]="model.servings" required /></label>
          <label>{{ i18n.t('form.prep') }} <input name="prepTime" type="number" min="0" [(ngModel)]="model.prepTimeMinutes" /></label>
          <label>{{ i18n.t('form.cook') }} <input name="cookTime" type="number" min="0" [(ngModel)]="model.cookTimeMinutes" /></label>
          <label>{{ i18n.t('form.mealType') }} <input name="mealType" [(ngModel)]="model.mealType" [placeholder]="i18n.t('list.mealType.placeholder')" /></label>
          <label>{{ i18n.t('form.course') }} <input name="course" [(ngModel)]="model.course" [placeholder]="i18n.t('list.course.placeholder')" /></label>
          <label>{{ i18n.t('form.method') }} <input name="method" [(ngModel)]="model.cookingMethod" [placeholder]="i18n.t('list.method.placeholder')" /></label>
          <label>{{ i18n.t('form.cuisine') }} <input name="cuisine" [(ngModel)]="model.cuisine" [placeholder]="i18n.t('list.cuisine.placeholder')" /></label>
          <label>{{ i18n.t('form.calories') }} <input name="calories" type="number" min="0" [(ngModel)]="model.caloriesPerServing" /></label>
          <label>{{ i18n.t('form.protein') }} <input name="protein" type="number" min="0" [(ngModel)]="model.proteinGramsPerServing" /></label>
          <label>{{ i18n.t('form.carbs') }} <input name="carbs" type="number" min="0" [(ngModel)]="model.carbohydratesGramsPerServing" /></label>
          <label>{{ i18n.t('form.fat') }} <input name="fat" type="number" min="0" [(ngModel)]="model.fatGramsPerServing" /></label>
          <label class="wide">{{ i18n.t('form.tags') }} <input name="tags" [(ngModel)]="tagText" [placeholder]="i18n.t('form.tags.placeholder')" /></label>
          <label class="wide">{{ i18n.t('form.source') }} <input name="sourceUrl" type="url" [(ngModel)]="model.sourceUrl" maxlength="2048" placeholder="https://example.com/recipe" /></label>
          <label class="publish-toggle">
            <input name="isPublished" type="checkbox" [(ngModel)]="model.isPublished" />
            {{ i18n.t('form.publish') }}
          </label>
        </div>
      </section>

      <section class="form-section">
        <div class="form-section-heading">
          <h2>{{ i18n.t('form.ingredients') }}</h2>
          <button class="button secondary small" type="button" (click)="addSection()">{{ i18n.t('form.addSection') }}</button>
        </div>
        @for (section of ingredientSections; track $index) {
          <div class="ingredient-subsection">
            <div class="ingredient-subsection-heading">
              <label>{{ i18n.t('form.sectionName') }} <input [name]="'ingredientSection' + $index" [(ngModel)]="section.name" required [placeholder]="i18n.t('form.sectionPlaceholder')" /></label>
              <button class="remove-button" type="button" (click)="removeSection($index)" [disabled]="ingredientSections.length === 1" [attr.aria-label]="i18n.t('form.removeSection')">{{ i18n.t('form.removeSection') }}</button>
            </div>
            @for (ingredient of section.ingredients; track $index) {
              <div class="ingredient-editor">
                <label>{{ i18n.t('form.ingredient') }} <input [name]="'ingredientName' + ingredientIndex(section, ingredient)" [(ngModel)]="ingredient.name" required /></label>
                <label>{{ i18n.t('form.quantity') }} <input [name]="'ingredientQty' + ingredientIndex(section, ingredient)" type="number" min="0.001" step="0.001" [(ngModel)]="ingredient.quantity" required /></label>
                <div class="ingredient-unit">
                  <label>{{ i18n.t('form.unit') }}
                    <select [name]="'ingredientUnit' + ingredientIndex(section, ingredient)" [ngModel]="unitChoice(ingredient.unit)" (ngModelChange)="setUnit(ingredient, $event)" required>
                      <option value="">{{ i18n.t('form.selectUnit') }}</option>
                      @for (unit of units; track unit) { <option [value]="unit">{{ unit }}</option> }
                      <option [value]="otherUnit">{{ i18n.t('form.otherUnit') }}</option>
                    </select>
                  </label>
                  @if (unitChoice(ingredient.unit) === otherUnit) {
                    <label>{{ i18n.t('form.customUnit') }} <input [name]="'ingredientCustomUnit' + ingredientIndex(section, ingredient)" [(ngModel)]="ingredient.unit" required /></label>
                  }
                </div>
                <label>{{ i18n.t('form.ingredientNotes') }} <input [name]="'ingredientNotes' + ingredientIndex(section, ingredient)" [(ngModel)]="ingredient.notes" [placeholder]="i18n.t('form.ingredientNotes.placeholder')" /></label>
                <button class="remove-button" type="button" (click)="removeIngredient(section, $index)" [disabled]="section.ingredients.length === 1" [attr.aria-label]="i18n.t('common.remove')">{{ i18n.t('common.remove') }}</button>
              </div>
            }
              <button class="button secondary small subsection-add-ingredient" type="button" (click)="addIngredient(section)">{{ i18n.t('form.addIngredient') }}</button>
          </div>
        }
      </section>

      <section class="form-section">
        <div class="form-section-heading"><h2>{{ i18n.t('form.steps') }}</h2><button class="button secondary small" type="button" (click)="model.steps.push('')">{{ i18n.t('form.addStep') }}</button></div>
        @for (step of model.steps; track $index) {
          <div class="step-editor">
            <span class="step-number">{{ $index + 1 }}</span>
            <textarea [name]="'step' + $index" [(ngModel)]="model.steps[$index]" rows="2" required></textarea>
            <button class="remove-button" type="button" (click)="removeAt(model.steps, $index)" [attr.aria-label]="i18n.t('form.removeStep')">{{ i18n.t('common.remove') }}</button>
          </div>
        }
      </section>

      <section class="form-section">
        <div class="form-section-heading"><h2>{{ i18n.t('form.equipment') }}</h2><button class="button secondary small" type="button" (click)="model.equipment.push('')">{{ i18n.t('form.addEquipment') }}</button></div>
        @for (item of model.equipment; track $index) {
          <div class="equipment-editor">
            <input [name]="'equipment' + $index" [(ngModel)]="model.equipment[$index]" [placeholder]="i18n.t('form.equipment.placeholder')" />
            <button class="remove-button" type="button" (click)="removeAt(model.equipment, $index)" [attr.aria-label]="i18n.t('form.removeEquipment')">{{ i18n.t('common.remove') }}</button>
          </div>
        }
      </section>
      @if (error()) { <p class="error-message" role="alert">{{ i18n.t(error()!) }}</p> }
      <div class="form-actions">
        <a class="button secondary" routerLink="/recipes">{{ i18n.t('common.cancel') }}</a>
        <button class="button" type="submit" [disabled]="saving()">{{ saving() ? i18n.t('form.saving') : i18n.t('form.save') }}</button>
      </div>
    </form>
  `
})
export class AdminRecipeFormComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  readonly languages = languages;
  readonly error = signal<TranslationKey | null>(null);
  readonly saving = signal(false);
  readonly recipeId = this.route.snapshot.paramMap.get('id');
  readonly units = ['g', 'kg', 'mg', 'ml', 'cl', 'dl', 'l', 'tsp', 'tbsp', 'piece', 'clove', 'can', 'package', 'pinch', 'handful', 'slice', 'bunch', 'cup'];
  readonly otherUnit = '__other__';
  ingredientSections: IngredientSection[] = [{ name: '', ingredients: [this.newIngredient()] }];
  tagText = '';
  model: RecipeWrite = {
    language: this.i18n.lang(),
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
    this.api.getRecipe(this.recipeId, true).subscribe({
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
          language: recipe.language,
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
      error: () => this.error.set('form.loadFailed')
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
    this.error.set(null);
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
        this.error.set('form.saveFailed');
      }
    });
  }
}

interface IngredientSection {
  name: string;
  ingredients: RecipeIngredient[];
}
