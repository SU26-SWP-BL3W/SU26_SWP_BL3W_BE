export type EventStatus = "open" | "closed";

export interface Event {
  id: string;
  title: string;
  /** Mùa học (Spring/Summer/Fall) — suy từ ngày bắt đầu, chỉ để hiển thị. */
  season?: string | null;
  /** Năm tổ chức. */
  year?: number | null;
  startDate: string; // ISO 8601
  endDate: string;
  registrationStartDate?: string | null;
  registrationEndDate?: string | null;
  /** Số đội tối đa được phép tham gia sự kiện. */
  maxTeams?: number | null;
  status: EventStatus;
  description: string;
  photoEventUrl?: string | null;
  /** Vai trò của user hiện tại trong event — chỉ được gắn ở danh sách "Của tôi" (useMyEvents). */
  myRole?: string | null;
  prizes?: import("../api/events").PrizeModel[];
}
