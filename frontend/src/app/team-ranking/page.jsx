'use client';
import { Navbar } from '@/components/Navbar';
import TeamRankingPage from '@/views/TeamRankingPage';
import { INIT_CRITERIA, INIT_TEAMS, MY_TEAM_ID } from '@/data';

export default function TeamRankingRoute() {
  return (
    <>
      <Navbar />
      <main style={{ minHeight: '100vh', background: '#f7f7f7', padding: '28px 32px', fontFamily: 'Inter, sans-serif' }}>
        <TeamRankingPage teams={INIT_TEAMS} criteria={INIT_CRITERIA} myId={MY_TEAM_ID} />
      </main>
    </>
  );
}
