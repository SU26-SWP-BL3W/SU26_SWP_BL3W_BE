'use client';

import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { useEvent, useEventModel, useEventRounds, useEventTracks } from '@/features/events/hooks/useEvents';
import { templatesApi } from '@/features/events/api/templates';
import { manageApi } from '@/features/events/api/manage';
import type { Event } from '@/features/events/types/event.types';
import type { EventRound, TrackItem, EventRole } from '@/features/events/api/manage';
import { Card } from './Card';
import { CardSkeleton } from './SkeletonLoaders';
import { formatDateTime } from '@/lib/date';
import { formatAdvancementRule } from '@/lib/events/advancementRule';

type TimelineVariant = 'admin' | 'participant';

interface Props {
  eventId: string;
  /** 'admin' hiển thị thêm Template · Judge · Mentor trong thẻ hạng mục. */
  variant?: TimelineVariant;
}

type NodeStatus = 'done' | 'active' | 'upcoming' | 'pending';

/** Một khoảng thời gian (một pha) của hạng mục. */
interface PhaseTime {
  start?: string | null;
  end?: string | null;
}

/** Dữ liệu để render một thẻ hạng mục: đúng 2 pha BE quản lý + thông tin admin. */
interface TrackCardData {
  id: string;
  trackName: string;
  description?: string;
  /** Nộp bài: startDate → endDate. */
  submit: PhaseTime;
  /** Chấm điểm: scoringStartDate → scoringEndDate. */
  scoring: PhaseTime;
  /** Chỉ có ở variant admin. */
  admin?: {
    template: string | null;
    submissionRules: string[];
    stats: { teams: number; mentors: number; judges: number };
  };
}

/** Một mốc lớn trên rail dọc: Mở đăng ký · từng Vòng · Kết quả chung cuộc.
 *  `pending` = backend chưa cấp ngày → render "Chưa cập nhật". */
interface TimelineNode {
  id: string;
  title: string;
  start?: string | null;
  end?: string | null;
  /** Dòng chi tiết một câu (quy tắc lên vòng…). */
  meta?: string;
  /** Cột số liệu bên phải (chỉ admin, cho mốc đăng ký). */
  sideStats?: { label: string; value: number }[];
  /** Các hạng mục của một vòng — render thành stack thẻ dưới tiêu đề vòng. */
  tracks?: TrackCardData[];
}

// ─── Status ──────────────────────────────────────────────────────────────────

/** Where a milestone sits relative to `now`. No dates → `pending`. */
function statusOf(now: number, start?: string | null, end?: string | null): NodeStatus {
  const s = start ? new Date(start).getTime() : NaN;
  const e = end ? new Date(end).getTime() : NaN;
  const hasS = !Number.isNaN(s);
  const hasE = !Number.isNaN(e);

  if (!hasS && !hasE) return 'pending';
  if (hasE && now > e) return 'done';
  if (hasS && now < s) return 'upcoming';
  // Mốc đơn ngày (chỉ có start, không có end) đã qua → coi như đã xong (xanh đậm),
  // thay vì kẹt ở "đang diễn ra" mãi. Mốc có khoảng thời gian thì vẫn "đang diễn ra".
  if (hasS && !hasE) return 'done';
  // now is within [start, end].
  return 'active';
}

/** Màu chấm trạng thái. */
const DOT: Record<NodeStatus, string> = {
  done: 'bg-primary-dark border-2 border-primary-dark',
  active: 'bg-primary border-2 border-primary',
  upcoming: 'bg-canvas border-2 border-hairline',
  pending: 'bg-canvas border-2 border-dashed border-mute',
};

/** Màu thanh viền trái của thẻ hạng mục theo trạng thái tổng. */
const CARD_BORDER_L: Record<NodeStatus, string> = {
  done: 'border-l-primary-dark',
  active: 'border-l-primary',
  upcoming: 'border-l-hairline',
  pending: 'border-l-mute',
};

// ─── Derive nodes from event data ─────────────────────────────────────────────

interface TrackDates {
  startDate?: string | null;
  endDate?: string | null;
  scoringStartDate?: string | null;
  scoringEndDate?: string | null;
}

interface BuildOpts {
  /** Bơm thông tin quản lý (template/judge/mentor) vào mỗi thẻ hạng mục. */
  admin?: boolean;
  /** Tra tên template chấm điểm từ id (chỉ dùng khi admin). */
  templateName?: (id: string | null) => string | null;
  /** Vai trò trong sự kiện (chỉ admin) — tính số đội/judge/mentor. */
  roles?: EventRole[];
  /** trackId → chuỗi yêu cầu nộp bài đã lưu (chỉ admin). */
  submissionRuleByTrackId?: Map<string, string>;
  /**
   * trackId → hai khoảng thời gian thực của hạng mục (Nộp bài / Chấm điểm).
   * Chỉ có trong model lồng GET /Events/{id}; danh sách phẳng /Tracks/event không trả.
   */
  trackDatesByTrackId?: Map<string, TrackDates>;
}

/** Tách chuỗi yêu cầu nộp bài đã lưu thành các dòng hiển thị (bỏ dòng trống). */
function parseSubmissionRuleLines(raw: string | null | undefined): string[] {
  if (!raw) return [];
  return raw
    .split('\n')
    .map((l) => l.trim())
    .filter(Boolean);
}

function buildTimeline(
  event: Event,
  rounds: EventRound[],
  tracks: TrackItem[],
  opts: BuildOpts = {},
): TimelineNode[] {
  const nodes: TimelineNode[] = [];
  const roles = opts.roles ?? [];
  const distinct = (arr: (string | null)[]) => new Set(arr.filter(Boolean)).size;

  // 1) Sự kiện bắt đầu (mốc đơn — event.startDate) — nằm trên mốc mở đăng ký.
  nodes.push({
    id: 'event-start',
    title: 'Bắt đầu sự kiện',
    start: event.startDate,
    end: null,
  });

  // 2) Mở đăng ký (chỉ ngày mở). Admin: số đội đã đăng ký (tổng đội phân biệt).
  nodes.push({
    id: 'registration-open',
    title: 'Mở đăng ký',
    start: event.registrationStartDate,
    end: null,
    sideStats: opts.admin
      ? [{ label: 'Đội đăng ký', value: distinct(roles.map((r) => r.teamId)) }]
      : undefined,
  });

  // 3) Đóng đăng ký (chỉ ngày đóng — tách từ khoảng đăng ký phía trên).
  nodes.push({
    id: 'registration-close',
    title: 'Đóng đăng ký',
    start: event.registrationEndDate,
    end: null,
  });

  // 4) Rounds, in order — mỗi vòng mang danh sách thẻ hạng mục của nó.
  const ordered = [...rounds].sort((a, b) => a.roundNumber - b.roundNumber);
  for (const round of ordered) {
    const roundTracks = tracks.filter((t) => t.roundId === round.id);

    // Mỗi hạng mục có đúng 2 khoảng thời gian do BE quản lý: Nộp bài
    // (startDate → endDate) và Chấm điểm (scoringStartDate → scoringEndDate).
    // Map trực tiếp từ API — KHÔNG nội suy/chia nhỏ thời gian.
    const trackCards: TrackCardData[] = roundTracks
      .map((t) => {
        const d = opts.trackDatesByTrackId?.get(t.id);
        return {
          id: t.id,
          trackName: t.trackName ?? '—',
          description: t.description || undefined,
          submit: { start: d?.startDate ?? null, end: d?.endDate ?? null },
          scoring: { start: d?.scoringStartDate ?? null, end: d?.scoringEndDate ?? null },
          admin: opts.admin
            ? {
                template: t.templateId ? opts.templateName?.(t.templateId) ?? '—' : null,
                submissionRules: parseSubmissionRuleLines(opts.submissionRuleByTrackId?.get(t.id)),
                stats: {
                  teams: distinct(roles.filter((r) => r.trackId === t.id).map((r) => r.teamId)),
                  mentors: t.mentors?.length ?? 0,
                  judges: t.judges?.length ?? 0,
                },
              }
            : undefined,
        };
      })
      .sort((a, b) => {
        const aTime = new Date(a.submit.start ?? a.scoring.start ?? '').getTime();
        const bTime = new Date(b.submit.start ?? b.scoring.start ?? '').getTime();
        const safeATime = Number.isNaN(aTime) ? Number.POSITIVE_INFINITY : aTime;
        const safeBTime = Number.isNaN(bTime) ? Number.POSITIVE_INFINITY : bTime;
        return safeATime - safeBTime || a.id.localeCompare(b.id);
      });

    nodes.push({
      id: `round-${round.id}`,
      title: `Vòng ${round.roundNumber}: ${round.roundName ?? '—'}`,
      start: round.startDate,
      end: round.endDate,
      meta: round.advancementRule ? formatAdvancementRule(round.advancementRule) : undefined,
      tracks: trackCards,
    });
  }

  // 5) Sự kiện kết thúc (mốc đơn — event.endDate).
  nodes.push({
    id: 'event-end',
    title: 'Kết thúc sự kiện',
    start: event.endDate,
    end: null,
  });

  return nodes;
}

// ─── Presentational pieces ────────────────────────────────────────────────────

/** Khoảng ngày giờ cho mốc lớn (rail). */
function DateRange({ status, start, end }: { status: NodeStatus; start?: string | null; end?: string | null }) {
  if (status === 'pending') {
    return <span className="t-body-md text-ink italic whitespace-nowrap">Chưa cập nhật thời gian</span>;
  }
  if (start && end) {
    return (
      <span className="t-body-md text-body whitespace-nowrap">
        {formatDateTime(start)} <span className="text-ink">→</span> {formatDateTime(end)}
      </span>
    );
  }
  return <span className="t-body-md text-body whitespace-nowrap">{formatDateTime(start ?? end)}</span>;
}

/** Khoảng ngày giờ gọn cho một pha bên trong thẻ hạng mục. */
function PhaseDate({ status, start, end }: { status: NodeStatus; start?: string | null; end?: string | null }) {
  if (status === 'pending') {
    return <span className="t-body-sm text-mute italic whitespace-nowrap">Chưa cập nhật</span>;
  }
  if (start && end) {
    return (
      <span className="t-body-sm text-body whitespace-nowrap">
        {formatDateTime(start)} <span className="text-mute">→</span> {formatDateTime(end)}
      </span>
    );
  }
  return <span className="t-body-sm text-body whitespace-nowrap">{formatDateTime(start ?? end)}</span>;
}

/** Một pha trong thẻ: [nhãn] [khoảng thời gian]. */
function PhaseRow({ now, label, time }: { now: number; label: string; time: PhaseTime }) {
  const status = statusOf(now, time.start, time.end);
  return (
    <div className="flex flex-wrap items-baseline gap-x-3 gap-y-0.5">
      <span className="t-body-sm text-ink shrink-0 w-24">{label}</span>
      <PhaseDate status={status} start={time.start} end={time.end} />
    </div>
  );
}

/** Thẻ một hạng mục: viền trái tô màu theo trạng thái tổng, bên trong 2 pha. */
function TrackCard({ track, now }: { track: TrackCardData; now: number }) {
  // Trạng thái tổng = từ lúc mở nộp bài đến khi đóng chấm điểm (chỉ chọn mốc thật).
  const overallStart = track.submit.start ?? track.scoring.start;
  const overallEnd = track.scoring.end ?? track.submit.end;
  const overall = statusOf(now, overallStart, overallEnd);
  return (
    <div
      className={`rounded-md border border-hairline border-l-[3px] ${CARD_BORDER_L[overall]} bg-canvas px-4 py-2.5 flex flex-col gap-1`}
    >
      <span className="t-body-strong text-ink">{track.trackName}</span>
      {track.description && <p className="t-body-sm text-body m-0">{track.description}</p>}

      <div className="flex flex-col gap-0.5 mt-0.5">
        <PhaseRow now={now} label="Nộp bài" time={track.submit} />
        <PhaseRow now={now} label="Chấm điểm" time={track.scoring} />
      </div>

      {track.admin && (
        <div className="mt-1 pt-1.5 border-t border-hairline flex flex-col sm:flex-row sm:justify-between gap-x-4 gap-y-1">
          {/* Trái: template + yêu cầu nộp bài. */}
          <div className="flex flex-col gap-1 min-w-0 flex-1">
            <p className="t-caption-sm m-0">
              <span className={track.admin.template ? 'text-ink' : 'text-error font-bold'}>
                {track.admin.template ? `Bộ tiêu chí: ${track.admin.template}` : 'Chưa gán bộ tiêu chí'}
              </span>
            </p>
            {track.admin.submissionRules.length > 0 && (
              <div>
                <p className="t-caption-sm text-ink m-0 mb-0.5">Yêu cầu nộp bài:</p>
                <ul className="m-0 pl-4 list-disc">
                  {track.admin.submissionRules.map((rule, i) => (
                    <li key={i} className="t-caption-sm text-ink">{rule}</li>
                  ))}
                </ul>
              </div>
            )}
          </div>

          {/* Phải: số liệu xếp dọc, ngang hàng với template. */}
          <div className="shrink-0 flex flex-col gap-1 sm:w-40">
            <p className="t-caption-sm text-mute m-0 whitespace-nowrap">
              Đội: <span className="text-ink">{track.admin.stats.teams}</span>
            </p>
            <p className="t-caption-sm text-mute m-0 whitespace-nowrap">
              Cố vấn: <span className="text-ink">{track.admin.stats.mentors}</span>
            </p>
            <p className="t-caption-sm text-mute m-0 whitespace-nowrap">
              Giám khảo: <span className="text-ink">{track.admin.stats.judges}</span>
            </p>
          </div>
        </div>
      )}
    </div>
  );
}

// ── Hình học rail (px) — 1 tầng chấm cho các mốc lớn ──
const DOT_X = 12; // tâm chấm
const DOT_Y = 11; // tâm chấm cách đỉnh dòng (canh dòng chữ đầu)

/** Đoạn đường dọc căn giữa tại `x`. `to='bottom'` = kéo hết đáy dòng. */
function Segment({ x, from, to }: { x: number; from: number; to: number | 'bottom' }) {
  return (
    <span
      className="absolute w-0.5 -translate-x-1/2 bg-hairline"
      style={{
        left: x,
        top: from,
        height: to === 'bottom' ? undefined : to - from,
        bottom: to === 'bottom' ? 0 : undefined,
      }}
    />
  );
}

/** Một mốc lớn trên rail: chấm + spine liền mạch + nội dung (+ stack thẻ hạng mục). */
function MilestoneRow({
  node,
  now,
  isFirst,
  isLast,
}: {
  node: TimelineNode;
  now: number;
  isFirst: boolean;
  isLast: boolean;
}) {
  const status = statusOf(now, node.start, node.end);
  return (
    <div className="relative flex">
      {/* Rail overlay: spine chạy liền mạch + chấm ở đỉnh dòng. */}
      <div className="absolute inset-0 pointer-events-none">
        {!isFirst && <Segment x={DOT_X} from={0} to={DOT_Y} />}
        {!isLast && <Segment x={DOT_X} from={DOT_Y} to="bottom" />}
        <span
          className={`absolute rounded-full w-3.5 h-3.5 ${DOT[status]}`}
          style={{ left: DOT_X, top: DOT_Y, transform: 'translate(-50%, -50%)' }}
        />
      </div>

      {/* Nội dung — pb ở đây để rail phủ hết chiều cao, line liền mạch.
          pl-24 cho mobile để né chấm; sm+ thụt sát chấm. */}
      <div className={`flex-1 min-w-0 pl-24 sm:pl-8 ${isLast ? '' : 'pb-10'}`}>
        {/* Header của mốc: [tiêu đề + meta] [thời gian] [số liệu] */}
        <div className="flex flex-col sm:flex-row sm:items-start sm:gap-x-3">
          <div className="flex flex-1 min-w-0 flex-wrap items-baseline gap-x-3 gap-y-1">
            <h3 className="t-heading-md text-ink m-0">{node.title}</h3>
            {node.meta && <p className="basis-full t-body-sm text-ink mt-0.5 mb-0">{node.meta}</p>}
            {/* Số liệu (vd Đội đăng ký) hiện dưới tiêu đề mốc, mỗi dòng một mục. */}
            {node.sideStats?.map((s) => (
              <p key={s.label} className="basis-full t-body-sm text-mute m-0">
                {s.label}: <span className="text-ink">{s.value}</span>
              </p>
            ))}
          </div>
          <div className="shrink-0 sm:text-right">
            <DateRange status={status} start={node.start} end={node.end} />
          </div>
        </div>

        {/* Stack thẻ hạng mục của vòng. */}
        {node.tracks && (
          <div className="mt-3 sm:pl-8 flex flex-col gap-3 max-w-2xl">
            {node.tracks.length === 0 ? (
              <p className="t-body-sm text-mute m-0">Chưa có hạng mục nào.</p>
            ) : (
              node.tracks.map((t) => <TrackCard key={t.id} track={t} now={now} />)
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Public component ─────────────────────────────────────────────────────────

/**
 * Vertical timeline of an event's milestones: registration → each round →
 * mỗi hạng mục là một thẻ với đúng 2 mốc BE quản lý (Nộp bài, Chấm điểm) → công
 * bố kết quả chung cuộc. Mốc chưa có ngày từ backend render là "Chưa cập nhật".
 */
export function EventTimeline({ eventId, variant = 'participant' }: Props) {
  const admin = variant === 'admin';
  const { data: event, isLoading: eventLoading } = useEvent(eventId);
  const { data: rounds = [], isLoading: roundsLoading } = useEventRounds(eventId);
  const { data: tracks = [], isLoading: tracksLoading } = useEventTracks(eventId);
  // Template & roles chỉ cần cho admin → không tải thừa ở participant.
  const { data: templates = [] } = useQuery({
    queryKey: ['templates'],
    queryFn: () => templatesApi.list(),
    enabled: admin,
    staleTime: 5 * 60 * 1000,
  });
  const { data: roles = [] } = useQuery({
    queryKey: ['eventRoles', eventId, undefined],
    queryFn: () => manageApi.listEventRoles(eventId),
    enabled: admin,
    staleTime: 2 * 60 * 1000,
  });
  // Thời gian Nộp bài / Chấm điểm của mỗi hạng mục (và yêu cầu nộp bài) chỉ có trong
  // model lồng của GET /Events/{id} (rounds[].tracks[]), không có ở /Tracks phẳng →
  // luôn tải model thô để map đúng thời gian cho cả admin lẫn participant.
  const { data: eventModel } = useEventModel(eventId);
  // Tính "now" một lần (tránh gọi Date.now() thuần trong render).
  const [now] = React.useState(() => Date.now());

  if (eventLoading || roundsLoading || tracksLoading) return <CardSkeleton />;
  if (!event) return null;

  const templateName = (id: string | null) =>
    id ? templates.find((t) => t.id === id)?.templateName ?? '—' : null;

  // trackId → yêu cầu nộp bài + thời gian Nộp bài/Chấm điểm (model lồng GET /Events/{id}).
  const submissionRuleByTrackId = new Map<string, string>();
  const trackDatesByTrackId = new Map<string, TrackDates>();
  const nestedRounds =
    (eventModel as {
      rounds?: Array<{
        tracks?: Array<{
          id?: string;
          submissionRuleDescription?: string | null;
          startDate?: string | null;
          endDate?: string | null;
          scoringStartDate?: string | null;
          scoringEndDate?: string | null;
        }>;
      }>;
    } | undefined)?.rounds ?? [];
  for (const r of nestedRounds) {
    for (const t of r.tracks ?? []) {
      if (!t.id) continue;
      submissionRuleByTrackId.set(t.id, t.submissionRuleDescription ?? '');
      trackDatesByTrackId.set(t.id, {
        startDate: t.startDate,
        endDate: t.endDate,
        scoringStartDate: t.scoringStartDate,
        scoringEndDate: t.scoringEndDate,
      });
    }
  }

  const nodes = buildTimeline(event, rounds, tracks, {
    admin,
    templateName,
    roles,
    submissionRuleByTrackId,
    trackDatesByTrackId,
  });

  return (
    <Card className="border-transparent">
      <h2 className="t-heading-md text-ink m-0 mb-4">Lịch trình sự kiện</h2>
      <div className="flex flex-col">
        {nodes.map((node, i) => (
          <MilestoneRow
            key={node.id}
            node={node}
            now={now}
            isFirst={i === 0}
            isLast={i === nodes.length - 1}
          />
        ))}
      </div>
    </Card>
  );
}
