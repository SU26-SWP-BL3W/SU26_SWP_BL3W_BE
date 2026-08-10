export type ScoreScaleType = "10" | "100" | "custom";

export interface WeightedCriterion {
  weight: number;
  maxScore?: number;
}

const SCORE_PRECISION = 100;
const EPSILON = 0.000_001;

export const roundScore = (value: number): number =>
  Math.round((value + Number.EPSILON) * SCORE_PRECISION) / SCORE_PRECISION;

export function resolveScoreScale(
  scaleType: ScoreScaleType,
  customScaleValue: number,
): number {
  if (scaleType === "custom") return customScaleValue;
  return Number(scaleType);
}

export function inferScoreScale(criteria: WeightedCriterion[]): {
  scaleType: ScoreScaleType;
  customScaleValue: number;
} {
  const totalWeight = criteria.reduce((sum, item) => sum + Number(item.weight ?? 0), 0);
  const totalMaxScore = roundScore(
    criteria.reduce((sum, item) => sum + Number(item.maxScore ?? 0), 0),
  );
  if (Math.abs(totalWeight - 100) < EPSILON && totalMaxScore > 0) {
    if (Math.abs(totalMaxScore - 10) < EPSILON) return { scaleType: "10", customScaleValue: 10 };
    if (Math.abs(totalMaxScore - 100) < EPSILON) return { scaleType: "100", customScaleValue: 100 };
    return { scaleType: "custom", customScaleValue: totalMaxScore };
  }

  // Không thể dùng tổng maxScore để suy ra thang điểm khi template đang được cấu
  // hình dở (tổng weight < 100). Tỷ lệ maxScore / weight vẫn cho ra đúng thang gốc.
  const inferredScales = criteria
    .map((item) => {
      const weight = Number(item.weight ?? 0);
      const maxScore = Number(item.maxScore ?? 0);
      return weight > 0 && maxScore > 0 ? roundScore((maxScore * 100) / weight) : 0;
    })
    .filter((value) => Number.isFinite(value) && value > 0);
  const scale = inferredScales[0] ?? 0;
  const referenceWeight = Number(criteria.find((item) => {
    const weight = Number(item.weight ?? 0);
    const maxScore = Number(item.maxScore ?? 0);
    return weight > 0 && maxScore > 0;
  })?.weight ?? 0);
  // maxScore lưu với 2 chữ số nên thang suy ngược có một khoảng sai số phụ
  // thuộc weight. Snap về hai thang chuẩn nếu giá trị nằm trong khoảng đó.
  const roundingTolerance = referenceWeight > 0 ? 0.5 / referenceWeight + EPSILON : EPSILON;

  if (Math.abs(scale - 10) <= roundingTolerance) return { scaleType: "10", customScaleValue: 10 };
  if (Math.abs(scale - 100) <= roundingTolerance) return { scaleType: "100", customScaleValue: 100 };
  if (scale > 0) return { scaleType: "custom", customScaleValue: scale };
  return { scaleType: "10", customScaleValue: 10 };
}

/**
 * Tính maxScore từ trọng số. Khi tổng trọng số đã đủ 100%, phần tử cuối nhận
 * phần dư sau làm tròn để tổng maxScore luôn đúng tuyệt đối bằng thang điểm.
 */
export function calculateMaxScores<T extends WeightedCriterion>(
  criteria: T[],
  totalScale: number,
): Array<T & { maxScore: number }> {
  if (!Number.isFinite(totalScale) || totalScale <= 0) {
    return criteria.map((item) => ({ ...item, maxScore: 0 }));
  }

  const totalWeight = criteria.reduce((sum, item) => sum + Number(item.weight || 0), 0);
  let allocated = 0;

  return criteria.map((item, index) => {
    const isLastOfCompleteSet =
      index === criteria.length - 1 && Math.abs(totalWeight - 100) < EPSILON;
    const maxScore = isLastOfCompleteSet
      ? roundScore(totalScale - allocated)
      : roundScore((Number(item.weight || 0) / 100) * totalScale);

    allocated = roundScore(allocated + maxScore);
    return { ...item, maxScore };
  });
}

export const sumMaxScores = (criteria: WeightedCriterion[]): number =>
  roundScore(criteria.reduce((sum, item) => sum + Number(item.maxScore ?? 0), 0));
