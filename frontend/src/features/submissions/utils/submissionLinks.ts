/**
 * Tiện ích cho FORM NỘP BÀI ĐỘNG theo cấu hình Hạng mục (Track).
 *
 * - Track có trường `submissionRuleDescription`: mỗi DÒNG là một loại link phải nộp
 *   (vd "Link github repo\nLink demo\nLink slide") -> form sinh đúng số ô input.
 * - Khi gửi lên BE, các link được gộp thành chuỗi JSON dạng [{label, url}, ...]
 *   và lưu trong trường `submissionUrl` (không đổi schema BE).
 * - Khi hiển thị, parse ngược lại; dữ liệu cũ chỉ là 1 URL thường thì vẫn hiển thị
 *   bình thường (tương thích ngược).
 */

export interface SubmissionLink {
  label: string;
  url: string;
}

/** Nhãn mặc định khi Track không cấu hình yêu cầu link / dữ liệu cũ dạng 1 URL. */
export const DEFAULT_LINK_LABEL = 'Link nộp bài';

/** Tách `submissionRuleDescription` (mỗi dòng 1 yêu cầu) thành danh sách nhãn. */
export function parseRuleLabels(rule?: string | null): string[] {
  if (!rule) return [];
  return rule
    .split(/\r?\n/)
    .map((s) => s.trim())
    .filter(Boolean);
}

/**
 * Parse chuỗi `submissionUrl` đã lưu:
 * - JSON mảng [{label, url}] -> trả đúng danh sách link.
 * - Chuỗi URL thường (dữ liệu cũ) / JSON không hợp lệ -> 1 link mặc định (fallback).
 */
export function parseSubmissionLinks(submissionUrl?: string | null): SubmissionLink[] {
  const raw = (submissionUrl ?? '').trim();
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) {
      const links = parsed
        .filter((x): x is { label?: unknown; url: string } => !!x && typeof x.url === 'string' && !!x.url)
        .map((x) => ({
          label: typeof x.label === 'string' && x.label.trim() ? x.label.trim() : DEFAULT_LINK_LABEL,
          url: x.url,
        }));
      if (links.length > 0) return links;
    }
  } catch {
    /* không phải JSON -> dữ liệu cũ dạng 1 URL */
  }
  return [{ label: DEFAULT_LINK_LABEL, url: raw }];
}
