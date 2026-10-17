import { Component, ElementRef, HostListener, computed, inject, input, model, signal, ChangeDetectionStrategy } from '@angular/core';
import { I18nService } from './i18n.service';
import { SelectOption } from './catalog.service';

// Dropdown with premade options. Multi-select keeps the panel open; single-select closes on pick.
@Component({
  selector: 'app-select-field',
  standalone: true,
  template: `
    <div
      class="select-trigger"
      role="combobox"
      tabindex="0"
      [attr.aria-expanded]="open()"
      [attr.aria-label]="ariaLabel()"
      (click)="toggle()"
      (keydown.enter)="toggle(); $event.preventDefault()"
      (keydown.escape)="open.set(false)">
      <span class="select-summary">
        @if (!selectedOptions().length) {
          <span class="select-placeholder">{{ placeholder() || i18n.t('select.any') }}</span>
        } @else if (multiple()) {
          @for (option of selectedOptions(); track option.value) {
            <span class="select-chip">
              {{ option.label }}
              <button type="button" class="chip-remove" (click)="remove(option.value, $event)" [attr.aria-label]="i18n.t('common.remove')">×</button>
            </span>
          }
        } @else {
          {{ selectedOptions()[0].label }}
        }
      </span>
      <span class="select-caret" aria-hidden="true">▾</span>
    </div>
    @if (open()) {
      <div class="select-panel">
        @if (searchable()) {
          <input
            class="select-search"
            type="search"
            [value]="query()"
            (input)="query.set($any($event.target).value)"
            [placeholder]="i18n.t('select.search')"
            [attr.aria-label]="i18n.t('select.search')" />
        }
        <ul class="select-options" role="listbox" [attr.aria-multiselectable]="multiple()">
          @for (option of filtered(); track option.value) {
            <li role="option" [attr.aria-selected]="isSelected(option.value)">
              <button type="button" class="select-option" [class.selected]="isSelected(option.value)" (click)="pick(option.value)">
                <span class="select-check" aria-hidden="true">{{ isSelected(option.value) ? '✓' : '' }}</span>
                {{ option.label }}
              </button>
            </li>
          } @empty {
            <li class="select-empty">{{ i18n.t('select.none') }}</li>
          }
        </ul>
        @if (multiple() && value().length) {
          <button type="button" class="select-clear" (click)="clear()">{{ i18n.t('select.clear') }}</button>
        }
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: `
    :host { position: relative; display: block; }
    .select-trigger {
      display: flex; align-items: center; justify-content: space-between; gap: 0.5rem;
      min-height: 42px; padding: 0.35rem 0.7rem; border: 1px solid var(--input-line);
      border-radius: 0; color: var(--ink); background: var(--input-bg);
      font-size: 0.95rem; font-weight: 300; cursor: pointer;
    }
    .select-trigger:focus { outline: 2px solid var(--focus); outline-offset: 1px; border-color: var(--green); }
    .select-summary { display: flex; flex-wrap: wrap; gap: 0.3rem; min-width: 0; }
    .select-placeholder { color: var(--muted); }
    .select-chip {
      display: inline-flex; align-items: center; gap: 0.25rem; padding: 0.15rem 0.2rem 0.15rem 0.55rem;
      border: 1px solid var(--line); color: var(--tag-ink); background: transparent; font-size: 0.7rem;
      letter-spacing: 0.08em; text-transform: uppercase;
    }
    .chip-remove {
      width: 1.2rem; height: 1.2rem; padding: 0; border: 0; border-radius: 50%;
      color: inherit; background: transparent; font-size: 0.95rem; line-height: 1; cursor: pointer;
    }
    .chip-remove:hover { background: var(--line); }
    .select-caret { color: var(--muted); flex: none; }
    .select-panel {
      position: absolute; z-index: 30; top: calc(100% + 4px); left: 0; right: 0;
      border: 1px solid var(--input-line); border-radius: 0; background: var(--paper);
      box-shadow: 0 14px 34px rgb(0 0 0 / 14%); padding: 0.4rem;
    }
    .select-search { margin-bottom: 0.4rem; }
    .select-options { max-height: 240px; margin: 0; padding: 0; overflow-y: auto; list-style: none; }
    .select-option {
      display: flex; align-items: center; gap: 0.5rem; width: 100%; padding: 0.45rem 0.5rem;
      border: 0; border-radius: 0; color: var(--ink); background: transparent;
      font: inherit; font-size: 0.88rem; text-align: left; cursor: pointer;
    }
    .select-option:hover { background: var(--green-wash); }
    .select-option.selected { color: var(--tag-ink); font-weight: 650; }
    .select-check { width: 1rem; flex: none; color: var(--green); }
    .select-empty { padding: 0.5rem; color: var(--muted); font-size: 0.85rem; }
    .select-clear {
      margin-top: 0.3rem; padding: 0.3rem 0.5rem; border: 0; color: var(--muted);
      background: transparent; font: inherit; font-size: 0.8rem; cursor: pointer; text-decoration: underline;
    }
  `
})
export class SelectFieldComponent {
  readonly i18n = inject(I18nService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly options = input.required<SelectOption[]>();
  readonly value = model<string[]>([]);
  readonly multiple = input(true);
  readonly searchable = input(false);
  readonly placeholder = input('');
  readonly ariaLabel = input('');

  readonly open = signal(false);
  readonly query = signal('');

  readonly selectedOptions = computed(() => {
    const options = this.options();
    return this.value()
      .map(value => options.find(option => option.value === value))
      .filter((option): option is SelectOption => !!option);
  });

  readonly filtered = computed(() => {
    const query = this.query().trim().toLocaleLowerCase();
    return query
      ? this.options().filter(option => option.label.toLocaleLowerCase().includes(query))
      : this.options();
  });

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: Event): void {
    if (this.open() && !event.composedPath().includes(this.host.nativeElement)) {
      this.open.set(false);
    }
  }

  toggle(): void {
    this.open.update(open => !open);
    this.query.set('');
  }

  isSelected(value: string): boolean {
    return this.value().includes(value);
  }

  pick(value: string): void {
    if (!this.multiple()) {
      this.value.set([value]);
      this.open.set(false);
      return;
    }

    this.value.update(current =>
      current.includes(value) ? current.filter(item => item !== value) : [...current, value]
    );
  }

  remove(value: string, event: Event): void {
    event.stopPropagation();
    this.value.update(current => current.filter(item => item !== value));
  }

  clear(): void {
    this.value.set([]);
  }
}
