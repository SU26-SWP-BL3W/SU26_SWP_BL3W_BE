import { formatPrizeValue } from '../formatPrizeValue';

describe('formatPrizeValue', () => {
  test.each([
    ['10000000', '10.000.000 VND'],
    ['10000000 VNĐ', '10.000.000 VND'],
    ['10,000,000 VND', '10.000.000 VND'],
    ['10000000 ₫', '10.000.000 VND'],
    [' 10000000 tiền mặt ', '10.000.000 tiền mặt VND'],
    ['10 triệu', '10 triệu VND'],
  ])('formats numeric value %s', (value, expected) => {
    expect(formatPrizeValue(value)).toBe(expected);
  });

  test.each(['Voucher 500k', 'Hiện vật'])('preserves non-monetary value %s', (value) => {
    expect(formatPrizeValue(value)).toBe(value);
  });
});
