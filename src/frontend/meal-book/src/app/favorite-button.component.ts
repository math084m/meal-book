import { Component, computed, inject, input } from '@angular/core';
import { AuthService } from './auth.service';
import { FavoriteService } from './favorite.service';
import { I18nService } from './i18n.service';

// Star toggle for personal favourites. Renders nothing for signed-out visitors.
@Component({
  selector: 'app-favorite-button',
  standalone: true,
  template: `
    @if (auth.currentUser()) {
      <button
        type="button"
        [class]="compact() ? 'fav-button compact' : 'button secondary'"
        [class.active]="favorite()"
        [attr.aria-pressed]="favorite()"
        [attr.aria-label]="label()"
        [title]="label()"
        (click)="toggle($event)"
      >{{ compact() ? (favorite() ? '★' : '☆') : (favorite() ? '★ ' : '☆ ') + label() }}</button>
    }
  `,
  styles: `
    .fav-button {
      width: 2.2rem; height: 2.2rem; border-radius: 50%; border: 1px solid var(--line);
      background: var(--paper); color: var(--green); font-size: 1.1rem; cursor: pointer; line-height: 1;
    }
    .fav-button.active { color: #d9a400; border-color: #d9a400; }
    .fav-button:hover { border-color: var(--hover-line); }
  `
})
export class FavoriteButtonComponent {
  readonly recipeId = input.required<string>();
  readonly compact = input(false);
  readonly auth = inject(AuthService);
  private readonly i18n = inject(I18nService);
  private readonly favorites = inject(FavoriteService);

  readonly favorite = computed(() => this.favorites.isFavorite(this.recipeId()));
  readonly label = computed(() => this.i18n.t(this.favorite() ? 'fav.remove' : 'fav.add'));

  toggle(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.favorites.toggle(this.recipeId());
  }
}
