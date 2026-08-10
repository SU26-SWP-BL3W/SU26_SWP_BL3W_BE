'use client';
import { useState, useEffect, useCallback } from 'react';
import Notif from '@/components/Notif';
import EditModal from '@/components/EditModal';
import ScoringView from '@/views/ScoringPage';
import { useCurrentUser } from '@/hooks/useAuth';
import { scoresApi } from '@/services/api';
import { eventRolesApi } from '@/features/events/api/eventRoles';
import { tracksApi, roundsApi } from '@/features/events/api/roundTrack';
import { resultsApi } from '@/features/results/api/results';
import { getCriteria } from '@/services/criteriaService';
import { getAllSubmissions } from '@/services/submissionService';
import { getErrorMessage } from '@/lib/apiError';

/**
 * Phần chấm điểm tái sử dụng từ trang /scoring, nhúng vào tab dashboard.
 * Dùng chung cho cả trang standalone lẫn tab trong sự kiện.
 *
 * Props: eventId (sự kiện đang mở), trackId (nếu có — judge chỉ chấm hạng mục được giao;
 * nếu không truyền sẽ lấy từ vai trò của judge trong sự kiện).
 *
 * @param {{ eventId: string, trackId?: string | null }} props
 */
export default function ScoringPanel({ eventId, trackId = null }) {
  const { data: currentUser } = useCurrentUser();

  const [criteria, setCriteria] = useState([]);
  const [teams, setTeams] = useState([]);
  const [eventRoleId, setEventRoleId] = useState(null);
  const [roundInfo, setRoundInfo] = useState(null); // { roundName, startDate, endDate }
  const [lock, setLock] = useState({ locked: false, message: null });
  const [editT, setEditT] = useState(null);
  const [notif, setNotif] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const sn = useCallback((m, t = 's') => {
    setNotif({ m, t });
    setTimeout(() => setNotif(null), 3000);
  }, []);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      if (!currentUser?.id || !eventId) {
        if (!cancelled) setLoading(false);
        return;
      }
      try {
        setLoading(true);
        setError(null);

        // 1) Vai trò của judge trong sự kiện → eventRoleId (bắt buộc cho Scores/save)
        //    và trackId nếu component không được truyền sẵn.
        const role = await eventRolesApi.getUserRole(currentUser.id, eventId);
        const roleId = role?.id ?? null;
        const effectiveTrackId = trackId ?? role?.trackId ?? null;
        if (!roleId || !effectiveTrackId) {
          throw new Error('Bạn chưa được phân công chấm hạng mục nào trong sự kiện này.');
        }

        // 2) Chi tiết track → templateId (bộ tiêu chí) + roundId (thời gian vòng thi).
        const track = await tracksApi.getById(effectiveTrackId);
        if (!track?.templateId) {
          throw new Error('Hạng mục này chưa được gán bộ tiêu chí chấm điểm.');
        }

        // 3) Tải song song: vòng thi, bộ tiêu chí, danh sách bài nộp, phiếu đã chấm,
        //    và trạng thái công bố kết quả (FinalResults của vòng đã tồn tại?).
        const [round, allCriteria, subsRes, existingScores, published] = await Promise.all([
          track.roundId ? roundsApi.getById(track.roundId) : Promise.resolve(null),
          getCriteria(track.templateId),
          getAllSubmissions({
            EventId: eventId,
            TrackId: effectiveTrackId,
            ...(track.roundId ? { RoundId: track.roundId } : {}),
            PageSize: 100,
          }),
          scoresApi.listByEventRole(roleId),
          track.roundId
            ? resultsApi.listRoundLeaderboard(track.roundId).then(r => r.length > 0).catch(() => false)
            : Promise.resolve(false),
        ]);

        if (cancelled) return;

        setRoundInfo(round ? {
          roundName: round.roundName ?? `Vòng ${round.roundNumber ?? '?'}`,
          startDate: round.startDate ?? null,
          endDate:   round.endDate   ?? null,
        } : null);

        // 4) Bộ tiêu chí: getCriteria(templateId) đã map sẵn shape ScoringView/EditModal cần
        //    ({ id, label, labelVi, desc, weight, maxScore, isActive }) với weight/maxScore lấy
        //    từ template. Nó trả về TOÀN BỘ tiêu chí hệ thống nên chỉ giữ tiêu chí đang bật &
        //    thực sự thuộc template (weight > 0).
        // templateId cần cho mỗi detail khi lưu điểm (Backend yêu cầu — xem saveEdit bên dưới).
        // QUAN TRỌNG: Backend validate điểm nhập KHÔNG được vượt quá `maxScore` thật lưu trong
        // DB của từng tiêu chí (trả lỗi 400 nếu vượt) — nên KHÔNG được hardcode maxScore ở FE,
        // phải dùng đúng giá trị backend trả về để tránh lệch với validate phía server.
        // Nếu data cũ trong DB bị sai (vd maxScore=1, 1.5 thay vì 10) thì đây là lỗi DATA cần
        // Admin sửa ở /criteria — không phải lỗi hiển thị của FE (xem fix-templatepage-he100.md).
        const crit = allCriteria
          .filter(c => c.isActive !== false && c.weight > 0)
          .map(c => ({ ...c, maxScore: c.maxScore ?? 0, templateId: track.templateId }));

        // 5) Nạp trước điểm đã chấm: submitResultId → điểm theo từng tiêu chí + nhận xét chung.
        const scored = existingScores.filter(s => s.submitResultId);
        const detailList = await Promise.all(
          scored.map(s =>
            scoresApi.getWithDetails(s.id)
              // `comment` đọc kiểu defensive (?? '') vì kiểu ScoreWithDetails hiện chưa khai báo
              // field này — nếu Backend có trả thật, vẫn nạp lại đúng; nếu không có, mặc định rỗng.
              .then(d => ({ submitResultId: s.submitResultId, details: d.details ?? [], comment: d.comment ?? '' }))
              .catch(() => ({ submitResultId: s.submitResultId, details: [], comment: '' })),
          ),
        );
        if (cancelled) return;

        const prefill = {};
        const prefillComment = {};
        for (const { submitResultId, details, comment } of detailList) {
          const byCriteria = Object.fromEntries(details.map(d => [d.criteriaId, d.value]));
          prefill[submitResultId] = crit.map(c => byCriteria[c.id] ?? 0);
          prefillComment[submitResultId] = comment;
        }

        // 6) Bài nộp → "teams" (ẩn danh khi hiển thị; id = submitResultId dùng để lưu điểm).
        const mapped = subsRes.items.map(s => ({
          id: s.id,
          teamId: s.teamId, // Thêm teamId thật để đồng bộ mã ẩn danh với SubmissionsPanel
          name: s.teamName ?? s.projectName ?? '—',
          scores: prefill[s.id] ?? crit.map(() => 0),
          // 1 nhận xét chung cho cả phiếu (Score.comment) — không còn theo từng tiêu chí.
          // Chưa chắc backend model gõ đúng field này (xem ghi chú ở bước 5) nên fallback rỗng.
          comment: prefillComment[s.id] ?? '',
        }));

        // 7) Khóa/mở form theo thời gian & trạng thái công bố (đồng bộ với Backend).
        // Ưu tiên dùng Track.scoringStartDate/scoringEndDate (cửa sổ chấm điểm thật của
        // Hạng mục). Chỉ fallback về Track.endDate/Round.endDate nếu Track chưa cấu hình
        // scoringStartDate (dữ liệu cũ) — round.endDate là mốc đóng TOÀN BỘ vòng thi, sau
        // cả chấm điểm + công bố, dùng nó để mở form chỉ là phương án dự phòng cuối cùng.
        const now = new Date();
        const scoringStart = track?.scoringStartDate ? new Date(track.scoringStartDate) : null;
        const scoringEnd = track?.scoringEndDate ? new Date(track.scoringEndDate) : null;
        const fallbackDate = track?.endDate
          ? new Date(track.endDate)
          : (round?.endDate ? new Date(round.endDate) : null);
        const startDate = scoringStart ?? fallbackDate;
        const notStarted = startDate ? now < startDate : false;
        const scoringOver = scoringEnd ? now > scoringEnd : false;
        let lockState = { locked: false, message: null };
        if (published) {
          lockState = { locked: true, message: 'Vòng thi này đã chốt kết quả, không thể chấm điểm thêm.' };
        } else if (scoringOver) {
          lockState = { locked: true, message: 'Đã hết thời gian chấm điểm cho hạng mục này.' };
        } else if (notStarted) {
          const when = startDate.toLocaleString('vi-VN', {
            day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
          });
          lockState = { locked: true, message: `Form chấm điểm sẽ được mở vào lúc ${when}.` };
        }

        setEventRoleId(roleId);
        setCriteria(crit);
        setTeams(mapped);
        setLock(lockState);
      } catch (e) {
        // Lỗi validate tự ném (Error thường, không có .response) → giữ nguyên message gốc.
        // Lỗi từ API (axios, có .response) → lấy message thật từ backend thay vì text
        // chung chung "Request failed with status code ..." để biết đúng lý do (vd hết
        // hạn chấm, chưa tới vòng chấm, đã công bố kết quả...).
        if (!cancelled) {
          setError(e?.response ? getErrorMessage(e, 'Không thể tải dữ liệu chấm điểm.') : (e?.message ?? 'Không thể tải dữ liệu chấm điểm.'));
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    load();
    return () => { cancelled = true; };
  }, [currentUser?.id, eventId, trackId]);

  const saveEdit = async (submitResultId, { details, comment }) => {
    // Chặn sớm ở client cho đúng logic khóa/mở; Backend vẫn là nguồn quyết định cuối cùng.
    if (lock.locked) {
      sn(lock.message ?? 'Chưa thể chấm điểm ở thời điểm này.', 'e');
      return;
    }
    // `details` đến từ EditModal đã được ghép sẵn theo criteriaId ({ criteriaId, value }),
    // không còn là mảng thô theo index nữa — tránh bug lệch điểm giữa các tiêu chí (từng xảy ra
    // khi `criteria` state ở đây đổi thứ tự so với lúc EditModal khởi tạo `scores`).
    const byCriteria = Object.fromEntries(criteria.map(c => [c.id, c]));
    try {
      await scoresApi.save({
        eventRoleId,
        submitResultId,
        isSubmitted: false,
        // Backend chỉ hỗ trợ 1 nhận xét chung cho cả phiếu (Score.comment) — không có nhận
        // xét theo từng tiêu chí (CriterionScoreLine không có field comment).
        comment,
        // Backend (SaveScoreRequestModel.cs) chỉ nhận field `value`, không có `score`.
        // templateId bắt buộc trong mỗi detail (thiếu → 400) — lấy theo đúng criteriaId,
        // không dựa vào vị trí mảng.
        details: details.map(d => ({
          criteriaId: d.criteriaId,
          value: d.value ?? 0,
          templateId: byCriteria[d.criteriaId]?.templateId,
        })),
      });
      // Cập nhật lại state local (dùng cho bảng danh sách + lần mở EditModal kế tiếp), ghép
      // theo đúng thứ tự `criteria` hiện tại — an toàn vì tra theo criteriaId, không theo index.
      const detailByCriteria = Object.fromEntries(details.map(d => [d.criteriaId, d]));
      const scores = criteria.map(c => detailByCriteria[c.id]?.value ?? 0);
      setTeams(p => p.map(t => t.id === submitResultId ? { ...t, scores, comment } : t));
      setEditT(null);
      sn('Đã lưu điểm thành công!');
    } catch (e) {
      // Đọc message lỗi từ Backend (400: vòng thi chưa kết thúc / 403: đã công bố kết quả).
      const msg = e?.response ? getErrorMessage(e, 'Lưu điểm thất bại, vui lòng thử lại') : (e?.message ?? 'Lưu điểm thất bại, vui lòng thử lại');
      sn(msg, 'e');
    }
  };

  const fmt = (iso) => iso
    ? new Date(iso).toLocaleString('vi-VN', {
        day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
      })
    : '—';

  return (
    <>
      <Notif n={notif} />
      {editT && !lock.locked && (
        <EditModal
          team={editT}
          criteria={criteria}
          onClose={() => setEditT(null)}
          onSave={(payload) => saveEdit(editT.id, payload)}
        />
      )}

      {loading ? (
        <div className="flex items-center justify-center py-20">
          <span className="loading loading-spinner" style={{ color: '#76b900' }} />
          <span className="ml-3 text-sm" style={{ color: '#757575' }}>Đang tải dữ liệu...</span>
        </div>
      ) : error ? (
        <div className="text-center py-20">
          <p className="text-sm font-semibold" style={{ color: '#d32f2f' }}>{error}</p>
        </div>
      ) : (
        <>
          {roundInfo && (
            <div
              className="mb-4 flex items-center gap-3 px-4 py-3"
              style={{ background: 'rgba(118,185,0,0.08)', border: '1px solid rgba(118,185,0,0.3)', borderRadius: 2 }}
            >
              <span style={{ color: '#76b900', fontSize: 14 }}>🏁</span>
              <div className="text-sm" style={{ color: '#5a8d00' }}>
                <span className="font-bold">{roundInfo.roundName}</span>
                {roundInfo.startDate && roundInfo.endDate && (
                  <span style={{ fontWeight: 400, color: '#757575', marginLeft: 8 }}>
                    {fmt(roundInfo.startDate)} → {fmt(roundInfo.endDate)}
                  </span>
                )}
              </div>
            </div>
          )}

          {teams.length === 0 ? (
            <div className="text-center py-20">
              <p className="text-sm" style={{ color: '#757575' }}>Chưa có đội nào nộp bài trong hạng mục của bạn.</p>
            </div>
          ) : (
            <>
              {lock.locked && (
                <div
                  className="mb-5 flex items-start gap-3 p-4"
                  style={{ background: 'rgba(255,193,7,.1)', border: '1px solid #ffc107', borderRadius: 2 }}
                  role="alert"
                >
                  <span style={{ color: '#b8860b', fontSize: 16, lineHeight: 1.2 }}>⚠</span>
                  <p className="text-sm m-0" style={{ color: '#8a6d00' }}>{lock.message}</p>
                </div>
              )}
              <ScoringView teams={teams} criteria={criteria} onEdit={setEditT} disabled={lock.locked} />
            </>
          )}
        </>
      )}
    </>
  );
}
