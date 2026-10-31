import { ShoppingItem } from './plan.service';
import { remainingShare, shoppingAmountText, shoppingSourcesText } from './shopping-amounts';
import { TestBed } from '@angular/core/testing';
import { CatalogService } from './catalog.service';
import { I18nService } from './i18n.service';
import { PlanComponent } from './plan.component';
import { PlanService } from './plan.service';

function item(overrides: Partial<ShoppingItem>): ShoppingItem {
  return { ingredientId: 'x', name: 'Tomato paste', quantity: 1, unit: 'mixed', checked: false, have: 0, recipes: [], customId: null, ...overrides };
}

describe('shopping amounts', () => {
  it('joins amounts in different units', () => {
    const mixed = item({ amounts: [{ quantity: 140, unit: 'g' }, { quantity: 1, unit: 'can' }] });
    expect(shoppingAmountText(mixed, 'en')).toBe('140 g + 1 can');
  });

  it('scales every amount by the share still to buy', () => {
    const mixed = item({ have: 0.5, amounts: [{ quantity: 200, unit: 'g' }, { quantity: 2, unit: 'can' }] });
    expect(remainingShare(mixed)).toBe(0.5);
    expect(shoppingAmountText(mixed, 'en', remainingShare(mixed))).toBe('100 g + 1 can');
  });

  it('falls back to quantity and unit when no amounts are sent', () => {
    expect(shoppingAmountText(item({ quantity: 500, unit: 'g' }), 'en')).toBe('500 g');
  });

  it('lists where the total comes from', () => {
    const text = shoppingSourcesText([{ recipe: 'Bolognese', quantity: 2, unit: 'can' }, { recipe: 'Chili', quantity: 70, unit: 'g' }], 'en');
    expect(text).toBe('Bolognese: 2 can · Chili: 70 g');
  });

  it('omits pantry defaults from copied lists and includes the remaining overridden mixed amounts', () => {
    TestBed.configureTestingModule({
      providers: [
        { provide: PlanService, useValue: {} },
        { provide: CatalogService, useValue: {} }
      ]
    });
    TestBed.inject(I18nService).setLanguage('en');
    const component = TestBed.runInInjectionContext(() => new PlanComponent());
    const text = component.listText({
      entries: [],
      usePantry: true,
      sections: [{ slug: 'pantry', name: 'Pantry', items: [
        item({ name: 'Salt', pantryItem: true, quantity: 10, unit: 'g', have: 10 }),
        item({ pantryItem: true, pantryOverride: true, have: 0.5,
          amounts: [{ quantity: 200, unit: 'g' }, { quantity: 2, unit: 'can' }] })
      ] }]
    });
    expect(text).not.toContain('Salt');
    expect(text).toContain('100 g + 1 can Tomato paste');
  });
});
