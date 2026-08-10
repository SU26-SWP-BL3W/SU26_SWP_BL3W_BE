import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { RegistrationStatusCard } from '../RegistrationStatusCard';
import type { UserSummary } from '@/services/api';

// Mock schoolsApi so useQuery in the component resolves immediately
jest.mock('@/services/api', () => ({
  schoolsApi: {
    list: jest.fn().mockResolvedValue({ data: [], currentPage: 1, pageSize: 100, totalItems: 0, totalPages: 1, hasPreviousPage: false, hasNextPage: false }),
  },
}));

const profile: UserSummary = {
  id: 'u1',
  email: 'a@e.com',
  fullName: 'Nguyễn Văn A',
  isStudent: true,
  isAdmin: false,
  isApproved: false,
  isFpt: true,
  studentCode: 'SE123',
  schoolId: 'fpt-1',
  photoStudentCardUrl: null,
};

const noop = jest.fn();

function withQuery(ui: React.ReactElement) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}>{ui}</QueryClientProvider>);
}

describe('RegistrationStatusCard', () => {
  test('pending shows Chờ xét duyệt + Cập nhật hồ sơ button (onEdit)', () => {
    const onEdit = jest.fn();
    withQuery(
      <RegistrationStatusCard
        status="pending"
        reason={null}
        profile={profile}
        onChangePassword={noop}
        onEdit={onEdit}
        onResubmit={noop}
      />,
    );
    expect(screen.getByText(/Chờ xét duyệt/i)).toBeInTheDocument();
    expect(screen.getByText('SE123')).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Cập nhật hồ sơ/i }));
    expect(onEdit).toHaveBeenCalled();
  });

  test('approved shows Đã được duyệt + Cập nhật hồ sơ (onEdit) + Đổi mật khẩu (onChangePassword)', () => {
    const onEdit = jest.fn();
    const onChangePassword = jest.fn();
    withQuery(
      <RegistrationStatusCard
        status="approved"
        reason={null}
        profile={profile}
        onChangePassword={onChangePassword}
        onEdit={onEdit}
        onResubmit={noop}
      />,
    );
    expect(screen.getByText(/Đã được duyệt/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Cập nhật hồ sơ/i }));
    expect(onEdit).toHaveBeenCalled();
    fireEvent.click(screen.getByRole('button', { name: /Đổi mật khẩu/i }));
    expect(onChangePassword).toHaveBeenCalled();
  });

  test('rejected shows Tài khoản bị từ chối + reason + Gửi lại (onResubmit)', () => {
    const onResubmit = jest.fn();
    withQuery(
      <RegistrationStatusCard
        status="rejected"
        reason="Ảnh thẻ không rõ"
        profile={profile}
        onChangePassword={noop}
        onEdit={noop}
        onResubmit={onResubmit}
      />,
    );
    expect(screen.getByText(/Tài khoản bị từ chối/i)).toBeInTheDocument();
    expect(screen.getByText(/Ảnh thẻ không rõ/i)).toBeInTheDocument();
    fireEvent.click(screen.getByRole('button', { name: /Gửi lại/i }));
    expect(onResubmit).toHaveBeenCalled();
  });
});
