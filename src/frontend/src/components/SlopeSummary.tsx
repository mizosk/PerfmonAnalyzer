import type React from 'react';
import type { SlopeResult } from '../types';

/**
 * 傾き要約表示コンポーネント
 * 傾き解析の結果をテーブル形式で表示する
 */

interface SlopeSummaryProps {
  results?: SlopeResult[];
}

export const SlopeSummary: React.FC<SlopeSummaryProps> = ({ results }) => {
  if (!results || results.length === 0) {
    return (
      <div className="slope-summary">
        <h2>傾き解析結果</h2>
        <p>解析結果がありません。</p>
      </div>
    );
  }

  return (
    <div className="slope-summary">
      <h2>傾き解析結果</h2>
      <table>
        <thead>
          <tr>
            <th>カウンター名</th>
            <th>傾き</th>
            <th>切片</th>
            <th>R²</th>
            <th>開始時間</th>
            <th>終了時間</th>
          </tr>
        </thead>
        <tbody>
          {results.map((result, index) => (
            <tr key={index}>
              <td>{result.counterName}</td>
              <td>{result.slope.toFixed(6)}</td>
              <td>{result.intercept.toFixed(4)}</td>
              <td>{result.rSquared.toFixed(4)}</td>
              <td>{result.startTime}</td>
              <td>{result.endTime}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
};
