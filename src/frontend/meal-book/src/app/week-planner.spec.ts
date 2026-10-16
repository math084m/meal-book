import { addDays, daysBetween, mondayOf, periodDays, maxPeriodDays, spanPosition } from './week-planner';

describe('week planner dates', () => {
  it('adds days across month and year boundaries', () => {
    expect(addDays('2026-10-30', 3)).toBe('2026-11-02');
    expect(addDays('2026-12-31', 1)).toBe('2027-01-01');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });

  it('counts days across the daylight saving change', () => {
    expect(daysBetween('2026-10-24', '2026-10-27')).toBe(3);
    expect(daysBetween('2026-03-28', '2026-03-30')).toBe(2);
  });

  it('finds the Monday of a week', () => {
    expect(mondayOf('2026-10-16')).toBe('2026-10-12');
    expect(mondayOf('2026-10-18')).toBe('2026-10-12');
    expect(mondayOf('2026-10-12')).toBe('2026-10-12');
  });

  it('lists every day of a period and caps its length', () => {
    expect(periodDays('2026-10-12', '2026-10-14')).toEqual(['2026-10-12', '2026-10-13', '2026-10-14']);
    expect(periodDays('2026-10-12', '2026-10-12')).toEqual(['2026-10-12']);
    expect(periodDays('2026-10-12', '2026-10-01')).toEqual(['2026-10-12']);
    expect(periodDays('2026-01-01', '2030-01-01').length).toBe(maxPeriodDays);
  });

  it('tells which day of a multi-day span a date is', () => {
    const entry = { plannedStart: '2026-10-20', plannedDays: 3 };
    expect(spanPosition(entry, '2026-10-19')).toBe(0);
    expect(spanPosition(entry, '2026-10-20')).toBe(1);
    expect(spanPosition(entry, '2026-10-22')).toBe(3);
    expect(spanPosition(entry, '2026-10-23')).toBe(0);
    expect(spanPosition({ plannedStart: null, plannedDays: 1 }, '2026-10-20')).toBe(0);
  });
});
