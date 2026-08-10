import { apiClient } from "@/services/api";

// ─── Round / Track upsert payloads (match backend Create/Update models) ─────────

export interface RoundUpsertPayload {
  eventId: string;
  roundName: string;
  roundNumber: number;
  startDate: string; // ISO 8601
  endDate: string;
  advancementRule: string;
}

export interface TrackUpsertPayload {
  eventId: string;
  roundId: string;
  trackName: string;
  templateId: string | null;
  description: string;
  /**
   * Serialized submission requirements. Persisted by PUT /Tracks/{id}
   * (UpdateTrackRequestModel). POST /Tracks (CreateTrackRequestModel) does not
   * yet accept it, so it's ignored when creating a new track until the backend
   * adds the field there too.
   */
  submissionRuleDescription: string;
  // ── Hai khoảng thời gian thực của hạng mục (BE: Create/UpdateTrackRequestModel). ──
  /** Nộp bài: mở nộp. */
  startDate?: string | null;
  /** Nộp bài: hạn chót nộp. */
  endDate?: string | null;
  /** Chấm điểm: mở → đóng cửa sổ chấm. */
  scoringStartDate?: string | null;
  scoringEndDate?: string | null;
}

/** Full round model returned by GET /Rounds/{id}. */
export interface RoundModel {
  id: string;
  eventId: string | null;
  roundName: string | null;
  roundNumber: number;
  startDate: string; // ISO 8601
  endDate: string;   // ISO 8601 — hạn nộp bài; giám khảo chỉ chấm sau mốc này
  advancementRule: string | null;
}

/** Full track model returned by GET /Tracks/{id}. */
export interface TrackModel {
  id: string;
  trackName: string;
  eventId: string;
  roundId: string | null;
  templateId: string | null;
  description: string | null;
  isActive: boolean;
  startDate?: string | null;
  endDate?: string | null;
  scoringStartDate?: string | null;
  scoringEndDate?: string | null;
}

export const roundsApi = {
  create: async (payload: RoundUpsertPayload): Promise<{ id: string }> => {
    const { data } = await apiClient.post<{ id: string }>("/Rounds", payload);
    return data;
  },
  update: (id: string, payload: RoundUpsertPayload): Promise<void> =>
    apiClient.put(`/Rounds/${encodeURIComponent(id)}`, payload).then(() => undefined),
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/Rounds/${encodeURIComponent(id)}`).then(() => undefined),

  /** GET /api/Rounds/{id} — chi tiết vòng thi (endDate dùng để khóa/mở form chấm). */
  getById: async (id: string): Promise<RoundModel> => {
    const { data } = await apiClient.get<RoundModel>(`/Rounds/${encodeURIComponent(id)}`);
    return data;
  },
};

export const tracksApi = {
  create: async (payload: TrackUpsertPayload): Promise<{ id: string }> => {
    const { data } = await apiClient.post<{ id: string }>("/Tracks", payload);
    return data;
  },
  update: (id: string, payload: TrackUpsertPayload): Promise<void> =>
    apiClient.put(`/Tracks/${encodeURIComponent(id)}`, payload).then(() => undefined),
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/Tracks/${encodeURIComponent(id)}`).then(() => undefined),

  /** GET /api/Tracks/{id} — track detail including templateId. */
  getById: async (id: string): Promise<TrackModel> => {
    const { data } = await apiClient.get<TrackModel>(`/Tracks/${encodeURIComponent(id)}`);
    return data;
  },

  /** PATCH /api/Tracks/{id}/assign-template — gán template vào track. */
  assignTemplate: (trackId: string, templateId: string): Promise<void> =>
    apiClient
      .patch(`/Tracks/${encodeURIComponent(trackId)}/assign-template`, { templateId })
      .then(() => undefined),
};
