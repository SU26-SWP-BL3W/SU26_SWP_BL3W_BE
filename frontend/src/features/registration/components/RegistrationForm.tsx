import { useMemo, useRef, useState, useEffect, type ChangeEvent, type FormEvent } from 'react';
import { useQuery } from '@tanstack/react-query';
import { storageApi, schoolsApi, type UpdateStudentProfileCommand } from '@/services/api';
import { useNotify } from '@/components/NotificationProvider';
import { getErrorMessage } from '@/lib/apiError';

interface Props {
  defaults: {
    fullName: string;
    studentCode?: string;
    isFpt?: boolean;
    schoolId?: string | null;
    photoStudentCardUrl?: string | null;
  };
  onSubmit: (cmd: UpdateStudentProfileCommand) => void | Promise<void>;
  /** Nút "Quay lại" cạnh nút gửi — chỉ hiện khi component cha truyền vào (vd khi mở form
   *  này từ trang hồ sơ, để quay lại card trạng thái mà không cần submit). */
  onBack?: () => void;
}

// ── OLD VERSION (commented out) ───────────────────────────────────────────────
// export function RegistrationForm({ defaults, onSubmit }: Props) {
//   const [fullName, setFullName] = useState(defaults.fullName);
//   const [schoolChoice, setSchoolChoice] = useState<'FPT' | 'OTHER'>(
//     defaults.isFpt !== false ? 'FPT' : 'OTHER',
//   );
//   const [schoolName, setSchoolName] = useState('');   // ← chỉ là text input đơn giản
//   const [studentCode, setStudentCode] = useState(defaults.studentCode ?? '');
//   ...
//   useEffect(() => {
//     if (defaults.schoolId && schoolChoice === 'OTHER') {
//       schoolsApi.list(1000).then((res) => {
//         const school = res.data.find((s) => s.id === defaults.schoolId);
//         if (school) setSchoolName(school.schoolName);
//       }).catch(() => {});
//     }
//   }, [defaults.schoolId, schoolChoice]);
//   ...
//   // Resolve schoolId khi submit: POST /Schools nếu chưa có → tìm theo tên
// }
// ─────────────────────────────────────────────────────────────────────────────

export function RegistrationForm({ defaults, onSubmit, onBack }: Props) {
  const [fullName, setFullName] = useState(defaults.fullName);
  const [schoolChoice, setSchoolChoice] = useState<'FPT' | 'OTHER'>(
    defaults.isFpt !== false ? 'FPT' : 'OTHER',
  );
  // search text người dùng gõ
  const [schoolSearch, setSchoolSearch] = useState('');
  // school đã chọn từ dropdown
  const [selectedSchool, setSelectedSchool] = useState<
    | { id: string; schoolName: string }
    | null
  >(null);
  const [showDropdown, setShowDropdown] = useState(false);

  const [studentCode, setStudentCode] = useState(defaults.studentCode ?? '');
  const [card, setCard] = useState<{ preview: string; file?: File } | null>(
    defaults.photoStudentCardUrl ? { preview: defaults.photoStudentCardUrl } : null,
  );
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const notify = useNotify();


  const needsCard = schoolChoice === 'OTHER';

  // ── Load danh sách trường từ DB ──────────────────────────────────────────
  const { data: schoolsData } = useQuery({
    queryKey: ['schools'],
    queryFn: () => schoolsApi.list(1000),
    staleTime: 5 * 60_000,
    enabled: schoolChoice === 'OTHER',
  });
  const allSchools = useMemo(() => schoolsData?.data ?? [], [schoolsData]);

  // Filter theo search text
  const filtered = schoolSearch.trim().length === 0
    ? allSchools.slice(0, 8)
    : allSchools.filter((s) =>
      s.schoolName.toLowerCase().includes(schoolSearch.toLowerCase()),
    ).slice(0, 8);

  // ── Khi load lần đầu, điền tên trường cũ vào ────────────────────────────
  useEffect(() => {
    const timer = window.setTimeout(() => {
      if (defaults.schoolId && schoolChoice === 'OTHER' && allSchools.length > 0) {
        const school = allSchools.find((s) => s.id === defaults.schoolId);
        if (school) {
          setSelectedSchool(school);
          setSchoolSearch(school.schoolName);
        }
      }
    }, 0);
    return () => window.clearTimeout(timer);
  }, [defaults.schoolId, schoolChoice, allSchools]);

  // ── Đóng dropdown khi click ra ngoài ────────────────────────────────────
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setShowDropdown(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // ── Handlers ─────────────────────────────────────────────────────────────
  function handleSchoolInputChange(e: ChangeEvent<HTMLInputElement>) {
    setSchoolSearch(e.target.value);
    setSelectedSchool(null); // reset lựa chọn khi gõ lại
    setShowDropdown(true);
  }

  function handleSelectSchool(school: { id: string; schoolName: string }) {
    setSelectedSchool(school);
    setSchoolSearch(school.schoolName);
    setShowDropdown(false);
  }

  function selectCard(e: ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;
    if (!file.type.startsWith('image/')) { setError('Vui lòng chọn một file ảnh hợp lệ.'); return; }
    if (file.size > 5 * 1024 * 1024) { setError('Ảnh thẻ không được vượt quá 5MB.'); return; }
    const reader = new FileReader();
    reader.onload = () => { setError(''); setCard({ preview: reader.result as string, file }); };
    reader.readAsDataURL(file);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');

    if (!studentCode.trim()) {
      setError('Vui lòng nhập mã số sinh viên.');
      return;
    }
    if (needsCard && !schoolSearch.trim()) {
      setError('Vui lòng chọn hoặc nhập tên trường của bạn.');
      return;
    }
    if (needsCard && !card) {
      setError('Vui lòng tải ảnh thẻ sinh viên.');
      return;
    }

    setBusy(true);
    try {
      // ── Resolve schoolId ────────────────────────────────────────────────
      let schoolId: string | null = null;

      if (schoolChoice === 'FPT') {
        const res = await schoolsApi.list();
        const fpt = res.data.find((s) => s.schoolName.toUpperCase().includes('FPT'));
        schoolId = fpt?.id ?? null;

      } else if (selectedSchool?.id) {
        // Đã chọn trường có sẵn từ dropdown
        schoolId = selectedSchool.id;
      }

      // ── Upload ảnh thẻ ───────────────────────────────────────────────────
      let photoStudentCardUrl: string | null = defaults.photoStudentCardUrl ?? null;
      if (card?.file) {
        photoStudentCardUrl = await storageApi.upload(card.file);
      } else if (!card) {
        photoStudentCardUrl = null;
      }

      await onSubmit({
        schoolId,
        studentCode: studentCode.trim(),
        photoStudentCardUrl,
        isFpt: schoolChoice === 'FPT',
        fullName: fullName.trim(),
      });
      notify.success('Đã gửi hồ sơ đăng ký. Vui lòng chờ ban tổ chức xét duyệt.');
    } catch (e) {
      const msg = getErrorMessage(e, 'Đã xảy ra lỗi. Vui lòng thử lại.');
      setError(msg);
      notify.error(msg);
    } finally {
      setBusy(false);
    }
  }

  const labelStyle = {
    fontSize: 11, fontWeight: 700,
    letterSpacing: '0.08em', textTransform: 'uppercase' as const,
    color: 'var(--color-mute)',
  };

  // ── Render ────────────────────────────────────────────────────────────────
  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-4 max-w-[36rem]">
      {error && (
        <p className="t-caption-sm" style={{ color: 'var(--color-error)', margin: 0 }}>
          {error}
        </p>
      )}

      {/* Họ và tên */}
      <label className="flex flex-col gap-1.5">
        <span style={labelStyle}>Họ và tên</span>
        <input
          className="text-input"
          value={fullName}
          onChange={(e) => setFullName(e.target.value)}
        />
      </label>

      {/* Chọn FPT / Khác */}
      <label className="flex flex-col gap-1.5">
        <span style={labelStyle}>Trường</span>
        <select
          className="text-input"
          value={schoolChoice}
          onChange={(e) => {
            setSchoolChoice(e.target.value as 'FPT' | 'OTHER');
            setSelectedSchool(null);
            setSchoolSearch('');
          }}
        >
          <option value="FPT">FPT University</option>
          <option value="OTHER">Trường khác</option>
        </select>
      </label>

      {/* Tìm kiếm trường — chỉ hiện khi chọn OTHER */}
      {needsCard && (
        <div className="flex flex-col gap-1.5" ref={dropdownRef} style={{ position: 'relative' }}>
          <span style={labelStyle}>Tìm kiếm trường</span>

          <input
            className="text-input"
            value={schoolSearch}
            onChange={handleSchoolInputChange}
            onFocus={() => setShowDropdown(true)}
            placeholder="Gõ tên trường để tìm kiếm..."
            autoComplete="off"
          />

          {/* Tag hiển thị trường đã chọn */}
          {selectedSchool && (
            <div className="flex items-center gap-2 mt-1">
              <span
                className="t-caption-sm font-bold px-2 py-1 rounded-sm"
                style={{
                  background: selectedSchool.id ? 'var(--color-primary-soft)' : 'rgba(229,32,32,0.08)',
                  color: selectedSchool.id ? 'var(--color-primary)' : 'var(--color-error)',
                  border: `1px solid ${selectedSchool.id ? 'var(--color-primary)' : 'var(--color-error)'}`,
                }}
              >
                {selectedSchool.id ? '✓ Đã chọn:' : '… Sẽ tạo mới:'} {selectedSchool.schoolName}
              </span>
              <button
                type="button"
                className="t-caption-sm"
                style={{ color: 'var(--color-mute)', background: 'none', border: 'none', cursor: 'pointer' }}
                onClick={() => { setSelectedSchool(null); setSchoolSearch(''); }}
              >
                Xóa
              </button>
            </div>
          )}

          {/* Dropdown gợi ý */}
          {showDropdown && schoolSearch.length > 0 && (
            <div
              style={{
                position: 'absolute', top: '100%', left: 0, right: 0, zIndex: 50,
                background: '#ffffff',
                border: '1px solid var(--color-hairline-strong)',
                borderRadius: 'var(--radius-sm)',
                boxShadow: '0 4px 16px rgba(0,0,0,0.1)',
                marginTop: 4, maxHeight: 220, overflowY: 'auto',
              }}
            >
              {/* Kết quả tìm kiếm */}
              {filtered.length > 0 && (
                <>
                  <div
                    className="t-caption-sm font-bold uppercase tracking-wider px-3 py-2"
                    style={{ color: 'var(--color-mute)', borderBottom: '1px solid var(--color-hairline)' }}
                  >
                    Trường có sẵn
                  </div>
                  {filtered.map((s) => (
                    <button
                      key={s.id}
                      type="button"
                      onMouseDown={() => handleSelectSchool(s)}
                      className="w-full text-left px-3 py-2.5 t-body-sm hover:bg-black/5 transition-colors"
                      style={{ background: 'none', border: 'none', cursor: 'pointer', display: 'block' }}
                    >
                      {s.schoolName}
                    </button>
                  ))}
                </>
              )}

              {/* Không tìm thấy gì */}
              {filtered.length === 0 && (
                <div className="px-3 py-3 t-caption-sm text-mute">
                  Không tìm thấy trường nào khớp.
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {/* MSSV */}
      <label className="flex flex-col gap-1.5">
        <span style={labelStyle}>Mã số sinh viên</span>
        <input
          className="text-input"
          value={studentCode}
          onChange={(e) => setStudentCode(e.target.value)}
          placeholder="VD: SE123456"
        />
      </label>

      {/* Ảnh thẻ */}
      {needsCard && (
        <div className="flex flex-col gap-1.5">
          <span style={labelStyle}>Ảnh thẻ sinh viên</span>
          {card ? (
            <div className="flex items-center gap-3">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={card.preview}
                alt="Ảnh thẻ sinh viên"
                className="w-24 h-16 object-cover rounded-sm border border-hairline"
              />
              <button
                type="button"
                className="t-caption-sm font-bold"
                style={{ color: 'var(--color-error)', background: 'none', border: 'none', cursor: 'pointer' }}
                onClick={() => setCard(null)}
              >
                Xóa
              </button>
            </div>
          ) : (
            <button
              type="button"
              className="btn btn-secondary btn-sm w-fit"
              onClick={() => fileRef.current?.click()}
            >
              Tải ảnh thẻ
            </button>
          )}
          <input ref={fileRef} type="file" accept="image/*" onChange={selectCard} style={{ display: 'none' }} />
        </div>
      )}

      <div className="flex items-center gap-3">
        {onBack && (
          <button
            type="button"
            className="btn btn-secondary w-fit"
            onClick={onBack}
            disabled={busy}
          >
            ← Quay lại
          </button>
        )}
        <button type="submit" className="btn btn-primary w-fit" disabled={busy}>
          {busy ? 'Đang xử lý…' : 'Gửi đăng ký'}
        </button>
      </div>
    </form>
  );
}
