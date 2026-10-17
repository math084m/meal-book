import { AmountLanguage, formatAmount, formatTotal } from './servings-scaling';
import { ShoppingAmount, ShoppingItem, ShoppingSource } from './plan.service';

function amountsOf(item: ShoppingItem): ShoppingAmount[] {
  return item.amounts?.length ? item.amounts : [{ quantity: item.quantity, unit: item.unit }];
}

// All amounts of a line, e.g. "140 g + 1 can", optionally reduced by the share already at home.
export function shoppingAmountText(item: ShoppingItem, language: AmountLanguage, remainingShare = 1): string {
  return amountsOf(item)
    .filter(amount => amount.quantity * remainingShare > 0)
    .map(amount => formatTotal(amount.quantity * remainingShare, amount.unit, language))
    .join(' + ');
}

// The share of the line still to buy, from 0 to 1.
export function remainingShare(item: ShoppingItem): number {
  if (item.quantity <= 0) return 0;
  return Math.max(0, 1 - item.have / item.quantity);
}

// "Bolognese: 2 tbsp · Chili: 1 can", showing where the total comes from.
export function shoppingSourcesText(sources: ShoppingSource[] | null | undefined, language: AmountLanguage): string {
  return (sources ?? [])
    .map(source => `${source.recipe}: ${formatAmount(source.quantity, source.unit, 1, language)}`)
    .join(' · ');
}
