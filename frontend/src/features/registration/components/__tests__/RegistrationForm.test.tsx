import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { RegistrationForm } from '../RegistrationForm';
import { NotificationProvider } from '@/components/NotificationProvider';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// Mock @/services/api so network calls never fire in tests.
jest.mock('@/services/api', () => ({
  schoolsApi: {
    list: jest.fn().mockResolvedValue({
      data: [{ id: 'fpt-1', schoolName: 'FPT University' }],
    }),
    create: jest.fn(),
  },
  storageApi: {
    upload: jest.fn().mockResolvedValue('https://cdn.example.com/card.jpg'),
  },
}));

const renderForm = (ui: React.ReactElement) => {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <NotificationProvider>{ui}</NotificationProvider>
    </QueryClientProvider>,
  );
};

describe('RegistrationForm', () => {
  test('FPT path: submits UpdateStudentProfileCommand with resolved schoolId', async () => {
    const onSubmit = jest.fn();
    renderForm(<RegistrationForm defaults={{ fullName: 'Nguyễn Văn A' }} onSubmit={onSubmit} />);

    expect((screen.getByLabelText(/Họ và tên/i) as HTMLInputElement).value).toBe('Nguyễn Văn A');

    fireEvent.change(screen.getByLabelText(/Mã số sinh viên/i), { target: { value: 'SE123' } });
    fireEvent.click(screen.getByRole('button', { name: /Gửi đăng ký/i }));

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        isFpt: true,
        schoolId: 'fpt-1',
        studentCode: 'SE123',
        photoStudentCardUrl: null,
        fullName: 'Nguyễn Văn A',
      }),
    );
  });

  test('blocks submit when MSSV empty', () => {
    const onSubmit = jest.fn();
    renderForm(<RegistrationForm defaults={{ fullName: 'A' }} onSubmit={onSubmit} />);
    fireEvent.click(screen.getByRole('button', { name: /Gửi đăng ký/i }));
    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getByText(/Vui lòng nhập mã số sinh viên/i)).toBeInTheDocument();
  });
});
