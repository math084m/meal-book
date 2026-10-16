import { Component, OnInit, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './auth.service';
import { I18nService, Language, languages } from './i18n.service';
import { ThemeService } from './theme.service';
import { CatalogService } from './catalog.service';
import { PlanService } from './plan.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  readonly auth = inject(AuthService);
  readonly i18n = inject(I18nService);
  readonly languages = languages;
  readonly themeService = inject(ThemeService);
  private readonly catalog = inject(CatalogService);
  readonly plan = inject(PlanService);

  setLanguage(event: Event): void {
    this.i18n.setLanguage((event.target as HTMLSelectElement).value as Language);
  }

  ngOnInit(): void {
    this.auth.initialize();
    this.catalog.load();
  }
}
