import { formatAmount, formatFriendly, formatTotal } from './servings-scaling';

describe('shopping totals', () => {
  it('formats a summed amount', () => {
    expect(formatTotal(500, 'g', 'en')).toBe('500 g');
    expect(formatTotal(3, 'pcs', 'en')).toBe('3 pcs');
  });
});

describe('servings scaling', () => {
  it('localizes stored units at original servings in Danish', () => {
    for (const [unit, translated] of [
      ['pieces', 'stk.'], ['pinch', 'knivspids'], ['as needed', 'efter behov'],
      ['as required', 'efter behov'], ['tbsp', 'spsk'], ['tsp', 'tsk'],
      ['cloves', 'fed'], ['handful', 'håndfuld'], ['slices', 'skiver']
    ]) {
      expect(formatAmount(1, unit, 1, 'da')).withContext(unit).toBe(`1 ${translated}`);
    }
    expect(formatAmount(2, ' AS REQUIRED ', 1, 'da')).toBe('2 efter behov');
  });

  it('localizes non-convertible units when servings or shopping totals change', () => {
    expect(formatAmount(4, 'pieces', 0.5, 'da')).toBe('2 stk.');
    expect(formatAmount(1, 'as required', 2, 'da')).toBe('2 efter behov');
    expect(formatTotal(3, 'pcs', 'da')).toBe('3 stk.');
  });

  it('preserves English and unknown unit labels', () => {
    expect(formatAmount(1, 'as required', 1, 'en')).toBe('1 as required');
    expect(formatAmount(1, 'custom unit', 1, 'da')).toBe('1 custom unit');
  });

  it('shows the stored amount untouched at factor 1', () => {
    expect(formatAmount(70, 'g', 1, 'en')).toBe('70 g');
    expect(formatAmount(50, 'ml', 1, 'en')).toBe('50 ml');
    expect(formatAmount(3, 'tsp', 1, 'en')).toBe('3 tsp');
  });

  it('shows ml amounts as spoons at the original servings when they fit', () => {
    expect(formatAmount(2.5, 'ml', 1, 'en')).toBe('½ tsp');
    expect(formatAmount(15, 'ml', 1, 'en')).toBe('1 tbsp');
    expect(formatAmount(30, 'ml', 1, 'da')).toBe('2 spsk');
  });

  it('picks the unit closest to a whole number', () => {
    expect(formatAmount(15, 'ml', 4 / 3, 'en')).toBe('4 tsp');
    expect(formatAmount(15, 'ml', 2, 'en')).toBe('2 tbsp');
    expect(formatAmount(15, 'ml', 1.5, 'en')).toBe('1½ tbsp');
    expect(formatAmount(2.5, 'ml', 2, 'en')).toBe('1 tsp');
    expect(formatAmount(15, 'ml', 1.5, 'da')).toBe('1½ spsk');
  });

  it('uses dl in Danish and ml/l in English', () => {
    expect(formatAmount(250, 'ml', 1.2, 'da')).toBe('3 dl');
    expect(formatAmount(250, 'ml', 1.2, 'en')).toBe('300 ml');
    expect(formatAmount(500, 'ml', 3, 'en')).toBe('1½ l');
  });

  it('falls back to rounded ml when nothing fits', () => {
    expect(formatAmount(50, 'ml', 1.3, 'en')).toBe('65 ml');
  });

  it('rebalances spoon units', () => {
    expect(formatAmount(3, 'tsp', 2, 'en')).toBe('2 tbsp');
    expect(formatAmount(1, 'tbsp', 0.5, 'en')).toBe('1½ tsp');
  });

  it('rounds weights', () => {
    expect(formatAmount(500, 'g', 2, 'en')).toBe('1 kg');
    expect(formatAmount(250, 'g', 1.25, 'en')).toBe('315 g');
    expect(formatAmount(70, 'g', 1.5, 'en')).toBe('105 g');
  });

  it('shows friendly fractions for countable units', () => {
    expect(formatAmount(1, 'piece', 1.5, 'en')).toBe('1½ piece');
    expect(formatAmount(4, 'pieces', 0.5, 'en')).toBe('2 pieces');
    expect(formatAmount(1, 'pieces', 1 / 3, 'en')).toBe('⅓ pieces');
  });

  it('converts pinches to and from teaspoons', () => {
    expect(formatAmount(1, 'pinch', 2, 'en')).toBe('2 pinches');
    expect(formatAmount(1, 'pinch', 4, 'en')).toBe('¼ tsp');
    expect(formatAmount(1, 'knivspids', 2, 'da')).toBe('2 knivspids');
    expect(formatAmount(0.25, 'tsp', 0.5, 'en')).toBe('2 pinches');
  });

  it('uses locale decimals', () => {
    expect(formatFriendly(2.1, 'da')).toBe('2,1');
  });
});
