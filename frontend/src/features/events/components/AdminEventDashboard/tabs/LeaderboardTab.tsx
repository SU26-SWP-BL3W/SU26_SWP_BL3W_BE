'use client';

import React, { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  useEventRounds,
  useRoundFinalResults,
  useCalculateRoundResults,
  useSetRoundPublishStatus,
  useCancelRoundResults,
  useTeams,
} from '@/features/events/hooks/useEvents';
import { useNotify } from '@/components/NotificationProvider';
import { useDialog } from '@/components/ConfirmDialogProvider';
import { StatusBadge } from '@/components/StatusBadge';
import { getErrorMessage } from '@/lib/apiError';
import { formatPrizeValue } from '@/lib/formatPrizeValue';
import { formatAdvancementRule } from '@/lib/events/advancementRule';
import { Card } from '../../EventDashboard/Card';
import { CardSkeleton } from '../../EventDashboard/SkeletonLoaders';
import { prizesApi } from '@/features/events/api/prizes';

interface LeaderboardTabProps {
  eventId: string;
}

export function LeaderboardTab({ eventId }: LeaderboardTabProps) {
  const notify = useNotify();
  const dialog = useDialog();
  const queryClient = useQueryClient();
  const { data: rounds = [], isLoading: roundsLoading } = useEventRounds(eventId);
  // Rounds sorted newest-first; default selection = latest round.
  const sortedRounds = useMemo(
    () => [...rounds].sort((a, b) => b.roundNumber - a.roundNumber),
    [rounds],
  );
  const [selectedRoundId, setSelectedRoundId] = useState<string>('');
  const selectedRound =
    sortedRounds.find((r) => r.id === selectedRoundId) ?? sortedRounds[0];
  const roundId = selectedRound?.id;

  const {
    data: results = [],
    isLoading: resultsLoading,
    error,
  } = useRoundFinalResults(roundId);
  const { data: teams = [], isLoading: teamsLoading } = useTeams();
  const prizesQuery = useQuery({
    queryKey: ['prizes', eventId],
    queryFn: () => prizesApi.listByEvent(eventId),
    enabled: !!eventId,
  });
  const assignPrize = useMutation({
    mutationFn: ({ finalResultId, prizeId }: { finalResultId: string; prizeId: string | null }) =>
      prizesApi.assignToResult(finalResultId, prizeId),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ['finalResults', roundId] });
      const prize = prizesQuery.data?.find((item) => item.id === variables.prizeId);
      notify.success(prize ? `Đã trao “${prize.prizeName}”.` : 'Đã bỏ giải thưởng khỏi đội.');
    },
    onError: (error) => notify.error(getErrorMessage(error, 'Không thể cập nhật giải thưởng.')),
  });
  const calculate = useCalculateRoundResults(roundId);
  const setPublishStatus = useSetRoundPublishStatus(roundId);
  const cancel = useCancelRoundResults(roundId);

  // topN chỉ là dự phòng: khi vòng CHƯA đặt luật thăng vòng thì mới cần nhập tay.
  const [topN, setTopN] = useState<string>('');

  // ─── Trạng thái vòng thi (suy ra từ dữ liệu — BE không có field status) ───
  // "Đã kết thúc" = đã qua endDate (đồng bộ với logic khóa form của ScoringPanel).
  // 3 trạng thái kết quả, dựa vào field `isPublished` THẬT từ Backend (FinalResultModel)
  // — trước đây suy sai bằng `results.length > 0`, khiến EC/Admin vừa "Tính kết quả"
  // (kết quả còn NHÁP) đã bị coi là "Đã công bố" vì họ có quyền xem cả nháp.
  const roundEnded = selectedRound?.endDate
    ? new Date() >= new Date(selectedRound.endDate)
    : false;
  const hasResults = results.length > 0;
  const isPublished = hasResults && results[0].isPublished;
  const busy = calculate.isPending || setPublishStatus.isPending || cancel.isPending || assignPrize.isPending;
  // Số đội thăng vòng lấy từ advancementRule (vd "top:2"). Có rule → BE tự quyết,
  // bỏ qua mọi topN gửi lên. Chưa có rule → cần EC nhập topN dự phòng.
  const advancementRule = selectedRound?.advancementRule?.trim() ?? '';
  const hasRule = advancementRule !== '';

  // ─── FE-01: Tính kết quả (KHÔNG tự công bố — chỉ ra bản nháp để rà soát) ───
  // Vòng có advancementRule → không gửi topN (BE tự quyết). Vòng chưa có rule →
  // gửi topN EC nhập (nếu bỏ trống, BE dùng mặc định của nó).
  const handleCalculate = async () => {
    if (!roundId || busy) return;
    let parsedTopN: number | undefined;
    if (!hasRule) {
      const raw = topN.trim();
      if (raw !== '') {
        parsedTopN = Number(raw);
        if (!Number.isInteger(parsedTopN) || parsedTopN <= 0) {
          notify.warning('Số đội thăng vòng phải là số nguyên dương, hoặc để trống.');
          return;
        }
      }
    }
    const ok = await dialog.confirm({
      title: 'Tính kết quả',
      message: roundEnded
        ? `Tính kết quả (bản nháp) cho "${selectedRound?.roundName ?? 'vòng thi'}"? Sau khi tính, bạn có thể rà soát trước khi bấm "Công bố" riêng.`
        : `Vòng thi hoặc (các) hạng mục CHƯA kết thúc nộp bài/chấm điểm. Tính kết quả bây giờ chỉ dựa trên dữ liệu hiện có và CÓ THỂ thay đổi nếu có bài nộp/điểm chấm mới sau đó. Vẫn muốn tính trước kết quả (bản nháp) cho "${selectedRound?.roundName ?? 'vòng thi'}"?`,
      confirmText: 'Tính kết quả',
    });
    if (!ok) return;

    calculate.mutate(parsedTopN, {
      onSuccess: (rows) => {
        notify.success(`Đã tính xong ${rows.length} đội (bản nháp) — rà soát rồi bấm "Công bố" để mọi người xem được.`);
      },
      // BE trả các lỗi nghiệp vụ: chưa chấm hết bài, vòng chưa đóng, không đủ quyền...
      onError: (err) => notify.error(getErrorMessage(err)),
    });
  };

  // ─── FE-03: Bật/tắt công bố — đảo được 2 chiều, KHÔNG mất dữ liệu đã tính
  // (khác hẳn "Xóa & tính lại" bên dưới, vốn xóa sạch để tính lại từ đầu). ───
  const handleTogglePublish = async () => {
    if (!roundId || !hasResults || busy) return;
    const nextPublished = !isPublished;
    const ok = await dialog.confirm({
      title: nextPublished ? 'Công bố kết quả' : 'Thu hồi về nháp',
      message: nextPublished
        ? (!roundEnded
            ? `Vòng thi vẫn đang diễn ra, bạn có chắc chắn muốn chốt kết quả và công bố luôn không? Mọi người sẽ xem được kết quả của "${selectedRound?.roundName ?? 'vòng thi'}" ngay lập tức.`
            : `Công bố bảng xếp hạng của "${selectedRound?.roundName ?? 'vòng thi'}"? Sau khi công bố, mọi người (không chỉ EC/Admin) sẽ xem được kết quả này.`)
        : `Thu hồi bảng xếp hạng của "${selectedRound?.roundName ?? 'vòng thi'}" về bản nháp? Chỉ EC/Admin xem được, người ngoài sẽ không thấy nữa. Dữ liệu Rank/Điểm vẫn được giữ nguyên, có thể công bố lại bất cứ lúc nào.`,
      confirmText: nextPublished ? 'Công bố' : 'Thu hồi',
    });
    if (!ok) return;

    setPublishStatus.mutate(nextPublished, {
      onSuccess: () => notify.success(nextPublished ? 'Đã công bố bảng xếp hạng.' : 'Đã thu hồi về bản nháp.'),
      onError: (err) => notify.error(getErrorMessage(err)),
    });
  };

  // ─── FE-02: Xóa & tính lại — XÓA SẠCH Rank/FinalScore/IsAdvanced đã tính, dùng khi
  // cần calculate lại từ đầu (vd phát hiện sai sót, đổi topN). KHÁC với bật/tắt công
  // bố ở trên (chỉ ẩn/hiện, không mất dữ liệu). ───
  const handleRecalculate = async () => {
    if (!roundId || !hasResults || busy) return;
    const ok = await dialog.confirm({
      title: 'Xóa kết quả để tính lại',
      message:
        `Xóa toàn bộ kết quả đã tính của "${selectedRound?.roundName ?? 'vòng thi'}" để tính lại từ đầu?\n\n` +
        (isPublished ? 'Bảng xếp hạng đang công bố sẽ bị gỡ khỏi mọi người xem.\n' : '') +
        'Chỉ thực hiện được khi vòng sau chưa có bài nộp/kết quả.',
      confirmText: 'Xóa & tính lại',
      danger: true,
    });
    if (!ok) return;

    cancel.mutate(undefined, {
      onSuccess: () => notify.success('Đã xóa kết quả. Bấm "Tính kết quả vòng" để tính lại.'),
      onError: (err) => notify.error(getErrorMessage(err)),
    });
  };

  if (error) {
    return (
      <div className="bg-error/10 border border-error rounded-sm p-6 text-center">
        <p className="t-body-md text-error font-bold">Không tải được bảng xếp hạng</p>
      </div>
    );
  }

  if (roundsLoading || teamsLoading) return <CardSkeleton />;

  if (sortedRounds.length === 0) {
    return (
      <Card title="Bảng xếp hạng">
        <p className="t-body-sm text-mute text-center py-8">Sự kiện chưa có vòng thi nào.</p>
      </Card>
    );
  }

  const teamName = (teamId: string | null) =>
    teams.find((t) => t.id === teamId)?.name ?? teamId ?? '—';
  // Use backend rank when present, otherwise sort by score desc.
  const ranked = [...results].sort(
    (a, b) => (a.rank || 9999) - (b.rank || 9999) || b.finalScore - a.finalScore,
  );

  return (
    <Card title="Bảng xếp hạng">
      {/* ─── Thanh điều khiển: chọn vòng + tính / hủy kết quả ─── */}
      <div className="flex flex-col md:flex-row md:items-end gap-3 mb-5 pb-5 border-b border-hairline">
        <div className="flex flex-col gap-1">
          <label className="t-caption-sm text-mute font-bold uppercase">Vòng thi</label>
          <select
            value={selectedRound?.id ?? ''}
            onChange={(e) => setSelectedRoundId(e.target.value)}
            className="border border-hairline rounded-sm px-3 py-2 t-body-sm text-ink bg-canvas min-w-56"
          >
            {sortedRounds.map((r) => (
              <option key={r.id} value={r.id}>
                {r.roundName ?? `Vòng ${r.roundNumber}`}
              </option>
            ))}
          </select>
        </div>

        {hasResults ? (
          // ─── FE-03: có kết quả (nháp hoặc đã công bố) → toggle bật/tắt công bố +
          // hành động riêng "Xóa & tính lại" khi cần làm lại từ đầu ───
          <div className="flex items-center gap-2 md:ml-auto">
            <StatusBadge tone={isPublished ? 'success' : 'pending'}>
              {isPublished ? 'Đã công bố' : 'Bản nháp — chưa công bố'}
            </StatusBadge>
            <button
              type="button"
              onClick={handleRecalculate}
              disabled={busy}
              className="btn btn-delete disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {cancel.isPending ? 'Đang tính lại…' : 'Tính lại'}
            </button>
            <button
              type="button"
              onClick={handleTogglePublish}
              disabled={busy}
              className="btn btn-update disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {setPublishStatus.isPending
                ? 'Đang lưu…'
                : isPublished ? 'Ẩn' : 'Công bố'}
            </button>
          </div>
        ) : (
          // ─── FE-01: chưa tính kết quả ───
          <>
            {hasRule ? (
              // Vòng đã đặt luật thăng vòng → BE tự quyết số đội, không cần nhập.
              <div className="flex flex-col gap-1">
                <label className="t-caption-sm text-mute font-bold uppercase">Luật thăng vòng</label>
                <span className="t-body-sm text-ink font-bold px-3 py-2 bg-surface-soft rounded-sm border border-hairline">
                  {formatAdvancementRule(advancementRule)}
                </span>
              </div>
            ) : (
              // Vòng chưa đặt luật → EC nhập số đội thăng (dự phòng).
              <div className="flex flex-col gap-1">
                <label className="t-caption-sm text-mute font-bold uppercase">
                  Số đội thăng vòng
                </label>
                <input
                  type="number"
                  min={1}
                  step={1}
                  value={topN}
                  onChange={(e) => setTopN(e.target.value)}
                  placeholder="Vòng chưa có luật — nhập số đội"
                  className="border border-hairline rounded-sm px-3 py-2 t-body-sm text-ink bg-canvas w-56"
                />
              </div>
            )}
            <div className="flex flex-col gap-1 md:ml-auto">
              <button
                type="button"
                onClick={handleCalculate}
                disabled={busy}
                className="px-4 py-2 rounded-sm t-body-sm font-bold bg-primary text-on-primary disabled:opacity-50 disabled:cursor-not-allowed hover:opacity-90 transition-opacity focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary"
              >
                {calculate.isPending ? 'Đang tính…' : 'Tính kết quả vòng'}
              </button>
            </div>
          </>
        )}
      </div>

      {resultsLoading ? (
        <CardSkeleton />
      ) : ranked.length > 0 ? (
        <div className="overflow-x-auto">
          <table className="w-full border-collapse">
            <thead>
              <tr className="border-b border-hairline-strong text-left">
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center w-16">Hạng</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2">Tên đội</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Điểm TB</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 text-center">Trạng thái</th>
                <th className="t-caption-md text-mute font-bold uppercase py-3 px-2 min-w-52">Giải thưởng</th>
              </tr>
            </thead>
            <tbody>
              {ranked.map((row, idx) => {
                const rank = row.rank || idx + 1;
                return (
                  <tr key={row.id} className="border-b border-hairline last:border-b-0">
                    <td className="py-3 px-2 text-center">
                      <span className="t-body-strong font-bold text-ink">{rank}</span>
                    </td>
                    <td className="t-body-sm font-bold text-ink py-3 px-2">{teamName(row.teamId)}</td>
                    <td className="t-heading-sm text-primary font-bold py-3 px-2 text-center">{row.finalScore}</td>
                    <td className="py-3 px-2 text-center">
                      <StatusBadge tone={row.isAdvanced ? 'success' : 'pending'}>
                        {row.isAdvanced ? 'Đi tiếp' : 'Dừng lại'}
                      </StatusBadge>
                    </td>
                    <td className="py-3 px-2">
                      <select
                        value={row.prizeId ?? ''}
                        disabled={assignPrize.isPending || prizesQuery.isLoading}
                        onChange={(event) => assignPrize.mutate({
                          finalResultId: row.id,
                          prizeId: event.target.value || null,
                        })}
                        className="text-input w-full min-w-48 disabled:opacity-60 disabled:cursor-not-allowed"
                        aria-label={`Giải thưởng của ${teamName(row.teamId)}`}
                      >
                        <option value="">Không đạt giải</option>
                        {(prizesQuery.data ?? []).map((prize) => (
                          <option key={prize.id} value={prize.id}>
                            {prize.prizeName} - {formatPrizeValue(prize.value)}
                          </option>
                        ))}
                      </select>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      ) : (
        <p className="t-body-sm text-mute text-center py-8">
          {roundEnded
            ? 'Chưa có kết quả. Bấm "Tính kết quả vòng" để ra bản nháp, rà soát rồi công bố riêng.'
            : 'Chưa có dữ liệu xếp hạng.'}
        </p>
      )}
    </Card>
  );
}
