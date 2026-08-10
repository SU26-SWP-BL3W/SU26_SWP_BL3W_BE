import { getNavLinksFor } from '../getNavLinksFor';

describe('getNavLinksFor', () => {
  test('guest → only Sự kiện', () => {
    expect(getNavLinksFor(null)).toEqual([{ label: 'Sự kiện', href: '/' }]);
  });
  test('student → Sự kiện + Khác', () => {
    expect(getNavLinksFor('student')).toEqual([
      { label: 'Sự kiện', href: '/' },
      { label: 'Khác',    href: '/other' },
    ]);
  });
  test('judge → Sự kiện + Khác', () => {
    expect(getNavLinksFor('judge')).toEqual([
      { label: 'Sự kiện', href: '/' },
      { label: 'Khác',    href: '/other' },
    ]);
  });
  test('admin → all four', () => {
    expect(getNavLinksFor('admin')).toEqual([
      { label: 'Sự kiện',             href: '/' },
      { label: 'Tiêu chí chấm điểm', href: '/criteria' },
      { label: 'Người dùng',         href: '/users' },
      { label: 'Khác',                href: '/other' },
    ]);
  });
});
