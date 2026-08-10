"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { authApi } from "@/services/api";
import type {
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  UserProfile,
} from "@/services/api";
import { useNotify } from "@/components/NotificationProvider";
import { getErrorMessage } from "@/lib/apiError";

// ─── Query keys ───────────────────────────────────────────────────────────────

export const AUTH_KEYS = {
  all: ["auth"] as const,
  me: ["auth", "me"] as const,
  mustChangePassword: ["auth", "mustChangePassword"] as const,
} as const;

const USER_STORAGE_KEY = "currentUser";
const MUST_CHANGE_KEY = "mustChangePassword";

// ─── Helpers ──────────────────────────────────────────────────────────────────

function persistTokens(accessToken: string, refreshToken: string) {
  localStorage.setItem("accessToken", accessToken);
  localStorage.setItem("refreshToken", refreshToken);
}

function clearTokens() {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
  localStorage.removeItem(USER_STORAGE_KEY);
  localStorage.removeItem(MUST_CHANGE_KEY);
}

/** Cờ "bắt buộc đổi mật khẩu" (tài khoản tạm) — lưu localStorage để còn hiệu lực sau khi F5. */
function setMustChangePassword(value: boolean) {
  if (typeof window === "undefined") return;
  if (value) localStorage.setItem(MUST_CHANGE_KEY, "1");
  else localStorage.removeItem(MUST_CHANGE_KEY);
}

function readMustChangePassword(): boolean {
  if (typeof window === "undefined") return false;
  return localStorage.getItem(MUST_CHANGE_KEY) === "1";
}

function persistUser(user: UserProfile) {
  localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(user));
}

function readPersistedUser(): UserProfile | null {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem(USER_STORAGE_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as UserProfile;
  } catch {
    return null;
  }
}

function hasToken(): boolean {
  if (typeof window === "undefined") return false;
  return !!localStorage.getItem("accessToken");
}

/** Build the in-app UserProfile from the backend login response. */
function loginResponseToProfile(res: LoginResponse): UserProfile {
  const role: UserProfile["role"] = res.isAdmin
    ? "ADMIN"
    : res.isStudent
      ? "STUDENT"
      : "MENTOR";
  return {
    id: res.userId,
    email: res.email,
    fullName: res.fullName,
    role,
    createdAt: new Date().toISOString(),
    stats: { eventsJoined: 0, projectScore: 0, rank: 0 },
  };
}

// ─── Hooks ────────────────────────────────────────────────────────────────────

/** Returns the cached current user (from login response / localStorage). */
export function useCurrentUser() {
  return useQuery({
    queryKey: AUTH_KEYS.me,
    queryFn: async () => readPersistedUser(),
    enabled: hasToken(),
    staleTime: Infinity,
    retry: false,
  });
}

export function useIsAuthenticated(): boolean {
  const { data } = useCurrentUser();
  return !!data;
}

/** Login mutation — persists tokens + user, navigates home. */
export function useLogin() {
  const queryClient = useQueryClient();
  const notify = useNotify();

  return useMutation({
    mutationFn: (payload: LoginRequest) => authApi.login(payload),
    onSuccess: (data) => {
      persistTokens(data.accessToken, data.refreshToken);
      const profile = loginResponseToProfile(data);
      persistUser(profile);
      queryClient.setQueryData(AUTH_KEYS.me, profile);

      // Tài khoản tạm (được mời vào đội): bật cờ bắt buộc đổi mật khẩu -> cổng chặn
      // toàn màn hình (ForcePasswordChangeGate) sẽ hiện form đổi mật khẩu ngay.
      const mustChange = data.mustChangePassword === true;
      setMustChangePassword(mustChange);
      queryClient.setQueryData(AUTH_KEYS.mustChangePassword, mustChange);

      notify.success(`Đăng nhập thành công. Chào mừng ${profile.fullName}!`);
      // Nếu người dùng đến từ một liên kết cần đăng nhập (vd: link phản hồi lời
      // mời trong email), quay lại đúng trang đó; nếu không thì về trang chủ.
      let redirectTo = "/";
      if (typeof window !== "undefined") {
        const saved = localStorage.getItem("postLoginRedirect");
        if (saved) {
          localStorage.removeItem("postLoginRedirect");
          redirectTo = saved;
        }
      }
      // ĐIỀU HƯỚNG CỨNG (không dùng router.replace): tải lại trang để mọi dữ liệu
      // đọc mới hoàn toàn theo tài khoản vừa đăng nhập. Điều hướng mềm giữ nguyên
      // cache React Query của tài khoản TRƯỚC (đội, sự kiện, thông báo... còn
      // staleTime) khiến hiển thị sai tài khoản/dữ liệu cũ cho tới khi người dùng F5.
      // (replace: Back không quay lại màn đăng nhập.)
      window.location.replace(redirectTo);
    },
    onError: (e) =>
      notify.error(getErrorMessage(e, "Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.")),
  });
}

/** Đăng nhập bằng Google — gửi idToken lên BE, lưu token + user, chuyển về trang chủ. */
export function useGoogleLogin() {
  const queryClient = useQueryClient();
  const router = useRouter();
  const notify = useNotify();

  return useMutation({
    mutationFn: (idToken: string) => authApi.googleLogin(idToken),
    onSuccess: (data) => {
      persistTokens(data.accessToken, data.refreshToken);
      const profile = loginResponseToProfile(data);
      persistUser(profile);
      queryClient.setQueryData(AUTH_KEYS.me, profile);
      notify.success(`Đăng nhập Google thành công. Chào mừng ${profile.fullName}!`);
      router.replace("/");
    },
    onError: (e) => notify.error(getErrorMessage(e, "Đăng nhập Google thất bại. Vui lòng thử lại.")),
  });
}

/**
 * Register mutation — backend does NOT return tokens, so we do not log the
 * user in here. Caller should switch the UI to login mode on success.
 */
export function useRegister() {
  const notify = useNotify();

  return useMutation({
    mutationFn: (payload: RegisterRequest) => authApi.register(payload),
    onSuccess: () =>
      notify.success("Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản."),
    onError: (e) => notify.error(getErrorMessage(e, "Đăng ký thất bại. Vui lòng thử lại.")),
  });
}

export function useLogout() {
  const queryClient = useQueryClient();
  const router = useRouter();
  const notify = useNotify();

  return useMutation({
    mutationFn: authApi.logout,
    onSuccess: () => {
      clearTokens();
      queryClient.clear();
      // Sau clear() phải set lại cờ = false để cổng bắt-buộc-đổi-MK re-render và ẩn ngay
      // (nếu không, overlay còn treo trên trang /auth cho tới khi F5).
      queryClient.setQueryData(AUTH_KEYS.mustChangePassword, false);
      notify.success("Đã đăng xuất.");
      router.push("/auth");
    },
    onError: () => {
      clearTokens();
      queryClient.clear();
      queryClient.setQueryData(AUTH_KEYS.mustChangePassword, false);
      router.push("/auth");
    },
  });
}

/** true khi tài khoản đang đăng nhập là tài khoản tạm bắt buộc đổi mật khẩu. Còn hiệu lực sau F5. */
export function useMustChangePassword(): boolean {
  const { data } = useQuery({
    queryKey: AUTH_KEYS.mustChangePassword,
    queryFn: () => readMustChangePassword(),
    staleTime: Infinity,
    retry: false,
  });
  return data === true;
}

/** Đổi mật khẩu — dùng cho cổng bắt buộc đổi mật khẩu của tài khoản tạm. */
export function useChangePassword() {
  const queryClient = useQueryClient();
  const notify = useNotify();

  return useMutation({
    mutationFn: (payload: {
      oldPassword: string;
      newPassword: string;
      confirmNewPassword: string;
    }) => authApi.changePassword(payload),
    onSuccess: () => {
      // Đổi xong -> BE đã gỡ IsTemporary; tắt cờ để cổng chặn biến mất.
      setMustChangePassword(false);
      queryClient.setQueryData(AUTH_KEYS.mustChangePassword, false);
      notify.success("Đổi mật khẩu thành công! Bạn có thể cập nhật hồ sơ và chấp nhận lời mời.");
    },
    onError: (e) => notify.error(getErrorMessage(e, "Đổi mật khẩu thất bại. Vui lòng thử lại.")),
  });
}
