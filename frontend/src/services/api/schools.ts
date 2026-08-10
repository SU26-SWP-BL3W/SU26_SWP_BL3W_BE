import apiClient from "./client";
import type { PagedResult, SchoolModel } from "./types";

const SCHOOLS_URL = "/Schools";

// export const schoolsApi = {
//   list: async (
//     pageSize = 100,
//     pageNumber = 1,
//   ): Promise<PagedResult<SchoolModel>> => {
//     const { data } = await apiClient.get<PagedResult<SchoolModel>>(SCHOOLS_URL, {
//       params: { PageNumber: pageNumber, PageSize: pageSize },
//     });
//     return data;
//   },
//
//   create: async (payload: { schoolName: string; address?: string }): Promise<SchoolModel> => {
//     const { data } = await apiClient.post<SchoolModel>(SCHOOLS_URL, payload);
//     return data;
//   },
// };

export interface SchoolWithUserCount extends SchoolModel {
  userCount: number;
}

export const schoolsApi = {
  /** GET /api/Schools — danh sách trường (phân trang) */
  list: async (
    pageSize = 100,
    pageNumber = 1,
  ): Promise<PagedResult<SchoolModel>> => {
    const { data } = await apiClient.get<PagedResult<SchoolModel>>(SCHOOLS_URL, {
      params: { PageNumber: pageNumber, PageSize: pageSize },
    });
    return data;
  },

  /** GET /api/Schools/{id} — chi tiết 1 trường */
  getById: async (id: string): Promise<SchoolModel> => {
    const { data } = await apiClient.get<SchoolModel>(`${SCHOOLS_URL}/${id}`);
    return data;
  },

  /** GET /api/Schools/with-user-count — danh sách kèm số lượng user */
  withUserCount: async (): Promise<SchoolWithUserCount[]> => {
    const { data } = await apiClient.get<SchoolWithUserCount[]>(
      `${SCHOOLS_URL}/with-user-count`,
    );
    return data;
  },

  /** POST /api/Schools — tạo trường mới */
  create: async (payload: { schoolName: string; address?: string }): Promise<SchoolModel> => {
    const { data } = await apiClient.post<SchoolModel>(SCHOOLS_URL, payload);
    return data;
  },

  /** PUT /api/Schools/{id} — cập nhật trường */
  update: async (
    id: string,
    payload: { schoolName?: string; address?: string },
  ): Promise<SchoolModel> => {
    const { data } = await apiClient.put<SchoolModel>(`${SCHOOLS_URL}/${id}`, payload);
    return data;
  },

  /** DELETE /api/Schools/{id} — xóa trường (soft delete) */
  delete: async (id: string): Promise<boolean> => {
    const { data } = await apiClient.delete<boolean>(`${SCHOOLS_URL}/${id}`);
    return data;
  },
};
