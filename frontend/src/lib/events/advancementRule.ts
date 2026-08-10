const ADVANCEMENT_RULE_PATTERN = /^\s*(top|percent|minscore)\s*:\s*(\d+(?:[.,]\d+)?)\s*$/i;

/** Convert the backend advancement-rule format into a user-facing sentence. */
export function formatAdvancementRule(rule?: string | null): string {
  const trimmedRule = rule?.trim();
  if (!trimmedRule) return '—';

  const match = trimmedRule.match(ADVANCEMENT_RULE_PATTERN);
  if (!match) return trimmedRule;

  const [, type, rawValue] = match;
  const value = rawValue.replace(',', '.');

  switch (type.toLowerCase()) {
    case 'minscore':
      return `Đội đạt điểm tối thiểu là ${value} sẽ được vào vòng tiếp theo`;
    case 'percent':
      return `Top ${value}% đội thi có điểm cao nhất sẽ được vào vòng tiếp theo`;
    case 'top':
      return `Top ${value} đội thi có điểm cao nhất sẽ được vào vòng tiếp theo`;
    default:
      return trimmedRule;
  }
}
