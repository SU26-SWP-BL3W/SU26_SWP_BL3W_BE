import type { AppRole } from '@/hooks/useUserRole';
import type { RegistrationStatus } from '@/features/registration/types';

export type EventTabId =
  | 'detail' | 'register' | 'createTeam' | 'myTeam' | 'submission'
  | 'results' | 'leaderboard' | 'judgeAssigned' | 'manage'
  | 'reviewSubmission' | 'appeal';

export interface EventTab { id: EventTabId; label: string; }

const TAB: Record<EventTabId, EventTab> = {
  detail:           { id: 'detail',           label: 'Chi tiết sự kiện' },
  register:         { id: 'register',          label: 'Hồ sơ cá nhân' },
  createTeam:       { id: 'createTeam',        label: 'Tạo đội' },
  myTeam:           { id: 'myTeam',            label: 'Đội của tôi' },
  submission:       { id: 'submission',        label: 'Nộp bài' },
  results:          { id: 'results',           label: 'Kết quả' },
  appeal:           { id: 'appeal',            label: 'Phúc khảo' },
  leaderboard:      { id: 'leaderboard',       label: 'Bảng xếp hạng' },
  judgeAssigned:    { id: 'judgeAssigned',     label: 'Đội được phân công' },
  manage:           { id: 'manage',            label: 'Quản lý' },
  // Gộp "Chấm điểm" + "Bài nộp" thành 1 tab — Judge thấy nút chấm, Mentor chỉ xem
  // (xem SubmissionsScoringPanel.jsx).
  reviewSubmission: { id: 'reviewSubmission',  label: 'Bài nộp' },
};

export function getEventTabs(args: {
  isAuthenticated: boolean;
  role: AppRole | null;
  eventRoleName?: string;
  hasTeam: boolean;
  registrationStatus?: RegistrationStatus | null;
}): EventTab[] {
  const { isAuthenticated, role, eventRoleName, hasTeam, registrationStatus = null } = args;

  if (!isAuthenticated) {
    return [TAB.detail];
  }

  // 1. Admin hệ thống, hoặc được phân công làm EventCoordinator/Admin trong event
  if (role === 'admin' || eventRoleName === 'EventCoordinator' || eventRoleName === 'Admin') {
    return [TAB.detail, TAB.manage];
  }

  // 2. Chỉ khi thực sự được phân làm Judge trong event này
  //    Chấm điểm & Bài nộp chỉ thấy của hạng mục (track) được giao.
  if (eventRoleName === 'Judge') {
    return [TAB.detail, TAB.reviewSubmission, TAB.leaderboard];
  }

  // 3. Mentor trong event này — Bài nộp chỉ thấy của hạng mục được phân công.
  if (eventRoleName === 'Mentor') {
    return [TAB.detail, TAB.reviewSubmission, TAB.leaderboard];
  }

  // 4. Có vai trò đội (Thí sinh đã có đội)
  if (eventRoleName === 'TeamLeader' || eventRoleName === 'TeamMember' || hasTeam) {
    return [TAB.detail, TAB.register, TAB.myTeam, TAB.submission, TAB.results, TAB.appeal, TAB.leaderboard];
  }

  // 5. Thí sinh tự do hoặc người dùng thông thường chưa có đội
  if (role === 'student') {
    // Tạo đội chỉ mở khi đã được duyệt.
    return registrationStatus === 'approved'
      ? [TAB.detail, TAB.register, TAB.createTeam, TAB.leaderboard]
      : [TAB.detail, TAB.register];
  }

  // Mặc định (ví dụ MENTOR toàn cục nhưng không tham gia event này)
  // Hiện thêm tab Tạo đội để hướng dẫn người dùng thường/chưa là sinh viên cập nhật hồ sơ
  return [TAB.detail, TAB.createTeam];
}
