import axios, {
  AxiosError,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from "axios";
import type { ApiError, BaseResponse } from "./types";

const BASE_URL = process.env.NEXT_PUBLIC_API_URL ?? "https://api.sealswp391.xyz/api";

const apiClient = axios.create({
  baseURL: BASE_URL,
  timeout: 15_000,
  headers: { "Content-Type": "application/json" },
});

/** Only endpoints that genuinely do not require an authenticated session. */
function isPublicAuthRoute(url?: string): boolean {
  if (!url) return false;

  const pathname = url.split("?")[0].toLowerCase();
  return [
    "/auth/login",
    "/auth/google-login",
    "/auth/register",
    "/auth/refresh-token",
    "/auth/verify-email",
    "/auth/resend-verification",
    "/auth/forgot-password",
    "/auth/reset-password",
  ].some((route) => pathname.endsWith(route));
}

// ─── Request: attach access token ─────────────────────────────────────────────
// Refresh itself is public so it must not carry the expired access token. Other
// authenticated /Auth routes (logout, change-password, profiles...) still need
// Bearer and must participate in the refresh flow.
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    if (typeof window !== "undefined" && !isPublicAuthRoute(config.url)) {
      const token = localStorage.getItem("accessToken");
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// ─── Refresh-token flow ───────────────────────────────────────────────────────
// Khi access token hết hạn (401), thử làm mới bằng refresh token rồi retry request
// cũ — thay vì đá người dùng ra ngoài giữa chừng. Chỉ logout khi refresh thất bại.

/** Gộp các 401 đồng thời vào MỘT lần gọi refresh (single-flight). */
let refreshPromise: Promise<string> | null = null;

async function refreshAccessToken(): Promise<string> {
  const refreshToken = localStorage.getItem("refreshToken");
  if (!refreshToken) throw new Error("No refresh token");
  // Route /Auth/refresh-token is public, so its 401 never triggers a refresh loop.
  console.warn("[auth] refresh: gọi /Auth/refresh-token…");
  const { data } = await apiClient.post<{ accessToken: string; refreshToken: string }>(
    "/Auth/refresh-token",
    { refreshToken },
  );
  if (!data?.accessToken) throw new Error("Refresh response thiếu accessToken");
  localStorage.setItem("accessToken", data.accessToken);
  if (data.refreshToken) localStorage.setItem("refreshToken", data.refreshToken);
  return data.accessToken;
}

/** Xóa toàn bộ phiên và điều hướng về trang đăng nhập. */
function forceLogout(): void {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem("currentUser");
  localStorage.removeItem("mustChangePassword");

  if (window.location.pathname !== "/") {
    window.location.href = "/auth";
  } else {
    window.location.reload();
  }
}

// ─── Response: unwrap BaseResponse envelope, handle 401 ───────────────────────
apiClient.interceptors.response.use(
  (response: AxiosResponse<BaseResponse<unknown>>) => {
    const env = response.data;
    // Tolerate non-enveloped responses (defensive).
    if (env && typeof env === "object" && "success" in env && "data" in env) {
      if (!env.success) {
        const err: ApiError = {
          message: env.message ?? "Request failed",
          statusCode: env.statusCode ?? response.status,
          // Preserve field-level validation details for the UI.
          details: env.data,
        };
        return Promise.reject({
          response: { status: env.statusCode ?? response.status, data: err },
          message: err.message,
        } as AxiosError<ApiError>);
      }
      return { ...response, data: env.data };
    }
    return response;
  },
  async (error: AxiosError<BaseResponse<unknown> | ApiError>) => {
    // Normalise enveloped error bodies → ApiError shape.
    const body = error.response?.data;
    if (body && typeof body === "object" && "success" in body) {
      const env = body as BaseResponse<unknown>;
      (error.response as AxiosResponse).data = {
        message: env.message,
        statusCode: env.statusCode,
        // Validation errors are returned inside BaseResponse.data.
        details: env.data,
      } satisfies ApiError;
    }

    const original = error.config as
      | (InternalAxiosRequestConfig & { _retry?: boolean })
      | undefined;

    if (
      error.response?.status === 401 &&
      typeof window !== "undefined" &&
      !isPublicAuthRoute(error.config?.url)
    ) {
      console.warn("[auth] 401 tại", error.config?.url, {
        hasRefreshToken: !!localStorage.getItem("refreshToken"),
        alreadyRetried: !!original?._retry,
      });
      // Còn refresh token và chưa thử retry → làm mới access token rồi gọi lại.
      if (original && !original._retry && localStorage.getItem("refreshToken")) {
        original._retry = true;
        try {
          const newToken = await (refreshPromise ??= refreshAccessToken().finally(() => {
            refreshPromise = null;
          }));
          original.headers.Authorization = `Bearer ${newToken}`;
          console.warn("[auth] refresh OK → retry", error.config?.url);
          return apiClient(original);
        } catch (e) {
          // Refresh thất bại (refresh token hết hạn/không hợp lệ) → mới thực sự logout.
          console.warn("[auth] refresh THẤT BẠI → logout", e);
          forceLogout();
          return Promise.reject(error);
        }
      }
      // Không có refresh token, hoặc retry vẫn 401 → kết thúc phiên.
      console.warn("[auth] logout: không có refresh token hoặc retry vẫn 401");
      forceLogout();
    }
    return Promise.reject(error);
  },
);

export default apiClient;
