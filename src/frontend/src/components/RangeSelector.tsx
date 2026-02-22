import type React from 'react';
import { useState, useCallback, useEffect } from 'react';
import type { TimeRange } from '../types';

/** バリデーションエラーメッセージ */
const VALIDATION_ERROR_START_AFTER_END = '開始日時は終了日時より前に設定してください';

/**
 * 範囲選択コンポーネントの props
 */
interface RangeSelectorProps {
  /** 現在適用中の範囲（外部制御、controlled） */
  range?: TimeRange;
  /** 後方互換: range が未指定の場合の初期値 */
  initialRange?: TimeRange;
  onRangeChange: (range: TimeRange) => void;
  /** リセットコールバック（提供時にリセットボタンを表示） */
  onReset?: () => void;
}

/**
 * ISO 8601 タイムスタンプを datetime-local 入力用の形式に変換する
 * 例: "2026-02-01T10:30:00.000Z" → "2026-02-01T10:30:00"
 */
function toDateTimeLocal(isoString: string): string {
  return isoString.slice(0, 19);
}

/**
 * 範囲選択コンポーネント
 * 解析対象の時間範囲を選択する
 * controlled モード: range prop で外部から同期
 */
export const RangeSelector: React.FC<RangeSelectorProps> = ({ range, initialRange, onRangeChange, onReset }) => {
  const effectiveRange = range ?? initialRange;
  const [start, setStart] = useState(
    effectiveRange ? toDateTimeLocal(effectiveRange.start) : ''
  );
  const [end, setEnd] = useState(
    effectiveRange ? toDateTimeLocal(effectiveRange.end) : ''
  );
  const [validationError, setValidationError] = useState<string | null>(null);

  // 外部からの range 変更を内部 state に同期（controlled モード）
  useEffect(() => {
    if (range) {
      setStart(toDateTimeLocal(range.start));
      setEnd(toDateTimeLocal(range.end));
      setValidationError(null);
    }
  }, [range]);

  /** 適用ボタンクリック時 */
  const handleApply = useCallback(() => {
    if (start && end) {
      if (start >= end) {
        setValidationError(VALIDATION_ERROR_START_AFTER_END);
        return;
      }
      setValidationError(null);
      onRangeChange({ start, end });
    }
  }, [start, end, onRangeChange]);

  return (
    <div className="range-selector">
      <h3>時間範囲</h3>
      <div className="range-selector__fields">
        <label>
          開始:
          <input
            type="datetime-local"
            step="1"
            value={start}
            onChange={(e) => setStart(e.target.value)}
          />
        </label>
        <label>
          終了:
          <input
            type="datetime-local"
            step="1"
            value={end}
            onChange={(e) => setEnd(e.target.value)}
          />
        </label>
      </div>
      <div className="range-selector__buttons">
        <button onClick={handleApply} disabled={!start || !end}>
          適用
        </button>
        {onReset && (
          <button className="range-selector__reset" onClick={onReset} type="button">
            リセット
          </button>
        )}
      </div>
      {validationError && (
        <p className="range-selector__error" role="alert">{validationError}</p>
      )}
    </div>
  );
};
