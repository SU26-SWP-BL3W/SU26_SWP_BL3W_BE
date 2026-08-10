import { apiClient } from "@/services/api";
import type { PagedResult } from "@/services/api";

// ─── Response models ──────────────────────────────────────────────────────────

/** Item trong danh sách bài nộp (GET /api/SubmitResults). */
export interface SubmitResultListItem {
  id: string;
  teamId: string;
  teamName?: string;        // optional — backend chưa trả
  trackId: string;
  submissionUrl?: string;   // ← field thật
  repoUrl?: string;         // giữ để tương thích
  projectName?: string;     // optional — backend chưa trả
  isActive: boolean;        // ← field thật
  createdTime?: string;     // ← field thật
  submittedAt?: string;     // giữ để tương thích
  status?: string;
  roundId?: string;
}

/** Chi tiết bài nộp (GET /api/SubmitResults/{id}). */
export interface SubmitResultDetail extends SubmitResultListItem {
  description?: string | null;
  demoUrl?: string | null;
  updatedAt?: string | null;
}

// ─── Request models ───────────────────────────────────────────────────────────

/** POST /api/SubmitResults — nộp bài lần đầu cho một Track. */
export interface CreateSubmitResultRequest {
  teamId: string;
  trackId: string;
  projectName: string;
  repoUrl: string;
  description?: string;
  demoUrl?: string;
}

/** PUT /api/SubmitResults/{id} — cập nhật bài nộp đã có. */
export interface UpdateSubmitResultRequest {
  projectName?: string;
  repoUrl?: string;
  description?: string;
  demoUrl?: string;
}

export interface ListSubmitResultParams {
  eventId?: string;
  teamId?: string;
  roundId?: string;
  trackId?: string;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  isAscending?: boolean;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export const submitResultsApi = {
  // GET /api/SubmitResults — danh sách bài nộp (Ban tổ chức / Giám khảo)
  list: async (params?: ListSubmitResultParams): Promise<SubmitResultListItem[]> => {
    const { data } = await apiClient.get<PagedResult<SubmitResultListItem>>("/SubmitResults", {
      params: {
        EventId:     params?.eventId,
        TeamId:      params?.teamId,
        RoundId:     params?.roundId,
        TrackId:     params?.trackId,
        PageNumber:  params?.pageNumber ?? 1,
        PageSize:    params?.pageSize   ?? 100,
        SortBy:      params?.sortBy,
        IsAscending: params?.isAscending,
      },
    });
    return data.data ?? [];
  },

  // GET /api/SubmitResults/{id} — chi tiết bài nộp
  getById: async (id: string): Promise<SubmitResultDetail> => {
    const { data } = await apiClient.get<SubmitResultDetail>(
      `/SubmitResults/${encodeURIComponent(id)}`,
    );
    return data;
  },

  // POST /api/SubmitResults — nộp bài lần đầu (teamId + trackId bắt buộc)
  create: async (body: CreateSubmitResultRequest): Promise<SubmitResultDetail> => {
    const { data } = await apiClient.post<SubmitResultDetail>("/SubmitResults", body);
    return data;
  },

  // PUT /api/SubmitResults/{id} — cập nhật bài nộp (TeamLeader / EventCoordinator)
  update: async (id: string, body: UpdateSubmitResultRequest): Promise<SubmitResultDetail> => {
    const { data } = await apiClient.put<SubmitResultDetail>(
      `/SubmitResults/${encodeURIComponent(id)}`,
      body,
    );
    return data;
  },

  // DELETE /api/SubmitResults/{id} — xóa bài nộp (TeamLeader / EventCoordinator)
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/SubmitResults/${encodeURIComponent(id)}`).then(() => undefined),
};
