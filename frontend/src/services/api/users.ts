import apiClient from "./client";
import type { PagedResult } from "./types";

/** User shape for search/autocomplete and the user list (matches UserModel). */
export interface UserSummary {
  id: string;
  email: string | null;
  fullName: string | null;
  studentCode: string | null;
  schoolId: string | null;
  isStudent: boolean;
  isAdmin: boolean;
  isApproved: boolean;
  /** True nếu tài khoản đang có lời từ chối còn hiệu lực (BE: UserModel.IsRejected).
   *  Optional: vài test mock / endpoint không kèm field này → tránh vỡ kiểu. */
  isRejected?: boolean;
  isFpt: boolean;
  photoStudentCardUrl?: string | null;
  /** True nếu là tài khoản được MỜI (judge/mentor) chưa kích hoạt — không phải thí sinh
   *  nộp hồ sơ để duyệt. BE: UserModel.IsTemporary. Optional để tránh vỡ kiểu ở mock cũ. */
  isTemporary?: boolean;
}

export interface UserListParams {
  search?: string;
  pageNumber?: number;
  pageSize?: number;
  /** Filter by approval state (e.g. false = chờ xét duyệt). */
  isApproved?: boolean;
  /** Filter by event — only users registered in this event. */
  eventId?: string;
  /** Filter to show only users who have submitted their profile (schoolId, etc). */
  hasSubmittedProfile?: boolean;
  sortBy?: string;
  isAscending?: boolean;
  isStudent?: boolean;
  isFpt?: boolean;
}

/** Matches backend CreateUserRequestModel. */
export interface CreateUserPayload {
  schoolId: string;
  studentCode?: string | null;
  email: string;
  password: string;
  fullName: string;
  isStudent: boolean;
  isAdmin: boolean;
}

/** Matches backend UpdateUserRequestModel. */
export interface UpdateUserPayload {
  schoolId?: string | null;
  studentCode?: string | null;
  fullName?: string | null;
  isStudent: boolean;
  isAdmin: boolean;
  isApproved: boolean;
  isFpt: boolean;
  photoStudentCardUrl?: string | null;
}

export const usersApi = {
  /** GET /api/Users?Search= — search users by email/name (requires auth). */
  search: async (query: string): Promise<UserSummary[]> => {
    const { data } = await apiClient.get<PagedResult<UserSummary>>("/Users", {
      params: { Search: query, PageNumber: 1, PageSize: 10 },
    });
    return data.data ?? [];
  },

  /** GET /api/Users — paged user list (requires auth). */
  list: async ({
    search = "",
    pageNumber = 1,
    pageSize = 50,
    isApproved,
    eventId,
    hasSubmittedProfile,
    sortBy = "CreatedTime",
    isAscending = false,
    isStudent,
    isFpt,
  }: UserListParams = {}): Promise<PagedResult<UserSummary>> => {
    const { data } = await apiClient.get<PagedResult<UserSummary>>("/Users", {
      params: {
        Search: search.trim() || undefined,
        IsApproved: isApproved,
        EventId: eventId || undefined,
        HasSubmittedProfile: hasSubmittedProfile,
        PageNumber: pageNumber,
        PageSize: pageSize,
        SortBy: sortBy,
        IsAscending: isAscending,
        IsStudent: isStudent,
        IsFpt: isFpt,
      },
    });
    return data;
  },

  /** POST /api/Users — create a user (admin only). */
  create: (payload: CreateUserPayload): Promise<void> =>
    apiClient.post("/Users", payload).then(() => undefined),

  /** PUT /api/Users/{id} — update a user (admin only). */
  update: (id: string, payload: UpdateUserPayload): Promise<void> =>
    apiClient.put(`/Users/${encodeURIComponent(id)}`, payload).then(() => undefined),

  /** DELETE /api/Users/{id} — soft-delete a user, i.e. remove their system access (admin only). */
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/Users/${encodeURIComponent(id)}`).then(() => undefined),

  /** GET /api/Users/profile — the current logged-in user's profile. */
  getProfile: async (): Promise<UserSummary> => {
    const { data } = await apiClient.get<UserSummary>("/Users/profile");
    return data;
  },

  /** POST /api/Users/{id}/approve — approve a student profile (admin/EC). */
  approve: (id: string): Promise<void> =>
    apiClient.post(`/Users/${encodeURIComponent(id)}/approve`).then(() => undefined),

  /** POST /api/Users/{id}/reject — reject a student profile with a reason (admin/EC). */
  reject: (id: string, reason: string): Promise<void> =>
    apiClient.post(`/Users/${encodeURIComponent(id)}/reject`, { reason }).then(() => undefined),
};
