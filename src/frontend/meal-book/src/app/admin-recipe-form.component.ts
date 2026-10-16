import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RecipeApiService, RecipeIngredient, RecipeWrite, apiUrl } from './recipe-api.service';
import { resizeImage } from './image-resize';
import { I18nService, TranslationKey, languages } from './i18n.service';
import { CatalogService } from './catalog.service';
import { SelectFieldComponent } from './select-field.component';

@Component({
  standalone: true,
  imports: [FormsModule, RouterLink, SelectFieldComponent],
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
          <div class="field"><span class="field-label">{{ i18n.t('form.mealType') }}</span><app-select-field [options]="catalog.options('mealType')" [(value)]="model.mealTypes" [ariaLabel]="i18n.t('form.mealType')" /></div>
          <div class="field"><span class="field-label">{{ i18n.t('form.course') }}</span><app-select-field [options]="catalog.options('course')" [(value)]="model.courses" [ariaLabel]="i18n.t('form.course')" /></div>
          <div class="field"><span class="field-label">{{ i18n.t('form.method') }}</span><app-select-field [options]="catalog.options('cookingMethod')" [(value)]="model.cookingMethods" [ariaLabel]="i18n.t('form.method')" /></div>
          <div class="field"><span class="field-label">{{ i18n.t('form.cuisine') }}</span><app-select-field [options]="catalog.options('cuisine')" [searchable]="true" [(value)]="model.cuisines" [ariaLabel]="i18n.t('form.cuisine')" /></div>
          <label>{{ i18n.t('form.calories') }} <input name="calories" type="number" min="0" [(ngModel)]="model.caloriesPerServing" /></label>
          <label>{{ i18n.t('form.protein') }} <input name="protein" type="number" min="0" [(ngModel)]="model.proteinGramsPerServing" /></label>
          <label>{{ i18n.t('form.carbs') }} <input name="carbs" type="number" min="0" [(ngModel)]="model.carbohydratesGramsPerServing" /></label>
          <label>{{ i18n.t('form.fat') }} <input name="fat" type="number" min="0" [(ngModel)]="model.fatGramsPerServing" /></label>
          <div class="field wide"><span class="field-label">{{ i18n.t('form.tags') }}</span><app-select-field [options]="catalog.options('tag')" [searchable]="true" [(value)]="model.tags" [ariaLabel]="i18n.t('form.tags')" /></div>
          <label class="wide">{{ i18n.t('form.source') }} <input name="sourceUrl" type="url" [(ngModel)]="model.sourceUrl" maxlength="2048" placeholder="https://example.com/recipe" /></label>
          <label class="publish-toggle">
            <input name="isPublished" type="checkbox" [(ngModel)]="model.isPublished" />
            {{ i18n.t('form.publish') }}
          </label>
        </div>
      </section>

      <section class="form-section">
        <h2>{{ i18n.t('form.image') }}</h2>
        <div class="image-editor">
          @if (previewUrl(); as src) { <img [src]="src" alt="" /> }
          <input type="file" accept="image/jpeg,image/png,image/webp" (change)="onImageChosen($event)" [attr.aria-label]="i18n.t('form.image')" />
          <span class="field-hint">{{ i18n.t('form.imageHint') }}</span>
          @if (previewUrl()) { <button class="remove-button" type="button" (click)="removeImage()">{{ i18n.t('form.imageRemove') }}</button> }
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
                <div class="field"><span class="field-label">{{ i18n.t('form.ingredient') }}</span>
                  <app-select-field [options]="catalog.ingredientOptions()" [searchable]="true" [multiple]="false"
                    [placeholder]="i18n.t('form.ingredient.pick')" [ariaLabel]="i18n.t('form.ingredient')"
                    [value]="ingredient.ingredientId ? [ingredient.ingredientId] : []"
                    (valueChange)="setIngredient(ingredient, $event[0])" />
                </div>
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
      <p class="field-hint">{{ i18n.t('form.ingredient.missing') }}</p>
      <div class="form-actions">
        <a class="button secondary" routerLink="/recipes">{{ i18n.t('common.cancel') }}</a>
        <button class="button" type="submit" [disabled]="saving()">{{ saving() ? i18n.t('form.saving') : i18n.t('form.save') }}</button>
      </div>
    </form>
  `,
  styles: `.field { display: flex; flex-direction: column; gap: 0.4rem; } .field-label { color: var(--label); font-size: 0.84rem; font-weight: 650; }`
})
export class AdminRecipeFormComponent implements OnInit {
  private readonly api = inject(RecipeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);
  readonly catalog = inject(CatalogService);
  readonly languages = languages;
  readonly error = signal<TranslationKey | null>(null);
  readonly saving = signal(false);
  readonly previewUrl = signal<string | null>(null);
  private pendingImage: Blob | null = null;
  private hadImage = false;
  readonly recipeId = this.route.snapshot.paramMap.get('id');
  readonly units = ['g', 'kg', 'mg', 'ml', 'cl', 'dl', 'l', 'tsp', 'tbsp', 'piece', 'clove', 'can', 'package', 'pinch', 'handful', 'slice', 'bunch', 'cup'];
  readonly otherUnit = '__other__';
  ingredientSections: IngredientSection[] = [{ name: '', ingredients: [this.newIngredient()] }];
  model: RecipeWrite = {
    language: this.i18n.lang(),
    name: '',
    description: '',
    servings: 4,
    prepTimeMinutes: 0,
    cookTimeMinutes: 0,
    mealTypes: [],
    courses: [],
    cookingMethods: [],
    cuisines: [],
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
    this.catalog.load();
    if (!this.recipeId) return;
    this.api.getRecipe(this.recipeId, true).subscribe({
      next: recipe => {
        this.hadImage = !!recipe.imageUrl;
        this.previewUrl.set(recipe.imageUrl ? `${apiUrl}${recipe.imageUrl}` : null);
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
          mealTypes: [...recipe.mealTypes],
          courses: [...recipe.courses],
          cookingMethods: [...recipe.cookingMethods],
          cuisines: [...recipe.cuisines],
          caloriesPerServing: recipe.caloriesPerServing ?? null,
          proteinGramsPerServing: recipe.proteinGramsPerServing ?? null,
          carbohydratesGramsPerServing: recipe.carbohydratesGramsPerServing ?? null,
          fatGramsPerServing: recipe.fatGramsPerServing ?? null,
          isPublished: recipe.isPublished,
          sourceUrl: recipe.sourceUrl ?? '',
          tags: [...recipe.tags],
          ingredients: recipe.ingredients.map(item => ({ ...item })),
          steps: [...recipe.steps],
          equipment: [...recipe.equipment]
        };
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
    return { ingredientId: null, name: '', quantity: 1, unit: '', section: '', notes: '' };
  }

  setIngredient(ingredient: RecipeIngredient, id: string | undefined): void {
    ingredient.ingredientId = id ?? null;
    ingredient.name = this.catalog.ingredientOptions().find(option => option.value === id)?.label ?? '';
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

  async onImageChosen(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    try {
      this.pendingImage = await resizeImage(file);
      this.previewUrl.set(URL.createObjectURL(this.pendingImage));
      this.error.set(null);
    } catch {
      this.error.set('form.imageFailed');
    }
    input.value = '';
  }

  removeImage(): void {
    this.pendingImage = null;
    this.previewUrl.set(null);
  }

  private finishSave(recipeId: string): void {
    const done = () => {
      this.saving.set(false);
      void this.router.navigate(['/recipes', recipeId]);
    };
    const failed = () => {
      this.saving.set(false);
      this.error.set('form.imageFailed');
      void this.router.navigate(['/admin/recipes', recipeId, 'edit']);
    };
    if (this.pendingImage) {
      this.api.uploadImage(recipeId, this.pendingImage).subscribe({ next: done, error: failed });
    } else if (this.hadImage && !this.previewUrl()) {
      this.api.deleteImage(recipeId).subscribe({ next: done, error: failed });
    } else {
      done();
    }
  }

  save(): void {
    this.error.set(null);
    if (this.ingredientSections.some(section => section.ingredients.some(item => !item.ingredientId))) {
      this.error.set('form.ingredient.required');
      return;
    }
    this.model.ingredients = this.ingredientSections.flatMap(section =>
      section.ingredients.map(ingredient => ({ ...ingredient, section: section.name.trim() }))
    );
    this.saving.set(true);
    const request = this.recipeId
      ? this.api.updateRecipe(this.recipeId, this.model)
      : this.api.createRecipe(this.model);
    request.subscribe({
      next: recipe => this.finishSave(recipe.id),
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
