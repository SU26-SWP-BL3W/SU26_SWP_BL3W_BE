'use client';
import { useState, useCallback } from 'react';
import { Navbar } from '@/components/Navbar';
import Notif from '@/components/Notif';
import DangKyPage from '@/views/DangKyPage';

export default function DangKyRoute() {
  const [notif, setNotif] = useState(null);

  const sn = useCallback((m, t = 's') => {
    setNotif({ m, t });
    setTimeout(() => setNotif(null), 3000);
  }, []);

  const handleSubmit = () => {
    sn('Đăng ký thành công! Vui lòng chờ admin xác nhận.', 's');
  };

  return (
    <>
      <Navbar />
      <Notif n={notif} />
      <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
        <DangKyPage onSubmit={handleSubmit} />
      </main>
    </>
  );
}
