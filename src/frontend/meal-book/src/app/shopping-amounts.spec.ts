import { ShoppingItem } from './plan.service';
import { remainingShare, shoppingAmountText, shoppingSourcesText } from './shopping-amounts';

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
});
