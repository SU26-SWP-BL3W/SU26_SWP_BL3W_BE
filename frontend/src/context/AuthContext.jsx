'use client';

/**
 * AuthContext — cầu nối giữa SU26 auth system và scoring-v2 role system.
 *
 * SU26's useLogin (hooks/useAuth.ts) lưu UserProfile vào localStorage:
 *   { id, email, fullName, role: "ADMIN" | "STUDENT" | "MENTOR" }
 *
 * Context này đọc localStorage và map sang role string của scoring-v2:
 *   "ADMIN"   → 'admin'
 *   "STUDENT" → 'team'
 *   "MENTOR"  → 'judge'
 *
 * Cũng hỗ trợ raw LoginResponse format ({ isAdmin, isStudent }) để
 * AuthContext.login() vẫn dùng được nếu cần.
 */

import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { authApi } from '../services/api/auth';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [role,    setRole]    = useState(null);
  const [userId,  setUserId]  = useState(null);
  const [loading, setLoading] = useState(true);

  // Khôi phục session từ localStorage khi app khởi động.
  // SU26's useLogin đã lưu UserProfile → deriveRole đọc được.
  useEffect(() => {
    const timer = window.setTimeout(() => {
      try {
        const raw   = localStorage.getItem('currentUser');
        const token = localStorage.getItem('accessToken');
        if (raw && token) {
          const user = JSON.parse(raw);
          setRole(deriveRole(user));
          setUserId(user.userId ?? user.id ?? null);
        }
      } catch { /* localStorage corrupt — bỏ qua */ }
      finally { setLoading(false); }
    }, 0);
    return () => window.clearTimeout(timer);
  }, []);

  // Đăng nhập qua authApi — lưu UserProfile (cùng format với SU26's useLogin)
  const login = useCallback(async (email, password) => {
    const data = await authApi.login({ email, password });
    localStorage.setItem('accessToken',  data.accessToken);
    localStorage.setItem('refreshToken', data.refreshToken);
    // Lưu ở format UserProfile (nhất quán với SU26's useLogin hook)
    const profile = {
      id:       data.userId,
      email:    data.email,
      fullName: data.fullName,
      role:     data.isAdmin ? 'ADMIN' : data.isStudent ? 'STUDENT' : 'MENTOR',
    };
    localStorage.setItem('currentUser', JSON.stringify(profile));
    const r = deriveRole(profile);
    setRole(r);
    setUserId(data.userId);
    return r;
  }, []);

  // Đăng nhập demo (dùng khi phát triển, không cần server)
  const loginDemo = useCallback((r) => {
    setRole(r);
    setUserId(`demo-${r}`);
  }, []);

  // Đăng xuất — xóa localStorage, SU26's useLogout cũng làm điều tương tự
  const logout = useCallback(async () => {
    try { await authApi.logout(); } catch { /* ignore */ }
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('currentUser');
    setRole(null);
    setUserId(null);
  }, []);

  return (
    <AuthContext.Provider value={{ role, userId, loading, login, loginDemo, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
};

// Map user object → role string dùng trong scoring-v2
// Xử lý cả 2 format:
//   UserProfile (SU26 useLogin):   { role: "ADMIN" | "STUDENT" | "MENTOR" }
//   LoginResponse (AuthContext.login): { isAdmin: bool, isStudent: bool }
const deriveRole = (user) => {
  if (user?.role === 'ADMIN'   || user?.isAdmin)   return 'admin';
  if (user?.role === 'STUDENT' || user?.isStudent) return 'team';
  if (user?.role === 'MENTOR')                     return 'judge';
  return 'judge';
};
