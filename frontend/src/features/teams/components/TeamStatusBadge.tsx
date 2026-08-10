import { StatusBadge, type StatusTone } from "@/components/StatusBadge";
import type { TeamStatus } from "@/features/teams/types/team.types";

/** Badge dùng chung cho đúng ba trạng thái TeamStatus do backend trả về. */
export function TeamStatusBadge({ status }: { status?: TeamStatus | string }) {
  const statuses: Record<string, { label: string; tone: StatusTone }> = {
    Forming: {
      label: "Đang lập đội",
      tone: "neutral",
    },
    PendingApproval: {
      label: "Đang chờ duyệt",
      tone: "pending",
    },
    Registered: {
      label: "Đã duyệt",
      tone: "success",
    },
  };
  const current = (status && statuses[status]) || {
    label: status || "—",
    tone: "neutral" as StatusTone,
  };

  return <StatusBadge tone={current.tone}>{current.label}</StatusBadge>;
}
