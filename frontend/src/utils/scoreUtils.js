/**
 * utils/scoreUtils.js
 *
 * Pure helper functions for score calculation and formatting.
 * Logic must match backend scoring formula.
 *
 * maxScore của từng tiêu chí đã được cố định khi cấu hình bộ tiêu chí, vì vậy
 * tổng điểm chỉ là tổng các điểm thành phần.
 */

// ── WEIGHTED SCORE ─────────────────────────────────────────────────────────────
export const calcScore = (scores, criteria) => {
  if (!scores || !criteria || !scores.length) return 0;
  const total = criteria.reduce((sum, _c, i) => sum + Number(scores[i] || 0), 0);
  return Math.round(total * 100) / 100;
};

// ── TOTAL WEIGHT CHECK ─────────────────────────────────────────────────────────
// Backend/template validation yêu cầu tổng trọng số bằng 100% trước khi hoàn tất.
export const totalWeight = (criteria) =>
  criteria.reduce((s, c) => s + Number(c.weight), 0);

// ── RANK HELPERS ───────────────────────────────────────────────────────────────
export const rankColor = (r) =>
  r === 1 ? '#ffd700' : r === 2 ? '#c0c8d0' : r === 3 ? '#cd7f32' : '#A09890';

export const rankBg = (r) =>
  r === 1 ? 'rgba(255,215,0,.06)'
  : r === 2 ? 'rgba(192,200,208,.04)'
  : r === 3 ? 'rgba(205,127,50,.05)'
  : 'transparent';

export const rankIcon = (r) =>
  r === 1 ? '🥇' : r === 2 ? '🥈' : r === 3 ? '🥉' : `#${r}`;

// ── DATE FORMAT ────────────────────────────────────────────────────────────────
// BACKEND: CoreHelper.SystemTimeNow = DateTimeOffset.UtcNow (stored UTC)
// Frontend: convert to UTC+7 for display (TimeHelper.ConvertToUtcPlus7)
export const formatDateTime = (isoString) => {
  if (!isoString) return '';
  const d = new Date(isoString);
  return d.toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' });
};

export const formatDate = (isoString) => {
  if (!isoString) return '';
  const d = new Date(isoString);
  return d.toLocaleDateString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' });
};

// ── STATUS LABEL ───────────────────────────────────────────────────────────────
// Map backend status enum strings → Vietnamese display labels
// BACKEND: RegistrationStatus, AppealStatus enums decorated with [CustomName]
export const STATUS_LABELS = {
  // Registration
  Confirmed: 'Đã xác nhận',
  Pending:   'Chờ duyệt',
  Approved:  'Đã duyệt',
  Rejected:  'Từ chối',
  // Appeal
  Waiting:   'Chờ xử lý',
  Reviewing: 'Đang xét',
  Accepted:  'Đã duyệt',
  // Submission
  Received:  'Đã nhận',
};

export const getStatusLabel = (status) => STATUS_LABELS[status] ?? status;

// ── BADGE CSS CLASS ────────────────────────────────────────────────────────────
export const getStatusBadgeClass = (status) => {
  const map = {
    Confirmed: 'bdg-g', Approved: 'bdg-g', Accepted: 'bdg-g', Received: 'bdg-g',
    Pending:   'bdg-w', Waiting:  'bdg-w',
    Reviewing: 'bdg-b',
    Rejected:  'bdg-r',
  };
  return map[status] ?? 'bdg-w';
};
