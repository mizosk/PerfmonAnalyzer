import { useState, useRef, useCallback } from 'react';
import type { Chart } from 'chart.js';
import './App.css';
import { FileUpload } from './components/FileUpload';
import { ChartView } from './components/ChartView';
import { RangeSelector } from './components/RangeSelector';
import { ExportButton } from './components/ExportButton';
import { ReportButton } from './components/ReportButton';
import { SlopeSummary } from './components/SlopeSummary';
import type { CounterInfo, TimeRange, SlopeResult, UploadResult } from './types';
import { getSessionData, analyzeSlopeForSession } from './services/api';

/**
 * カウンターデータから時間範囲を算出するヘルパー
 */
function computeTimeRange(counters: CounterInfo[]): TimeRange | undefined {
  let minTime = '';
  let maxTime = '';
  for (const counter of counters) {
    for (const dp of counter.dataPoints) {
      if (!minTime || dp.timestamp < minTime) minTime = dp.timestamp;
      if (!maxTime || dp.timestamp > maxTime) maxTime = dp.timestamp;
    }
  }
  return minTime && maxTime ? { start: minTime, end: maxTime } : undefined;
}

function App() {
  const chartRef = useRef<Chart<'line'>>(null);

  // セッション状態
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [counters, setCounters] = useState<CounterInfo[]>([]);
  const [timeRange, setTimeRange] = useState<TimeRange | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  // 傾き分析状態
  const [slopeResults, setSlopeResults] = useState<SlopeResult[]>([]);
  const [threshold, setThreshold] = useState(50);
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  /** 傾き分析を実行 */
  const runSlopeAnalysis = useCallback(async (sid: string, range: TimeRange, th: number) => {
    setIsAnalyzing(true);
    try {
      const response = await analyzeSlopeForSession(sid, range, th);
      setSlopeResults(response.results);
    } catch {
      // 分析エラーは致命的ではないので結果をクリア
      setSlopeResults([]);
    } finally {
      setIsAnalyzing(false);
    }
  }, []);

  /** ファイルアップロード成功時 */
  const handleUploadSuccess = useCallback((result: UploadResult) => {
    setSessionId(result.sessionId);
    setCounters(result.counters);
    const range = computeTimeRange(result.counters);
    setTimeRange(range);
    setError(null);
    setSlopeResults([]);
    if (range) {
      runSlopeAnalysis(result.sessionId, range, threshold);
    }
  }, [runSlopeAnalysis, threshold]);

  /** 時間範囲変更時にデータを再取得 */
  const handleRangeChange = useCallback(async (range: TimeRange) => {
    if (!sessionId) return;
    setIsLoading(true);
    try {
      const response = await getSessionData(sessionId, range.start, range.end);
      setCounters(response.counters);
      setTimeRange(range);
      setError(null);
      runSlopeAnalysis(sessionId, range, threshold);
    } catch {
      setError('データの取得に失敗しました。');
    } finally {
      setIsLoading(false);
    }
  }, [sessionId, runSlopeAnalysis, threshold]);

  /** 閾値変更時 */
  const handleThresholdChange = useCallback((value: number) => {
    setThreshold(value);
  }, []);

  return (
    <div className="app">
      <header className="app-header">
        <h1>Perfmon Analyzer</h1>
      </header>

      <main className="app-main">
        <section className="upload-section">
          <FileUpload onUploadSuccess={handleUploadSuccess} />
        </section>

        {error && <div className="error-message">{error}</div>}

        {counters.length > 0 && (
          <>
            <section className="controls-section">
              <RangeSelector key={sessionId} initialRange={timeRange} onRangeChange={handleRangeChange} />
              <ExportButton chartRef={chartRef} />
              <ReportButton
                chartRef={chartRef}
                sessionId={sessionId}
                startTime={timeRange?.start ?? ''}
                endTime={timeRange?.end ?? ''}
                threshold={threshold}
              />
            </section>

            <div className="content-layout">
              <section className="chart-section">
                {isLoading && <div className="loading-overlay">読み込み中...</div>}
                <ChartView ref={chartRef} counters={counters} />
              </section>

              <section className="summary-section">
                {isAnalyzing && <div className="loading-overlay">分析中...</div>}
                <SlopeSummary
                  results={slopeResults}
                  threshold={threshold}
                  onThresholdChange={handleThresholdChange}
                />
              </section>
            </div>
          </>
        )}
      </main>
    </div>
  );
}

export default App;
