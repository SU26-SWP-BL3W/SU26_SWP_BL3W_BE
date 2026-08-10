'use client';
import { useState, useCallback } from 'react';
import { Navbar } from '@/components/Navbar';
import Notif from '@/components/Notif';
import SubmitProjectPage from '@/views/SubmitProjectPage';
import { INIT_TEAMS, INIT_SUBMISSIONS, MY_TEAM_ID } from '@/data';

export default function SubmitProjectRoute() {
  const myTeam = INIT_TEAMS.find(t => t.id === MY_TEAM_ID);
  const [submissions, setSubmissions] = useState(INIT_SUBMISSIONS);
  const [notif, setNotif] = useState(null);

  const sn = useCallback((m, t = 's') => {
    setNotif({ m, t });
    setTimeout(() => setNotif(null), 3000);
  }, []);

  const handleSubmit = (data) => {
    setSubmissions(p => [
      ...p,
      {
        id: `S-${Date.now()}`,
        ...data,
        submittedAt: new Date().toLocaleString('vi-VN'),
        status: 'Đã nhận',
      },
    ]);
    sn('Đã nộp bài thành công!');
  };

  return (
    <>
      <Navbar />
      <Notif n={notif} />
      <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
        <SubmitProjectPage myTeam={myTeam} submissions={submissions} onSubmit={handleSubmit} />
      </main>
    </>
  );
}
