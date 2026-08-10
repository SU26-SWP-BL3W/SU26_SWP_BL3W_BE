import apiClient from '@/services/api/client';
import type { UserRejectionModel } from '@/services/api';

export const userRejectionsApi = {
  /** GET /api/UserRejections/user/{userId} — rejections recorded against a user.
   *  BE trả BaseResponse<IEnumerable> (mảng phẳng); interceptor đã bóc .data nên `data` chính là mảng. */
  listForUser: async (userId: string): Promise<UserRejectionModel[]> => {
    const { data } = await apiClient.get<UserRejectionModel[]>(
      `/UserRejections/user/${encodeURIComponent(userId)}`,
    );
    return data ?? [];
  },

  /** DELETE /api/UserRejections/{id} — soft-delete a rejection record. */
  remove: (id: string): Promise<void> =>
    apiClient.delete(`/UserRejections/${encodeURIComponent(id)}`).then(() => undefined),
};
