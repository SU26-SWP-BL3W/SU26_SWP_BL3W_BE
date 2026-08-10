'use client';
import { Navbar } from '@/components/Navbar';
import SubmissionsPanel from '@/features/events/components/shared/SubmissionsPanel';

export default function SubmissionRoute() {
  return (
    <>
      <Navbar />
      <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
        <SubmissionsPanel />
      </main>
    </>
  );
}
