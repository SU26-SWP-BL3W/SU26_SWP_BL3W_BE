import { apiClient } from '@/services/api';
import type { PagedResult } from '@/services/api';

export type AppealStatus = number | 'Pending' | 'Approved' | 'Rejected' | string;

export interface AppealModel {
  id: string;
  submitResultId: string;
  teamId?: string | null;
  teamName?: string | null;
  reason: string;
  response?: string | null;
  status: AppealStatus;
  statusLabel?: string | null;
  createdTime?: string | null;
  createdAt?: string | null;
  lastUpdatedTime?: string | null;
  assignedJudgeId?: string | null;
}

export interface AppealPage {
  items: AppealModel[];
  pageIndex: number;
  totalPages: number;
  totalItems: number;
}

const toPage = (data: PagedResult<AppealModel>, fallbackPage: number): AppealPage => ({
  items: data?.data ?? [],
  pageIndex: data?.currentPage ?? fallbackPage,
  totalPages: data?.totalPages ?? 1,
  totalItems: data?.totalItems ?? data?.data?.length ?? 0,
});

export const appealsApi = {
  listByRound: async (roundId: string, pageIndex = 1, pageSize = 20): Promise<AppealPage> => {
    const { data } = await apiClient.get<PagedResult<AppealModel>>(
      `/Appeals/round/${encodeURIComponent(roundId)}`,
      { params: { PageNumber: pageIndex, PageSize: pageSize, SortBy: 'CreatedTime', IsAscending: false } },
    );
    return toPage(data, pageIndex);
  },

  listByTeam: async (teamId: string, pageIndex = 1, pageSize = 20): Promise<AppealPage> => {
    const { data } = await apiClient.get<PagedResult<AppealModel>>(
      `/Appeals/team/${encodeURIComponent(teamId)}`,
      { params: { PageNumber: pageIndex, PageSize: pageSize, SortBy: 'CreatedTime', IsAscending: false } },
    );
    return toPage(data, pageIndex);
  },

  create: async (payload: { submitResultId: string; reason: string }): Promise<AppealModel> => {
    const { data } = await apiClient.post<AppealModel>('/Appeals', payload);
    return data;
  },

  respond: async (
    id: string,
    payload: { status: 1 | 2; response: string; assignedJudgeId?: string },
  ): Promise<AppealModel> => {
    const { data } = await apiClient.put<AppealModel>(
      `/Appeals/${encodeURIComponent(id)}/respond`,
      { appealId: id, ...payload },
    );
    return data;
  },

  getAssignedAppeals: async (eventRoleId: string): Promise<AppealModel[]> => {
    const { data } = await apiClient.get<PagedResult<AppealModel> | AppealModel[]>(
      `/Appeals/assigned/${encodeURIComponent(eventRoleId)}`,
    );
    return Array.isArray(data) ? data : (data?.data ?? []);
  },
};
