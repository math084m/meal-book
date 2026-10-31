export type AmountLanguage = 'en' | 'da';

// Every volume unit is defined relative to 1 ml, so any unit can be converted to any other.
const mlPerUnit: Record<string, number> = {
  pinch: 0.3125,
  pinches: 0.3125,
  knivspids: 0.3125,
  dash: 0.625,
  ml: 1,
  cl: 10,
  dl: 100,
  l: 1000,
  tsp: 5,
  teaspoon: 5,
  teaspoons: 5,
  tsk: 5,
  tbsp: 15,
  tablespoon: 15,
  tablespoons: 15,
  spsk: 15
};

const gramsPerUnit: Record<string, number> = { g: 1, kg: 1000 };

const danishUnitLabels: { aliases: string[]; label: string }[] = [
  { aliases: ['piece', 'pieces', 'pcs', 'stk', 'stk.'], label: 'stk.' },
  { aliases: ['pinch', 'pinches'], label: 'knivspids' },
  { aliases: ['dash', 'dashes'], label: 'stænk' },
  { aliases: ['tsp', 'teaspoon', 'teaspoons'], label: 'tsk' },
  { aliases: ['tbsp', 'tablespoon', 'tablespoons'], label: 'spsk' },
  { aliases: ['as needed', 'as required'], label: 'efter behov' },
  { aliases: ['to taste'], label: 'efter smag' },
  { aliases: ['clove', 'cloves'], label: 'fed' },
  { aliases: ['can', 'cans', 'tin', 'tins'], label: 'dåser' },
  { aliases: ['package', 'packages', 'pack', 'packs'], label: 'pakker' },
  { aliases: ['handful', 'handfuls'], label: 'håndfuld' },
  { aliases: ['small handful'], label: 'lille håndfuld' },
  { aliases: ['slice', 'slices'], label: 'skiver' },
  { aliases: ['bunch', 'bunches'], label: 'bundt' },
  { aliases: ['ball', 'balls'], label: 'kugler' },
  { aliases: ['bottle', 'bottles'], label: 'flasker' },
  { aliases: ['bag', 'bags'], label: 'poser' },
  { aliases: ['roll', 'rolls'], label: 'ruller' },
  { aliases: ['box', 'boxes'], label: 'æsker' }
];

function localizedUnit(unit: string, language: AmountLanguage): string {
  if (language !== 'da') {
    return unit;
  }

  const key = unit.trim().toLowerCase();
  return danishUnitLabels.find(item => item.aliases.includes(key))?.label ?? unit;
}

const fractions: { value: number; symbol: string }[] = [
  { value: 0, symbol: '' },
  { value: 0.25, symbol: '¼' },
  { value: 1 / 3, symbol: '⅓' },
  { value: 0.5, symbol: '½' },
  { value: 2 / 3, symbol: '⅔' },
  { value: 0.75, symbol: '¾' },
  { value: 1, symbol: '' }
];

interface DisplayUnit {
  name: (language: AmountLanguage, value: number) => string;
  ml: number;
  min: number;
  max: number;
  // Roundings to try in order of preference: whole numbers first, then halves, then quarters.
  steps: number[];
  languages?: AmountLanguage[];
}

// Ordered largest first so that the larger unit wins when two units fit equally well.
const displayUnits: DisplayUnit[] = [
  { name: () => 'l', ml: mlPerUnit['l'], min: 1, max: Infinity, steps: [1, 0.5, 0.25] },
  { name: () => 'dl', ml: mlPerUnit['dl'], min: 1, max: 9.5, steps: [1, 0.5, 0.25], languages: ['da'] },
  { name: language => (language === 'da' ? 'spsk' : 'tbsp'), ml: mlPerUnit['tbsp'], min: 1, max: 4, steps: [1, 0.5] },
  { name: language => (language === 'da' ? 'tsk' : 'tsp'), ml: mlPerUnit['tsp'], min: 0.25, max: 6, steps: [1, 0.5, 0.25] },
  {
    name: (language, value) => (language === 'da' ? 'knivspids' : value === 1 ? 'pinch' : 'pinches'),
    ml: mlPerUnit['pinch'],
    min: 1,
    max: 3,
    steps: [1]
  }
];

const tolerance = 0.04;

function locale(language: AmountLanguage): string {
  return language === 'da' ? 'da-DK' : 'en-GB';
}

export function formatNumber(value: number, language: AmountLanguage, maxDigits = 2): string {
  return new Intl.NumberFormat(locale(language), { maximumFractionDigits: maxDigits }).format(value);
}

// Shows 1.5 as "1½" and 0.33 as "⅓" when the value is close to a common fraction.
export function formatFriendly(value: number, language: AmountLanguage): string {
  const whole = Math.floor(value);
  const rest = value - whole;
  const match = fractions.find(fraction => Math.abs(fraction.value - rest) <= 0.04);
  if (!match) {
    return formatNumber(value, language, value >= 10 ? 0 : 1);
  }

  const total = whole + (match.value === 1 ? 1 : 0);
  if (!match.symbol) {
    return formatNumber(total, language);
  }

  return whole === 0 ? match.symbol : `${whole}${match.symbol}`;
}

// Picks the unit where the amount lands closest to a whole number, then a half, then a quarter.
function bestVolumeFit(ml: number, language: AmountLanguage): string | null {
  let best: { rank: number; text: string } | null = null;
  for (const unit of displayUnits) {
    if (unit.languages && !unit.languages.includes(language)) {
      continue;
    }

    const value = ml / unit.ml;
    if (value < unit.min || value > unit.max) {
      continue;
    }

    for (let rank = 0; rank < unit.steps.length; rank++) {
      const step = unit.steps[rank];
      const snapped = Math.round(value / step) * step;
      if (snapped > 0 && Math.abs(snapped - value) / value <= tolerance) {
        if (!best || rank < best.rank) {
          best = { rank, text: `${formatFriendly(snapped, language)} ${unit.name(language, snapped)}` };
        }

        break;
      }
    }
  }

  return best ? best.text : null;
}

function volumeText(ml: number, language: AmountLanguage): string {
  const fit = bestVolumeFit(ml, language);
  if (fit) {
    return fit;
  }

  const rounded = ml < 20 ? Math.round(ml) : Math.round(ml / 5) * 5;
  return `${formatNumber(rounded, language, 0)} ml`;
}

function weightText(grams: number, language: AmountLanguage): string {
  if (grams >= 1000) {
    return `${formatNumber(Math.round(grams / 10) / 100, language)} kg`;
  }

  const rounded = grams < 10 ? Math.round(grams * 10) / 10 : grams < 100 ? Math.round(grams) : Math.round(grams / 5) * 5;
  return `${formatNumber(rounded, language, 1)} g`;
}

// Formats a summed amount (already in a base unit like ml or g) with the best-fitting display unit.
export function formatTotal(quantity: number, unit: string, language: AmountLanguage): string {
  return formatAmount(quantity / 2, unit, 2, language);
}

// At factor 1 keep the stored amount but localize its unit; otherwise choose a fitting unit and rounding.
export function formatAmount(quantity: number, unit: string, factor: number, language: AmountLanguage): string {
  const key = unit.trim().toLowerCase();
  if (factor === 1) {
    // Amounts stored in ml (e.g. 2.5 ml) read better as spoons when they fit.
    const spoonFit = key === 'ml' ? bestVolumeFit(quantity, language) : null;
    return spoonFit ?? `${formatNumber(quantity, language, 3)} ${localizedUnit(unit, language)}`.trim();
  }

  const scaled = quantity * factor;
  if (key in mlPerUnit) {
    return volumeText(scaled * mlPerUnit[key], language);
  }

  if (key in gramsPerUnit) {
    return weightText(scaled * gramsPerUnit[key], language);
  }

  return `${formatFriendly(scaled, language)} ${localizedUnit(unit, language)}`.trim();
}
