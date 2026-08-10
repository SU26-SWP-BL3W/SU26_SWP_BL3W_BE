/**
 * Vòng đời trạng thái của đội (khớp enum `TeamStatus` phía Backend — xác nhận từ
 * `ConfirmTeamRegistrationCommandHandler`/`ApproveTeamRegistrationCommandHandler`):
 *
 *   Forming ──(confirm-registration)──► PendingApproval ──(approve-registration)──► Registered
 *                                              │
 *                                              └──(reject-registration, kèm reason)──► Forming
 *
 * `PendingApproval` không tự "hết hạn" hay có nhánh khác — chỉ thoát ra qua đúng 2
 * API approve/reject ở trên (do EventCoordinator gọi).
 */
export type TeamStatus = 'Forming' | 'PendingApproval' | 'Registered';

export interface TeamMember {
  userId: string;
  fullName: string;
  email: string;
  /** Best-effort: backend exposes a role string; team leaders contain "leader". */
  isLeader: boolean;
  /** MSSV — có sẵn trong Teams/{id}.members (BE trả kèm). */
  studentCode?: string | null;
  /** ID trường — dùng để join với schoolsApi.list() ra tên trường hiển thị.
   *  LƯU Ý: field này CHƯA có trong response Teams/{id}.members hiện tại (chỉ có
   *  ở EventRoles/event.user) — để optional, UI phải tự chấp nhận có thể rỗng. */
  schoolId?: string | null;
}

export interface TeamModel {
  id: string;
  teamName: string;
  description: string | null;
  members: TeamMember[];
  /** Lý do gần nhất đội bị EC/Admin từ chối duyệt, do Backend trả về. */
  lastRejectReason?: string | null;
  /** Trạng thái đội — xem TeamStatus. Để `string` dự phòng giá trị lạ/chưa map từ BE
   *  (vd trong lúc BE đang migrate dữ liệu cũ), FE luôn phải xử lý nhánh "không nhận
   *  diện được" thay vì ép kiểu cứng. */
  status?: TeamStatus | (string & {});
}

/** Matches backend CreateTeamRequestModel. */
export interface CreateTeamPayload {
  name: string;
  description: string;
  eventId: string;
  /** The creating student is the team leader. */
  leaderId: string;
}
