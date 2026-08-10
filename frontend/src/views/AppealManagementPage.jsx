import { useState } from 'react';
import { StatusBadge } from '../utils.jsx';
import AppealModal from '../components/AppealModal.jsx';

export default function AppealManagementPage({ appeals, teams, criteria, onUpdate, onDel }) {
  const [view, setView] = useState(null);
  return (
    <div className="animate-fadeUp">
      <div className="mb-7">
        <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Danh sách đơn phúc khảo</h2>
        <p className="text-sm mt-1" style={{ color: '#757575' }}>Xem xét và xử lý các đơn phúc khảo từ các đội.</p>
      </div>

      {view && (
        <AppealModal
          appeal={view}
          team={teams.find(t => t.id === view.teamId)}
          criteria={criteria}
          onClose={() => setView(null)}
          onUpdate={(id, st, sc) => { onUpdate(id, st, sc); setView(null); }}
        />
      )}

      {appeals.length === 0 && (
        <div className="text-center py-20 text-sm" style={{ color: '#757575' }}>Chưa có đơn phúc khảo nào.</div>
      )}

      {appeals.map((a, i) => (
        <div key={a.id} className="mb-4 overflow-hidden"
          style={{ background: '#fff', border: '1px solid #cccccc', borderRadius: 2, animationDelay: `${i * 0.07}s` }}>
          <div className="flex items-center gap-4" style={{ borderBottom: '1px solid #e5e5e5', padding: '16px 24px' }}>
            <span className="text-xs font-bold uppercase tracking-wider" style={{ color: '#76b900' }}>{a.teamId}</span>
            <span className="text-sm font-bold" style={{ color: '#000' }}>{a.teamName}</span>
            <span className="text-xs" style={{ color: '#757575' }}>{a.email}</span>
            <div className="ml-auto"><StatusBadge status={a.status} /></div>
          </div>
          <div style={{ padding: '20px 24px' }}>
            <div className="text-xs font-bold uppercase tracking-widest mb-2" style={{ color: '#757575' }}>LÝ DO PHÚC KHẢO</div>
            <div className="text-sm leading-relaxed mb-4" style={{ color: '#1a1a1a' }}>{a.reason}</div>
            <div className="flex justify-end gap-2.5">
              <button className="btn btn-view btn-sm flex items-center gap-1.5"
                onClick={() => setView(a)}>Xem</button>
              <button className="btn btn-delete btn-sm flex items-center gap-1.5"
                onClick={() => onDel(a.id)}>Xóa</button>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
}
