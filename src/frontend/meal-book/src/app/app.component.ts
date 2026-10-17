import { Component, ElementRef, HostListener, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet, NavigationStart } from '@angular/router';
import { AuthService } from './auth.service';
import { I18nService, Language, languages } from './i18n.service';
import { ThemeService } from './theme.service';
import { CatalogService } from './catalog.service';
import { PlanService } from './plan.service';

type MenuName = 'admin' | 'account';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly languages = languages;
  readonly themeService = inject(ThemeService);
  private readonly catalog = inject(CatalogService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly router = inject(Router);
  readonly plan = inject(PlanService);
  readonly openMenu = signal<MenuName | null>(null);

  constructor() {
    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) this.openMenu.set(null);
    });
  }

  toggleMenu(name: MenuName): void {
    this.openMenu.update(current => (current === name ? null : name));
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: Event): void {
    if (!(event.target as HTMLElement).closest('.menu')) this.openMenu.set(null);
  }

  @HostListener('document:keydown.escape')
  closeOnEscape(): void {
    this.openMenu.set(null);
  }

  initial(): string {
    return (this.auth.currentUser()?.email ?? '?').charAt(0).toUpperCase();
  }

  signOut(): void {
    this.openMenu.set(null);
    this.auth.logout();
  }

  setLanguage(event: Event): void {
    this.i18n.setLanguage((event.target as HTMLSelectElement).value as Language);
  }

  ngOnInit(): void {
    this.auth.initialize();
    this.catalog.load();
  }
}
