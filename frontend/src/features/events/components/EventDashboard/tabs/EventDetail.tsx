'use client';

import React from 'react';
import { useEvent } from '@/features/events/hooks/useEvents';
import { Card } from '../Card';
import { CardSkeleton } from '../SkeletonLoaders';
import { EventTimeline } from '../EventTimeline';
import { EventPhoto } from '../../EventPhoto';
import { formatPrizeValue } from '@/lib/formatPrizeValue';

interface Props { eventId: string; userId: string; }

/**
 * Participant view of an event: same read-only structure the admin sees
 * (detail card + rounds/tracks), but without any edit controls.
 */
export function EventDetailTab({ eventId }: Props) {
  const { data: event, isLoading, error } = useEvent(eventId);

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4 md:gap-6">
        <CardSkeleton />
        <CardSkeleton />
      </div>
    );
  }
  if (error || !event) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được sự kiện</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      <Card className="border-transparent">
        <div className="space-y-4">
          <EventPhoto url={event.photoEventUrl} alt={event.title} />
          <h1 className="text-ink text-center font-bold m-0 leading-tight text-4xl md:text-5xl">{event.title}</h1>
          {(event.season || event.year || event.maxTeams) && (
            <div className="flex flex-wrap items-center justify-center gap-2 md:gap-4 text-ink m-0 font-bold text-lg md:text-xl">
              {event.season || event.year ? (
                <span>{[event.season, event.year].filter(Boolean).join(' · ')}</span>
              ) : null}
              {(event.season || event.year) && event.maxTeams ? (
                <span className="text-mute px-2">•</span>
              ) : null}
              {event.maxTeams && (
                <span className="flex items-center gap-1.5" title="Số đội tối đa">
                  <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                    <circle cx="9" cy="7" r="4"></circle>
                    <path d="M23 21v-2a4 4 0 0 0-3-3.87"></path>
                    <path d="M16 3.13a4 4 0 0 1 0 7.75"></path>
                  </svg>
                  Giới hạn {event.maxTeams} đội
                </span>
              )}
            </div>
          )}
          <p className="text-body whitespace-pre-line leading-relaxed max-w-2xl mx-auto text-center font-normal text-base md:text-lg">
            {event.description || 'Chưa có mô tả.'}
          </p>

          {event.prizes && event.prizes.length > 0 && (
            <div className="mt-8 border-t border-line pt-6 w-full">
              <h2 className="text-xl md:text-2xl font-bold text-ink text-center mb-6">Cơ cấu giải thưởng</h2>
              <div className="flex flex-wrap justify-center gap-4">
                {event.prizes.map((prize) => (
                  <div key={prize.id} className="bg-surface border border-line rounded-lg p-4 text-center min-w-[200px] flex-1 max-w-[250px]">
                    <h3 className="font-bold text-lg text-primary m-0">{prize.prizeName}</h3>
                    <p className="text-ink font-semibold m-0 mt-2 tabular-nums">{formatPrizeValue(prize.value)}</p>
                    <p className="text-mute text-sm m-0 mt-1">Số lượng: {prize.quantity}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </Card>

      <div className="w-full max-w-7xl mx-auto">
        <EventTimeline eventId={eventId} />
      </div>
    </div>
  );
}
