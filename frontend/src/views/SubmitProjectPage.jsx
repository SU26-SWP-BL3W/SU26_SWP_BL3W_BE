import { useState } from 'react';
import { StatusBadge } from '../components/StatusBadge';

export default function SubmitProjectPage({ myTeam, submissions, onSubmit }) {
  const myS = submissions.find(s => s.teamId === myTeam?.id);
  const [f,  setF]  = useState({ projectName: '', teamName: myTeam?.name || '', repo: '' });
  const [ed, setEd] = useState(!myS);
  const ok = f.projectName.trim() && f.teamName.trim() && f.repo.trim();

  const go = () => { if (!ok) return; onSubmit({ ...f, teamId: myTeam?.id }); setEd(false); };

  return (
    <div className="animate-fadeUp" style={{ maxWidth: 680, margin: '0 auto' }}>
      <div className="mb-6">
        <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>Nộp bài</h2>
        <p className="text-sm mt-1" style={{ color: '#757575' }}>Nộp bài dự thi của nhóm bạn.</p>
      </div>

      {/* ── Đã nộp — view mode ── */}
      {myS && !ed && (
        <div className="p-7 mb-6"
          style={{ background: '#fff', border: '2px solid #76b900', borderRadius: 2, borderLeft: '4px solid #76b900' }}>
          <div className="flex items-center gap-3 mb-5">
            <div>
              <div className="text-base font-bold" style={{ color: '#76b900' }}>Bài đã được nộp thành công</div>
              <div className="text-xs mt-0.5" style={{ color: '#757575' }}>Nộp lúc {myS.submittedAt}</div>
            </div>
            <StatusBadge tone="success" className="ml-auto">{myS.status}</StatusBadge>
          </div>
          {[['TÊN DỰ ÁN', myS.projectName], ['TÊN NHÓM', myS.teamName]].map(([k, v]) => (
            <div key={k} className="p-3.5 mb-2.5 grid items-center" style={{ background: '#f7f7f7', gridTemplateColumns: '120px 1fr', borderRadius: 2 }}>
              <span className="text-xs font-bold uppercase tracking-widest" style={{ color: '#757575' }}>{k}</span>
              <span className="text-sm font-bold" style={{ color: '#000' }}>{v}</span>
            </div>
          ))}
          <div className="p-3.5 mb-5 grid items-center gap-2"
            style={{ background: '#f7f7f7', gridTemplateColumns: '120px 1fr', borderRadius: 2 }}>
            <span className="text-xs font-bold uppercase tracking-widest" style={{ color: '#757575' }}>REPO</span>
            <a href={myS.repo} target="_blank" rel="noreferrer" className="text-xs break-all"
              style={{ color: '#76b900', textDecoration: 'none' }}>{myS.repo}</a>
          </div>
          <button className="btn btn-outline w-full"
            onClick={() => { setF({ projectName: myS.projectName, teamName: myS.teamName, repo: myS.repo }); setEd(true); }}>
            Chỉnh sửa bài nộp
          </button>
        </div>
      )}

      {/* ── Form ── */}
      {ed && (
        <div style={{ background: '#fff', border: '1px solid #cccccc', borderRadius: 2 }}>
          <div className="flex items-center gap-3.5 p-5"
            style={{ background: '#f7f7f7', borderBottom: '1px solid #cccccc' }}>
            <div>
              <div className="text-sm font-bold" style={{ color: '#000' }}>Form nộp bài</div>
              <div className="text-xs" style={{ color: '#757575' }}>Điền đầy đủ thông tin bài dự thi.</div>
            </div>
          </div>
          <div className="p-7">
            {[['Tên bài / Tên dự án *', 'projectName', 'VD: SmartFarm AI, EduBot Platform...'], ['Tên nhóm *', 'teamName', 'VD: Nhóm Sao Băng']].map(([lb, k, ph]) => (
              <div key={k} className="mb-5">
                <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>{lb}</label>
                <input value={f[k]} onChange={e => setF({ ...f, [k]: e.target.value })} placeholder={ph} className="input-field" />
              </div>
            ))}
            <div className="mb-7">
              <label className="block text-xs font-bold uppercase tracking-wider mb-2" style={{ color: '#757575' }}>Link repo GitHub *</label>
              <input value={f.repo} onChange={e => setF({ ...f, repo: e.target.value })}
                placeholder="https://github.com/your-team/project" className="input-field" />
              {f.repo && !f.repo.startsWith('https://') && (
                <div className="text-xs mt-1.5" style={{ color: '#df6500' }}>Link nên bắt đầu bằng https://</div>
              )}
            </div>
            <div className="flex gap-3">
              {myS && <button className="btn btn-outline flex-1" onClick={() => setEd(false)}>Hủy</button>}
              <button onClick={go} className="btn btn-primary" disabled={!ok} style={{ flex: 3 }}>
                {myS ? 'Cập nhật bài nộp' : 'Xác nhận nộp bài'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
