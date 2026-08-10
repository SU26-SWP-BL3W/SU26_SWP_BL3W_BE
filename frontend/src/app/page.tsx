'use client';

import { useEffect, useState } from 'react';
import { Navbar } from "@/components/Navbar";
import { UserProfileHeader } from "@/features/user/components/UserProfileHeader";
import { AnnouncementBanner } from "@/features/user/components/AnnouncementBanner";
import { EventSection } from "@/features/events/components/EventSection";
import { HeroSection } from "@/features/home/components/HeroSection";
import { FeaturesSection } from "@/features/home/components/FeaturesSection";
import { StatsSection } from "@/features/home/components/StatsSection";
import { useCurrentUser } from "@/hooks/useAuth";
import { Footer } from "@/components/Footer";

export default function HomePage() {
  const { data: user } = useCurrentUser();
  const isLoggedIn = !!user?.id;

  const [mounted, setMounted] = useState(false);
  useEffect(() => {
    const timer = window.setTimeout(() => setMounted(true), 0);
    return () => window.clearTimeout(timer);
  }, []);

  if (!mounted) {
    return (
      <div className="min-h-screen bg-canvas flex items-center justify-center">
        <p className="t-body-md text-mute">Đang tải…</p>
      </div>
    );
  }

  return (
    <>
      <Navbar />
      {/* tabIndex=-1 allows programmatic focus on route change (a11y) */}
      <main tabIndex={-1} style={{ outline: "none", minHeight: "100vh" }}>
        {isLoggedIn ? (
          <>
            <UserProfileHeader />
            <AnnouncementBanner />
          </>
        ) : (
          <>
            <HeroSection />
            <FeaturesSection />
            <StatsSection />
          </>
        )}
        <div id="events-section">
          <EventSection />
        </div>
      </main>
      <Footer />
    </>
  );
}
