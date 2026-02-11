import { useMemo } from 'react';
import type React from 'react';
import type { SlopeResult } from '../types';

/**
 * 傾きサマリ表示コンポーネント
 *
 * 傾き解析の結果をテーブル形式で表示し、
 * 閾値超過行を警告色で強調する。
 */

interface SlopeSummaryProps {
  /** 傾き分析結果の配列 */
  results: SlopeResult[];
  /** 警告閾値 (KB/10min) — この値を超えると警告表示 */
  threshold: number;
  /** 閾値変更時のコールバック */
  onThresholdChange?: (value: number) => void;
}

export const SlopeSummary: React.FC<SlopeSummaryProps> = ({
  results,
  threshold,
  onThresholdChange,
}) => {
  // 傾き降順でソート（元配列を変更しない）— results が変わらない限り再計算しない
  const sorted = useMemo(
    () => [...results].sort((a, b) => b.slopeKBPer10Min - a.slopeKBPer10Min),
    [results],
  );

  // 閾値の即時反映を優先するため、バックエンド由来の isWarning ではなく
  // フロントエンド側で slopeKBPer10Min > threshold により判定を行う
  const isWarning = (result: SlopeResult): boolean =>
    result.slopeKBPer10Min > threshold;

  return (
    <div className="slope-summary">
      <div className="slope-summary__header">
        <h2>傾き解析結果</h2>
        {onThresholdChange && (
          <label className="slope-summary__threshold">
            閾値 (KB/10min)
            <input
              type="number"
              value={threshold}
              min="0"
              step="1"
              onChange={(e) => onThresholdChange(Number(e.target.value))}
            />
          </label>
        )}
      </div>

      {sorted.length === 0 ? (
        <p>解析結果がありません。</p>
      ) : (
        <table className="slope-summary__table" aria-label="傾き解析結果一覧">
          <thead>
            <tr>
              <th>カウンタ名</th>
              <th>傾き (KB/10min)</th>
              <th>R²</th>
              <th>判定</th>
            </tr>
          </thead>
          <tbody>
            {sorted.map((result) => {
              const warn = isWarning(result);
              return (
                <tr
                  key={result.counterName}
                  className={warn ? 'warning' : 'ok'}
                >
                  <td>{result.counterName}</td>
                  <td>{result.slopeKBPer10Min.toFixed(2)}</td>
                  <td>{result.rSquared.toFixed(3)}</td>
                  <td>{warn ? '⚠️ 警告' : '✓ OK'}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
};
