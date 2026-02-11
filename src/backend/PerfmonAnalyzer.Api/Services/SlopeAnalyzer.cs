using PerfmonAnalyzer.Api.Models;

namespace PerfmonAnalyzer.Api.Services;

/// <summary>
/// 最小二乗法（OLS）による傾き分析サービス
/// </summary>
public class SlopeAnalyzer : ISlopeAnalyzer
{
    /// <summary>
    /// 線形回帰の最小データポイント数
    /// </summary>
    private const int MinDataPoints = 2;

    /// <summary>
    /// 浮動小数点のゼロ判定用イプシロン
    /// </summary>
    private const double Epsilon = 1e-15;

    /// <inheritdoc/>
    public List<SlopeResult> Calculate(
        IReadOnlyList<CounterInfo> counters,
        DateTime startTime,
        DateTime endTime,
        double thresholdKBPer10Min)
    {
        var results = new List<SlopeResult>();

        foreach (var counter in counters)
        {
            // 時間範囲内のデータポイントを抽出し、NaN を除外
            var filtered = counter.DataPoints
                .Where(dp => dp.Timestamp >= startTime
                          && dp.Timestamp <= endTime
                          && !double.IsNaN(dp.Value))
                .ToList();

            // データポイントが不足している場合はスキップ
            if (filtered.Count < MinDataPoints)
            {
                continue;
            }

            var (slopeBytesPerMin, rSquared) = LinearRegression(filtered);

            // bytes/min → KB/10min
            double slopeKBPer10Min = slopeBytesPerMin * 10.0 / 1024.0;

            results.Add(new SlopeResult
            {
                CounterName = counter.DisplayName,
                SlopeKBPer10Min = slopeKBPer10Min,
                IsWarning = Math.Abs(slopeKBPer10Min) > thresholdKBPer10Min,
                RSquared = rSquared,
            });
        }

        return results;
    }

    /// <summary>
    /// 最小二乗法による線形回帰を行い、傾きと決定係数（R²）を返す
    /// </summary>
    /// <param name="data">データポイントのリスト（2件以上）</param>
    /// <returns>傾き（値/分）と決定係数 R² のタプル</returns>
    public static (double slope, double rSquared) LinearRegression(List<DataPoint> data)
    {
        int n = data.Count;
        double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

        // 時間を分単位の数値に変換（基準点からの経過分）
        var baseTime = data[0].Timestamp;

        foreach (var point in data)
        {
            double x = (point.Timestamp - baseTime).TotalMinutes;
            double y = point.Value;

            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumX2 += x * x;
        }

        double denominator = n * sumX2 - sumX * sumX;

        // 全データポイントが同一時刻の場合（分母が0）
        if (Math.Abs(denominator) < Epsilon)
        {
            return (0.0, double.NaN);
        }

        double slope = (n * sumXY - sumX * sumY) / denominator;
        double intercept = (sumY - slope * sumX) / n;

        // R²（決定係数）の算出
        double meanY = sumY / n;
        double ssTotal = 0;
        double ssResidual = 0;

        foreach (var point in data)
        {
            double x = (point.Timestamp - baseTime).TotalMinutes;
            double y = point.Value;
            double predicted = slope * x + intercept;

            ssTotal += (y - meanY) * (y - meanY);
            ssResidual += (y - predicted) * (y - predicted);
        }

        // 全てのY値が同じ場合（ssTotal が 0）
        double rSquared = Math.Abs(ssTotal) < Epsilon
            ? (Math.Abs(ssResidual) < Epsilon ? 1.0 : 0.0)
            : 1.0 - ssResidual / ssTotal;

        return (slope, rSquared);
    }
}
