import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';
import { I18nService } from './i18n.service';
import { RecipeApiService } from './recipe-api.service';

describe('AppComponent', () => {
  beforeEach(async () => {
    localStorage.clear();
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should request recipes in the selected language', () => {
    const i18n = TestBed.inject(I18nService);
    i18n.setLanguage('da');
    TestBed.inject(RecipeApiService).getRecipes().subscribe();
    const http = TestBed.inject(HttpTestingController);
    const request = http.expectOne(req => req.url.endsWith('/api/recipes'));
    expect(request.request.params.get('lang')).toBe('da');
    request.flush([]);
  });

  it('should translate UI text', () => {
    const i18n = TestBed.inject(I18nService);
    i18n.setLanguage('da');
    expect(i18n.t('nav.recipes')).toBe('Opskrifter');
    expect(i18n.t('common.minutes', { n: 5 })).toBe('5 min');
  });
});
