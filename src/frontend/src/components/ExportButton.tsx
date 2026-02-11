import type React from 'react';
import { useCallback } from 'react';
import type { Chart } from 'chart.js';

/**
 * エクスポートボタンコンポーネントの props
 */
interface ExportButtonProps {
  chartRef: React.RefObject<Chart<'line'> | null>;
}

/**
 * エクスポートボタンコンポーネント
 * Chart.js のインスタンスからPNG画像をダウンロードする
 */
export const ExportButton: React.FC<ExportButtonProps> = ({ chartRef }) => {
  /** PNG画像としてダウンロード */
  const handleExportPng = useCallback(() => {
    const chart = chartRef.current;
    if (!chart) return;

    const imageUrl = chart.toBase64Image();
    const link = document.createElement('a');
    link.download = 'chart.png';
    link.href = imageUrl;
    link.click();
  }, [chartRef]);

  return (
    <div className="export-button">
      <button onClick={handleExportPng}>
        PNG エクスポート
      </button>
    </div>
  );
};
