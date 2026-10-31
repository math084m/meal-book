import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { AuthService, CurrentUser } from './auth.service';
import { PlanService, ShoppingItem } from './plan.service';

describe('PlanService pantry overrides', () => {
  let service: PlanService;
  let http: HttpTestingController;
  let user: ReturnType<typeof signal<CurrentUser | null>>;
  const item: ShoppingItem = {
    ingredientId: 'salt', name: 'Salt', quantity: 200, unit: 'g', have: 200,
    pantryItem: true, checked: false, recipes: [], customId: null
  };

  beforeEach(() => {
    user = signal<CurrentUser | null>(null);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: AuthService, useValue: { currentUser: user } }
      ]
    });
    service = TestBed.inject(PlanService);
    http = TestBed.inject(HttpTestingController);
    TestBed.tick();
  });

  afterEach(() => http.verify());

  it('saves a private fraction, including zero, without changing shared have amounts', () => {
    spyOn(service, 'refresh');
    for (const amount of [100, 0]) {
      service.setHave(item, amount);
      const request = http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/salt/shopping'));
      expect(request.request.body).toEqual({ haveFraction: amount / 200 });
      request.flush(null);
    }
    http.expectNone(req => req.url.endsWith('/api/meal-plan/have'));
    expect(service.refresh).toHaveBeenCalledTimes(2);
  });

  it('resets pantry defaults with null and reports failed personal saves', () => {
    spyOn(service, 'refresh');
    service.resetPantry({ ...item, pantryOverride: true });
    const request = http.expectOne(req => req.url.endsWith('/api/pantry/ingredients/salt/shopping'));
    expect(request.request.body).toEqual({ haveFraction: null });
    request.flush({}, { status: 500, statusText: 'Server error' });
    expect(service.pantryError()).toBeTrue();
    expect(service.refresh).toHaveBeenCalled();
  });

  it('keeps the existing shared endpoint for non-pantry ingredients', () => {
    spyOn(service, 'refresh');
    service.setHave({ ...item, pantryItem: false }, 50);
    const request = http.expectOne(req => req.url.endsWith('/api/meal-plan/have'));
    expect(request.request.body).toEqual({ ingredientId: 'salt', unit: 'g', quantity: 50 });
    request.flush(null);
    http.expectNone(req => req.url.includes('/api/pantry/'));
  });

  it('discards an in-flight personal shopping response after logout', () => {
    // Avoid a real hub connection; exercise the HTTP response and identity guard.
    spyOn(service as unknown as { start(): void }, 'start');
    user.set({ id: 'owner', email: 'owner@example.test', displayName: 'Owner', isAdmin: false });
    TestBed.tick();
    service.refresh();
    const request = http.expectOne(req => req.url.endsWith('/api/meal-plan'));
    user.set(null);
    TestBed.tick();
    request.flush({ entries: [], sections: [], usePantry: true });
    expect(service.plan()).toBeNull();
    http.expectNone(req => req.url.endsWith('/api/meal-plan/suggestions'));
  });
});
