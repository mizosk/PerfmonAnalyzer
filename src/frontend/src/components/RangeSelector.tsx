import type React from 'react';
import { useState, useCallback } from 'react';
import type { TimeRange } from '../types';

/**
 * 範囲選択コンポーネント
 * 解析対象の時間範囲を選択する
 */

interface RangeSelectorProps {
  onRangeChange?: (range: TimeRange) => void;
}

export const RangeSelector: React.FC<RangeSelectorProps> = ({ onRangeChange }) => {
  const [start, setStart] = useState('');
  const [end, setEnd] = useState('');

  const handleApply = useCallback(() => {
    if (start && end) {
      onRangeChange?.({ start, end });
    }
  }, [start, end, onRangeChange]);

  return (
    <div className="range-selector">
      <h2>範囲選択</h2>
      <div>
        <label>
          開始:
          <input
            type="datetime-local"
            value={start}
            onChange={(e) => setStart(e.target.value)}
          />
        </label>
      </div>
      <div>
        <label>
          終了:
          <input
            type="datetime-local"
            value={end}
            onChange={(e) => setEnd(e.target.value)}
          />
        </label>
      </div>
      <button onClick={handleApply} disabled={!start || !end}>
        適用
      </button>
    </div>
  );
};
