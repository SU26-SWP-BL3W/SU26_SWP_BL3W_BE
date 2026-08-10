import { useState, type FormEvent } from 'react';
import { authApi } from '@/services/api';
import { useNotify } from '@/components/NotificationProvider';
import { getErrorMessage } from '@/lib/apiError';

interface Props {
  /** Gọi khi đổi mật khẩu thành công — component cha thường dùng để quay lại card trạng thái. */
  onSuccess: () => void;
  /** Nút "Quay lại" — quay về mà không cần đổi mật khẩu. */
  onBack: () => void;
}

const labelStyle = {
  fontSize: 11, fontWeight: 700,
  letterSpacing: '0.08em', textTransform: 'uppercase' as const,
  color: 'var(--color-mute)',
};

export function ChangePasswordForm({ onSuccess, onBack }: Props) {
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmNewPassword, setConfirmNewPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const notify = useNotify();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');

    if (!oldPassword.trim()) {
      setError('Vui lòng nhập mật khẩu hiện tại.');
      return;
    }
    if (!newPassword.trim()) {
      setError('Vui lòng nhập mật khẩu mới.');
      return;
    }
    if (newPassword !== confirmNewPassword) {
      setError('Mật khẩu mới và xác nhận mật khẩu không khớp.');
      return;
    }
    if (newPassword === oldPassword) {
      setError('Mật khẩu mới phải khác mật khẩu hiện tại.');
      return;
    }

    setBusy(true);
    try {
      await authApi.changePassword({ oldPassword, newPassword, confirmNewPassword });
      notify.success('Đã đổi mật khẩu thành công.');
      onSuccess();
    } catch (e) {
      // Lấy message thật từ backend (vd sai mật khẩu hiện tại, mật khẩu mới không đạt
      // yêu cầu độ mạnh...) thay vì text chung chung của axios.
      const msg = getErrorMessage(e, 'Đổi mật khẩu thất bại. Vui lòng thử lại.');
      setError(msg);
      notify.error(msg);
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4 max-w-[36rem]">
      {error && (
        <p className="t-caption-sm" style={{ color: 'var(--color-error)', margin: 0 }}>
          {error}
        </p>
      )}

      <label className="flex flex-col gap-1.5">
        <span style={labelStyle}>Mật khẩu hiện tại</span>
        <input
          type="password"
          className="text-input"
          value={oldPassword}
          onChange={(e) => setOldPassword(e.target.value)}
          autoComplete="current-password"
        />
      </label>

      <label className="flex flex-col gap-1.5">
        <span style={labelStyle}>Mật khẩu mới</span>
        <input
          type="password"
          className="text-input"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          autoComplete="new-password"
        />
      </label>

      <label className="flex flex-col gap-1.5">
        <span style={labelStyle}>Xác nhận mật khẩu mới</span>
        <input
          type="password"
          className="text-input"
          value={confirmNewPassword}
          onChange={(e) => setConfirmNewPassword(e.target.value)}
          autoComplete="new-password"
        />
      </label>

      <div className="flex items-center gap-3">
        <button
          type="button"
          className="btn btn-secondary w-fit"
          onClick={onBack}
          disabled={busy}
        >
          ← Quay lại
        </button>
        <button type="submit" className="btn btn-primary w-fit" disabled={busy}>
          {busy ? 'Đang xử lý…' : 'Đổi mật khẩu'}
        </button>
      </div>
    </form>
  );
}
