import {
  calculateMaxScores,
  inferScoreScale,
  resolveScoreScale,
  sumMaxScores,
} from "@/lib/scoringScale";

describe("scoringScale", () => {
  it("tính maxScore theo thang 10", () => {
    const result = calculateMaxScores(
      [{ weight: 20 }, { weight: 30 }, { weight: 50 }],
      10,
    );
    expect(result.map((item) => item.maxScore)).toEqual([2, 3, 5]);
  });

  it("dồn sai số làm tròn vào phần tử cuối khi tổng weight bằng 100", () => {
    const result = calculateMaxScores(
      [{ weight: 33.33 }, { weight: 33.33 }, { weight: 33.34 }],
      10,
    );
    expect(result.map((item) => item.maxScore)).toEqual([3.33, 3.33, 3.34]);
    expect(sumMaxScores(result)).toBe(10);
  });

  it("không dồn toàn bộ phần dư khi bộ tiêu chí chưa đủ 100%", () => {
    expect(calculateMaxScores([{ weight: 25 }], 100)[0].maxScore).toBe(25);
  });

  it("hỗ trợ và suy ra thang tùy chỉnh từ dữ liệu đã lưu", () => {
    expect(resolveScoreScale("custom", 900)).toBe(900);
    expect(inferScoreScale([{ weight: 40, maxScore: 360 }, { weight: 60, maxScore: 540 }]))
      .toEqual({ scaleType: "custom", customScaleValue: 900 });
  });

  it("suy ra đúng thang điểm khi tổng trọng số chưa đủ 100", () => {
    expect(inferScoreScale([{ weight: 20, maxScore: 2 }]))
      .toEqual({ scaleType: "10", customScaleValue: 10 });
    expect(inferScoreScale([{ weight: 33.33, maxScore: 3.33 }]))
      .toEqual({ scaleType: "10", customScaleValue: 10 });
    expect(inferScoreScale([{ weight: 25, maxScore: 225 }]))
      .toEqual({ scaleType: "custom", customScaleValue: 900 });
  });
});
