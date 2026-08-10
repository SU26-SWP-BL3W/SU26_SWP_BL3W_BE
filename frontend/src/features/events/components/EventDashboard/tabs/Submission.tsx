'use client';

import React, { useState } from 'react';
import type { AxiosError } from 'axios';
import { useEventModel, useEventRounds, useEventTracks } from '@/features/events/hooks/useEvents';
import { useTeamSubmissions, useCreateSubmission, useUpdateSubmission } from '@/features/submissions/hooks/useSubmissions';
import type { SubmissionModel } from '@/features/submissions/api/submissions';
import { useNotify } from '@/components/NotificationProvider';
import { StatusBadge } from '@/components/StatusBadge';
import {
  parseRuleLabels,
  parseSubmissionLinks,
  DEFAULT_LINK_LABEL,
  type SubmissionLink,
} from '@/features/submissions/utils/submissionLinks';

/** Surface the backend's message (e.g. "Đã hết hạn nộp bài cho vòng thi này.")
 *  instead of a generic failure. */
function submitErrorMessage(err: unknown): string {
  const ax = err as AxiosError<{ message?: string }>;
  return ax?.response?.data?.message || ax?.message || 'Nộp bài thất bại.';
}

const fmt = (iso: string) =>
  new Date(iso).toLocaleString('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });

interface RoundInfo { id: string; roundName: string | null; startDate: string; endDate: string; }
type RoundSubmissionStatus = 'not-open' | 'open' | 'closed';

function roundSubmissionStatus(round: RoundInfo, now: number): RoundSubmissionStatus {
  const start = new Date(round.startDate).getTime();
  const end = new Date(round.endDate).getTime();
  if (now < start) return 'not-open';
  if (now <= end) return 'open';
  return 'closed';
}

const ROUND_STATUS_LABEL: Record<RoundSubmissionStatus, string> = {
  'not-open': 'Chưa mở',
  open: 'Đã mở',
  closed: 'Đã đóng',
};

interface EventModelWithSubmissionRules {
  rounds?: Array<{
    id?: string;
    tracks?: Array<{
      id: string;
      trackName?: string | null;
      submissionRuleDescription?: string | null;
      startDate?: string | null;
      endDate?: string | null;
    }>;
  }>;
}

interface Props { teamId: string; eventId: string; }

export function SubmissionTab({ teamId, eventId }: Props) {
  const notify = useNotify();
  const { data: rounds = [] } = useEventRounds(eventId);
  const { data: tracks = [] } = useEventTracks(eventId);

  // Yêu cầu nộp bài chỉ có trong model lồng của GET /Events/{id} (rounds[].tracks[])
  const { data: eventModel } = useEventModel(eventId);

  const [roundId, setRoundId] = useState('');
  const [trackId, setTrackId] = useState('');
  // FORM ĐỘNG: mỗi phần tử = 1 ô nhập link theo cấu hình của Track (submissionRuleDescription).
  // Track không cấu hình -> 1 ô "Link nộp bài" như cũ.
  const [links, setLinks] = useState<SubmissionLink[]>([]);
  const [description, setDesc] = useState('');
  const [copiedUrl, setCopiedUrl] = useState<string | null>(null);
  // Bài đang sửa (null = đang ở chế độ nộp mới). Chỉ sửa được link + mô tả, trong thời gian vòng còn mở.
  const [editing, setEditing] = useState<SubmissionModel | null>(null);
  const [now] = useState(() => Date.now());

  // eventId bắt buộc: filter phân quyền BE cần EventId (thiếu sẽ bị 400).
  const {
    data: existing = [],
    isLoading: existingLoading,
    error: existingError,
  } = useTeamSubmissions(teamId, eventId);
  const create = useCreateSubmission(teamId);
  const update = useUpdateSubmission(teamId);

  const allRounds = rounds as RoundInfo[];
  type SubmissionTrack = {
    id: string;
    roundId: string;
    trackName: string | null;
    submissionRuleDescription?: string | null;
  };
  const flatTracks = tracks as SubmissionTrack[];
  const nestedRounds =
    (eventModel as EventModelWithSubmissionRules | undefined)?.rounds ?? [];
  const nestedTracks: SubmissionTrack[] = nestedRounds.flatMap((round) =>
    (round.tracks ?? []).map((track) => ({
      id: track.id,
      roundId: round.id ?? '',
      trackName: track.trackName ?? null,
      submissionRuleDescription: track.submissionRuleDescription,
    })),
  );
  const flatTrackIds = new Set(flatTracks.map((track) => track.id.toLowerCase()));
  const allTracks = [
    ...flatTracks,
    ...nestedTracks.filter((track) => !flatTrackIds.has(track.id.toLowerCase())),
  ];

  // Sinh danh sách ô nhập link theo cấu hình của Track đang chọn:
  // mỗi dòng trong submissionRuleDescription = 1 loại link phải nộp.
  const buildLinksForTrack = (tid: string): SubmissionLink[] => {
    // Tìm rules từ nested model thay vì flat model (do BE không trả field này ở flat list)
    let ruleDesc: string | null | undefined = null;
    for (const r of nestedRounds) {
      const track = (r.tracks ?? []).find(
        (item) => item.id.toLowerCase() === tid.toLowerCase(),
      );
      if (track) {
        ruleDesc = track.submissionRuleDescription;
        break;
      }
    }

    const labels = parseRuleLabels(ruleDesc);
    return labels.length > 0
      ? labels.map((label) => ({ label, url: '' }))
      : [{ label: DEFAULT_LINK_LABEL, url: '' }];
  };

  const tracksForRound = allTracks.filter(
    (track) => track.roundId.toLowerCase() === roundId.toLowerCase(),
  );
  const trackName = (id: string) =>
    allTracks.find((track) => track.id.toLowerCase() === id.toLowerCase())?.trackName ?? '—';
  const trackSubmissionWindow = (tid: string) => {
    for (const round of nestedRounds) {
      const track = (round.tracks ?? []).find(
        (item) => item.id.toLowerCase() === tid.toLowerCase(),
      );
      if (track) return { startDate: track.startDate, endDate: track.endDate };
    }
    return undefined;
  };
  const trackSubmissionStatus = (tid: string): RoundSubmissionStatus | undefined => {
    const window = trackSubmissionWindow(tid);
    if (!window?.startDate || !window.endDate) return undefined;
    return roundSubmissionStatus(
      {
        id: tid,
        roundName: null,
        startDate: window.startDate,
        endDate: window.endDate,
      },
      now,
    );
  };

  // Vòng của một bài nộp (suy từ track) — để biết bài còn trong hạn sửa hay không.
  const roundOfTrack = (tid: string): RoundInfo | undefined => {
    const track = allTracks.find((item) => item.id.toLowerCase() === tid.toLowerCase());
    return track
      ? allRounds.find(
        (round) => round.id.toLowerCase() === track.roundId.toLowerCase(),
      )
      : undefined;
  };
  const isRoundOpen = (r?: RoundInfo) => {
    if (!r) return false;
    return roundSubmissionStatus(r, now) === 'open';
  };

  const selectedRound = allRounds.find((r) => r.id === roundId);
  const selectedRoundStatus = selectedRound
    ? roundSubmissionStatus(selectedRound, now)
    : undefined;
  const selectedTrackStatus = trackId ? trackSubmissionStatus(trackId) : undefined;
  const selectedTrackWindow = trackId ? trackSubmissionWindow(trackId) : undefined;
  const selectedClosed = !!selectedRoundStatus && selectedRoundStatus !== 'open';
  const selectedTrackClosed = !!trackId && selectedTrackStatus !== 'open';

  // Gộp danh sách link thành giá trị gửi lên BE (trường submissionUrl):
  // - Track có cấu hình nhiều loại link -> JSON.stringify([{label,url},...])
  // - 1 ô mặc định (track không cấu hình) -> gửi chuỗi URL thường như cũ (tương thích ngược)
  const linksToSubmissionUrl = (list: SubmissionLink[]): string => {
    const cleaned = list.map((l) => ({ label: l.label, url: l.url.trim() }));
    const isConfigured = cleaned.length > 1 || (cleaned.length === 1 && cleaned[0].label !== DEFAULT_LINK_LABEL);
    return isConfigured ? JSON.stringify(cleaned) : (cleaned[0]?.url ?? '');
  };

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    const submissionUrl = linksToSubmissionUrl(links);
    if (editing) {
      // isActive gửi đúng giá trị hiện tại — chỉ Event Coordinator được đổi (BE chặn nếu đổi).
      update.mutate(
        { id: editing.id, submissionUrl, description, isActive: editing.isActive },
        { onSuccess: () => { setEditing(null); setLinks(trackId ? buildLinksForTrack(trackId) : []); setDesc(''); } },
      );
      return;
    }
    create.mutate(
      { teamId, trackId, submissionUrl, description },
      { onSuccess: () => { setLinks(buildLinksForTrack(trackId)); setDesc(''); } },
    );
  };

  const startEdit = (s: SubmissionModel) => {
    setEditing(s);
    // Hiện lại đúng các link đã nộp (JSON -> danh sách; chuỗi cũ -> 1 link mặc định).
    setLinks(parseSubmissionLinks(s.submissionUrl));
    setDesc(s.description ?? '');
  };
  const cancelEdit = () => { setEditing(null); setLinks(trackId ? buildLinksForTrack(trackId) : []); setDesc(''); };

  const mutError = editing ? update.error : create.error;
  const pending = editing ? update.isPending : create.isPending;

  return (
    <section className="p-6 max-w-2xl mx-auto space-y-6">
      <h2 className="t-heading-md">{editing ? 'Sửa bài nộp' : 'Nộp bài'}</h2>

      <form onSubmit={submit} className="space-y-3 border border-hairline rounded-sm bg-white p-4 md:p-6">
        {editing ? (
          <p className="t-body-sm text-mute">
            Đang sửa bài của hạng mục <b className="text-ink">{trackName(editing.trackId)}</b> — chỉ đổi được link và mô tả.
          </p>
        ) : (
          <>
            <label className="block">
              <span className="t-body-sm font-bold">Vòng</span>
              <select required value={roundId} onChange={(e) => { setRoundId(e.target.value); setTrackId(''); setLinks([]); }} className="input w-full mt-1">
                <option value="">— Chọn vòng —</option>
                {allRounds.map((r) => {
                  const status = roundSubmissionStatus(r, now);
                  return (
                    <option key={r.id} value={r.id}>
                      {r.roundName ?? 'Vòng ' + r.id.slice(0, 4)}
                      {' '}({ROUND_STATUS_LABEL[status]})
                    </option>
                  );
                })}
              </select>
            </label>

            {selectedRound && (
              <p className={`t-body-sm ${selectedClosed ? 'text-error font-bold' : 'text-mute'}`}>
                Thời gian nộp: {fmt(selectedRound.startDate)} → {fmt(selectedRound.endDate)}
                {selectedRoundStatus === 'not-open' && ' — Vòng chưa mở'}
                {selectedRoundStatus === 'open' && ' — Đã mở nộp bài'}
                {selectedRoundStatus === 'closed' && ' — Đã hết hạn nộp bài'}
              </p>
            )}

            <label className="block">
              <span className="t-body-sm font-bold">Hạng mục thi đấu</span>
              <select
                required value={trackId}
                onChange={(e) => {
                  const v = e.target.value;
                  setTrackId(v);
                  // Đọc cấu hình của track vừa chọn -> sinh đúng số ô nhập link
                  setLinks(v ? buildLinksForTrack(v) : []);
                }}
                className="input w-full mt-1" disabled={!roundId}
              >
                <option value="">— Chọn hạng mục —</option>
                {tracksForRound.map((t) => {
                  const status = trackSubmissionStatus(t.id);
                  return (
                    <option key={t.id} value={t.id}>
                      {t.trackName ?? 'Hạng mục ' + t.id.slice(0, 4)}
                      {' '}({status ? ROUND_STATUS_LABEL[status] : 'Chưa cập nhật thời gian'})
                    </option>
                  );
                })}
              </select>
            </label>

            {trackId && (
              <p className={`t-body-sm ${selectedTrackClosed ? 'text-error font-bold' : 'text-mute'}`}>
                {selectedTrackWindow?.startDate && selectedTrackWindow.endDate
                  ? `Thời gian nộp hạng mục: ${fmt(selectedTrackWindow.startDate)} → ${fmt(selectedTrackWindow.endDate)}`
                  : 'Hạng mục chưa được cập nhật thời gian nộp bài'}
                {selectedTrackStatus === 'not-open' && ' — Hạng mục chưa mở'}
                {selectedTrackStatus === 'open' && ' — Đã mở nộp bài'}
                {selectedTrackStatus === 'closed' && ' — Đã hết hạn nộp bài'}
              </p>
            )}
          </>
        )}

        {/* FORM ĐỘNG: mỗi loại link trong cấu hình track = 1 ô nhập riêng */}
        {links.length === 0 ? (
          <p className="t-body-sm text-mute">Chọn vòng và hạng mục để hiện các link cần nộp.</p>
        ) : (
          links.map((lnk, idx) => {
            const isGithub = lnk.label.toLowerCase().includes('github');
            return (
              <label key={idx} className="block">
                <span className="t-body-sm font-bold">{lnk.label}</span>
                <input
                  required type="url" value={lnk.url} 
                  placeholder={isGithub ? "https://github.com/tentaikhoan/tenduano" : "https://…"}
                  onChange={(e) =>
                    setLinks((prev) => prev.map((x, i) => (i === idx ? { ...x, url: e.target.value } : x)))
                  }
                  pattern={isGithub ? "^https?:\\/\\/(www\\.)?github\\.com\\/.*" : "https?:\\/\\/.*"}
                  title={isGithub ? "Vui lòng nhập đúng đường dẫn từ github.com (vd: https://github.com/user/repo)" : "Vui lòng nhập đường dẫn hợp lệ (bắt đầu bằng http:// hoặc https://)"}
                  className="input w-full mt-1"
                />
              </label>
            );
          })
        )}

        <label className="block">
          <span className="t-body-sm font-bold">Mô tả</span>
          <textarea value={description} onChange={(e) => setDesc(e.target.value)} rows={3} className="input w-full mt-1" />
        </label>

        {mutError ? <p className="t-body-sm text-error">{submitErrorMessage(mutError)}</p> : null}

        <div className="flex gap-2">
          <button
            type="submit"
            disabled={pending || (!editing && (selectedClosed || selectedTrackClosed))}
            className={`btn ${editing ? 'btn-update' : 'btn-create'}`}
          >
            {pending ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Nộp bài'}
          </button>
          {editing && (
            <button type="button" onClick={cancelEdit} className="btn">
              Hủy
            </button>
          )}
        </div>
      </form>

      <div>
        <h3 className="t-body-md font-bold mb-2">Đã nộp</h3>
        {existingLoading ? (
          <p className="t-body-sm text-mute">Đang tải…</p>
        ) : existingError ? (
          <p className="t-body-sm text-error">{submitErrorMessage(existingError)}</p>
        ) : existing.length === 0 ? (
          <p className="t-body-sm text-mute">Chưa có bài nộp nào.</p>
        ) : (
          <ul className="space-y-3">
            {existing.map((s) => {
              const r = roundOfTrack(s.trackId);
              const canEdit =
                isRoundOpen(r) && trackSubmissionStatus(s.trackId) === 'open';
              return (
                <li key={s.id} className="border border-hairline rounded-sm bg-white p-4 space-y-2">
                  {/* Dòng 1: trạng thái + hạng mục / vòng + nút Sửa */}
                  <div className="flex items-center justify-between gap-3 flex-wrap">
                    <div className="flex items-center gap-2 flex-wrap min-w-0">
                      <StatusBadge tone="success">Đã nộp</StatusBadge>
                      <StatusBadge tone="neutral">{trackName(s.trackId)}</StatusBadge>
                      {r && <span className="t-body-sm text-ink">{r.roundName ?? 'Vòng thi'}</span>}
                    </div>
                    {canEdit ? (
                      <button type="button" onClick={() => startEdit(s)} className="btn btn-update shrink-0 text-xs px-3 py-1">
                        Sửa bài
                      </button>
                    ) : (
                      <StatusBadge tone="inactive" className="shrink-0">Đã khóa</StatusBadge>
                    )}
                  </div>

                  {/* Danh sách link đã nộp: JSON [{label,url}] -> từng dòng theo nhãn;
                      dữ liệu cũ (1 URL thường) -> 1 dòng như trước (tương thích ngược). */}
                  {parseSubmissionLinks(s.submissionUrl).map((lnk, i) => (
                    <div key={i} className="flex items-center gap-2 min-w-0">
                      <span className="t-body-sm font-bold shrink-0">{lnk.label}:</span>
                      <a
                        href={lnk.url} target="_blank" rel="noreferrer"
                        className="t-body-sm text-primary underline truncate flex-1 min-w-0"
                        title={lnk.url}
                      >
                        {lnk.url}
                      </a>
                      <button
                        type="button"
                        className="btn btn-outline btn-sm ml-auto shrink-0"
                        onClick={() => {
                          navigator.clipboard?.writeText(lnk.url)
                            .then(() => {
                              setCopiedUrl(lnk.url);
                              notify.success(`Đã copy ${lnk.label}.`);
                              window.setTimeout(() => setCopiedUrl((current) => current === lnk.url ? null : current), 2000);
                            })
                            .catch(() => notify.error('Không copy được, hãy copy thủ công.'));
                        }}
                      >
                        {copiedUrl === lnk.url ? 'Đã copy' : 'Copy'}
                      </button>
                    </div>
                  ))}

                  {/* Mô tả (nếu có) */}
                  {s.description ? (
                    <p className="t-body-sm text-mute m-0">
                      <b className="text-ink">Mô tả:</b> {s.description}
                    </p>
                  ) : null}

                  {/* Meta: thời gian nộp + hạn sửa */}
                  <div className="flex items-center gap-4 flex-wrap t-body-sm text-mute">
                    <span>Nộp lúc: {fmt(s.createdTime)}</span>
                    {r && (canEdit ? (
                      <span className="font-bold" style={{ color: '#5a8d00' }}>
                        Còn sửa được đến {fmt(r.endDate)}
                      </span>
                    ) : (
                      <span>Hết hạn sửa: {fmt(r.endDate)}</span>
                    ))}
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </section>
  );
}
