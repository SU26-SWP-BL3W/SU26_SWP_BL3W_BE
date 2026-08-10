import { formatAdvancementRule } from '../advancementRule';

describe('formatAdvancementRule', () => {
  test.each([
    ['minScore:150', 'Đội đạt điểm tối thiểu là 150 sẽ được vào vòng tiếp theo'],
    ['percent:50', 'Top 50% đội thi có điểm cao nhất sẽ được vào vòng tiếp theo'],
    ['top:5', 'Top 5 đội thi có điểm cao nhất sẽ được vào vòng tiếp theo'],
    ['minScore:7.5', 'Đội đạt điểm tối thiểu là 7.5 sẽ được vào vòng tiếp theo'],
  ])('formats %s', (rule, expected) => {
    expect(formatAdvancementRule(rule)).toBe(expected);
  });

  it('handles spacing and case differences', () => {
    expect(formatAdvancementRule('  PERCENT : 25  ')).toBe(
      'Top 25% đội thi có điểm cao nhất sẽ được vào vòng tiếp theo',
    );
  });

  it('keeps an unknown rule unchanged', () => {
    expect(formatAdvancementRule('custom rule')).toBe('custom rule');
  });

  it('renders an empty rule as a dash', () => {
    expect(formatAdvancementRule(null)).toBe('—');
  });
});
