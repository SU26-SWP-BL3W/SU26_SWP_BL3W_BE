'use client';
import { useState, useEffect, useCallback } from 'react';
import Notif from '@/components/Notif';
import EditModal from '@/components/EditModal';
import ScoreBreakdownModal from '@/components/ScoreBreakdownModal';
import { useCurrentUser } from '@/hooks/useAuth';
import { scoresApi } from '@/services/api';
import { eventRolesApi } from '@/features/events/api/eventRoles';
import { tracksApi, roundsApi } from '@/features/events/api/roundTrack';
import { resultsApi } from '@/features/results/api/results';
import { getCriteria } from '@/services/criteriaService';
import { submitResultsApi } from '@/features/events/api/submitResults';
import { parseSubmissionLinks } from '@/features/submissions/utils/submissionLinks';
import { getErrorMessage } from '@/lib/apiError';
import { calcScoreNormalized } from '@/utils.jsx';
import { StatusBadge } from '@/components/StatusBadge';
import { appealsApi } from '@/features/appeals/api/appeals';

/**
 * Panel gộp "Bài nộp" + "Chấm điểm" — thay thế ScoringPanel.jsx + SubmissionsPanel.jsx.
 * Dùng chung cho cả dashboard người tham gia (/events/{id}) lẫn dashboard quản lý của
 * Admin/EC (/events/{id}/manage — xem AdminSidebar.tsx, tab "Bài nộp").
 *
 * Phân quyền theo roleName (EventRoleModel — xem eventRoles.ts):
 *  - "Judge": thấy nút "Chấm / Sửa", BẮT BUỘC phải có track được phân công (throw lỗi
 *    nếu không có). Cần eventRoleId để gọi Scores/save.
 *  - Mọi role khác (EventCoordinator/Mentor/Admin/TeamLeader/Member...): CHỈ XEM — thấy
 *    nút "Xem chi tiết" mở breakdown read-only (điểm + nhận xét của MỌI giám khảo, qua
 *    GET /Scores/team/{teamId}/breakdown — xem scores.ts). Không gọi Scores/save.
 *    KHÔNG bắt buộc có track riêng — nếu có track (vd Mentor/EC được gán 1 hạng mục) thì
 *    xem đúng track đó; nếu không có track nào (vd Admin quản lý cả sự kiện) thì xem
 *    TOÀN BỘ bài nộp của mọi track trong sự kiện (xem loadWholeEvent bên dưới).
 *
 * Nguồn dữ liệu bài nộp: submitResultsApi (TS, mới) — KHÔNG dùng getAllSubmissions/
 * submissionService.js (cũ) nữa, để tránh lệch dữ liệu giữa 2 nguồn (đã phát hiện: khác
 * timezone parsing, khác cách suy ra projectName — xem scoring-form-guide-FINAL.md).
 *
 * Props: eventId (sự kiện đang mở), trackId (nếu component cha đã biết trước hạng mục;
 * nếu không truyền sẽ lấy từ vai trò của user trong sự kiện, hoặc xem toàn sự kiện nếu
 * user không gắn với track nào — xem trên).
 *
 * @param {{ eventId: string, trackId?: string | null }} props
 */
export default function SubmissionsScoringPanel({ eventId, trackId = null }) {
  const { data: currentUser } = useCurrentUser();

  const [isJudge, setIsJudge] = useState(false);
  const [criteria, setCriteria] = useState([]);
  const [submissions, setSubmissions] = useState([]);
  const [eventRoleId, setEventRoleId] = useState(null);
  const [roundInfo, setRoundInfo] = useState(null); // { roundName, startDate, endDate }
  // Judge có thể được giao chấm NHIỀU track cùng lúc (nhiều bản ghi EventRole, mỗi bản
  // 1 track). `GET /EventRoles/user-role` chỉ trả 1 bản ghi nên không đủ — dùng
  // `listMyRoles` (lọc client-side từ listByEvent) để lấy hết, rồi cho Judge chọn track
  // đang muốn chấm qua dropdown. `myTracks` rỗng nghĩa là chưa xác định (đang tải) hoặc
  // user không phải Judge/không có track nào.
  const [myTracks, setMyTracks] = useState([]); // [{ trackId, roleId, trackName }]
  const [selectedTrackId, setSelectedTrackId] = useState(null);
  // Dropdown lọc danh sách bài nộp theo 1 đội cụ thể (teamId) — rỗng nghĩa là hiện tất cả.
  const [selectedFilterTeamId, setSelectedFilterTeamId] = useState('');
  const [lock, setLock] = useState({ locked: false, message: null });
  // Tách riêng khỏi `lock` — lock.locked còn bật vì lý do "chưa tới hạn chấm", không chỉ
  // vì đã công bố. Chỉ dùng cờ này để quyết định ẩn nút "Chấm/Sửa" → "Xem chi tiết".
  const [resultsPublished, setResultsPublished] = useState(false);
  const [editT, setEditT] = useState(null);       // bài nộp đang chấm (Judge)
  const [viewT, setViewT] = useState(null);        // bài nộp đang xem breakdown (viewer)
  const [notif, setNotif] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  // Tăng số này để buộc useEffect load lại toàn bộ — dùng khi phát hiện trạng thái đã
  // đổi ở phía backend (vd vòng vừa bị công bố ở tab/phiên khác) mà state hiện tại
  // (lock/resultsPublished) đang cũ, khiến form vẫn hiện editable dù backend đã khóa.
  const [reloadKey, setReloadKey] = useState(0);

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

        // 1) Vai trò của user trong sự kiện — roleName (Judge hay không) + trackId được
        //    phân công NẾU CÓ. Chỉ Judge bắt buộc phải có track được phân công — các role
        //    khác (Admin/EC/Mentor/TeamLead/Member) có thể không gắn với 1 track cụ thể nào
        //    (vd Admin quản lý toàn sự kiện) — khi đó xem TOÀN BỘ mọi track trong sự kiện,
        //    không báo lỗi chặn như trước.
        //
        //    Judge có thể được giao NHIỀU track cùng lúc (nhiều bản ghi EventRole).
        //    `getUserRole` (GET /EventRoles/user-role) chỉ trả về 1 bản ghi nên không đủ —
        //    dùng `listMyRoles` để lấy hết các role Judge của user trong event này, cho
        //    phép chọn track đang muốn chấm qua dropdown (xem UI bên dưới). Chỉ áp dụng khi
        //    `trackId` KHÔNG được component cha ép cứng (giữ nguyên hành vi cũ nếu có).
        let judge = false;
        let roleId = null;
        let effectiveTrackId = trackId ?? null;
        let judgeTracks = [];

        if (trackId) {
          const role = await eventRolesApi.getUserRole(currentUser.id, eventId);
          roleId = role?.id ?? null;
          judge = role?.roleName === 'Judge';
        } else {
          const allRoles = await eventRolesApi.listMyRoles(currentUser.id, eventId);
          const judgeRoles = allRoles.filter((r) => r.roleName === 'Judge' && r.trackId);
          judge = judgeRoles.length > 0;

          if (judge) {
            judgeTracks = judgeRoles.map((r) => ({
              trackId: r.trackId,
              roleId: r.id,
              trackName: r.trackName ?? null,
            }));
            const picked =
              judgeTracks.find((t) => t.trackId === selectedTrackId) ?? judgeTracks[0];
            effectiveTrackId = picked.trackId;
            roleId = picked.roleId;
          } else {
            // Không phải Judge — vai trò khác (nếu có) chỉ dùng để biết trackId gán sẵn.
            const role = allRoles[0] ?? null;
            roleId = role?.id ?? null;
            effectiveTrackId = role?.trackId ?? null;
          }
        }

        if (judge && (!roleId || !effectiveTrackId)) {
          throw new Error('Bạn chưa được phân công chấm hạng mục nào trong sự kiện này.');
        }

        if (!cancelled) setMyTracks(judgeTracks);
        if (!cancelled && judge && !selectedTrackId && effectiveTrackId) {
          setSelectedTrackId(effectiveTrackId);
        }

        if (effectiveTrackId) {
          await loadScopedToTrack({ judge, roleId, effectiveTrackId });
        } else {
          await loadWholeEvent({ judge });
        }
      } catch (e) {
        if (!cancelled) {
          setError(e?.response ? getErrorMessage(e, 'Không thể tải dữ liệu.') : (e?.message ?? 'Không thể tải dữ liệu.'));
        }
      } finally {
        if (!cancelled) setLoading(false);
      }
    };

    // ── Nhánh A: có 1 track cụ thể (Judge luôn ở nhánh này; Mentor/EC nếu được gán track) ──
    const loadScopedToTrack = async ({ judge, roleId, effectiveTrackId }) => {
      // 2) Chi tiết track → templateId (bộ tiêu chí) + roundId (thời gian vòng thi).
      const track = await tracksApi.getById(effectiveTrackId);
      if (!track?.templateId) {
        throw new Error('Hạng mục này chưa được gán bộ tiêu chí chấm điểm.');
      }

      // 3) Tải song song: vòng thi, bộ tiêu chí, danh sách bài nộp (nguồn thống nhất
      //    submitResultsApi), và — chỉ Judge mới cần — phiếu đã chấm + trạng thái công bố.
      const [round, allCriteria, subsItems, existingScores, published, assignedAppeals] = await Promise.all([
        track.roundId ? roundsApi.getById(track.roundId) : Promise.resolve(null),
        getCriteria(track.templateId),
        submitResultsApi.list({ eventId, trackId: effectiveTrackId, pageSize: 100 }),
        judge ? scoresApi.listByEventRole(roleId) : Promise.resolve([]),
        judge && track.roundId
          ? resultsApi.listRoundLeaderboard(track.roundId).then(r => r.length > 0).catch(() => false)
          : Promise.resolve(false),
        judge ? appealsApi.getAssignedAppeals(roleId) : Promise.resolve([]),
      ]);

      if (cancelled) return;

      setRoundInfo(round ? {
        roundName: round.roundName ?? `Vòng ${round.roundNumber ?? '?'}`,
        startDate: round.startDate ?? null,
        endDate: round.endDate ?? null,
      } : null);

      // 4) Bộ tiêu chí: hệ 100 — dùng maxScore/weight thật từ backend, không hardcode
      //    (Backend validate nghiêm ngặt điểm nhập theo maxScore thật — xem
      //    scoring-form-guide-FINAL.md, Vòng 2).
      const crit = allCriteria
        .filter(c => c.isActive !== false && c.weight > 0)
        .map(c => ({ ...c, maxScore: c.maxScore ?? 0, templateId: track.templateId }));

      // 5a) JUDGE — nạp trước điểm + nhận xét đã chấm của CHÍNH họ, theo submitResultId.
      let prefillScores = {};
      let prefillComment = {};
      // `totalScore` lấy thẳng từ backend khi có; fallback phía FE cộng các điểm thành
      // phần vì maxScore đã chứa toàn bộ quy tắc trọng số của template.
      let prefillTotalScore = {};
      let submittedBySubmission = {};
      if (judge) {
        const scored = existingScores.filter(s => s.submitResultId);
        const detailList = await Promise.all(
          scored.map(s =>
            scoresApi.getWithDetails(s.id)
              .then(d => ({
                submitResultId: s.submitResultId,
                details: d.details ?? [],
                comment: d.comment ?? '',
                totalScore: d.totalScore,
                isSubmitted: d.isSubmitted ?? s.isSubmitted ?? false,
              }))
              .catch(() => ({
                submitResultId: s.submitResultId,
                details: [],
                comment: '',
                totalScore: null,
                isSubmitted: s.isSubmitted ?? false,
              })),
          ),
        );
        if (cancelled) return;
        for (const { submitResultId, details, comment, totalScore, isSubmitted } of detailList) {
          const byCriteria = Object.fromEntries(details.map(d => [d.criteriaId, d.value]));
          prefillScores[submitResultId] = crit.map(c => byCriteria[c.id] ?? 0);
          prefillComment[submitResultId] = comment;
          prefillTotalScore[submitResultId] = typeof totalScore === 'number' ? totalScore : null;
          submittedBySubmission[submitResultId] = isSubmitted === true;
        }
      }
      const scoreBySubmissionId = new Map(
        existingScores
          .filter(score => score.submitResultId)
          .map(score => [score.submitResultId, score]),
      );
      const appealedSubmissionIds = new Set(
        assignedAppeals
          .filter(appeal => {
            if (!appeal.submitResultId) return false;
            const score = scoreBySubmissionId.get(appeal.submitResultId);
            if (!score?.isSubmitted || !score.lastUpdatedTime) return true;

            // Một appeal chỉ còn hiệu lực chấm lại khi nó mới hơn lần cập nhật phiếu điểm
            // gần nhất. Sau khi Judge chốt điểm phúc khảo, Score.lastUpdatedTime mới hơn
            // thời điểm appeal nên F5 không mở lại quyền chấm lần nữa.
            const appealTime = appeal.lastUpdatedTime ?? appeal.createdTime ?? appeal.createdAt;
            if (!appealTime) return true;
            return new Date(score.lastUpdatedTime).getTime() <= new Date(appealTime).getTime();
          })
          .map(appeal => appeal.submitResultId),
      );

      // 5b) Nạp breakdown điểm của MỌI giám khảo, theo teamId (1 lần/đội, tránh gọi
      //     trùng nếu 1 đội có nhiều bài nộp trong cùng track). CHỈ dùng cho VIEWER
      //     (không phải Judge) — Backend CỐ TÌNH chặn Judge gọi API này (403 "Bạn chỉ
      //     có thể xem điểm của đội mình.") để giám khảo không thấy điểm đồng nghiệp
      //     rồi bị ảnh hưởng khi chấm. Từng thử mở rộng cho Judge khi đã công bố
      //     (published) — SAI, đã revert. Judge luôn dùng `prefillScores`/`prefillComment`
      //     (điểm của chính họ, đã có sẵn ở bước 5a) để xem lại, kể cả sau khi công bố.
      let breakdownBySubmission = {};
      if (!judge) {
        const uniqueTeamIds = [...new Set(subsItems.map(s => s.teamId).filter(Boolean))];
        const breakdowns = await Promise.all(
          uniqueTeamIds.map(tid => scoresApi.getTeamBreakdown(tid).catch(() => null)),
        );
        if (cancelled) return;
        for (const b of breakdowns) {
          for (const sub of (b?.submissions ?? [])) {
            if (sub.submitResultId) breakdownBySubmission[sub.submitResultId] = sub;
          }
        }
      }

      // 6) Bài nộp → shape hiển thị chung cho cả 2 chế độ.
      const mapped = subsItems.map((s, i) => {
        const rawUrl = s.submissionUrl ?? s.repoUrl ?? '';
        const submittedAt = s.createdTime
          ? new Date(s.createdTime.replace('+00:00', 'Z')).toLocaleString('vi-VN', {
            day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
          })
          : '—';
        const breakdown = breakdownBySubmission[s.id] ?? null;
        const scoredByJudge = judge
          ? existingScores.some(es => es.submitResultId === s.id)
          : (breakdown?.judgeScores?.length ?? 0) > 0;

        return {
          id: s.id,
          teamId: s.teamId,
          index: i,
          name: s.teamName ?? '—',
          links: parseSubmissionLinks(rawUrl),
          submittedAt,
          status: s.isActive === false ? 'Không hoạt động' : 'Đã nhận',
          // Judge:
          scores: prefillScores[s.id] ?? crit.map(() => 0),
          comment: prefillComment[s.id] ?? '',
          totalScore: prefillTotalScore[s.id] ?? null,
          isSubmitted: judge ? (submittedBySubmission[s.id] ?? false) : ((breakdown?.judgeScores?.length ?? 0) > 0 && breakdown.judgeScores.every(js => js.isSubmitted)),
          isAppealed: appealedSubmissionIds.has(s.id),
          // Viewer:
          breakdown,
          // Chung — quyết định khung màu cam/xanh.
          scored: scoredByJudge,
        };
      });

      // 7) Khóa/mở form chấm (chỉ áp dụng cho Judge — viewer không chấm nên không cần khóa).
      // Ưu tiên dùng Track.scoringStartDate/scoringEndDate (cửa sổ chấm điểm thật của Hạng
      // mục). Chỉ fallback về Track.endDate/Round.endDate nếu Track chưa cấu hình
      // scoringStartDate (dữ liệu cũ) — round.endDate là mốc đóng TOÀN BỘ vòng thi, sau cả
      // chấm điểm + công bố, dùng nó để mở form chỉ là phương án dự phòng cuối cùng.
      let lockState = { locked: false, message: null };
      if (judge) {
        const now = new Date();
        const scoringStart = track?.scoringStartDate ? new Date(track.scoringStartDate) : null;
        const scoringEnd = track?.scoringEndDate ? new Date(track.scoringEndDate) : null;
        const fallbackDate = track?.endDate
          ? new Date(track.endDate)
          : (round?.endDate ? new Date(round.endDate) : null);
        const startDate = scoringStart ?? fallbackDate;
        const notStarted = startDate ? now < startDate : false;
        const scoringOver = scoringEnd ? now > scoringEnd : false;
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
      }

      console.log('[SubmissionsScoringPanel] Judge scoring lock check', {
        trackId: effectiveTrackId,
        trackName: track?.trackName,
        now: new Date().toISOString(),
        scoringStartDate: track?.scoringStartDate ?? null,
        scoringEndDate: track?.scoringEndDate ?? null,
        trackEndDate: track?.endDate ?? null,
        roundEndDate: round?.endDate ?? null,
        published,
        locked: lockState.locked,
        message: lockState.message,
      });

      setIsJudge(judge);
      setEventRoleId(roleId);
      setCriteria(crit);
      setSubmissions(mapped);
      setLock(lockState);
      setResultsPublished(!!published);
    };

    // ── Nhánh B: KHÔNG có track cụ thể (Admin/EC/TeamLead/Member không gắn track riêng)
    //    → xem TOÀN BỘ bài nộp của mọi track trong sự kiện, chỉ ở chế độ xem (không chấm
    //    được — Judge luôn bắt buộc có track nên không bao giờ rơi vào nhánh này).
    //    Không cần bộ tiêu chí dùng chung vì mỗi bài nộp tự mang breakdown riêng (tên tiêu
    //    chí/điểm/trọng số theo đúng track của nó — xem getTeamBreakdown).
    const loadWholeEvent = async ({ judge }) => {
      const subsItems = await submitResultsApi.list({ eventId, pageSize: 100 });
      if (cancelled) return;

      const uniqueTeamIds = [...new Set(subsItems.map(s => s.teamId).filter(Boolean))];
      const breakdowns = await Promise.all(
        uniqueTeamIds.map(tid => scoresApi.getTeamBreakdown(tid).catch(() => null)),
      );
      if (cancelled) return;

      const breakdownBySubmission = {};
      for (const b of breakdowns) {
        for (const sub of (b?.submissions ?? [])) {
          if (sub.submitResultId) breakdownBySubmission[sub.submitResultId] = sub;
        }
      }

      const mapped = subsItems.map((s, i) => {
        const rawUrl = s.submissionUrl ?? s.repoUrl ?? '';
        const submittedAt = s.createdTime
          ? new Date(s.createdTime.replace('+00:00', 'Z')).toLocaleString('vi-VN', {
            day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
          })
          : '—';
        const breakdown = breakdownBySubmission[s.id] ?? null;

        return {
          id: s.id,
          teamId: s.teamId,
          index: i,
          name: s.teamName ?? '—',
          links: parseSubmissionLinks(rawUrl),
          submittedAt,
          status: s.isActive === false ? 'Không hoạt động' : 'Đã nhận',
          trackName: breakdown?.trackName ?? null, // hiện thêm vì giờ gộp nhiều track
          scores: [],
          comment: '',
          breakdown,
          scored: (breakdown?.judgeScores?.length ?? 0) > 0,
          isSubmitted: (breakdown?.judgeScores?.length ?? 0) > 0 && breakdown.judgeScores.every(js => js.isSubmitted),
        };
      });

      setRoundInfo(null); // không có 1 vòng thi cụ thể khi xem gộp nhiều track
      setIsJudge(judge); // luôn false ở nhánh này
      setEventRoleId(null);
      setCriteria([]);
      setSubmissions(mapped);
      setLock({ locked: false, message: null });
      setResultsPublished(false); // không có 1 round cụ thể để biết trạng thái công bố
    };

    load();
    return () => { cancelled = true; };
  }, [currentUser?.id, eventId, trackId, reloadKey, selectedTrackId]);

  const saveEdit = async (submitResultId, { details, comment, isSubmitted }) => {
    if (lock.locked && !editT?.isAppealed) {
      sn(lock.message ?? 'Chưa thể chấm điểm ở thời điểm này.', 'e');
      return;
    }
    const byCriteria = Object.fromEntries(criteria.map(c => [c.id, c]));
    try {
      // `save` trả về Score đã tính lại — dùng thẳng `totalScore` của nó thay vì tự tính
      // lại bằng calcScore() (xem ghi chú ở bước 5a), để card hiện đúng ngay không cần đợi
      // reload.
      const saved = await scoresApi.save({
        eventRoleId,
        submitResultId,
        isSubmitted,
        comment,
        details: details.map(d => ({
          criteriaId: d.criteriaId,
          value: d.value ?? 0,
          templateId: byCriteria[d.criteriaId]?.templateId,
        })),
      });
      const detailByCriteria = Object.fromEntries(details.map(d => [d.criteriaId, d]));
      const scores = criteria.map(c => detailByCriteria[c.id]?.value ?? 0);
      const totalScore = typeof saved?.totalScore === 'number' ? saved.totalScore : null;
      setSubmissions(p => p.map(s => s.id === submitResultId ? {
        ...s,
        scores,
        comment,
        totalScore,
        scored: true,
        isSubmitted,
        isAppealed: isSubmitted ? false : s.isAppealed,
      } : s));
      setEditT(null);
      sn(isSubmitted ? 'Đã chốt điểm thành công!' : 'Đã lưu bản nháp!');
    } catch (e) {
      const msg = e?.response ? getErrorMessage(e, 'Lưu điểm thất bại, vui lòng thử lại') : (e?.message ?? 'Lưu điểm thất bại, vui lòng thử lại');
      // 403 = vòng thi đã công bố kết quả (Backend chặn chấm/sửa) — có thể xảy ra dù FE
      // đang hiện form editable, nếu vòng vừa bị công bố ở tab/phiên khác trong lúc trang
      // này đã mở sẵn (state `lock`/`resultsPublished` bị cũ). Thay vì để form đứng yên
      // khiến người dùng bấm lại vô ích, đóng modal + tải lại toàn bộ dữ liệu ngay để UI
      // phản ánh đúng thực tế (chuyển hẳn sang "Xem chi tiết").
      if (e?.response?.status === 403) {
        setEditT(null);
        setReloadKey(k => k + 1);
        sn('Vòng thi này vừa được công bố kết quả nên không thể chấm/sửa nữa — đã tải lại dữ liệu mới nhất.', 'e');
        return;
      }
      sn(msg, 'e');
    }
  };

  const fmt = (iso) => iso
    ? new Date(iso).toLocaleString('vi-VN', {
      day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
    })
    : '—';

  // 8 ký tự đầu của teamId, dùng thay cho nhãn ẩn danh "Bài nộp #i" cũ.
  const teamLabel = (s) => `Đội: ${String(s.teamId || s.id || '').slice(0, 8).toUpperCase()}`;

  // Danh sách teamId duy nhất có bài nộp — dùng cho dropdown lọc theo đội.
  const teamOptions = [...new Map(
    submissions.map((s) => [s.teamId ?? s.id, { teamId: s.teamId ?? s.id, label: teamLabel(s) }]),
  ).values()];

  const filteredSubmissions = selectedFilterTeamId
    ? submissions.filter((s) => (s.teamId ?? s.id) === selectedFilterTeamId)
    : submissions;

  // Điểm trung bình của mọi giám khảo đã chấm (từ breakdown) — dùng khi không phải
  // Judge đang ở chế độ sửa (viewer, hoặc Judge sau khi đã công bố kết quả).
  const avgBreakdownScore = (breakdown) => {
    const scores = (breakdown?.judgeScores ?? [])
      .map(js => js.totalScore)
      .filter(v => typeof v === 'number');
    if (scores.length === 0) return null;
    return scores.reduce((a, b) => a + b, 0) / scores.length;
  };

  // Mẫu số điểm tối đa lấy TỪ CHÍNH bài nộp (breakdown.judgeScores[0].criteria) —
  // cần khi xem gộp toàn sự kiện (nhiều track/template khác nhau), vì lúc đó state
  // `criteria` global chỉ được nạp cho 1 track (effectiveTrackId) nên luôn ra 0 cho
  // các bài nộp thuộc track khác. Trả về null nếu bài chưa có giám khảo nào chấm.
  const breakdownMaxScore = (breakdown) => {
    const lines = breakdown?.judgeScores?.[0]?.criteria;
    if (!lines || lines.length === 0) return null;
    return Math.round(lines.reduce((sum, c) => sum + Number(c.maxScore || 0), 0) * 100) / 100;
  };

  return (
    <>
      <Notif n={notif} />
      {editT && (
        <EditModal
          team={editT}
          criteria={criteria}
          onClose={() => setEditT(null)}
          onSave={(payload) => saveEdit(editT.id, payload)}
          // Sau khi vòng công bố, Judge vẫn mở được modal này để xem lại điểm CHÍNH HỌ
          // đã chấm (dữ liệu đã có sẵn trong `editT.scores`/`editT.comment`) — nhưng ở
          // chế độ chỉ xem, không sửa được nữa. KHÔNG dùng ScoreBreakdownModal ở đây vì
          // modal đó cần dữ liệu breakdown (mọi giám khảo) mà Judge không được phép gọi.
          readOnly={editT.isSubmitted || resultsPublished || lock.locked}
          isAppealed={editT.isAppealed}
        />
      )}
      {viewT && (
        <ScoreBreakdownModal
          teamLabel={teamLabel(viewT)}
          submission={viewT.breakdown}
          onClose={() => setViewT(null)}
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
        <div className="animate-fadeUp">
          <div className="flex justify-between items-start mb-7">
            <div>
              <h2 className="text-xl font-bold m-0" style={{ color: '#000' }}>
                {isJudge ? 'Bảng chấm điểm' : 'Danh sách bài nộp'}
              </h2>
              <p className="text-sm mt-1" style={{ color: '#757575' }}>
                {isJudge
                  ? `Chấm và quản lý điểm số các đội thi · ${criteria.length} Bộ tiêu chí`
                  : 'Xem bài nộp và kết quả chấm điểm từ các đội thi.'}
              </p>
            </div>
            <StatusBadge tone="success">{submissions.length} bài nộp</StatusBadge>
          </div>

          {myTracks.length > 1 && (
            <div className="mb-5">
              <label className="text-xs font-bold uppercase tracking-widest" style={{ color: '#757575' }}>
                Hạng mục đang chấm
              </label>
              <select
                value={selectedTrackId ?? ''}
                onChange={(e) => setSelectedTrackId(e.target.value)}
                className="block mt-1 border rounded-sm px-3 py-2 text-sm"
                style={{ borderColor: '#e0e0e0' }}
              >
                {myTracks.map((t) => (
                  <option key={t.trackId} value={t.trackId}>
                    {t.trackName ?? t.trackId}
                  </option>
                ))}
              </select>
            </div>
          )}

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

          {isJudge && lock.locked && (
            <div
              className="mb-5 flex items-start gap-3 p-4"
              style={{ background: 'rgba(255,193,7,.1)', border: '1px solid #ffc107', borderRadius: 2 }}
              role="alert"
            >
              <span style={{ color: '#b8860b', fontSize: 16, lineHeight: 1.2 }}>⚠</span>
              <p className="text-sm m-0" style={{ color: '#8a6d00' }}>{lock.message}</p>
            </div>
          )}

          {submissions.length > 0 && (
            <div className="mb-5">
              <label className="text-xs font-bold uppercase tracking-widest" style={{ color: '#757575' }}>
                Lọc theo đội
              </label>
              <select
                value={selectedFilterTeamId}
                onChange={(e) => setSelectedFilterTeamId(e.target.value)}
                className="block mt-1 border rounded-sm px-3 py-2 text-sm"
                style={{ borderColor: '#e0e0e0' }}
              >
                <option value="">Tất cả đội</option>
                {teamOptions.map((t) => (
                  <option key={t.teamId} value={t.teamId}>{t.label}</option>
                ))}
              </select>
            </div>
          )}

          {submissions.length === 0 ? (
            <div className="text-center py-20">
              <p className="text-sm" style={{ color: '#757575' }}>Chưa có đội nào nộp bài.</p>
            </div>
          ) : filteredSubmissions.length === 0 ? (
            <div className="text-center py-20">
              <p className="text-sm" style={{ color: '#757575' }}>Không có bài nộp nào cho đội đã chọn.</p>
            </div>
          ) : (
            <div className="grid gap-4" style={{ gridTemplateColumns: '1fr 1fr' }}>
              {filteredSubmissions.map((s) => {
                // Khung màu: cam = chưa chấm, xanh lá = đã chấm (ít nhất 1 giám khảo với
                // viewer; chính họ với Judge).
                const borderColor = s.scored ? '#76b900' : '#df6500';
                // Judge LUÔN dùng điểm của chính mình. Ưu tiên `totalScore` backend đã tính
                // sẵn (đúng theo value/maxScore thật của từng tiêu chí) — chỉ fallback về
                // calcScore() (giả định maxScore=10) khi chưa có totalScore (bài chưa chấm).
                // Chỉ viewer (không phải Judge) mới dùng điểm trung bình từ breakdown.
                const score = isJudge
                  ? (s.totalScore ?? calcScoreNormalized(s.scores, criteria))
                  : avgBreakdownScore(s.breakdown);

                return (
                  <div
                    key={s.id}
                    className="card-hover p-6"
                    style={{ background: '#ffffff', border: `1.5px solid ${borderColor}`, borderRadius: 2, animationDelay: `${s.index * 0.06}s` }}
                  >
                    <div className="flex justify-between items-start mb-1">
                      <div className="flex items-center gap-2 flex-wrap">
                        {/* Trước đây ẩn danh theo "Bài nộp #i" — giờ hiện 8 ký tự đầu teamId
                            theo yêu cầu, không còn ẩn danh hoàn toàn nữa. */}
                        <span className="text-sm font-bold" style={{ color: '#000' }}>{teamLabel(s)}</span>
                        <StatusBadge tone={s.isSubmitted ? 'success' : s.scored ? 'processing' : 'pending'}>
                          {s.isSubmitted ? 'Đã chốt điểm' : s.scored ? 'Bản nháp' : 'Chưa chấm'}
                        </StatusBadge>
                        {/* Chỉ xuất hiện khi xem gộp toàn sự kiện (nhiều track) — giúp phân biệt
                            bài nộp thuộc hạng mục nào. */}
                        {s.trackName && (
                          <StatusBadge tone="neutral">
                            {s.trackName}
                          </StatusBadge>
                        )}
                      </div>
                      {/* Điểm tổng luôn hiện ra ngoài (không chỉ riêng Judge nữa) — Judge đang
                          sửa thì là điểm của chính họ; còn lại (viewer, hoặc Judge sau khi đã
                          công bố) là điểm trung bình mọi giám khảo từ breakdown. */}
                      <div className="text-right">
                        <div className="text-2xl font-black leading-none" style={{ color: '#76b900' }}>
                          {score != null ? score.toFixed(2) : '—'}
                        </div>
                        <div className="text-xs" style={{ color: '#757575' }}>
                          /{isJudge
                            ? Math.round(criteria.reduce((sum, c) => sum + Number(c.maxScore || 0), 0) * 100) / 100
                            : (breakdownMaxScore(s.breakdown)
                              ?? Math.round(criteria.reduce((sum, c) => sum + Number(c.maxScore || 0), 0) * 100) / 100)}
                        </div>
                      </div>
                    </div>

                    <div className="grid gap-2.5 my-3" style={{ gridTemplateColumns: '1fr 1fr' }}>
                      <div className="p-3" style={{ background: '#f7f7f7', borderRadius: 2 }}>
                        <div className="text-[10px] tracking-widest mb-1 font-bold uppercase" style={{ color: '#757575' }}>Trạng thái</div>
                        <StatusBadge tone="processing">{s.status}</StatusBadge>
                      </div>
                      <div className="p-3" style={{ background: '#f7f7f7', borderRadius: 2 }}>
                        <div className="text-[10px] tracking-widest mb-1 font-bold uppercase" style={{ color: '#757575' }}>Thời gian nộp</div>
                        <div className="text-sm font-bold" style={{ color: '#000' }}>{s.submittedAt}</div>
                      </div>
                    </div>

                    {(s.links.length > 0 ? s.links : []).map((lnk, li) => (
                      <div key={li} className="p-3 flex items-center gap-2.5 mb-1.5" style={{ background: '#f7f7f7', borderRadius: 2 }}>
                        <div className="text-[10px] tracking-widest shrink-0 font-bold uppercase" style={{ color: '#757575', minWidth: 50 }}>
                          {lnk.label}
                        </div>
                        <a href={lnk.url} target="_blank" rel="noreferrer" className="text-xs break-all"
                          style={{ color: '#76b900', textDecoration: 'none' }}>{lnk.url}</a>
                      </div>
                    ))}

                    {isJudge && s.scored && (
                      <div className="mt-2.5 flex flex-col gap-1.5">
                        {criteria.slice(0, 3).map((c, ci) => (
                          <div key={ci} className="flex items-center gap-2">
                            <div className="text-xs shrink-0" style={{ color: '#757575', width: 96 }}>{c.label}</div>
                            <div className="flex-1 h-1.5" style={{ background: '#e5e5e5', borderRadius: 2 }}>
                              <div style={{ height: '100%', width: `${c.maxScore > 0 ? ((s.scores[ci] || 0) / c.maxScore) * 100 : 0}%`, background: '#76b900' }} />
                            </div>
                            <div className="text-xs font-bold w-6 text-right" style={{ color: '#000' }}>{s.scores[ci] || 0}</div>
                          </div>
                        ))}
                        {criteria.length > 3 && (
                          <div className="text-xs" style={{ color: '#757575' }}>+{criteria.length - 3} Bộ tiêu chí khác...</div>
                        )}
                      </div>
                    )}

                    <div className="flex justify-end mt-4">
                      {isJudge ? (
                        <button className={`btn btn-sm ${(s.isSubmitted && !s.isAppealed) || resultsPublished || (lock.locked && !s.isAppealed) ? 'btn-view' : 'btn-update'}`}
                          onClick={() => setEditT(s)}
                          style={{ cursor: 'pointer' }}>
                          {s.isAppealed
                            ? 'Chấm lại (Phúc khảo)'
                            : s.isSubmitted || resultsPublished || lock.locked
                              ? 'Xem chi tiết'
                              : 'Chấm / Sửa'}
                        </button>
                      ) : (
                        <button className="btn btn-view btn-sm"
                          onClick={() => setViewT(s)}
                          style={{ cursor: 'pointer' }}>
                          Xem chi tiết
                        </button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      )}
    </>
  );
}
