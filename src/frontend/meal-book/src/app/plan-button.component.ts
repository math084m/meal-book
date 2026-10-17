import { Component, computed, inject, input, ChangeDetectionStrategy } from '@angular/core';
import { AuthService } from './auth.service';
import { I18nService } from './i18n.service';
import { PlanService } from './plan.service';

// Toggles a recipe on the shared meal plan. Renders nothing for signed-out visitors.
@Component({
  selector: 'app-plan-button',
  standalone: true,
  template: `
    @if (auth.currentUser()) {
      <button
        type="button"
        [class]="compact() ? 'plan-button compact' : 'button'"
        [class.secondary]="!compact() && planned()"
        [class.active]="planned()"
        [attr.aria-pressed]="planned()"
        [attr.aria-label]="compact() ? label() : null"
        [title]="label()"
        (click)="toggle($event)"
      >{{ compact() ? (planned() ? '✓' : '+') : label() }}</button>
    }
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: `
    .plan-button {
      width: 2.2rem; height: 2.2rem; border-radius: 50%; border: 1px solid var(--line);
      background: var(--paper); color: var(--green); font-size: 1.15rem; font-weight: 700;
      cursor: pointer; line-height: 1;
    }
    .plan-button.active { background: var(--green); color: var(--button-ink); border-color: var(--green); }
    .plan-button:hover { border-color: var(--hover-line); }
  `
})
export class PlanButtonComponent {
  readonly recipeId = input.required<string>();
  readonly compact = input(false);
  readonly auth = inject(AuthService);
  private readonly i18n = inject(I18nService);
  private readonly plan = inject(PlanService);

  readonly planned = computed(() => this.plan.isPlanned(this.recipeId()));
  readonly label = computed(() => this.i18n.t(this.planned() ? 'plan.remove' : 'plan.add'));

  toggle(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.planned()) {
      this.plan.remove(this.recipeId());
    } else {
      this.plan.add(this.recipeId());
    }
  }
}
