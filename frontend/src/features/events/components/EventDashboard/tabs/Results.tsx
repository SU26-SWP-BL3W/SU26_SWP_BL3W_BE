'use client';

import React from 'react';
import { ResultsAccordion } from '@/features/results/components/ResultsAccordion';

interface Props { teamId: string; eventId: string; }

export function ResultsTab({ teamId, eventId }: Props) {
  return (
    <section className="p-6 max-w-3xl mx-auto space-y-4">
      <h2 className="t-heading-md">Kết quả</h2>
      <ResultsAccordion teamId={teamId} eventId={eventId} />
    </section>
  );
}
