import type React from 'react';
import { useState, useCallback } from 'react';
import type { Chart } from 'chart.js';
import { generateReport } from '../services/api';

/**
 * レポートボタンコンポーネントの props
 */
interface ReportButtonProps {
  chartRef: React.RefObject<Chart<'line'> | null>;
  sessionId: string | null;
  startTime: string;
  endTime: string;
  threshold: number;
}

/**
 * レポート生成ボタンコンポーネント
 * チャート画像を取得してバックエンドでレポートを生成し、ファイルとしてダウンロードする
 */
export const ReportButton: React.FC<ReportButtonProps> = ({
  chartRef,
  sessionId,
  startTime,
  endTime,
  threshold,
}) => {
  const [format, setFormat] = useState<'html' | 'md'>('html');
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /** レポートを生成してダウンロード */
  const handleGenerateReport = useCallback(async () => {
    if (!sessionId) return;

    setIsGenerating(true);
    setError(null);
    try {
      // Chart.js からグラフ画像を取得
      const chart = chartRef.current;
      const chartImageBase64 = chart ? chart.toBase64Image() : '';

      // バックエンドにリクエスト送信
      const blob = await generateReport({
        sessionId,
        startTime,
        endTime,
        thresholdKBPer10Min: threshold,
        chartImageBase64,
        format,
      });

      // Blobをファイルとしてダウンロード
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      const extension = format === 'md' ? 'md' : 'html';
      link.download = `perfmon_report.${extension}`;
      link.href = url;
      link.click();
      URL.revokeObjectURL(url);
    } catch (err) {
      console.error('レポート生成に失敗しました:', err);
      setError('レポート生成に失敗しました。もう一度お試しください。');
    } finally {
      setIsGenerating(false);
    }
  }, [chartRef, sessionId, startTime, endTime, threshold, format]);

  return (
    <div className="report-button">
      <select
        value={format}
        onChange={(e) => setFormat(e.target.value as 'html' | 'md')}
        disabled={isGenerating}
      >
        <option value="html">HTML</option>
        <option value="md">Markdown</option>
      </select>
      <button
        onClick={handleGenerateReport}
        disabled={!sessionId || isGenerating}
      >
        {isGenerating ? '生成中...' : 'レポート生成'}
      </button>
      {error && <p className="report-error" role="alert">{error}</p>}
    </div>
  );
};
