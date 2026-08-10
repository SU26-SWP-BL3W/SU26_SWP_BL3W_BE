'use client';

import { Navbar } from '@/components/Navbar';
import { GlobalProfile } from '@/features/user/components/GlobalProfile';
import { useCurrentUser } from '@/hooks/useAuth';

import { useEffect, useState } from 'react';

export default function ProfilePage() {
  const { data: user, isLoading } = useCurrentUser();
  const [mounted, setMounted] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => setMounted(true), 0);
    return () => window.clearTimeout(timer);
  }, []);

  if (!mounted || isLoading) return null;

  if (!user) {
    return (
      <div className="min-h-screen flex flex-col bg-slate-50">
        <Navbar />
        <div className="p-8 text-center text-mute mt-8">Vui lòng đăng nhập để xem hồ sơ.</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex flex-col bg-slate-50">
      <Navbar />
      <main className="flex-1 py-8">
        <div className="container mx-auto">
          <GlobalProfile />
        </div>
      </main>
    </div>
  );
}
