export const maxPeriodDays = 42;

// Dates are plain 'YYYY-MM-DD' strings, so time zones can never shift a planned day.
export function parseDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

export function formatDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function addDays(value: string, days: number): string {
  const date = parseDate(value);
  date.setDate(date.getDate() + days);
  return formatDate(date);
}

export function daysBetween(from: string, to: string): number {
  const utc = (value: string) => {
    const date = parseDate(value);
    return Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
  };
  return Math.round((utc(to) - utc(from)) / 86_400_000);
}

export function mondayOf(value: string): string {
  const offset = (parseDate(value).getDay() + 6) % 7;
  return addDays(value, -offset);
}

// Every day from `from` to `to` inclusive, capped so a mistyped year cannot render thousands of columns.
export function periodDays(from: string, to: string): string[] {
  const count = Math.min(maxPeriodDays, Math.max(1, daysBetween(from, to) + 1));
  return Array.from({ length: count }, (_, index) => addDays(from, index));
}

export interface ScheduledSpan {
  plannedStart: string | null;
  plannedDays: number;
}

// 1-based position of `day` within the span, or 0 when the recipe is not cooked that day.
export function spanPosition(entry: ScheduledSpan, day: string): number {
  if (!entry.plannedStart) return 0;
  const offset = daysBetween(entry.plannedStart, day);
  return offset >= 0 && offset < entry.plannedDays ? offset + 1 : 0;
}
