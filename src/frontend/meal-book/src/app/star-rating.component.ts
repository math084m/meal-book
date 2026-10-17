import { ChangeDetectionStrategy, Component, ElementRef, inject, input, model } from '@angular/core';
import { I18nService, TranslationKey } from './i18n.service';

// Interactive mode is a radiogroup with roving focus: arrow keys move and select,
// clicking the selected star again clears the rating. Read-only mode is a labelled image.
@Component({
  selector: 'app-star-rating',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    @if (readonly()) {
      <span class="stars readonly" role="img" [attr.aria-label]="i18n.t('stars.value', { n: value() ?? 0 })">
        @for (star of stars; track star) {
          <span class="star" [class.filled]="star <= (value() ?? 0)" aria-hidden="true">★</span>
        }
      </span>
    } @else {
      <span class="stars" role="radiogroup" [attr.aria-label]="label() || i18n.t('detail.rating')" (keydown)="onKey($event)">
        @for (star of stars; track star) {
          <button
            type="button"
            class="star"
            role="radio"
            [class.filled]="star <= (value() ?? 0)"
            [attr.aria-checked]="value() === star"
            [attr.aria-label]="optionLabel(star)"
            [tabIndex]="star === focusTarget() ? 0 : -1"
            (click)="select(star)">★</button>
        }
      </span>
    }
  `,
  styles: [`
    :host { display: inline-flex; }
    .stars { display: inline-flex; gap: 0.1rem; }
    .star {
      padding: 0 0.05rem;
      border: 0;
      background: none;
      color: var(--line);
      font-size: 1.6rem;
      line-height: 1;
      cursor: pointer;
    }
    .readonly .star { font-size: 1rem; cursor: default; }
    .star.filled { color: var(--rating); }
    button.star:hover, button.star:focus-visible { transform: scale(1.12); }
    button.star:focus-visible { outline: 2px solid var(--green); outline-offset: 2px; border-radius: 4px; }
  `]
})
export class StarRatingComponent {
  readonly i18n = inject(I18nService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  readonly value = model<number | null>(null);
  readonly readonly = input(false);
  readonly label = input('');
  readonly stars = [1, 2, 3, 4, 5];

  optionLabel(star: number): string {
    return this.i18n.t(`detail.rating.${star}` as TranslationKey);
  }

  focusTarget(): number {
    return this.value() ?? 1;
  }

  select(star: number): void {
    this.value.set(this.value() === star ? null : star);
  }

  onKey(event: KeyboardEvent): void {
    const current = this.value() ?? 0;
    let next: number | null = null;
    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowUp':
        next = Math.min(5, current + 1);
        break;
      case 'ArrowLeft':
      case 'ArrowDown':
        next = Math.max(1, current - 1);
        break;
      case 'Home':
        next = 1;
        break;
      case 'End':
        next = 5;
        break;
    }
    if (next === null) return;
    event.preventDefault();
    this.value.set(next);
    queueMicrotask(() =>
      this.host.nativeElement.querySelectorAll<HTMLButtonElement>('button.star')[next - 1]?.focus());
  }
}
