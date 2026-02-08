import type React from 'react';
import { useState, useCallback } from 'react';

/**
 * エクスポートボタンコンポーネント
 * 解析結果を CSV または JSON 形式でダウンロードする
 */

interface ExportButtonProps {
  onExport?: (format: 'csv' | 'json') => void;
  disabled?: boolean;
}

export const ExportButton: React.FC<ExportButtonProps> = ({ onExport, disabled = false }) => {
  const [isExporting, setIsExporting] = useState(false);

  const handleExport = useCallback(async (format: 'csv' | 'json') => {
    setIsExporting(true);
    try {
      onExport?.(format);
    } finally {
      setIsExporting(false);
    }
  }, [onExport]);

  return (
    <div className="export-button">
      <h2>エクスポート</h2>
      <button
        onClick={() => handleExport('csv')}
        disabled={disabled || isExporting}
      >
        CSV エクスポート
      </button>
      <button
        onClick={() => handleExport('json')}
        disabled={disabled || isExporting}
      >
        JSON エクスポート
      </button>
    </div>
  );
};
