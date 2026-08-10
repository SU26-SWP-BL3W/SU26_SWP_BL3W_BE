'use client';

import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { appealsApi, type AppealModel } from '@/features/appeals/api/appeals';
import { useEventRounds } from '@/features/events/hooks/useEvents';
import { resultsApi } from '@/features/results/api/results';
import { useNotify } from '@/components/NotificationProvider';
import { getErrorMessage } from '@/lib/apiError';
import { formatDateTime } from '@/lib/date';
import { submitResultsApi } from '@/features/events/api/submitResults';
import { manageApi } from '@/features/events/api/manage';
import { eventRolesApi } from '@/features/events/api/eventRoles';

interface Props {
  eventId: string;
  teamId?: string;
  isLeader?: boolean;
  mode?: 'manager' | 'team';
}

const statusOf = (appeal: AppealModel) => {
  const raw = String(appeal.statusLabel ?? appeal.status).toLowerCase();
  if (raw === '1' || raw.includes('approved') || raw.includes('chấp nhận') || raw.includes('duyệt')) {
    return { label: 'Đã chấp nhận', cls: 'text-primary border-primary' };
  }
  if (raw === '2' || raw.includes('rejected') || raw.includes('từ chối')) {
    return { label: 'Đã từ chối', cls: 'text-error border-error' };
  }
  return { label: 'Đang chờ', cls: 'text-warning border-warning' };
};

export default function AppealsPanel({ eventId, teamId = '', isLeader = false, mode = 'manager' }: Props) {
  const manager = mode === 'manager';
  const notify = useNotify();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [roundId, setRoundId] = useState('');
  const [responding, setResponding] = useState<AppealModel | null>(null);
  const [response, setResponse] = useState('');
  const [assignedJudgeId, setAssignedJudgeId] = useState('');
  const [creating, setCreating] = useState(false);
  const [submitResultId, setSubmitResultId] = useState('');
  const [reason, setReason] = useState('');
  const { data: rounds = [] } = useEventRounds(manager ? eventId : '');

  const selectedRoundId = roundId || rounds[0]?.id || '';

  const queryKey = manager
    ? ['appeals', 'round', selectedRoundId, page]
    : ['appeals', 'team', teamId, page];
  const appealsQuery = useQuery({
    queryKey,
    queryFn: () => manager
      ? appealsApi.listByRound(selectedRoundId, page, 20)
      : appealsApi.listByTeam(teamId, page, 20),
    enabled: manager ? !!selectedRoundId : !!teamId,
  });

  const teamsQuery = useQuery({
    queryKey: ['appealTeams', eventId],
    queryFn: () => manageApi.listTeams(eventId),
    enabled: manager && !!eventId,
  });
  const submissionsQuery = useQuery({
    queryKey: ['appealSubmissions', eventId, selectedRoundId],
    queryFn: () => submitResultsApi.list({ eventId, roundId: selectedRoundId, pageSize: 200 }),
    enabled: manager && !!eventId && !!selectedRoundId,
  });
  const submissionFromList = submissionsQuery.data?.find(
    (submission) => submission.id === responding?.submitResultId,
  );
  const respondingSubmissionQuery = useQuery({
    queryKey: ['appealSubmission', responding?.submitResultId],
    queryFn: () => submitResultsApi.getById(responding!.submitResultId),
    enabled: manager && !!responding?.submitResultId && !submissionFromList,
  });
  const respondingSubmission = submissionFromList ?? respondingSubmissionQuery.data;
  const judgesQuery = useQuery({
    queryKey: ['appealJudges', eventId, respondingSubmission?.trackId],
    queryFn: () => eventRolesApi.listByRole(eventId, respondingSubmission!.trackId, 'Judge'),
    enabled: manager && !!responding && !!respondingSubmission?.trackId,
  });

  const breakdownQuery = useQuery({
    queryKey: ['appealEligibleSubmissions', teamId],
    queryFn: () => resultsApi.getTeamBreakdown(teamId),
    enabled: !manager && !!teamId && isLeader,
  });
  const eligible = useMemo(
    () => (breakdownQuery.data?.submissions ?? []).filter((s) => s.judgeScores.length > 0),
    [breakdownQuery.data],
  );

  const respondMutation = useMutation({
    mutationFn: ({ status }: { status: 1 | 2 }) =>
      appealsApi.respond(responding!.id, {
        status,
        response: response.trim(),
        ...(status === 1 ? { assignedJudgeId } : {}),
      }),
    onSuccess: (_data, variables) => {
      notify.success(variables.status === 1 ? 'Đã chấp nhận đơn phúc khảo.' : 'Đã từ chối đơn phúc khảo.');
      setResponding(null);
      setResponse('');
      setAssignedJudgeId('');
      queryClient.invalidateQueries({ queryKey: ['appeals', 'round', selectedRoundId] });
    },
    onError: (error) => notify.error(getErrorMessage(error, 'Không thể phản hồi đơn phúc khảo.')),
  });

  const createMutation = useMutation({
    mutationFn: () => appealsApi.create({ submitResultId, reason: reason.trim() }),
    onSuccess: () => {
      notify.success('Đã gửi đơn phúc khảo.');
      setCreating(false);
      setSubmitResultId('');
      setReason('');
      queryClient.invalidateQueries({ queryKey: ['appeals', 'team', teamId] });
    },
    onError: (error) => notify.error(getErrorMessage(error, 'Không thể gửi đơn phúc khảo.')),
  });

  const data = appealsQuery.data;
  const teamNameOf = (appeal: AppealModel) => {
    if (!manager) return appeal.teamName?.trim() || 'Đội của tôi';
    if (appeal.teamName?.trim()) return appeal.teamName;
    const teamId = appeal.teamId
      || submissionsQuery.data?.find((submission) => submission.id === appeal.submitResultId)?.teamId;
    return teamsQuery.data?.find((team) => team.id === teamId)?.name || teamId || 'Không xác định';
  };
  return (
    <section className="w-full space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h2 className="t-heading-md m-0">Phúc khảo</h2>
          <p className="t-body-sm text-mute mt-1">
            {manager ? 'Xem xét và phản hồi đơn phúc khảo theo vòng thi.' : 'Theo dõi lịch sử phúc khảo của đội.'}
          </p>
        </div>
        {manager ? (
          <label className="flex flex-col gap-1">
            <span className="t-caption-sm text-mute font-bold uppercase">Vòng thi</span>
            <select className="text-input min-w-56" value={selectedRoundId} onChange={(e) => { setRoundId(e.target.value); setPage(1); }}>
              {rounds.map((round) => <option key={round.id} value={round.id}>{round.roundName ?? `Vòng ${round.roundNumber}`}</option>)}
            </select>
          </label>
        ) : isLeader ? (
          <button type="button" className="btn btn-primary" onClick={() => setCreating(true)}>Gửi phúc khảo</button>
        ) : null}
      </div>

      {appealsQuery.isLoading ? <p className="t-body-sm text-mute">Đang tải đơn phúc khảo…</p> : null}
      {appealsQuery.isError ? <p className="t-body-sm text-error">{getErrorMessage(appealsQuery.error, 'Không tải được đơn phúc khảo.')}</p> : null}
      {!appealsQuery.isLoading && !data?.items.length ? <div className="card text-center text-mute">Chưa có đơn phúc khảo nào.</div> : null}

      {!!data?.items.length && (
        <div className="w-full">
          <table className="w-full table-fixed border-collapse">
            <colgroup>
              <col style={{ width: manager ? '14%' : '16%' }} />
              <col style={{ width: manager ? '25.5%' : '27%' }} />
              <col className="hidden md:table-column" style={{ width: manager ? '14%' : '15%' }} />
              <col style={{ width: manager ? '13%' : '15%' }} />
              <col style={{ width: manager ? '25.5%' : '27%' }} />
              {manager && <col style={{ width: '10%' }} />}
            </colgroup>
            <thead>
              <tr className="border-b border-hairline-strong text-left">
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Đội</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Lý do</th>
                <th className="hidden md:table-cell t-caption-md text-mute font-bold uppercase py-3 px-2">Ngày gửi</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Trạng thái</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Phản hồi</th>
                {manager && <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-right">Thao tác</th>}
              </tr>
            </thead>
            <tbody>
              {data.items.map((appeal) => {
                const status = statusOf(appeal);
                return (
                  <tr key={appeal.id} className="border-b border-hairline last:border-b-0 align-top">
                    <td className="t-body-sm font-bold text-ink py-3 px-2 break-words">{teamNameOf(appeal)}</td>
                    <td className="t-body-sm text-body py-3 px-2 whitespace-pre-wrap break-words">{appeal.reason}</td>
                    <td className="hidden md:table-cell t-body-sm text-body py-3 px-2 break-words">{formatDateTime(appeal.createdTime ?? appeal.createdAt)}</td>
                    <td className="py-3 px-2">
                      <span className={`inline-block max-w-full bg-canvas border rounded-sm px-2 py-1 t-caption-sm font-bold uppercase break-words ${status.cls}`}>{status.label}</span>
                    </td>
                    <td className="t-body-sm text-body py-3 px-2 whitespace-pre-wrap break-words">
                      {appeal.response?.trim() || <span className="text-mute italic">Chưa có phản hồi</span>}
                    </td>
                    {manager && (
                      <td className="py-3 px-2 text-right">
                        <button type="button" className="btn btn-primary btn-sm max-w-full px-2" onClick={() => { setResponding(appeal); setResponse(appeal.response ?? ''); setAssignedJudgeId(appeal.assignedJudgeId ?? ''); }}>
                          Phản hồi
                        </button>
                      </td>
                    )}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {(data?.totalPages ?? 1) > 1 && <div className="flex justify-end items-center gap-2">
        <button className="btn btn-outline btn-sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Trước</button>
        <span className="t-body-sm">{page}/{data?.totalPages}</span>
        <button className="btn btn-outline btn-sm" disabled={page >= (data?.totalPages ?? 1)} onClick={() => setPage((p) => p + 1)}>Sau</button>
      </div>}

      {responding && <div className="modal-overlay" role="presentation" onMouseDown={() => setResponding(null)} style={{ zIndex: 200, padding: '80px 16px 24px', alignItems: 'flex-start' }}>
        <div className="modal-box" role="dialog" aria-modal="true" onMouseDown={(e) => e.stopPropagation()} style={{ width: 'min(100%, 640px)', maxWidth: 640, minWidth: 0, flexShrink: 0 }}>
          <h3 className="t-heading-sm mt-0">Phản hồi đơn phúc khảo</h3>
          <p className="t-body-sm text-mute">{responding.reason}</p>
          <label className="flex flex-col gap-1">
            <span className="t-caption-sm font-bold uppercase">Nội dung phản hồi</span>
            <textarea className="text-input resize-y" style={{ minHeight: 120, width: '100%' }} value={response} onChange={(e) => setResponse(e.target.value)} placeholder="Nhập nội dung phản hồi…" />
          </label>
          <label className="flex flex-col gap-1 mt-3">
            <span className="t-caption-sm font-bold uppercase">Giám khảo chấm lại</span>
            <select
              className="text-input"
              value={assignedJudgeId}
              onChange={(e) => setAssignedJudgeId(e.target.value)}
              disabled={judgesQuery.isLoading || !respondingSubmission?.trackId}
            >
              <option value="">
                {judgesQuery.isLoading ? 'Đang tải giám khảo...' : 'Chọn giám khảo'}
              </option>
              {(judgesQuery.data ?? []).map((judge) => (
                <option key={judge.id} value={judge.id}>
                  {judge.user?.fullName || judge.user?.email || judge.userId}
                </option>
              ))}
            </select>
            {respondingSubmissionQuery.isLoading && (
              <span className="t-caption-sm text-mute">Đang xác định hạng mục của bài nộp...</span>
            )}
            {!respondingSubmissionQuery.isLoading && !respondingSubmission?.trackId && (
              <span className="t-caption-sm text-error">Không xác định được hạng mục của bài nộp.</span>
            )}
            {judgesQuery.isError && (
              <span className="t-caption-sm text-error">Không tải được danh sách giám khảo.</span>
            )}
            {!judgesQuery.isLoading && !judgesQuery.isError && respondingSubmission?.trackId && !judgesQuery.data?.length && (
              <span className="t-caption-sm text-warning">Hạng mục này chưa có giám khảo.</span>
            )}
          </label>
          <div className="flex flex-wrap justify-end gap-2 mt-4">
            <button className="btn btn-outline" onClick={() => { setResponding(null); setAssignedJudgeId(''); }}>Hủy</button>
            <button className="btn btn-outline-danger" disabled={!response.trim() || respondMutation.isPending} onClick={() => respondMutation.mutate({ status: 2 })}>Từ chối</button>
            <button className="btn btn-primary" disabled={!response.trim() || !assignedJudgeId || respondMutation.isPending} onClick={() => respondMutation.mutate({ status: 1 })}>Xác nhận duyệt</button>
          </div>
        </div>
      </div>}

      {creating && <div className="modal-overlay" role="presentation" onMouseDown={() => setCreating(false)} style={{ zIndex: 200, padding: '80px 16px 24px', alignItems: 'flex-start' }}>
        <form className="modal-box" style={{ width: 'min(100%, 640px)', maxWidth: 640, minWidth: 0, flexShrink: 0 }} onSubmit={(e) => { e.preventDefault(); createMutation.mutate(); }} onMouseDown={(e) => e.stopPropagation()}>
          <h3 className="t-heading-sm mt-0">Gửi đơn phúc khảo</h3>
          <label className="flex flex-col gap-1"><span className="t-caption-sm font-bold uppercase">Bài nộp đã có điểm</span>
            <select className="text-input" required value={submitResultId} onChange={(e) => setSubmitResultId(e.target.value)}>
              <option value="">Chọn bài nộp</option>
              {eligible.map((submission) => <option key={submission.submitResultId} value={submission.submitResultId}>{submission.roundName} — {submission.trackName}</option>)}
            </select>
          </label>
          {!breakdownQuery.isLoading && eligible.length === 0 && <p className="t-body-sm text-mute">Đội chưa có bài nộp nào đã được chấm điểm.</p>}
          <label className="flex flex-col gap-1 mt-3"><span className="t-caption-sm font-bold uppercase">Lý do</span>
            <textarea className="text-input min-h-32 resize-y" required value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Trình bày lý do phúc khảo…" />
          </label>
          <div className="flex justify-end gap-2 mt-4"><button type="button" className="btn btn-outline" onClick={() => setCreating(false)}>Hủy</button><button className="btn btn-primary" disabled={!submitResultId || !reason.trim() || createMutation.isPending}>Gửi phúc khảo</button></div>
        </form>
      </div>}
    </section>
  );
}
