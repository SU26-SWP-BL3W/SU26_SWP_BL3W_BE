import apiClient from "./client";
import type { PagedResult } from "./types";

// ─── Score types ──────────────────────────────────────────────────────────────

export interface ScoreDetail {
  id: string;
  scoreId?: string | null;
  templateId?: string | null;
  criteriaId: string | null;
  /** BE dùng `value` (không phải `score`) cho điểm từng tiêu chí. */
  value: number;
}

export interface Score {
  id: string;
  eventRoleId: string | null;
  submitResultId: string | null;
  totalScore: number;
  isSubmitted?: boolean;
  createdTime?: string | null;
  lastUpdatedTime?: string | null;
}

/** Returned by GET /api/Scores/{id}/detail */
export interface ScoreWithDetails extends Score {
  details: ScoreDetail[];
}

// ─── Team score breakdown types (GET /Scores/team/{teamId}/breakdown) ─────────
// Xác nhận qua Swagger — dùng cho chế độ xem read-only (Mentor/EC/Admin): xem điểm
// của TẤT CẢ giám khảo cho mọi bài nộp của 1 đội, không phân theo eventRoleId như
// listByEventRole (dành riêng cho Judge xem/sửa điểm của chính mình).

/** 1 dòng điểm của 1 tiêu chí trong breakdown (không có criteriaId, chỉ có tên). */
export interface CriterionScoreLine {
  criteriaName: string | null;
  value: number;
  maxScore: number;
  weight: number;
}

/** Điểm + nhận xét của 1 giám khảo cho 1 bài nộp. */
export interface JudgeScoreBreakdown {
  judgeName: string | null;
  totalScore: number;
  /** Nhận xét chung của giám khảo cho cả phiếu (không có nhận xét theo từng tiêu chí). */
  comment: string | null;
  criteria: CriterionScoreLine[] | null;
}

/** Điểm của 1 bài nộp (1 track/round), kèm điểm từ mọi giám khảo đã chấm. */
export interface SubmissionScoreBreakdown {
  submitResultId: string | null;
  trackName: string | null;
  roundId: string | null;
  roundName: string | null;
  roundPublished: boolean;
  judgeScores: JudgeScoreBreakdown[] | null;
}

/** Toàn bộ điểm của 1 đội, gộp mọi bài nộp (mọi track/round). */
export interface TeamScoreBreakdownModel {
  teamId: string | null;
  teamName: string | null;
  submissions: SubmissionScoreBreakdown[] | null;
}

// ─── Request payload types ────────────────────────────────────────────────────

export interface CreateScorePayload {
  eventRoleId: string;
  submitResultId: string;
}

/** POST /api/Scores/save — gộp tạo/cập nhật phiếu chấm + chi tiết 1 lần.
 *  Mỗi detail dùng `value` (khớp SaveScoreDetailItem của BE); `comment` là tùy chọn. */
export interface SaveScorePayload {
  eventRoleId: string;
  submitResultId: string;
  isSubmitted: boolean;
  comment?: string;
  details: { criteriaId: string; value: number; templateId?: string }[];
}

export interface CreateScoreDetailPayload {
  scoreId: string;
  criteriaId: string;
  value: number;
}

// ─── ScoreDetails API ─────────────────────────────────────────────────────────

export const scoreDetailsApi = {
  // POST /api/ScoreDetails
  create: async (payload: CreateScoreDetailPayload): Promise<ScoreDetail> => {
    const { data } = await apiClient.post<ScoreDetail>("/ScoreDetails", payload);
    return data;
  },

  // GET /api/ScoreDetails/{id}
  getById: async (id: string): Promise<ScoreDetail> => {
    const { data } = await apiClient.get<ScoreDetail>(`/ScoreDetails/${id}`);
    return data;
  },

  // PUT /api/ScoreDetails/{id}
  update: async (id: string, value: number): Promise<ScoreDetail> => {
    const { data } = await apiClient.put<ScoreDetail>(`/ScoreDetails/${id}`, { value });
    return data;
  },

  // DELETE /api/ScoreDetails/{id}
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/ScoreDetails/${id}`).then(() => undefined),

  // GET /api/ScoreDetails/score/{scoreId}
  listByScore: async (scoreId: string): Promise<ScoreDetail[]> => {
    const { data } = await apiClient.get<PagedResult<ScoreDetail>>(
      `/ScoreDetails/score/${scoreId}`,
    );
    return data.data ?? [];
  },
};

// ─── Scores API ───────────────────────────────────────────────────────────────

export const scoresApi = {
  // POST /api/Scores — tạo phiếu chấm mới (TotalScore = 0)
  create: async (payload: CreateScorePayload): Promise<Score> => {
    const { data } = await apiClient.post<Score>("/Scores", payload);
    return data;
  },

  // POST /api/Scores/save — gộp tạo/cập nhật phiếu chấm + toàn bộ chi tiết 1 lần.
  // TotalScore tự động tính lại; tiêu chí mới → thêm, đã có → cập nhật, không còn → xóa.
  save: async (payload: SaveScorePayload): Promise<Score> => {
    const { data } = await apiClient.post<Score>("/Scores/save", payload);
    return data;
  },

  // GET /api/Scores/{id}
  getById: async (id: string): Promise<Score> => {
    const { data } = await apiClient.get<Score>(`/Scores/${id}`);
    return data;
  },

  // PUT /api/Scores/{id}
  update: async (id: string, payload: Partial<CreateScorePayload>): Promise<Score> => {
    const { data } = await apiClient.put<Score>(`/Scores/${id}`, payload);
    return data;
  },

  // DELETE /api/Scores/{id} — cascade xóa toàn bộ ScoreDetail liên quan
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/Scores/${id}`).then(() => undefined),

  // GET /api/Scores/{id}/detail — lấy phiếu chấm kèm toàn bộ chi tiết 1 lần
  getWithDetails: async (id: string): Promise<ScoreWithDetails> => {
    const { data } = await apiClient.get<ScoreWithDetails>(`/Scores/${id}/detail`);
    return data;
  },

  // GET /api/Scores/event-role/{eventRoleId} — lấy danh sách phiếu chấm theo giám khảo
  listByEventRole: async (eventRoleId: string): Promise<Score[]> => {
    const { data } = await apiClient.get<PagedResult<Score>>(
      `/Scores/event-role/${eventRoleId}`,
      {
        params: {
          pageNumber: 1,
          pageSize: 200,
          sortBy: "CreatedTime",
          isAscending: false,
        },
      },
    );
    return data.data ?? [];
  },

  // GET /api/Scores/team/{teamId}/breakdown — điểm của TẤT CẢ giám khảo, mọi bài nộp
  // của 1 đội. Dùng cho chế độ xem read-only (Mentor/EC/Admin) — không tự chấm được,
  // chỉ xem lại điểm + nhận xét mà (các) giám khảo đã chấm.
  getTeamBreakdown: async (teamId: string): Promise<TeamScoreBreakdownModel> => {
    const { data } = await apiClient.get<TeamScoreBreakdownModel>(
      `/Scores/team/${encodeURIComponent(teamId)}/breakdown`,
    );
    return data;
  },
};
