import { apiClient } from "@/services/api";
import type { PagedResult } from "@/services/api";
import type { Event } from "../types/event.types";
import { normalizeSeason } from "@/lib/events/season";

// ─── Backend list model (CreateEventResponse / EventModel) ─────────────────────

/** Matches backend EventModel returned by GET /api/Events. */
export interface EventModel {
  id: string;
  eventName: string | null;
  season: string | null;
  year: number;
  startDate: string;
  endDate: string;
  description: string | null;
  createdTime: string;
  lastUpdatedTime: string;
  status?: boolean;
  photoEventUrl?: string | null;
  registrationStartDate?: string | null;
  registrationEndDate?: string | null;
  maxTeams?: number | null;
  prizes?: PrizeModel[];
}

export interface PrizeModel {
  id: string;
  eventId: string;
  prizeName: string;
  value: string;
  quantity: number;
}

/** Map a backend EventModel to the card UI `Event` shape. */
export function toUiEvent(e: EventModel): Event {
  // Use backend status if present, otherwise derive it from the end date.
  const open = e.status !== undefined ? e.status : new Date(e.endDate).getTime() >= Date.now();
  return {
    id: e.id,
    title: e.eventName?.trim() || "(Chưa đặt tên)",
    season: normalizeSeason(e.season),
    year: e.year ?? null,
    startDate: e.startDate,
    endDate: e.endDate,
    registrationStartDate: e.registrationStartDate ?? null,
    registrationEndDate: e.registrationEndDate ?? null,
    maxTeams: e.maxTeams ?? null,
    status: open ? "open" : "closed",
    description: e.description ?? "",
    photoEventUrl: e.photoEventUrl ?? null,
    prizes: e.prizes,
  };
}

// ─── Create-event payload (matches backend CreateEventRequestModel) ────────────

/** Track inside a round — matches TrackRequestDto. */
export interface CreateTrackPayload {
  trackName: string;
  description: string;
  /** Template GUID, or null when no template is selected (avoids a 500). */
  templateId: string | null;
  submissionRuleDescription: string;
  // ── Hai khoảng thời gian thực của hạng mục (BE: TrackRequestDto). ──
  /** Nộp bài: mở → hạn nộp. */
  startDate?: string | null;
  endDate?: string | null;
  /** Chấm điểm: mở → đóng cửa sổ chấm. */
  scoringStartDate?: string | null;
  scoringEndDate?: string | null;
}

/** Round inside an event — matches RoundRequestDto. */
export interface CreateRoundPayload {
  roundName: string;
  roundNumber: number;
  startDate: string; // ISO 8601
  endDate: string;
  advancementRule: string;
  tracks: CreateTrackPayload[];
}

/** Full create-event body — matches CreateEventRequestModel. */
export interface CreateEventPayload {
  eventName: string;
  season: string;
  year: number;
  startDate: string; // ISO 8601
  endDate: string;
  description: string;
  status: boolean;
  photoEventUrl?: string | null;
  registrationStartDate?: string | null;
  registrationEndDate?: string | null;
  maxTeams?: number;
  rounds: CreateRoundPayload[];
}

/** Full update-event body — matches UpdateEventRequestModel (event-level only). */
export interface UpdateEventPayload {
  eventName: string;
  season: string;
  year: number;
  startDate: string; // ISO 8601
  endDate: string;
  description: string;
  status: boolean;
  photoEventUrl?: string | null;
  registrationStartDate?: string | null;
  registrationEndDate?: string | null;
  maxTeams: number;
}

/** Subset of CreateEventResponseModel we care about. */
export interface CreateEventResponse {
  id: string;
  eventName: string;
  season: string;
  year: number;
  startDate: string;
  endDate: string;
  description: string;
  createdTime: string;
  photoEventUrl?: string | null;
  maxTeams?: number | null;
}

export const eventsApi = {
  /** GET /api/Events — list events, mapped to the card UI shape. */
  list: async (status?: boolean): Promise<Event[]> => {
    const params: Record<string, string | number | boolean> = {
      PageNumber: 1,
      PageSize: 100,
      SortBy: "eventName",
      IsAscending: true,
    };
    if (status !== undefined) {
      params.Status = status;
    }
    const { data } = await apiClient.get<PagedResult<EventModel>>("/Events", {
      params,
    });
    return (data.data ?? []).map(toUiEvent);
  },

  /** GET /api/Events/{id} — raw backend model (for prefilling the edit form). */
  getModelById: async (id: string): Promise<EventModel> => {
    const { data } = await apiClient.get<EventModel>(
      `/Events/${encodeURIComponent(id)}`,
    );
    return { ...data, season: normalizeSeason(data.season) };
  },

  /** PUT /api/Events/{id} — update event-level fields. */
  update: async (id: string, payload: UpdateEventPayload): Promise<EventModel> => {
    const { data } = await apiClient.put<EventModel>(
      `/Events/${encodeURIComponent(id)}`,
      payload,
    );
    return { ...data, season: normalizeSeason(data.season) };
  },

  join: (id: string): Promise<void> =>
    apiClient.post(`/events/${id}/join`).then(() => undefined),

  /** POST /api/Events — requires an authenticated (admin) Bearer token. */
  create: (payload: CreateEventPayload): Promise<CreateEventResponse> =>
    apiClient.post<CreateEventResponse>("/Events", payload).then((r) => r.data),

  /** DELETE /api/Events/{id} — deletes an event. */
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/Events/${encodeURIComponent(id)}`).then(() => undefined),
};
