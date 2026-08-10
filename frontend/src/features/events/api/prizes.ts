import { apiClient } from '@/services/api';

export interface PrizeModel {
  id: string;
  eventId: string;
  prizeName: string;
  value: string;
  quantity: number;
}

export interface UpsertPrizePayload {
  prizeName: string;
  value: string;
  quantity: number;
}

export const prizesApi = {
  listByEvent: async (eventId: string): Promise<PrizeModel[]> => {
    const { data } = await apiClient.get<PrizeModel[]>(
      `/Events/${encodeURIComponent(eventId)}/Prizes`,
    );
    return data ?? [];
  },

  create: async (eventId: string, payload: UpsertPrizePayload): Promise<PrizeModel> => {
    const { data } = await apiClient.post<PrizeModel>(
      `/Events/${encodeURIComponent(eventId)}/Prizes`,
      payload,
    );
    return data;
  },

  update: async (id: string, payload: UpsertPrizePayload): Promise<void> => {
    await apiClient.put(`/Prizes/${encodeURIComponent(id)}`, payload);
  },

  remove: async (id: string): Promise<void> => {
    await apiClient.delete(`/Prizes/${encodeURIComponent(id)}`);
  },

  assignToResult: async (finalResultId: string, prizeId: string | null): Promise<void> => {
    await apiClient.patch(
      `/FinalResults/${encodeURIComponent(finalResultId)}/assign-prize`,
      { prizeId },
    );
  },
};
