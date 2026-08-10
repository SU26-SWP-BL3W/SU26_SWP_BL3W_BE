/**
 * apiClient.js — Shim cho các service của scoring-system-v2.
 *
 * Dùng chung 1 Axios instance với SU26 (src/services/api/client.ts).
 * Instance đó có:
 *   - baseURL = NEXT_PUBLIC_API_URL (mặc định "https://api.sealswp391.xyz/api")
 *   - JWT Bearer token tự động
 *   - Interceptor unwrap BaseResponse<T>: response.data = env.data
 *
 * Vì interceptor đã unwrap, các service KHÔNG cần đọc .data?.data nữa.
 * (Đã được sửa trong từng service.)
 */
export { default } from './api/client';
