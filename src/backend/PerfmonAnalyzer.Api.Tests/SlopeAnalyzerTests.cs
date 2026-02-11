using PerfmonAnalyzer.Api.Models;
using PerfmonAnalyzer.Api.Services;

namespace PerfmonAnalyzer.Api.Tests;

public class SlopeAnalyzerTests
{
    private readonly ISlopeAnalyzer _analyzer = new SlopeAnalyzer();

    #region ヘルパーメソッド

    /// <summary>
    /// 指定した傾き（bytes/min）で線形に増加するデータポイントを生成
    /// </summary>
    private static List<DataPoint> CreateLinearDataPoints(
        DateTime baseTime, int count, double slopeBytesPerMin, double initialValue = 0)
    {
        var points = new List<DataPoint>();
        for (int i = 0; i < count; i++)
        {
            points.Add(new DataPoint
            {
                Timestamp = baseTime.AddMinutes(i),
                Value = initialValue + slopeBytesPerMin * i
            });
        }
        return points;
    }

    /// <summary>
    /// CounterInfo を作成するヘルパー
    /// </summary>
    private static CounterInfo CreateCounter(string counterName, DataPoint[] dataPoints)
    {
        return new CounterInfo
        {
            MachineName = "SERVER",
            Category = "Memory",
            InstanceName = "",
            CounterName = counterName,
            DisplayName = $"\\\\SERVER\\Memory\\{counterName}",
            DataPoints = dataPoints
        };
    }

    #endregion

    #region LinearRegression の基本テスト

    [Fact]
    public void LinearRegression_PerfectLinearData_ReturnsExactSlope()
    {
        // Arrange: 傾き 100 bytes/min の完全な直線データ
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = CreateLinearDataPoints(baseTime, 10, slopeBytesPerMin: 100);

        // Act
        var (slope, rSquared) = SlopeAnalyzer.LinearRegression(data);

        // Assert
        Assert.Equal(100.0, slope, precision: 6);
        Assert.Equal(1.0, rSquared, precision: 6);
    }

    [Fact]
    public void LinearRegression_FlatData_ReturnsZeroSlope()
    {
        // Arrange: 傾き 0（一定値）
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = CreateLinearDataPoints(baseTime, 10, slopeBytesPerMin: 0, initialValue: 500);

        // Act
        var (slope, rSquared) = SlopeAnalyzer.LinearRegression(data);

        // Assert
        Assert.Equal(0.0, slope, precision: 6);
        // R² は分母が0になるため NaN または 1 になりうる（定数データ）
        // 定数データの場合、全ポイントが平均と一致するため R² = 1.0 とする
        Assert.Equal(1.0, rSquared, precision: 6);
    }

    [Fact]
    public void LinearRegression_NegativeSlope_ReturnsNegativeValue()
    {
        // Arrange: 傾き -50 bytes/min
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = CreateLinearDataPoints(baseTime, 10, slopeBytesPerMin: -50, initialValue: 1000);

        // Act
        var (slope, rSquared) = SlopeAnalyzer.LinearRegression(data);

        // Assert
        Assert.Equal(-50.0, slope, precision: 6);
        Assert.Equal(1.0, rSquared, precision: 6);
    }

    [Fact]
    public void LinearRegression_WithNoise_RSquaredLessThanOne()
    {
        // Arrange: 傾き 100 に少しノイズを加えたデータ
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = new List<DataPoint>
        {
            new() { Timestamp = baseTime.AddMinutes(0), Value = 5 },
            new() { Timestamp = baseTime.AddMinutes(1), Value = 110 },
            new() { Timestamp = baseTime.AddMinutes(2), Value = 195 },
            new() { Timestamp = baseTime.AddMinutes(3), Value = 305 },
            new() { Timestamp = baseTime.AddMinutes(4), Value = 398 },
        };

        // Act
        var (slope, rSquared) = SlopeAnalyzer.LinearRegression(data);

        // Assert: 傾きは約100付近、R² は1未満だが高い値
        Assert.InRange(slope, 90, 110);
        Assert.InRange(rSquared, 0.99, 1.0);
    }

    #endregion

    #region Calculate メソッドのテスト

    [Fact]
    public void Calculate_KnownSlope_ReturnsCorrectKBPer10Min()
    {
        // Arrange: 傾き 1024 bytes/min → 1024 * 10 / 1024 = 10 KB/10min
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var endTime = baseTime.AddMinutes(59);
        var dataPoints = CreateLinearDataPoints(baseTime, 60, slopeBytesPerMin: 1024);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Available Bytes", dataPoints.ToArray())
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, endTime, thresholdKBPer10Min: 50);

        // Assert
        Assert.Single(results);
        Assert.Equal(10.0, results[0].SlopeKBPer10Min, precision: 2);
        Assert.False(results[0].IsWarning); // 10 < 50 なので Warning ではない
    }

    [Fact]
    public void Calculate_ExceedsThreshold_IsWarningTrue()
    {
        // Arrange: 傾き 5120 bytes/min → 5120 * 10 / 1024 = 50 KB/10min → 閾値「ちょうど」
        //   閾値超過を確認するため 5121 bytes/min にする → 50.009... KB/10min
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var endTime = baseTime.AddMinutes(59);
        var dataPoints = CreateLinearDataPoints(baseTime, 60, slopeBytesPerMin: 5121);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Available Bytes", dataPoints.ToArray())
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, endTime, thresholdKBPer10Min: 50);

        // Assert
        Assert.Single(results);
        Assert.True(results[0].IsWarning);
    }

    [Fact]
    public void Calculate_ExactThreshold_IsWarningFalse()
    {
        // Arrange: 傾き 5120 bytes/min → ちょうど 50 KB/10min → 閾値以下
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var endTime = baseTime.AddMinutes(59);
        var dataPoints = CreateLinearDataPoints(baseTime, 60, slopeBytesPerMin: 5120);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Available Bytes", dataPoints.ToArray())
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, endTime, thresholdKBPer10Min: 50);

        // Assert
        Assert.Single(results);
        Assert.False(results[0].IsWarning);
    }

    [Fact]
    public void Calculate_MultipleCounters_ReturnsResultsForEach()
    {
        // Arrange
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var endTime = baseTime.AddMinutes(59);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Counter1", CreateLinearDataPoints(baseTime, 60, 1024).ToArray()),
            CreateCounter("Counter2", CreateLinearDataPoints(baseTime, 60, 2048).ToArray()),
            CreateCounter("Counter3", CreateLinearDataPoints(baseTime, 60, 512).ToArray()),
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, endTime, thresholdKBPer10Min: 50);

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Calculate_TimeRangeFilter_OnlyIncludesDataInRange()
    {
        // Arrange: 0〜120分のデータを作成し、30〜90分だけ分析
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var dataPoints = CreateLinearDataPoints(baseTime, 121, slopeBytesPerMin: 1024);

        var startTime = baseTime.AddMinutes(30);
        var endTime = baseTime.AddMinutes(90);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Available Bytes", dataPoints.ToArray())
        };

        // Act
        var results = _analyzer.Calculate(counters, startTime, endTime, thresholdKBPer10Min: 50);

        // Assert: 傾きは同じ（線形データなので範囲を絞っても同じ）
        Assert.Single(results);
        Assert.Equal(10.0, results[0].SlopeKBPer10Min, precision: 2);
    }

    [Fact]
    public void Calculate_SkipsNaNValues()
    {
        // Arrange: NaN を含むデータ
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = new[]
        {
            new DataPoint { Timestamp = baseTime.AddMinutes(0), Value = 0 },
            new DataPoint { Timestamp = baseTime.AddMinutes(1), Value = double.NaN },
            new DataPoint { Timestamp = baseTime.AddMinutes(2), Value = 200 },
            new DataPoint { Timestamp = baseTime.AddMinutes(3), Value = double.NaN },
            new DataPoint { Timestamp = baseTime.AddMinutes(4), Value = 400 },
        };

        var counters = new List<CounterInfo>
        {
            CreateCounter("TestCounter", data)
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(4), thresholdKBPer10Min: 50);

        // Assert: NaN を除外して 3 ポイントで回帰 → 傾き 100 bytes/min
        Assert.Single(results);
        var slopeBytesPerMin = results[0].SlopeKBPer10Min * 1024 / 10;
        Assert.Equal(100.0, slopeBytesPerMin, precision: 1);
    }

    [Fact]
    public void Calculate_InsufficientDataPoints_ReturnsEmptyOrHandlesGracefully()
    {
        // Arrange: データポイントが1つしかない
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = new[]
        {
            new DataPoint { Timestamp = baseTime, Value = 100 },
        };

        var counters = new List<CounterInfo>
        {
            CreateCounter("TestCounter", data)
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(10), thresholdKBPer10Min: 50);

        // Assert: データポイントが不足している場合、結果に含めない
        Assert.Empty(results);
    }

    [Fact]
    public void Calculate_RSquaredIsComputed()
    {
        // Arrange: 完全な直線データ → R² = 1.0
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var dataPoints = CreateLinearDataPoints(baseTime, 60, slopeBytesPerMin: 1024);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Available Bytes", dataPoints.ToArray())
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(59), thresholdKBPer10Min: 50);

        // Assert
        Assert.Single(results);
        Assert.Equal(1.0, results[0].RSquared, precision: 6);
    }

    #endregion

    #region パフォーマンステスト

    [Fact]
    public void Calculate_3600Points_CompletesWithin500ms()
    {
        // Arrange: 3600 ポイント
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var dataPoints = CreateLinearDataPoints(baseTime, 3600, slopeBytesPerMin: 1024);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Perf Counter", dataPoints.ToArray())
        };

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(3599), thresholdKBPer10Min: 50);
        sw.Stop();

        // Assert
        Assert.Single(results);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"計算に {sw.ElapsedMilliseconds}ms かかりました（上限: 500ms）");
    }

    #endregion

    #region CounterName のテスト

    [Fact]
    public void Calculate_ReturnsDisplayNameAsCounterName()
    {
        // Arrange
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var dataPoints = CreateLinearDataPoints(baseTime, 60, slopeBytesPerMin: 1024);

        var counter = CreateCounter("Available Bytes", dataPoints.ToArray());
        var counters = new List<CounterInfo> { counter };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(59), thresholdKBPer10Min: 50);

        // Assert: DisplayName が結果の CounterName に使われる
        Assert.Equal(counter.DisplayName, results[0].CounterName);
    }

    #endregion

    #region 負の傾きの警告テスト

    [Fact]
    public void Calculate_NegativeSlope_IsWarningBasedOnAbsoluteValue()
    {
        // Arrange: 負の傾き（メモリ減少）→ 絶対値で閾値を判定
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var dataPoints = CreateLinearDataPoints(baseTime, 60, slopeBytesPerMin: -5121, initialValue: 1000000);

        var counters = new List<CounterInfo>
        {
            CreateCounter("Available Bytes", dataPoints.ToArray())
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(59), thresholdKBPer10Min: 50);

        // Assert: 絶対値が閾値を超えているので Warning
        Assert.Single(results);
        Assert.True(results[0].IsWarning);
    }

    #endregion

    #region 追加テスト（レビュー指摘対応）

    [Fact]
    public void Calculate_EmptyCounterList_ReturnsEmptyResults()
    {
        // Arrange: 空のカウンターリスト
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var counters = new List<CounterInfo>();

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(60), thresholdKBPer10Min: 50);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Calculate_AllDataPointsNaN_SkipsCounter()
    {
        // Arrange: 全データポイントが NaN
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = new[]
        {
            new DataPoint { Timestamp = baseTime.AddMinutes(0), Value = double.NaN },
            new DataPoint { Timestamp = baseTime.AddMinutes(1), Value = double.NaN },
            new DataPoint { Timestamp = baseTime.AddMinutes(2), Value = double.NaN },
        };

        var counters = new List<CounterInfo>
        {
            CreateCounter("TestCounter", data)
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(2), thresholdKBPer10Min: 50);

        // Assert: 全 NaN なのでフィルタ後にデータ不足 → 結果に含まれない
        Assert.Empty(results);
    }

    [Fact]
    public void Calculate_ExactlyTwoDataPoints_ReturnsResult()
    {
        // Arrange: MinDataPoints の境界値（ちょうど 2 ポイント）
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0);
        var data = new[]
        {
            new DataPoint { Timestamp = baseTime.AddMinutes(0), Value = 0 },
            new DataPoint { Timestamp = baseTime.AddMinutes(1), Value = 1024 },
        };

        var counters = new List<CounterInfo>
        {
            CreateCounter("TestCounter", data)
        };

        // Act
        var results = _analyzer.Calculate(counters, baseTime, baseTime.AddMinutes(1), thresholdKBPer10Min: 50);

        // Assert: 2 ポイントでも計算が成功し、結果が返る
        Assert.Single(results);
        // 傾き 1024 bytes/min → 10 KB/10min
        Assert.Equal(10.0, results[0].SlopeKBPer10Min, precision: 2);
        // 2 点の直線 → R² = 1.0
        Assert.Equal(1.0, results[0].RSquared, precision: 6);
    }

    #endregion
}
