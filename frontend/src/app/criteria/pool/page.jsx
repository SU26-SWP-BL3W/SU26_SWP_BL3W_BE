'use client';
import { useState, useEffect, useCallback } from 'react';
import { Navbar } from '@/components/Navbar';
import Notif from '@/components/Notif';
import CriteriaPage from '@/views/CriteriaPage';
import { getCriteria } from '@/services/criteriaService';

export default function CriteriaPoolRoute() {
  const [criteria, setCriteria] = useState([]);
  const [notif,    setNotif]    = useState(null);

  useEffect(() => {
    getCriteria()
      .then(setCriteria)
      .catch(err => console.error('[criteria/pool] load failed', err));
  }, []);

  const sn = useCallback((m, t = 's') => {
    setNotif({ m, t });
    setTimeout(() => setNotif(null), 3000);
  }, []);

  return (
    <>
      <Navbar />
      <Notif n={notif} />
      <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
        <CriteriaPage criteria={criteria} setCriteria={setCriteria} sn={sn} />
      </main>
    </>
  );
}
