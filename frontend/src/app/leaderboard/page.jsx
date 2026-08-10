'use client';
import { useState, useMemo, useCallback } from 'react';
import { Navbar } from '@/components/Navbar';
import Notif from '@/components/Notif';
import EditModal from '@/components/EditModal';
import ExportModal from '@/components/ExportModal';
import LeaderboardPage from '@/views/LeaderboardPage';
import { INIT_CRITERIA, INIT_TEAMS } from '@/data';
import { calcScore } from '@/utils/scoreUtils';

export default function LeaderboardRoute() {
  const [criteria] = useState(INIT_CRITERIA);
  const [teams, setTeams] = useState(INIT_TEAMS);
  const [editT, setEditT] = useState(null);
  const [showExp, setShowExp] = useState(false);
  const [sortBy, setSortBy] = useState('score');
  const [notif, setNotif] = useState(null);

  const sn = useCallback((m, t = 's') => {
    setNotif({ m, t });
    setTimeout(() => setNotif(null), 3000);
  }, []);

  const ranked = useMemo(() =>
    [...teams].sort((a, b) => calcScore(b.scores, criteria) - calcScore(a.scores, criteria)),
    [teams, criteria]);

  const saveEdit = (tid, scores, cmts) => {
    setTeams(p => p.map(t => t.id === tid ? { ...t, scores, comments: cmts } : t));
    setEditT(null);
    sn('Đã lưu điểm thành công!');
  };

  return (
    <>
      <Navbar />
      <Notif n={notif} />
      {editT && (
        <EditModal team={editT} criteria={criteria}
          onClose={() => setEditT(null)}
          onSave={(s, c) => saveEdit(editT.id, s, c)} />
      )}
      {showExp && (
        <ExportModal ranked={ranked} criteria={criteria}
          onClose={() => setShowExp(false)} sn={sn} />
      )}
      <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
        <LeaderboardPage teams={teams} criteria={criteria}
          sortBy={sortBy} setSortBy={setSortBy}
          onEdit={setEditT} onExport={() => setShowExp(true)} />
      </main>
    </>
  );
}
