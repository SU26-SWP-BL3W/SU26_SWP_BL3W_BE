import { useState, useMemo } from 'react';
import {
  getCriteria,
  createCriteria,
  updateCriteria,
  deleteCriteria,
  toggleCriteriaStatus,
} from '../services/criteriaService';

const BLANK = { criteriaName: '', description: '', isActive: true };
const COLORS = ['#76b900','#0046a4','#df6500','#952fc6','#0D9488','#5a8d00'];
const PAGE_SIZE = 10;

export default function CriteriaPage({ criteria, setCriteria, sn }) {
  const [ed,     setEd]     = useState(null);
  const [f,      setF]      = useState({ ...BLANK });
  const [saving, setSaving] = useState(false);
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState('all'); // 'all' | 'active' | 'inactive'
  const [page,   setPage]   = useState(1);

  const openNew  = ()  => { setF({ ...BLANK }); setEd('new'); };
  const openEdit = (c) => { setF({ criteriaName: c.label, description: c.desc ?? '', isActive: c.isActive !== false }); setEd(c.id); };
  const close    = ()  => setEd(null);

  const refresh = () => getCriteria().then(setCriteria).catch(console.error);

  const save = async () => {
    if (!f.criteriaName.trim()) { sn('Vui lòng điền tên tiêu chí', 'e'); return; }
    setSaving(true);
    try {
      if (ed === 'new') {
        await createCriteria(f);
        sn('Đã thêm tiêu chí!');
      } else {
        await updateCriteria(ed, f);
        sn('Đã cập nhật tiêu chí!');
      }
      close();
      await refresh();
    } catch (e) {
      const msg = e?.response?.data?.message ?? e?.message ?? 'Lỗi khi lưu tiêu chí';
      sn(msg, 'e');
      console.error('[saveCriteria]', e?.response?.status, e?.response?.data ?? e);
    } finally {
      setSaving(false);
    }
  };

  const del = async (id) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa tiêu chí này không?')) return;
    try {
      await deleteCriteria(id);
      sn('Đã xóa tiêu chí.');
      await refresh();
    } catch (e) {
      const msg = e?.response?.data?.message ?? 'Lỗi khi xóa tiêu chí';
      sn(msg, 'e');
    }
  };

  const toggle = async (id, currentActive) => {
    try {
      await toggleCriteriaStatus(id);
      sn(currentActive ? 'Đã tắt tiêu chí.' : 'Đã bật tiêu chí.');
      await refresh();
    } catch {
      sn('Lỗi khi thay đổi trạng thái', 'e');
    }
  };

  const filtered = useMemo(() => {
    let list = criteria;
    if (filter === 'active')   list = list.filter(c => c.isActive !== false);
    if (filter === 'inactive') list = list.filter(c => c.isActive === false);
    const q = search.trim().toLowerCase();
    if (q) list = list.filter(c => c.label?.toLowerCase().includes(q) || c.desc?.toLowerCase().includes(q));
    return list;
  }, [criteria, search, filter]);

  const totalPages  = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const currentPage = Math.min(page, totalPages);
  const paged       = filtered.slice((currentPage - 1) * PAGE_SIZE, currentPage * PAGE_SIZE);

  const hasWeight = criteria.some(c => c.weight > 0);
  const totalW    = criteria.reduce((s, c) => s + (c.weight ?? 0), 0);

  return (
    <div className="animate-fadeUp">

      {/* ── Modal tạo / sửa ─────────────────────────────────────────────────── */}
      {ed !== null && (
        <div className="modal-overlay">
          <div className="modal-box" style={{ maxWidth: 520 }}>
            <div className="flex justify-between items-start mb-6">
              <div>
                <div className="text-lg font-bold" style={{ color: '#000' }}>
                  {ed === 'new' ? 'Thêm tiêu chí mới' : 'Chỉnh sửa tiêu chí'}
                </div>
                <div className="text-xs mt-1" style={{ color: '#757575' }}>
                  Tên và mô tả lưu trên backend. Trọng số được quản lý qua bộ tiêu chí.
                </div>
              </div>
              <button className="btn-hover" onClick={close}
                style={{ background: 'transparent', border: 'none', color: '#757575', fontSize: 18, lineHeight: 1 }}>✕</button>
            </div>

            <div className="mb-4">
              <label className="block text-xs font-bold mb-1.5 uppercase tracking-wider" style={{ color: '#757575' }}>Tên tiêu chí *</label>
              <input
                value={f.criteriaName}
                onChange={e => setF({ ...f, criteriaName: e.target.value })}
                placeholder="VD: Tính sáng tạo"
                className="input-field"
              />
            </div>

            <div className="mb-6">
              <label className="block text-xs font-bold mb-1.5 uppercase tracking-wider" style={{ color: '#757575' }}>Mô tả</label>
              <input
                value={f.description}
                onChange={e => setF({ ...f, description: e.target.value })}
                placeholder="Mô tả ngắn về tiêu chí..."
                className="input-field"
              />
            </div>

            <div className="flex justify-end gap-3">
              <button className="btn btn-outline" onClick={close}>Hủy</button>
              <button className={`btn ${ed === 'new' ? 'btn-create' : 'btn-update'}`} onClick={save} disabled={saving}>
                {saving ? 'Đang lưu...' : ed === 'new' ? 'Thêm tiêu chí' : 'Lưu thay đổi'}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* ── Header ─────────────────────────────────────────────────────────── */}
      <div className="flex justify-between items-start mb-7">
        <div>
          <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Tiêu chí chấm điểm</h2>
          <p className="text-sm mt-1" style={{ color: '#757575' }}>
            Quản lý bộ tiêu chí. Tất cả điểm chấm sẽ dựa theo bộ này.
          </p>
        </div>
        <button className="btn btn-create" onClick={openNew}>Thêm tiêu chí</button>
      </div>

      {/* ── Thanh tìm kiếm + lọc ────────────────────────────────────────────── */}
      <div className="flex gap-3 mb-5 flex-wrap items-center">
        <input
          value={search}
          onChange={e => { setSearch(e.target.value); setPage(1); }}
          placeholder="Tìm tiêu chí..."
          className="input-field flex-1"
          style={{ minWidth: 180 }}
        />
        {search && (
          <button onClick={() => { setSearch(''); setPage(1); }}
            className="btn-hover text-xs px-2 py-1"
            style={{ background: '#f7f7f7', border: '1px solid #ccc', borderRadius: 2, color: '#757575' }}>✕</button>
        )}
        {(['all', 'active', 'inactive']).map(f => (
          <button key={f}
            onClick={() => { setFilter(f); setPage(1); }}
            className="btn-hover px-3 py-1.5 text-xs font-bold"
            style={{
              borderRadius: 2, border: '1px solid',
              background:  filter === f ? '#000' : '#f7f7f7',
              color:       filter === f ? '#fff' : '#757575',
              borderColor: filter === f ? '#000' : '#cccccc',
            }}>
            {f === 'all' ? 'Tất cả' : f === 'active' ? 'Đang bật' : 'Đã tắt'}
          </button>
        ))}
        <span className="text-xs ml-auto" style={{ color: '#757575', whiteSpace: 'nowrap' }}>
          {filtered.length} / {criteria.length} tiêu chí
        </span>
      </div>

      {/* ── Thanh trọng số ──────────────────────────────────────────────────── */}
      {hasWeight && (
        <div className="p-5 mb-6" style={{ background: '#f7f7f7', border: '1px solid #cccccc', borderRadius: 2 }}>
          <div className="flex justify-between mb-2.5">
            <span className="text-sm" style={{ color: '#757575' }}>Tổng trọng số (từ bộ tiêu chí)</span>
            <span className="text-sm font-bold" style={{ color: totalW === 100 ? '#76b900' : '#df6500' }}>
              {totalW} / 100
            </span>
          </div>
          <div className="h-2 overflow-hidden flex gap-0.5" style={{ background: '#e5e5e5', borderRadius: 2 }}>
            {criteria.map((c, i) => (
              <div key={i} style={{
                height: '100%', width: `${c.weight ?? 0}%`,
                background: COLORS[i % COLORS.length],
                transition: 'width .4s',
              }} />
            ))}
          </div>
          <div className="flex gap-3 mt-2.5 flex-wrap">
            {criteria.map((c, i) => (
              <div key={i} className="flex items-center gap-1.5 text-xs" style={{ color: '#757575' }}>
                <div style={{ width: 8, height: 8, background: COLORS[i % COLORS.length], borderRadius: 2 }} />
                {c.label} {c.weight ? `(${c.weight}%)` : ''}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── Empty state ─────────────────────────────────────────────────────── */}
      {criteria.length === 0 && (
        <div className="text-center py-20" style={{ color: '#757575' }}>
          <div className="text-base font-bold mb-1.5" style={{ color: '#76b900' }}>Chưa có tiêu chí nào</div>
          <div className="text-sm">Bấm &ldquo;+ Thêm tiêu chí&rdquo; để bắt đầu.</div>
        </div>
      )}
      {criteria.length > 0 && filtered.length === 0 && (
        <div className="text-center py-10" style={{ color: '#757575' }}>
          <div className="text-sm">Không tìm thấy tiêu chí nào phù hợp.</div>
        </div>
      )}

      {/* ── Danh sách tiêu chí ──────────────────────────────────────────────── */}
      {/* {criteria.map((c) => ( */}
      {paged.map((c) => (
        <div
          key={c.id}
          className="card-hover p-6 mb-3"
          style={{
            background: '#ffffff',
            border: '1px solid #cccccc',
            borderRadius: 2,
            opacity: c.isActive === false ? 0.55 : 1,
            transition: 'opacity .3s',
          }}
        >
          <div className="flex justify-between items-start">
            <div className="flex-1">
              <div className="flex items-center gap-3 mb-1 flex-wrap">
                {c.weight > 0 && (
                  <div className="px-2.5 py-0.5 text-xs font-bold"
                    style={{ background: 'rgba(118,185,0,.12)', border: '1px solid rgba(118,185,0,.3)', color: '#76b900', borderRadius: 2 }}>
                    {c.weight}%
                  </div>
                )}
                <div className="text-base font-bold" style={{ color: '#000' }}>{c.label}</div>
                {c.isActive === false && (
                  <div className="px-2 py-0.5 text-xs font-bold"
                    style={{ background: 'rgba(229,32,32,.08)', border: '1px solid rgba(229,32,32,.25)', color: '#e52020', borderRadius: 2 }}>
                    Đã tắt
                  </div>
                )}
              </div>
              {c.desc && (
                <div className="text-xs" style={{ color: '#757575' }}>{c.desc}</div>
              )}
            </div>

            <div className="flex gap-2 ml-4 shrink-0">
              <button
                className="btn btn-update btn-sm"
                onClick={() => toggle(c.id, c.isActive !== false)}
                style={{ background: '#f7f7f7', border: '1px solid #cccccc', color: '#000', borderRadius: 2 }}
              >
                {c.isActive === false ? 'Bật' : 'Tắt'}
              </button>
              <button
                className="btn btn-update btn-sm"
                onClick={() => openEdit(c)}
              >Sửa</button>
              <button
                className="btn btn-delete btn-sm"
                onClick={() => del(c.id)}
              >Xóa</button>
            </div>
          </div>
        </div>
      ))}

      {/* ── Phân trang ──────────────────────────────────────────────────────── */}
      {totalPages > 1 && (
        <div className="flex justify-center items-center gap-2 mt-6">
          <button
            className="btn-hover px-3 py-1.5 text-xs font-bold"
            onClick={() => setPage(p => Math.max(1, p - 1))}
            disabled={currentPage === 1}
            style={{ background: '#f7f7f7', border: '1px solid #cccccc', color: currentPage === 1 ? '#ccc' : '#000', borderRadius: 2 }}>
            ← Trước
          </button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map(n => (
            <button key={n}
              className="btn-hover px-3 py-1.5 text-xs font-bold"
              onClick={() => setPage(n)}
              style={{
                borderRadius: 2, border: '1px solid',
                background:  currentPage === n ? '#000' : '#f7f7f7',
                color:       currentPage === n ? '#fff' : '#000',
                borderColor: currentPage === n ? '#000' : '#cccccc',
              }}>{n}</button>
          ))}
          <button
            className="btn-hover px-3 py-1.5 text-xs font-bold"
            onClick={() => setPage(p => Math.min(totalPages, p + 1))}
            disabled={currentPage === totalPages}
            style={{ background: '#f7f7f7', border: '1px solid #cccccc', color: currentPage === totalPages ? '#ccc' : '#000', borderRadius: 2 }}>
            Sau →
          </button>
        </div>
      )}
    </div>
  );
}
