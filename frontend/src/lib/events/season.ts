const SEASON_NAMES: Record<string, 'Spring' | 'Summer' | 'Fall'> = {
  spring: 'Spring',
  'mùa xuân': 'Spring',
  summer: 'Summer',
  'mùa hè': 'Summer',
  fall: 'Fall',
  autumn: 'Fall',
  'mùa thu': 'Fall',
};

/** Normalize legacy Vietnamese season values to the canonical API/UI values. */
export function normalizeSeason(season?: string | null): string | null {
  const value = season?.trim();
  if (!value) return null;
  return SEASON_NAMES[value.toLocaleLowerCase('vi-VN')] ?? value;
}
