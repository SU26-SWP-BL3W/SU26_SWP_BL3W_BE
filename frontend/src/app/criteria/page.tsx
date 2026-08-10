'use client';

import { useCallback, useState } from 'react';
import { Navbar } from '@/components/Navbar';
import Notif from '@/components/Notif';
import TemplatePage from '@/views/TemplatePage';
import { Footer } from '@/components/Footer';

type NotificationState = {
  m: string;
  t: string;
} | null;

export default function CriteriaPage() {
  const [notif, setNotif] = useState<NotificationState>(null);

  const sn = useCallback((m: string, t = 's') => {
    setNotif({ m, t });
    window.setTimeout(() => setNotif(null), 3000);
  }, []);

  return (
    <>
      <Navbar />
      <Notif n={notif} />
      <main
        style={{
          minHeight: '100vh',
          background: '#f7f7f7',
          padding: '28px 32px',
          fontFamily: 'Inter, sans-serif',
        }}
      >
        <TemplatePage sn={sn} />
      </main>
      <Footer />
    </>
  );
}
