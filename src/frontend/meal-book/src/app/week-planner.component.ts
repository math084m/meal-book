import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { I18nService } from './i18n.service';
import { PlanEntry, PlanService } from './plan.service';
import { RecipeImagePipe } from './recipe-image.pipe';
import { addDays, daysBetween, formatDate, maxPeriodDays, mondayOf, periodDays, spanPosition } from './week-planner';

const storageKey = 'mealbook-week-period';

interface DayCell {
  date: string;
  label: string;
  today: boolean;
  items: { entry: PlanEntry; position: number }[];
}

@Component({
  selector: 'app-week-planner',
  standalone: true,
  imports: [FormsModule, RouterLink, RecipeImagePipe],
  template: `
    <section class="week-page">
      <div class="section-heading">
        <div>
          <p class="eyebrow">{{ i18n.t('plan.eyebrow') }}</p>
          <h1>{{ i18n.t('week.title') }}</h1>
          <p class="muted">{{ i18n.t('week.intro') }}</p>
        </div>
        <a routerLink="/plan" class="button secondary small">{{ i18n.t('week.toList') }}</a>
      </div>

      <form class="detail-card period-form" (submit)="$event.preventDefault()">
        <label>
          {{ i18n.t('week.from') }}
          <input type="date" name="from" [ngModel]="from()" (ngModelChange)="setFrom($event)" />
        </label>
        <label>
          {{ i18n.t('week.to') }}
          <input type="date" name="to" [ngModel]="to()" (ngModelChange)="setTo($event)" />
        </label>
        <span class="period-actions">
          <button type="button" class="button secondary small" (click)="shift(-1)">← {{ i18n.t('week.prev') }}</button>
          <button type="button" class="button secondary small" (click)="thisWeek()">{{ i18n.t('week.thisWeek') }}</button>
          <button type="button" class="button secondary small" (click)="shift(1)">{{ i18n.t('week.next') }} →</button>
        </span>
      </form>
      @if (tooLong()) { <p class="muted" role="status">{{ i18n.t('week.capped', { n: maxDays }) }}</p> }

      @if (plan.plan(); as data) {
        <section class="detail-card tray" (dragover)="allowDrop($event)" (drop)="dropOnTray($event)">
          <div class="plan-head">
            <h2>{{ i18n.t('week.tray') }}</h2>
            <a routerLink="/recipes" class="link-button">{{ i18n.t('week.addMore') }}</a>
          </div>
          @if (tray().length) {
            <p class="muted small-text">{{ selected() ? i18n.t('week.hintPlace') : i18n.t('week.hint') }}</p>
            <ul class="tray-list">
              @for (entry of tray(); track entry.recipeId) {
                <li>
                  <button type="button" class="chip" draggable="true"
                    [class.selected]="selected() === entry.recipeId"
                    [attr.aria-pressed]="selected() === entry.recipeId"
                    (click)="toggleSelected(entry.recipeId)"
                    (dragstart)="dragStart($event, entry.recipeId)"
                    (dragend)="dragging.set(null)">
                    @if (entry.imageUrl | recipeImage; as src) { <img class="chip-thumb" [src]="src" alt="" draggable="false" /> }
                    <span class="chip-name">{{ entry.recipeName }}</span>
                    @if (entry.plannedStart) { <span class="chip-note">{{ shortDate(entry.plannedStart) }}</span> }
                  </button>
                </li>
              }
            </ul>
          } @else if (data.entries.length) {
            <p class="muted">{{ i18n.t('week.allPlaced') }}</p>
          } @else {
            <p class="muted">{{ i18n.t('plan.empty') }}</p>
          }
        </section>

        <div class="days">
          @for (day of days(); track day.date) {
            <section class="day" [class.today]="day.today" [class.over]="overDay() === day.date"
              (dragover)="dragOver($event, day.date)" (dragleave)="overDay.set(null)" (drop)="dropOnDay($event, day.date)">
              <h3>{{ day.label }}</h3>
              @for (item of day.items; track item.entry.recipeId) {
                @if (item.position === 1) {
                  <div class="entry" draggable="true" (dragstart)="dragStart($event, item.entry.recipeId)" (dragend)="dragging.set(null)">
                    <a [routerLink]="['/recipes', item.entry.recipeId]" class="entry-name">{{ item.entry.recipeName }}</a>
                    <div class="entry-controls">
                      <span class="stepper" role="group" [attr.aria-label]="i18n.t('week.daysAria', { name: item.entry.recipeName })">
                        <button type="button" class="button secondary small" [disabled]="item.entry.plannedDays <= 1" (click)="setDays(item.entry, item.entry.plannedDays - 1)">−</button>
                        <span>{{ i18n.t('week.days', { n: item.entry.plannedDays }) }}</span>
                        <button type="button" class="button secondary small" [disabled]="item.entry.plannedDays >= 14" (click)="setDays(item.entry, item.entry.plannedDays + 1)">+</button>
                      </span>
                      <button type="button" class="link-button" [attr.aria-label]="i18n.t('week.unscheduleAria', { name: item.entry.recipeName })" (click)="plan.schedule(item.entry.recipeId, null)">✕</button>
                    </div>
                  </div>
                } @else {
                  <div class="entry continues" draggable="true" (dragstart)="dragStart($event, item.entry.recipeId)" (dragend)="dragging.set(null)">
                    <span class="entry-name">↳ {{ item.entry.recipeName }}</span>
                    <span class="chip-note">{{ i18n.t('week.dayOf', { n: item.position, total: item.entry.plannedDays }) }}</span>
                  </div>
                }
              }
              @if (selected()) {
                <button type="button" class="button secondary small place" (click)="place(day.date)">{{ i18n.t('week.placeHere') }}</button>
              } @else if (!day.items.length) {
                <p class="muted small-text empty-day">{{ i18n.t('week.emptyDay') }}</p>
              }
            </section>
          }
        </div>
      } @else if (plan.error()) {
        <div class="state-card" role="alert">{{ i18n.t('plan.loadError') }}</div>
      } @else {
        <div class="state-card" role="status">{{ i18n.t('plan.loading') }}</div>
      }
    </section>
  `,
  styles: `
    .week-page { display: flex; flex-direction: column; gap: 1.2rem; }
    .period-form { display: flex; flex-wrap: wrap; align-items: flex-end; gap: 1rem; }
    .period-form label { flex: 0 1 11rem; }
    .period-form input { min-height: 40px; }
    .period-actions { display: inline-flex; flex-wrap: wrap; gap: 0.5rem; }
    .plan-head { display: flex; justify-content: space-between; align-items: baseline; gap: 1rem; }
    .small-text { font-size: 0.85rem; }
    .link-button { background: none; border: 0; padding: 0; color: var(--green); font-size: 0.85rem; text-decoration: underline; cursor: pointer; }
    .tray-list { list-style: none; margin: 0.6rem 0 0; padding: 0; display: flex; flex-wrap: wrap; gap: 0.5rem; }
    .chip { display: inline-flex; align-items: center; gap: 0.5rem; padding: 0.3rem 0.7rem 0.3rem 0.3rem; border: 1px solid var(--line); background: var(--paper); color: var(--ink); font: inherit; font-size: 0.9rem; cursor: grab; touch-action: manipulation; }
    .chip.selected { border-color: var(--green); outline: 2px solid var(--green); outline-offset: 1px; }
    .chip-thumb { width: 2.4rem; height: 1.7rem; object-fit: cover; border-radius: 0; }
    .chip-name { font-weight: 600; }
    .chip-note { font-size: 0.75rem; color: var(--muted); }
    .days { display: grid; grid-template-columns: repeat(auto-fill, minmax(11.5rem, 1fr)); gap: 0.8rem; }
    .day { display: flex; flex-direction: column; gap: 0.5rem; min-height: 8rem; padding: 0.7rem; border: 1px solid var(--line); background: var(--surface, var(--paper)); }
    .day h3 { margin: 0; font-size: 0.8rem; letter-spacing: 0.1em; text-transform: uppercase; color: var(--muted); }
    .day.today { border-color: var(--green); }
    .day.today h3 { color: var(--green); }
    .day.over { background: color-mix(in srgb, var(--green) 14%, var(--paper)); border-style: dashed; }
    .entry { display: flex; flex-direction: column; gap: 0.4rem; padding: 0.5rem; border: 1px solid var(--line); background: var(--paper); cursor: grab; }
    .entry.continues { flex-direction: row; justify-content: space-between; align-items: center; border-style: dashed; color: var(--muted); }
    .entry-name { font-weight: 600; font-size: 0.9rem; color: var(--ink); text-decoration: none; overflow-wrap: anywhere; }
    .entry.continues .entry-name { font-weight: 400; color: var(--muted); }
    .entry-controls { display: flex; justify-content: space-between; align-items: center; gap: 0.4rem; }
    .stepper { display: inline-flex; align-items: center; gap: 0.35rem; font-size: 0.8rem; }
    .place { align-self: stretch; }
    .empty-day { margin: 0; }
  `
})
export class WeekPlannerComponent {
  readonly plan = inject(PlanService);
  readonly i18n = inject(I18nService);
  readonly maxDays = maxPeriodDays;

  readonly from = signal('');
  readonly to = signal('');
  readonly selected = signal<string | null>(null);
  readonly dragging = signal<string | null>(null);
  readonly overDay = signal<string | null>(null);

  readonly tooLong = computed(() => daysBetween(this.from(), this.to()) + 1 > maxPeriodDays);

  readonly days = computed<DayCell[]>(() => {
    const entries = this.plan.plan()?.entries ?? [];
    const today = formatDate(new Date());
    const lang = this.i18n.lang();
    return periodDays(this.from(), this.to()).map(date => ({
      date,
      today: date === today,
      label: this.format(date, lang, { weekday: 'short', day: 'numeric', month: 'short' }),
      items: entries
        .map(entry => ({ entry, position: spanPosition(entry, date) }))
        .filter(item => item.position > 0)
    }));
  });

  // Recipes with no day yet, or planned for days outside the period being shown.
  readonly tray = computed(() => {
    const placed = new Set(this.days().flatMap(day => day.items.map(item => item.entry.recipeId)));
    return (this.plan.plan()?.entries ?? []).filter(entry => !placed.has(entry.recipeId));
  });

  constructor() {
    const stored = this.readStored();
    if (stored) {
      this.from.set(stored.from);
      this.to.set(stored.to);
    } else {
      this.thisWeek();
    }
  }

  shortDate(date: string): string {
    return this.format(date, this.i18n.lang(), { day: 'numeric', month: 'short' });
  }

  setFrom(value: string): void {
    if (!value) return;
    this.from.set(value);
    if (daysBetween(value, this.to()) < 0) this.to.set(value);
    this.store();
  }

  setTo(value: string): void {
    if (!value) return;
    this.to.set(value);
    if (daysBetween(this.from(), value) < 0) this.from.set(value);
    this.store();
  }

  thisWeek(): void {
    const monday = mondayOf(formatDate(new Date()));
    this.from.set(monday);
    this.to.set(addDays(monday, 6));
    this.store();
  }

  // Moves the whole period by its own length, so a 10-day trip steps 10 days at a time.
  shift(direction: 1 | -1): void {
    const length = Math.min(maxPeriodDays, Math.max(1, daysBetween(this.from(), this.to()) + 1));
    this.from.set(addDays(this.from(), direction * length));
    this.to.set(addDays(this.to(), direction * length));
    this.store();
  }

  toggleSelected(recipeId: string): void {
    this.selected.update(current => (current === recipeId ? null : recipeId));
  }

  place(date: string): void {
    const recipeId = this.selected();
    if (!recipeId) return;
    this.scheduleOn(recipeId, date);
    this.selected.set(null);
  }

  setDays(entry: PlanEntry, days: number): void {
    if (entry.plannedStart) this.plan.schedule(entry.recipeId, entry.plannedStart, days);
  }

  dragStart(event: DragEvent, recipeId: string): void {
    event.dataTransfer?.setData('text/plain', recipeId);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
    this.dragging.set(recipeId);
  }

  allowDrop(event: DragEvent): void {
    if (this.dragging()) event.preventDefault();
  }

  dragOver(event: DragEvent, date: string): void {
    if (!this.dragging()) return;
    event.preventDefault();
    this.overDay.set(date);
  }

  dropOnDay(event: DragEvent, date: string): void {
    event.preventDefault();
    const recipeId = this.dragging() ?? event.dataTransfer?.getData('text/plain');
    this.dragging.set(null);
    this.overDay.set(null);
    if (recipeId) this.scheduleOn(recipeId, date);
  }

  dropOnTray(event: DragEvent): void {
    event.preventDefault();
    const recipeId = this.dragging() ?? event.dataTransfer?.getData('text/plain');
    this.dragging.set(null);
    if (recipeId) this.plan.schedule(recipeId, null);
  }

  // Keeps the length of a recipe that already spans several days when it moves.
  private scheduleOn(recipeId: string, date: string): void {
    const entry = this.plan.plan()?.entries.find(item => item.recipeId === recipeId);
    this.plan.schedule(recipeId, date, entry?.plannedStart ? entry.plannedDays : 1);
  }

  private format(date: string, lang: string, options: Intl.DateTimeFormatOptions): string {
    const [year, month, day] = date.split('-').map(Number);
    return new Intl.DateTimeFormat(lang, options).format(new Date(year, month - 1, day));
  }

  private readStored(): { from: string; to: string } | null {
    try {
      const value = JSON.parse(localStorage.getItem(storageKey) ?? 'null') as { from?: string; to?: string } | null;
      const valid = (text?: string) => !!text && /^\d{4}-\d{2}-\d{2}$/.test(text);
      return value && valid(value.from) && valid(value.to) && daysBetween(value.from!, value.to!) >= 0
        ? { from: value.from!, to: value.to! }
        : null;
    } catch {
      return null;
    }
  }

  private store(): void {
    localStorage.setItem(storageKey, JSON.stringify({ from: this.from(), to: this.to() }));
  }
}
