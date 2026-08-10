'use client';

import { useQuery } from '@tanstack/react-query';
import { useEventRounds, useEventTracks } from '@/features/events/hooks/useEvents';
import { templatesApi } from '../../api/templates';
import { Card } from '../EventDashboard/Card';
import { CardSkeleton } from '../EventDashboard/SkeletonLoaders';
import { formatAdvancementRule } from '@/lib/events/advancementRule';

interface Props {
  eventId: string;
}

function formatDate(iso: string) {
  if (!iso) return '—';
  const d = new Date(iso);
  return Number.isNaN(d.getTime())
    ? '—'
    : d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
}

/** Read-only view of an event's full structure: rounds → tracks (same data
 *  captured at creation, different layout). */
export function EventStructureView({ eventId }: Props) {
  const { data: rounds = [], isLoading: roundsLoading } = useEventRounds(eventId);
  const { data: tracks = [], isLoading: tracksLoading } = useEventTracks(eventId);
  const { data: templates = [] } = useQuery({
    queryKey: ['templates'],
    queryFn: () => templatesApi.list(),
    staleTime: 5 * 60 * 1000,
  });

  const templateName = (id: string | null) =>
    id ? templates.find((t) => t.id === id)?.templateName ?? '—' : '—';

  if (roundsLoading || tracksLoading) return <CardSkeleton />;

  if (rounds.length === 0) {
    return (
      <Card title="Các vòng thi">
        <p className="t-body-sm text-mute text-center py-8">Sự kiện chưa có vòng thi nào.</p>
      </Card>
    );
  }

  const ordered = [...rounds].sort((a, b) => a.roundNumber - b.roundNumber);

  return (
    <div className="flex flex-col gap-4 md:gap-6">
      {ordered.map((round) => {
        const roundTracks = tracks.filter((t) => t.roundId === round.id);
        return (
          <Card
            key={round.id}
            title={`Vòng ${round.roundNumber}: ${round.roundName ?? '—'}`}
          >
            <div className="flex flex-col gap-4">
              {/* Round meta */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="bg-surface-soft border border-hairline rounded-sm px-4 py-3">
                  <p className="t-caption-xs text-mute uppercase font-bold m-0">Bắt đầu</p>
                  <p className="t-body-sm text-ink m-0 mt-1">{formatDate(round.startDate)}</p>
                </div>
                <div className="bg-surface-soft border border-hairline rounded-sm px-4 py-3">
                  <p className="t-caption-xs text-mute uppercase font-bold m-0">Kết thúc</p>
                  <p className="t-body-sm text-ink m-0 mt-1">{formatDate(round.endDate)}</p>
                </div>
                <div className="bg-surface-soft border border-hairline rounded-sm px-4 py-3">
                  <p className="t-caption-xs text-mute uppercase font-bold m-0">Quy tắc lên vòng</p>
                  <p className="t-body-sm text-ink m-0 mt-1">
                    {formatAdvancementRule(round.advancementRule)}
                  </p>
                </div>
              </div>

              {/* Tracks */}
              <div className="flex flex-col gap-3">
                <p className="t-caption-md text-mute uppercase font-bold m-0">
                  Hạng mục ({roundTracks.length})
                </p>
                {roundTracks.length === 0 ? (
                  <p className="t-body-sm text-mute">Chưa có hạng mục nào.</p>
                ) : (
                  roundTracks.map((track) => (
                    <div
                      key={track.id}
                      className="border border-hairline rounded-sm p-4 flex flex-col gap-2"
                    >
                      <div className="flex items-center justify-between gap-3 flex-wrap">
                        <span className="t-body-strong text-ink">{track.trackName ?? '—'}</span>
                        <span className="t-caption-sm text-mute">
                          Bộ tiêu chí: {templateName(track.templateId)}
                        </span>
                      </div>
                      {track.description && (
                        <p className="t-body-sm text-body m-0">{track.description}</p>
                      )}
                      <div className="flex gap-6 flex-wrap">
                        <span className="t-caption-sm text-mute">
                          Giám khảo: {track.judges?.length ?? 0}
                        </span>
                        <span className="t-caption-sm text-mute">
                          Cố vấn: {track.mentors?.length ?? 0}
                        </span>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </div>
          </Card>
        );
      })}
    </div>
  );
}
