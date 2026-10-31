import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { CatalogService } from './catalog.service';
import { I18nService } from './i18n.service';
import { PantryComponent } from './pantry.component';
import { PlanService } from './plan.service';
import { By } from '@angular/platform-browser';
import { SelectFieldComponent } from './select-field.component';

describe('PantryComponent', () => {
  let fixture: ComponentFixture<PantryComponent>;
  let http: HttpTestingController;
  let refresh: jasmine.Spy;

  beforeEach(() => {
    refresh = jasmine.createSpy('refresh');
    TestBed.configureTestingModule({
      imports: [PantryComponent],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: PlanService, useValue: { refresh } },
        { provide: CatalogService, useValue: {
          load: () => undefined,
          ingredientOptions: signal([{ value: 'salt', label: 'Salt' }, { value: 'oil', label: 'Oil' }])
        } }
      ]
    });
    TestBed.inject(I18nService).setLanguage('en');
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PantryComponent);
    fixture.detectChanges();
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: false, items: [{ ingredientId: 'salt', haveFraction: null }]
    });
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('adds and removes catalog ingredients and refreshes the shopping list after saving', () => {
    expect(fixture.componentInstance.choices().map(option => option.value)).toEqual(['oil']);
    fixture.componentInstance.selected = ['oil'];
    fixture.componentInstance.add();
    http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/oil') && req.method === 'PUT').flush(null);
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: false,
      items: [{ ingredientId: 'salt', haveFraction: null }, { ingredientId: 'oil', haveFraction: null }]
    });
    expect(fixture.componentInstance.selected).toEqual([]);
    expect(fixture.componentInstance.choices()).toEqual([]);
    expect(refresh).toHaveBeenCalledTimes(1);

    fixture.componentInstance.remove('salt');
    http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/salt') && req.method === 'DELETE').flush(null);
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: false, items: [{ ingredientId: 'oil', haveFraction: null }]
    });
    expect(fixture.componentInstance.listed().map(item => item.name)).toEqual(['Oil']);
    expect(refresh).toHaveBeenCalledTimes(2);
  });

  it('persists the opt-in toggle before showing it as enabled', () => {
    const checkbox = fixture.nativeElement.querySelector('input[type="checkbox"]') as HTMLInputElement;
    checkbox.click();
    expect(checkbox.checked).toBeFalse();
    const request = http.expectOne(req => req.url.endsWith('/api/pantry/settings'));
    expect(request.request.body).toEqual({ useForShopping: true });
    request.flush(null);
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: true, items: [{ ingredientId: 'salt', haveFraction: null }]
    });
    fixture.detectChanges();
    expect(checkbox.checked).toBeTrue();
    expect(refresh).toHaveBeenCalled();
  });

  it('adds every selected ingredient and reloads only after the batch completes', () => {
    fixture.componentInstance.data.set({ useForShopping: false, items: [] });
    fixture.detectChanges();
    const picker = fixture.debugElement.query(By.directive(SelectFieldComponent)).componentInstance as SelectFieldComponent;
    picker.pick('salt');
    picker.pick('oil');
    fixture.detectChanges();
    expect(fixture.componentInstance.selected).toEqual(['salt', 'oil']);
    fixture.componentInstance.add();
    http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/salt')).flush(null);
    http.expectNone(req => req.url.endsWith('/api/pantry'));
    expect(fixture.componentInstance.busy()).toBeTrue();
    http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/oil')).flush(null);
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: false,
      items: [{ ingredientId: 'salt', haveFraction: null }, { ingredientId: 'oil', haveFraction: null }]
    });
    expect(fixture.componentInstance.selected).toEqual([]);
    expect(refresh).toHaveBeenCalledTimes(1);
  });

  it('keeps failed batch selections for retry and displays ingredients already saved', () => {
    fixture.componentInstance.data.set({ useForShopping: false, items: [] });
    fixture.componentInstance.selected = ['salt', 'oil'];
    fixture.componentInstance.add();
    http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/salt')).flush(null);
    http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/oil'))
      .flush({}, { status: 500, statusText: 'Server error' });
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: false, items: [{ ingredientId: 'salt', haveFraction: null }]
    });
    expect(fixture.componentInstance.listed().map(item => item.name)).toEqual(['Salt']);
    expect(fixture.componentInstance.selected).toEqual(['oil']);
    expect(fixture.componentInstance.error()).toBeTrue();
    expect(fixture.componentInstance.busy()).toBeFalse();
    expect(refresh).toHaveBeenCalledTimes(1);
    fixture.componentInstance.add();
    http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/oil')).flush(null);
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: false,
      items: [{ ingredientId: 'salt', haveFraction: null }, { ingredientId: 'oil', haveFraction: null }]
    });
    expect(fixture.componentInstance.selected).toEqual([]);
    expect(fixture.componentInstance.error()).toBeFalse();
  });

  it('keeps the saved toggle and displays an error when saving fails', () => {
    const checkbox = fixture.nativeElement.querySelector('input[type="checkbox"]') as HTMLInputElement;
    checkbox.click();
    http.expectOne(req => req.url.endsWith('/api/pantry/settings'))
      .flush({}, { status: 500, statusText: 'Server error' });
    http.expectOne(req => req.url.endsWith('/api/pantry')).flush({
      useForShopping: false, items: [{ ingredientId: 'salt', haveFraction: null }]
    });
    fixture.detectChanges();
    expect(checkbox.checked).toBeFalse();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.componentInstance.busy()).toBeFalse();
    expect(refresh).toHaveBeenCalledTimes(1);
  });
});
