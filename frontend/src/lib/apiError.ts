import type { AxiosError } from 'axios';

interface ApiErrorBody {
  message?: string;
  statusCode?: number;
  errors?: Record<string, string[]>;
  details?: unknown;
}

interface BackendFieldError {
  key?: string;
  value?: string[];
}

/** Safely read an HTTP response status without weakening callers to `any`. */
export function getHttpStatus(error: unknown): number | undefined {
  return (error as AxiosError<ApiErrorBody>)?.response?.status;
}

function fieldLabel(field: string): string {
  const key = field.replace(/^\$\./, '').toLowerCase();
  const labels: Record<string, string> = {
    eventname: 'Tên sự kiện',
    startdate: 'Thời gian bắt đầu sự kiện',
    enddate: 'Thời gian kết thúc sự kiện',
    registrationstartdate: 'Thời gian mở đăng ký',
    registrationenddate: 'Thời gian kết thúc đăng ký',
  };
  return labels[key] ?? '';
}

function formatFieldError(field: string, message: string): string | null {
  // ASP.NET adds this duplicate top-level error when one field cannot deserialize.
  if (field.toLowerCase() === 'requestmodel') return null;

  const label = fieldLabel(field);
  if (/could not be converted to system\.datetime/i.test(message)) {
    return label
      ? `${label} không hợp lệ. Vui lòng chọn ngày và giờ hợp lệ.`
      : 'Có thời gian không hợp lệ. Vui lòng chọn lại ngày và giờ.';
  }

  return label ? `${label}: ${message}` : message;
}

function formatFieldErrors(data: ApiErrorBody): string[] {
  const recordErrors = data.errors
    ? Object.entries(data.errors).flatMap(([field, messages]) =>
        messages
          .map((message) => formatFieldError(field, message))
          .filter((message): message is string => Boolean(message)),
      )
    : [];
  if (recordErrors.length) return recordErrors;

  if (!Array.isArray(data.details)) return [];
  return (data.details as BackendFieldError[]).flatMap((item) => {
    if (!Array.isArray(item.value)) return [];
    return item.value
      .map((message) => formatFieldError(item.key ?? '', message))
      .filter((message): message is string => Boolean(message));
  });
}

/** Reword backend phrasings into friendlier user-facing Vietnamese. */
function humanizeMessage(message: string): string {
  return message.replace(/đã tồn tại trong hệ thống/gi, 'đã được sử dụng');
}

/**
 * Extract a user-facing message from a failed API call.
 *
 * Priority: field-level validation errors → backend `message` → caller's
 * action-specific fallback → a status-aware generic message. Raw .NET
 * exception strings ("... Exception ...", "... was thrown") are treated as
 * non-user-facing and skipped.
 */
export function getErrorMessage(e: unknown, fallback?: string): string {
  const res = (e as AxiosError<ApiErrorBody>)?.response;
  const data = res?.data;

  // 1. Field-level validation errors from the backend.
  const fieldMsgs = data ? formatFieldErrors(data) : [];
  if (fieldMsgs.length) return humanizeMessage(fieldMsgs.join(' '));

  // 2. A human-readable message from the backend.
  if (data?.message && !/Exception|was thrown/i.test(data.message)) {
    return humanizeMessage(data.message);
  }

  // 3. Caller's action-specific fallback (e.g. "Tạo sự kiện thất bại…").
  if (fallback) return fallback;

  // 4. No response at all → the request never reached / returned from the server.
  if (!res) {
    return 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra kết nối mạng rồi thử lại.';
  }

  // 5. Status-aware generic message. For 4xx the request itself is invalid,
  //    so "thử lại" with the same input would just fail again.
  const status = res.status ?? data?.statusCode;
  if (status === 401 || status === 403) {
    return 'Bạn không có quyền thực hiện thao tác này.';
  }
  if (status && status >= 400 && status < 500) {
    return 'Thao tác không hợp lệ. Vui lòng kiểm tra lại thông tin đã nhập.';
  }
  return 'Hệ thống đang gặp sự cố. Vui lòng thử lại sau ít phút.';
}
