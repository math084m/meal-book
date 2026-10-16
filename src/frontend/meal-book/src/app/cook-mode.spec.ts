import { parseStepDuration } from './cook-mode.component';

describe('parseStepDuration', () => {
  it('parses minutes and hours', () => {
    expect(parseStepDuration('Simmer for 10 minutes.')).toBe(600);
    expect(parseStepDuration('Bake 1 hour')).toBe(3600);
    expect(parseStepDuration('Lad simre i 20 minutter')).toBe(1200);
    expect(parseStepDuration('Steg i 2 timer')).toBe(7200);
  });

  it('uses the upper bound of a range', () => {
    expect(parseStepDuration('Cook 8-10 min')).toBe(600);
  });

  it('returns null when there is no duration', () => {
    expect(parseStepDuration('Chop the onions.')).toBeNull();
    expect(parseStepDuration('Add 200 g flour')).toBeNull();
  });
});
