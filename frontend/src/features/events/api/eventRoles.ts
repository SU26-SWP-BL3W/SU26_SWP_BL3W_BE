import { apiClient } from "@/services/api";
import type { PagedResult } from "@/services/api";

// ─── Models ───────────────────────────────────────────────────────────────────

/** Bản ghi vai trò của một user trong một event. */
export interface EventRoleModel {
  id: string;          // eventRoleId — dùng cho Scores/save
  userId: string;
  eventId: string;
  trackId: string | null;
  teamId: string | null;
  roleName: "Judge" | "EventCoordinator" | "TeamLeader" | "Member" | string;
  user?: {
    id: string | null;
    email: string | null;
    fullName: string | null;
  } | null;
}

// ─── API ──────────────────────────────────────────────────────────────────────

export const eventRolesApi = {
  /**
   * GET /api/EventRoles/user-role?UserId=&EventId=
   * Lấy eventRoleId + trackId của judge đang đăng nhập.
   * ⚠ eventRoleId ≠ userId — phải lấy từ endpoint này cho Scores/save.
   */
  getUserRole: async (userId: string, eventId: string): Promise<EventRoleModel | null> => {
    const { data } = await apiClient.get<EventRoleModel | null>("/EventRoles/user-role", {
      params: { UserId: userId, EventId: eventId },
    });
    return data || null;
  },

  /**
   * GET /api/EventRoles/event?EventId=&TrackId=&RoleName=
   * Danh sách bản ghi EventRole của tất cả judge trong một track.
   * Không dùng /event/role vì endpoint đó chỉ trả UserModel, thiếu EventRole.id
   * cần cho assignedJudgeId khi duyệt phúc khảo.
   */
  listByRole: async (
    eventId: string,
    trackId: string,
    roleName: string,
  ): Promise<EventRoleModel[]> => {
    const { data } = await apiClient.get<PagedResult<EventRoleModel>>(
      "/EventRoles/event",
      {
        params: {
          EventId: eventId,
          TrackId: trackId,
          RoleName: roleName,
          ActiveOnly: true,
          PageNumber: 1,
          PageSize: 200,
        },
      },
    );
    return data.data ?? [];
  },

  /**
   * GET /api/EventRoles/check?UserId=&EventId=&RoleName=
   * Kiểm tra user có phải judge không (true/false).
   */
  checkRole: async (userId: string, eventId: string, roleName: string): Promise<boolean> => {
    const { data } = await apiClient.get<boolean>("/EventRoles/check", {
      params: { UserId: userId, EventId: eventId, RoleName: roleName },
    });
    return data;
  },

  /**
   * GET /api/EventRoles/event?EventId=
   * Lấy toàn bộ vai trò trong 1 event — dùng để hiển thị role theo event trong bảng user.
   */
  listByEvent: async (eventId: string): Promise<EventRoleModel[]> => {
    const { data } = await apiClient.get<PagedResult<EventRoleModel>>(
      "/EventRoles/event",
      { params: { EventId: eventId, PageNumber: 1, PageSize: 500 } },
    );
    return data.data ?? [];
  },

  /**
   * Lấy TẤT CẢ bản ghi vai trò của 1 user trong 1 event (có thể nhiều dòng — vd Judge
   * được giao chấm nhiều track cùng lúc). `GET /EventRoles/user-role` chỉ trả về 1 bản
   * ghi nên không đủ cho trường hợp này; ở đây tận dụng `listByEvent` (trả toàn bộ role
   * của event) rồi lọc theo userId ở client, vì backend chưa có endpoint lọc theo cả
   * UserId lẫn trả về danh sách (chỉ có bản 1-object ở `getUserRole`).
   */
  listMyRoles: async (userId: string, eventId: string): Promise<EventRoleModel[]> => {
    const all = await eventRolesApi.listByEvent(eventId);
    return all.filter((r) => r.userId === userId);
  },
};
