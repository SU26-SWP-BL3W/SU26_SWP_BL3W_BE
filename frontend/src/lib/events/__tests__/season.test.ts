import { normalizeSeason } from '../season';

describe('normalizeSeason', () => {
  test.each([
    ['Mùa Xuân', 'Spring'],
    ['Mùa Hè', 'Summer'],
    ['Mùa Thu', 'Fall'],
    ['spring', 'Spring'],
    ['SUMMER', 'Summer'],
    ['Autumn', 'Fall'],
  ])('normalizes %s to %s', (input, expected) => {
    expect(normalizeSeason(input)).toBe(expected);
  });

  it('keeps an unknown value unchanged', () => {
    expect(normalizeSeason('Winter')).toBe('Winter');
  });

  it('returns null for an empty value', () => {
    expect(normalizeSeason('  ')).toBeNull();
    expect(normalizeSeason(null)).toBeNull();
  });
});
