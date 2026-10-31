import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { Observable, switchMap } from 'rxjs';
import { CatalogService } from './catalog.service';
import { I18nService } from './i18n.service';
import { Pantry, PantryService } from './pantry.service';
import { PlanService } from './plan.service';
import { SelectFieldComponent } from './select-field.component';

@Component({
  selector: 'app-pantry',
  standalone: true,
  imports: [SelectFieldComponent],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <section class="auth-card">
      <h2>{{ i18n.t('pantry.title') }}</h2>
      <p class="muted">{{ i18n.t('pantry.intro') }}</p>
      @if (error()) { <p class="error-message" role="alert">{{ i18n.t('pantry.error') }}</p> }
      @if (data(); as pantry) {
        <label class="pantry-toggle">
          <input type="checkbox" [checked]="pantry.useForShopping" [disabled]="busy()" (change)="setEnabled($event)" />
          {{ i18n.t('pantry.use') }}
        </label>
        <p class="field-hint">{{ i18n.t('pantry.hint') }}</p>
        <div class="pantry-add">
          <app-select-field [options]="choices()" [(value)]="selected" [multiple]="false" [searchable]="true"
            [placeholder]="i18n.t('pantry.choose')" [ariaLabel]="i18n.t('pantry.choose')" />
          <button type="button" class="button small" [disabled]="busy() || !selected.length" (click)="add()">{{ i18n.t('pantry.add') }}</button>
        </div>
        @if (busy()) { <p class="muted" role="status">{{ i18n.t('pantry.saving') }}</p> }
        <ul class="pantry-list">
          @for (item of listed(); track item.ingredientId) {
            <li>
              <span class="pantry-name">{{ item.name }}
                @if (item.haveFraction !== null) { <span class="field-hint">{{ i18n.t('pantry.override') }}</span> }
              </span>
              @if (item.haveFraction !== null) {
                <button type="button" class="link-button" [disabled]="busy()" (click)="reset(item.ingredientId)">{{ i18n.t('pantry.reset') }}</button>
              }
              <button type="button" class="link-button" [disabled]="busy()" [attr.aria-label]="i18n.t('pantry.remove', { name: item.name })"
                (click)="remove(item.ingredientId)">{{ i18n.t('common.remove') }}</button>
            </li>
          } @empty {
            <li class="muted">{{ i18n.t('pantry.empty') }}</li>
          }
        </ul>
      } @else if (error()) {
        <button type="button" class="button secondary small" (click)="load()">{{ i18n.t('pantry.retry') }}</button>
      } @else {
        <p class="muted" role="status">{{ i18n.t('pantry.loading') }}</p>
      }
    </section>
  `,
  styles: `
    .pantry-toggle { flex-direction: row; align-items: center; gap: 0.7rem; }
    .pantry-toggle input { width: 1.25rem; min-height: 0; height: 1.25rem; accent-color: var(--green); }
    .pantry-add { display: flex; align-items: flex-start; gap: 0.5rem; margin: 1rem 0; }
    app-select-field { flex: 1; min-width: 0; }
    .pantry-list { list-style: none; margin: 0; padding: 0; }
    .pantry-list li { display: flex; align-items: center; flex-wrap: wrap; gap: 0.7rem; padding: 0.7rem 0; border-top: 1px solid var(--line); }
    .pantry-name { flex: 1; min-width: 8rem; }
    .pantry-name .field-hint { display: block; }
  `
})
export class PantryComponent implements OnInit {
  readonly i18n = inject(I18nService);
  private readonly catalog = inject(CatalogService);
  private readonly pantry = inject(PantryService);
  private readonly plan = inject(PlanService);

  readonly data = signal<Pantry | null>(null);
  readonly busy = signal(false);
  readonly error = signal(false);
  selected: string[] = [];

  readonly listed = computed(() => {
    const options = this.catalog.ingredientOptions();
    return (this.data()?.items ?? []).map(item => ({
      ...item,
      name: options.find(option => option.value === item.ingredientId)?.label ?? this.i18n.t('pantry.ingredient')
    })).sort((a, b) => a.name.localeCompare(b.name, this.i18n.lang()));
  });
  readonly choices = computed(() => {
    const ids = new Set(this.data()?.items.map(item => item.ingredientId));
    return this.catalog.ingredientOptions().filter(option => !ids.has(option.value));
  });

  ngOnInit(): void {
    this.catalog.load();
    this.load();
  }

  load(): void {
    this.error.set(false);
    this.pantry.get().subscribe({
      next: data => this.data.set(data),
      error: () => this.error.set(true)
    });
  }

  setEnabled(event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    const enabled = checkbox.checked;
    checkbox.checked = this.data()?.useForShopping ?? false;
    this.mutate(this.pantry.setUseForShopping(enabled));
  }

  add(): void {
    const id = this.selected[0];
    if (!id || this.busy()) return;
    this.mutate(this.pantry.add(id), () => { this.selected = []; });
  }

  remove(id: string): void {
    this.mutate(this.pantry.remove(id));
  }

  reset(id: string): void {
    this.mutate(this.pantry.setHaveFraction(id, null));
  }

  private mutate(request: Observable<void>, afterSave?: () => void): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set(false);
    request.pipe(switchMap(() => this.pantry.get())).subscribe({
      next: data => {
        this.data.set(data);
        this.busy.set(false);
        afterSave?.();
        this.plan.refresh();
      },
      error: () => {
        this.busy.set(false);
        this.error.set(true);
        this.loadAfterFailure();
      }
    });
  }

  private loadAfterFailure(): void {
    this.pantry.get().subscribe({ next: data => this.data.set(data), error: () => undefined });
  }
}
