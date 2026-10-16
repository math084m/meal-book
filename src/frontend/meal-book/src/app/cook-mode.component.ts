import { Component, HostListener, OnDestroy, computed, effect, inject, signal, untracked } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { I18nService } from './i18n.service';
import { Recipe, RecipeApiService } from './recipe-api.service';
import { formatAmount } from './servings-scaling';

interface WakeLockSentinelLike {
  release(): Promise<void>;
}

// Finds the first "10 minutes" / "1-2 timer" style duration in a step and returns seconds (upper bound of a range).
export function parseStepDuration(text: string): number | null {
  const match = /(\d+)(?:\s*[-–]\s*(\d+))?\s*(hours?|hrs?|timer|timen|time|minutes?|mins?|minutter|minut)\b/i.exec(text);
  if (!match) return null;
  const value = Number(match[2] ?? match[1]);
  const unit = match[3].toLowerCase();
  const hours = /^(h|t)/.test(unit);
  const seconds = value * (hours ? 3600 : 60);
  return seconds > 0 && seconds <= 12 * 3600 ? seconds : null;
}

@Component({
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (recipe(); as item) {
      <div class="cook-screen">
        <header class="cook-top">
          <a class="button secondary" [routerLink]="['/recipes', item.id]">✕ {{ i18n.t('cook.exit') }}</a>
          <strong class="cook-title">{{ item.name }}</strong>
          <button type="button" class="button secondary" [attr.aria-expanded]="showIngredients()" (click)="showIngredients.set(!showIngredients())">
            {{ i18n.t('cook.ingredients') }}
          </button>
        </header>

        @if (showIngredients()) {
          <section class="cook-ingredients" aria-live="polite">
            <ul>
              @for (ingredient of item.ingredients; track $index) {
                <li><strong>{{ amount(ingredient, item.servings) }}</strong> {{ ingredient.name }}</li>
              }
            </ul>
          </section>
        }

        @if (!item.steps.length) {
          <p class="state-card">{{ i18n.t('cook.noSteps') }}</p>
        } @else if (finished()) {
          <section class="cook-step cook-finished">
            <h1>{{ i18n.t('cook.finished') }}</h1>
            <a class="button" [routerLink]="['/recipes', item.id]">{{ i18n.t('cook.logIt') }}</a>
            <button type="button" class="button secondary" (click)="index.set(item.steps.length - 1)">{{ i18n.t('cook.back') }}</button>
          </section>
        } @else {
          <div class="cook-progress" role="progressbar" [attr.aria-valuenow]="index() + 1" aria-valuemin="1" [attr.aria-valuemax]="item.steps.length">
            <div class="cook-progress-bar" [style.width.%]="((index() + 1) / item.steps.length) * 100"></div>
          </div>
          <section class="cook-step" (touchstart)="touchStart($event)" (touchend)="touchEnd($event)">
            <p class="eyebrow">{{ i18n.t('cook.step', { n: index() + 1, total: item.steps.length }) }}</p>
            <p class="cook-text">{{ item.steps[index()] }}</p>
            @if (stepSeconds(); as seconds) {
              <div class="cook-timer">
                @if (timerEnd() === null) {
                  <button type="button" class="button secondary" (click)="startTimer(seconds)">⏱ {{ i18n.t('cook.timer.start', { time: format(seconds) }) }}</button>
                } @else {
                  <span class="cook-timer-value" [class.done]="remaining() === 0" role="timer">{{ remaining() === 0 ? i18n.t('cook.timer.done') : format(remaining()) }}</span>
                  <button type="button" class="button secondary" (click)="stopTimer()">{{ i18n.t('cook.timer.stop') }}</button>
                }
              </div>
            }
          </section>
          <nav class="cook-nav">
            <button type="button" class="button secondary" [disabled]="index() === 0" (click)="go(-1)">← {{ i18n.t('cook.prev') }}</button>
            <button type="button" class="button" (click)="go(1)">
              {{ index() === item.steps.length - 1 ? i18n.t('cook.done') : i18n.t('cook.next') + ' →' }}
            </button>
          </nav>
        }
      </div>
    } @else if (error()) {
      <div class="state-card" role="alert">{{ i18n.t('detail.notFoundShort') }}</div>
    } @else {
      <div class="state-card" role="status">{{ i18n.t('detail.loading') }}</div>
    }
  `,
  styles: `
    .cook-screen { display: grid; gap: 1.25rem; max-width: 52rem; margin: 0 auto; }
    .cook-top { display: flex; gap: 0.75rem; align-items: center; justify-content: space-between; }
    .cook-title { flex: 1; text-align: center; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    .cook-ingredients { background: var(--paper); border: 1px solid var(--line); border-radius: 12px; padding: 1rem 1.25rem; }
    .cook-ingredients ul { margin: 0; padding-left: 1.1rem; display: grid; gap: 0.3rem; }
    .cook-progress { height: 6px; border-radius: 3px; background: var(--line); overflow: hidden; }
    .cook-progress-bar { height: 100%; background: var(--green); transition: width 0.2s; }
    .cook-step { background: var(--paper); border: 1px solid var(--line); border-radius: 16px; padding: 2rem 1.5rem; min-height: 40vh; display: grid; align-content: center; gap: 1.25rem; }
    .cook-text { font-size: clamp(1.4rem, 4vw, 2.2rem); line-height: 1.4; margin: 0; }
    .cook-timer { display: flex; gap: 1rem; align-items: center; }
    .cook-timer-value { font-size: 2rem; font-weight: 700; font-variant-numeric: tabular-nums; color: var(--green); }
    .cook-timer-value.done { color: #c0392b; animation: blink 1s steps(2) infinite; }
    .cook-nav { display: flex; justify-content: space-between; gap: 1rem; }
    .cook-nav .button { flex: 1; font-size: 1.1rem; padding: 0.9rem 1rem; }
    .cook-finished { text-align: center; justify-items: center; }
    @keyframes blink { 50% { opacity: 0.3; } }
  `
})
export class CookModeComponent implements OnDestroy {
  private readonly api = inject(RecipeApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly i18n = inject(I18nService);

  readonly recipe = signal<Recipe | null>(null);
  readonly error = signal(false);
  readonly index = signal(0);
  readonly showIngredients = signal(false);
  readonly timerEnd = signal<number | null>(null);
  readonly now = signal(Date.now());
  private readonly servings = signal<number | null>(null);
  private wakeLock: WakeLockSentinelLike | null = null;
  private ticker: ReturnType<typeof setInterval> | null = null;
  private touchX = 0;

  readonly finished = computed(() => {
    const item = this.recipe();
    return !!item && item.steps.length > 0 && this.index() >= item.steps.length;
  });
  readonly stepSeconds = computed(() => {
    const step = this.recipe()?.steps[this.index()];
    return step ? parseStepDuration(step) : null;
  });
  readonly remaining = computed(() => {
    const end = this.timerEnd();
    return end === null ? 0 : Math.max(0, Math.ceil((end - this.now()) / 1000));
  });

  constructor() {
    const requested = Number(this.route.snapshot.queryParamMap.get('servings'));
    if (Number.isFinite(requested) && requested >= 1) this.servings.set(Math.min(100, Math.round(requested)));
    effect(() => {
      this.i18n.lang();
      untracked(() => this.load());
    });
    effect(() => {
      this.index();
      untracked(() => this.stopTimer());
    });
    effect(() => {
      if (this.remaining() === 0 && this.timerEnd() !== null) {
        untracked(() => navigator.vibrate?.([300, 150, 300]));
      }
    });
    void this.acquireWakeLock();
  }

  ngOnDestroy(): void {
    this.stopTimer();
    void this.wakeLock?.release().catch(() => undefined);
  }

  @HostListener('document:visibilitychange')
  onVisibility(): void {
    if (document.visibilityState === 'visible') void this.acquireWakeLock();
  }

  @HostListener('window:keydown', ['$event'])
  onKey(event: KeyboardEvent): void {
    if (event.key === 'ArrowRight') this.go(1);
    else if (event.key === 'ArrowLeft') this.go(-1);
    else if (event.key === 'Escape') void this.router.navigate(['/recipes', this.recipe()?.id ?? '']);
  }

  go(delta: number): void {
    const item = this.recipe();
    if (!item) return;
    this.index.update(value => Math.min(item.steps.length, Math.max(0, value + delta)));
  }

  touchStart(event: TouchEvent): void {
    this.touchX = event.changedTouches[0].clientX;
  }

  touchEnd(event: TouchEvent): void {
    const delta = event.changedTouches[0].clientX - this.touchX;
    if (Math.abs(delta) > 60) this.go(delta < 0 ? 1 : -1);
  }

  startTimer(seconds: number): void {
    this.now.set(Date.now());
    this.timerEnd.set(Date.now() + seconds * 1000);
    this.ticker ??= setInterval(() => this.now.set(Date.now()), 250);
  }

  stopTimer(): void {
    this.timerEnd.set(null);
    if (this.ticker) {
      clearInterval(this.ticker);
      this.ticker = null;
    }
  }

  format(totalSeconds: number): string {
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    const pad = (value: number) => String(value).padStart(2, '0');
    return hours ? `${hours}:${pad(minutes)}:${pad(seconds)}` : `${minutes}:${pad(seconds)}`;
  }

  amount(ingredient: Recipe['ingredients'][number], baseServings: number): string {
    const target = this.servings() ?? baseServings;
    return formatAmount(ingredient.quantity, ingredient.unit, target / baseServings, this.i18n.lang() === 'da' ? 'da' : 'en');
  }

  private load(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.error.set(true);
      return;
    }
    this.api.getRecipe(id).subscribe({
      next: value => {
        this.recipe.set(value);
        this.error.set(false);
      },
      error: () => this.error.set(true)
    });
  }

  private async acquireWakeLock(): Promise<void> {
    try {
      const wakeLock = (navigator as Navigator & { wakeLock?: { request(type: 'screen'): Promise<WakeLockSentinelLike> } }).wakeLock;
      this.wakeLock = (await wakeLock?.request('screen')) ?? null;
    } catch {
      this.wakeLock = null;
    }
  }
}
