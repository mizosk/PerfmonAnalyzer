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
import { analyzeSlopeForSession } from './services/api';

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
  /** 全データの時間範囲（不変、countersから算出） */
  const [fullTimeRange, setFullTimeRange] = useState<TimeRange | undefined>(undefined);
  /** ユーザー選択範囲（スロープ解析とオーバーレイ表示に使用） */
  const [selectedRange, setSelectedRange] = useState<TimeRange | undefined>(undefined);
  const [error, setError] = useState<string | null>(null);

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
    setFullTimeRange(range);
    setSelectedRange(range);
    setError(null);
    setSlopeResults([]);
    if (range) {
      runSlopeAnalysis(result.sessionId, range, threshold);
    }
  }, [runSlopeAnalysis, threshold]);

  /** 時間範囲変更時（RangeSelector の「適用」ボタン経由） */
  const handleRangeChange = useCallback(async (range: TimeRange) => {
    if (!sessionId) return;
    setSelectedRange(range);
    setError(null);
    runSlopeAnalysis(sessionId, range, threshold);
  }, [sessionId, runSlopeAnalysis, threshold]);

  /** チャートドラッグ選択時 */
  const handleDragSelect = useCallback((range: TimeRange) => {
    if (!sessionId) return;
    setSelectedRange(range);
    runSlopeAnalysis(sessionId, range, threshold);
  }, [sessionId, runSlopeAnalysis, threshold]);

  /** リセットボタン押下時（全範囲に戻す） */
  const handleReset = useCallback(() => {
    if (!sessionId || !fullTimeRange) return;
    setSelectedRange(fullTimeRange);
    runSlopeAnalysis(sessionId, fullTimeRange, threshold);
  }, [sessionId, fullTimeRange, runSlopeAnalysis, threshold]);

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
              <RangeSelector
                range={selectedRange}
                onRangeChange={handleRangeChange}
                onReset={handleReset}
              />
              <ExportButton chartRef={chartRef} />
              <ReportButton
                chartRef={chartRef}
                sessionId={sessionId}
                startTime={selectedRange?.start ?? ''}
                endTime={selectedRange?.end ?? ''}
                threshold={threshold}
              />
            </section>

            <div className="content-layout">
              <section className="chart-section">
                <ChartView
                  ref={chartRef}
                  counters={counters}
                  selectedRange={selectedRange}
                  fullTimeRange={fullTimeRange}
                  onDragSelect={handleDragSelect}
                />
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
