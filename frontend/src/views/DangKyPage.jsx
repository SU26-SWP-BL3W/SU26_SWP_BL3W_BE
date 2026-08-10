import { useState } from 'react';
import { StatusBadge } from '../components/StatusBadge';

const emptyMember = () => ({ name: '', id: '' });

function FptForm({ onSubmit }) {
  const [hocKi,   setHocKi]   = useState('');
  const [gmail,   setGmail]   = useState('');
  const [leader,  setLeader]  = useState({ name: '', mssv: '' });
  const [members, setMembers] = useState([emptyMember()]);

  const updMember    = (i, k, v) => { const a = [...members]; a[i] = { ...a[i], [k]: v }; setMembers(a); };
  const addMember    = () => setMembers(p => [...p, emptyMember()]);
  const removeMember = i  => setMembers(p => p.filter((_, j) => j !== i));

  const handleSubmit = () => {
    if (!hocKi || !gmail || !leader.name || !leader.mssv) return;
    onSubmit({ type: 'fpt', hocKi, gmail, leader, members });
  };

  return (
    <div className="card-fpt p-6 animate-fadeUp">
      <div className="flex items-center gap-3 mb-5">
        <div className="flex items-center justify-center font-black text-sm"
          style={{ width: 36, height: 36, background: 'rgba(118,185,0,.15)', border: '1px solid #76b900', color: '#5a8d00', borderRadius: 2 }}>FPT</div>
        <div>
          <div className="font-bold text-sm" style={{ color: '#5a8d00' }}>Sinh viên FPT University</div>
          <div className="text-xs mt-0.5" style={{ color: '#757575' }}>Hệ thống xác nhận tự động · Duyệt ngay lập tức</div>
        </div>
        <StatusBadge tone="success" className="ml-auto">Tự động duyệt</StatusBadge>
      </div>

      <div className="mb-4">
        <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>HỌC KÌ *</label>
        <input value={hocKi} onChange={e => setHocKi(e.target.value)} placeholder="VD: K19, K20, K21..." className="input-field" />
      </div>

      <div className="mb-4">
        <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>LEADER NHÓM *</label>
        <div className="grid gap-3" style={{ gridTemplateColumns: '1fr 1fr' }}>
          <input value={leader.name} onChange={e => setLeader({ ...leader, name: e.target.value })} placeholder="Họ và tên leader" className="input-field" />
          <input value={leader.mssv} onChange={e => setLeader({ ...leader, mssv: e.target.value })} placeholder="Mã số sinh viên" className="input-field" />
        </div>
      </div>

      <div className="mb-4">
        <div className="flex justify-between items-center mb-2">
          <label className="text-xs font-bold uppercase tracking-wider" style={{ color: '#757575' }}>THÀNH VIÊN</label>
          <button className="btn-hover px-3 py-1 text-xs font-bold"
            style={{ background: 'rgba(118,185,0,.1)', border: '1px solid rgba(118,185,0,.3)', color: '#5a8d00', borderRadius: 2 }}
            onClick={addMember}>+ Thêm</button>
        </div>
        {members.map((m, i) => (
          <div key={i} className="grid gap-2 mb-2 items-center" style={{ gridTemplateColumns: '1fr 1fr 32px' }}>
            <input value={m.name} onChange={e => updMember(i, 'name', e.target.value)} placeholder={`Họ tên thành viên ${i + 1}`} className="input-field" />
            <input value={m.id}   onChange={e => updMember(i, 'id',   e.target.value)} placeholder="Mã số sinh viên"              className="input-field" />
            <button className="btn-hover flex items-center justify-center text-sm"
              style={{ width: 32, height: 44, background: 'rgba(229,32,32,.08)', border: '1px solid rgba(229,32,32,.2)', color: '#e52020', borderRadius: 2 }}
              onClick={() => removeMember(i)}>✕</button>
          </div>
        ))}
      </div>

      <div className="mb-5">
        <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>GMAIL NHẬN THÔNG BÁO *</label>
        <input value={gmail} onChange={e => setGmail(e.target.value)} placeholder="example@gmail.com" type="email" className="input-field" />
      </div>

      <button className="btn btn-primary w-full" onClick={handleSubmit}>
        ĐĂNG KÝ — XÁC NHẬN NGAY
      </button>
    </div>
  );
}

function ExternalForm({ onSubmit }) {
  const [truong,  setTruong]  = useState('');
  const [gmail,   setGmail]   = useState('');
  const [leader,  setLeader]  = useState({ name: '', cccd: '' });
  const [members, setMembers] = useState([emptyMember()]);

  const updMember    = (i, k, v) => { const a = [...members]; a[i] = { ...a[i], [k]: v }; setMembers(a); };
  const addMember    = () => setMembers(p => [...p, emptyMember()]);
  const removeMember = i  => setMembers(p => p.filter((_, j) => j !== i));

  const handleSubmit = () => {
    if (!truong || !gmail || !leader.name || !leader.cccd) return;
    onSubmit({ type: 'external', truong, gmail, leader, members });
  };

  return (
    <div className="card-ext p-6 animate-fadeUp">
      <div className="flex items-center gap-3 mb-5">
        <div className="flex items-center justify-center font-black text-xs"
          style={{ width: 36, height: 36, background: '#f7f7f7', border: '1px solid #cccccc', color: '#757575', borderRadius: 2 }}>EXT</div>
        <div>
          <div className="font-bold text-sm" style={{ color: '#000' }}>Trường ngoài FPT</div>
          <div className="text-xs mt-0.5" style={{ color: '#757575' }}>Cần chờ quản trị viên xét duyệt</div>
        </div>
        <StatusBadge tone="pending" className="ml-auto">Chờ xét duyệt</StatusBadge>
      </div>

      <div className="mb-4">
        <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>TÊN TRƯỜNG *</label>
        <input value={truong} onChange={e => setTruong(e.target.value)} placeholder="VD: Đại học Bách Khoa Hà Nội" className="input-field" />
      </div>

      <div className="mb-4">
        <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>LEADER NHÓM *</label>
        <div className="grid gap-3" style={{ gridTemplateColumns: '1fr 1fr' }}>
          <input value={leader.name} onChange={e => setLeader({ ...leader, name: e.target.value })} placeholder="Họ và tên leader" className="input-field" />
          <input value={leader.cccd} onChange={e => setLeader({ ...leader, cccd: e.target.value })} placeholder="Số căn cước công dân" className="input-field" />
        </div>
      </div>

      <div className="mb-4">
        <div className="flex justify-between items-center mb-2">
          <label className="text-xs font-bold uppercase tracking-wider" style={{ color: '#757575' }}>THÀNH VIÊN</label>
          <button className="btn-hover px-3 py-1 text-xs font-bold"
            style={{ background: '#f7f7f7', border: '1px solid #cccccc', color: '#000', borderRadius: 2 }}
            onClick={addMember}>+ Thêm</button>
        </div>
        {members.map((m, i) => (
          <div key={i} className="grid gap-2 mb-2 items-center" style={{ gridTemplateColumns: '1fr 1fr 32px' }}>
            <input value={m.name} onChange={e => updMember(i, 'name', e.target.value)} placeholder={`Họ tên thành viên ${i + 1}`} className="input-field" />
            <input value={m.id}   onChange={e => updMember(i, 'id',   e.target.value)} placeholder="Số CCCD"                       className="input-field" />
            <button className="btn-hover flex items-center justify-center text-sm"
              style={{ width: 32, height: 44, background: 'rgba(229,32,32,.08)', border: '1px solid rgba(229,32,32,.2)', color: '#e52020', borderRadius: 2 }}
              onClick={() => removeMember(i)}>✕</button>
          </div>
        ))}
      </div>

      <div className="mb-5">
        <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>GMAIL NHẬN THÔNG BÁO *</label>
        <input value={gmail} onChange={e => setGmail(e.target.value)} placeholder="example@gmail.com" type="email" className="input-field" />
      </div>

      <button className="btn btn-outline w-full" onClick={handleSubmit}>
        GỬI ĐƠN ĐĂNG KÝ — CHỜ XÉT DUYỆT
      </button>
    </div>
  );
}

export default function DangKyPage({ onSubmit }) {
  const [mode, setMode] = useState(null);

  return (
    <div className="animate-fadeUp" style={{ maxWidth: 700, margin: '0 auto' }}>
      <div className="mb-7">
        <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Đăng ký tham gia</h2>
        <p className="text-sm mt-1" style={{ color: '#757575' }}>Chọn loại trường của bạn để điền form phù hợp.</p>
      </div>

      {!mode && (
        <div className="grid gap-4" style={{ gridTemplateColumns: '1fr 1fr' }}>
          <button className="btn-hover text-left p-6"
            style={{ background: 'rgba(118,185,0,.06)', border: '2px solid rgba(118,185,0,.35)', borderRadius: 2 }}
            onClick={() => setMode('fpt')}>
            <div className="flex items-center justify-center font-black text-xl mb-4"
              style={{ width: 48, height: 48, background: 'rgba(118,185,0,.15)', border: '1px solid #76b900', color: '#5a8d00', borderRadius: 2 }}>FPT</div>
            <div className="font-bold text-base mb-1.5" style={{ color: '#5a8d00' }}>FPT University</div>
            <div className="text-xs leading-relaxed" style={{ color: '#757575' }}>Sinh viên FPT · Xác nhận tự động · Duyệt ngay lập tức</div>
            <div className="mt-3 text-xs font-bold" style={{ color: '#76b900' }}>Duyệt tự động →</div>
          </button>
          <button className="btn-hover text-left p-6"
            style={{ background: '#f7f7f7', border: '2px solid #cccccc', borderRadius: 2 }}
            onClick={() => setMode('external')}>
            <div className="flex items-center justify-center font-black text-sm mb-4"
              style={{ width: 48, height: 48, background: '#fff', border: '1px solid #cccccc', color: '#757575', borderRadius: 2 }}>EXT</div>
            <div className="font-bold text-base mb-1.5" style={{ color: '#000' }}>Trường ngoài FPT</div>
            <div className="text-xs leading-relaxed" style={{ color: '#757575' }}>Trường khác · Cần căn cước công dân · Chờ admin xét duyệt</div>
            <div className="mt-3 text-xs font-bold" style={{ color: '#757575' }}>Gửi đơn xét duyệt →</div>
          </button>
        </div>
      )}

      {mode && (
        <div>
          <button className="btn-hover flex items-center gap-2 mb-4 text-xs font-bold"
            style={{ background: 'transparent', border: 'none', color: '#757575', padding: 0 }}
            onClick={() => setMode(null)}>← Quay lại chọn loại trường</button>
          {mode === 'fpt'      && <FptForm      onSubmit={onSubmit} />}
          {mode === 'external' && <ExternalForm onSubmit={onSubmit} />}
        </div>
      )}
    </div>
  );
}
