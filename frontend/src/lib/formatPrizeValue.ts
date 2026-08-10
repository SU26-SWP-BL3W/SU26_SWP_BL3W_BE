const vietnameseNumberFormatter = new Intl.NumberFormat('vi-VN');

const NUMERIC_VALUE_PATTERN = /^(\d{1,3}(?:[.,\s]\d{3})+|\d{4,})(.*)$/;
const CURRENCY_SUFFIX_PATTERN = /\s*(?:VND|VNĐ|₫)\s*$/iu;

/**
 * Formats the numeric part of a prize value and adds a consistent VND suffix.
 * Non-monetary free-form values such as "Hiện vật" remain unchanged.
 */
export function formatPrizeValue(value: string): string {
  const trimmedValue = value.trim();
  const match = trimmedValue.match(NUMERIC_VALUE_PATTERN);
  let formattedValue = trimmedValue;

  if (match) {
    const numericValue = Number(match[1].replace(/[.,\s]/g, ''));
    if (Number.isSafeInteger(numericValue)) {
      formattedValue = `${vietnameseNumberFormatter.format(numericValue)}${match[2]}`;
    }
  }

  if (!/^\d/.test(formattedValue)) return formattedValue;

  return `${formattedValue.replace(CURRENCY_SUFFIX_PATTERN, '').trimEnd()} VND`;
}
