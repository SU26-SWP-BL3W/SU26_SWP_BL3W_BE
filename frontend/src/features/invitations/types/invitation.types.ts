export type InvitationType = 'TEAM' | 'EVENT_ROLE' | 'TEAM_LEADER_TRANSFER';

export type InvitationStatus = 'PendingAccept' | 'Accepted' | 'Declined';

export interface GlobalInvitation {
  invitationId: string;
  /** 'TEAM' = mời vào đội · 'TEAM_LEADER_TRANSFER' = yêu cầu chuyển quyền trưởng nhóm · 'EVENT_ROLE' = mời vai trò sự kiện */
  type: InvitationType;
  targetId: string;       // teamId hoặc eventId
  targetName: string;     // Tên đội hoặc Tên sự kiện
  inviterName?: string;
  role: string;           // MEMBER, JUDGE, MENTOR, EC, Trưởng nhóm...
  trackName?: string | null; // Tên hạng mục (chỉ có với Judge/Mentor theo hạng mục)
  status?: InvitationStatus; // PendingAccept => còn nút; Accepted/Declined => lịch sử
  respondedAt?: string | null;
  expiresAt: string;
}

export interface MyInvitationsResponse {
  totalPending: number;
  invitations: GlobalInvitation[];
}
