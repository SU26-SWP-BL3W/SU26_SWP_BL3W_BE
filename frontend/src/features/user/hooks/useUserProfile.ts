"use client";

import { useQuery } from "@tanstack/react-query";
import type { UserProfile } from "@/services/api/types";

const USER_STORAGE_KEY = "currentUser";

/** React Query key for the cached profile. Login writes to this so the home
 *  header updates immediately (no reload). */
export const USER_PROFILE_KEY = ["user", "profile"] as const;

/** Profile captured at login. The backend exposes no /users/me/profile endpoint,
 *  so the login response (persisted to localStorage) is the source of truth. */
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

export function useUserProfile() {
  return useQuery({
    queryKey: USER_PROFILE_KEY,
    queryFn: async (): Promise<UserProfile | null> => readPersistedUser(),
    staleTime: 5 * 60_000,
    retry: false,
  });
}
