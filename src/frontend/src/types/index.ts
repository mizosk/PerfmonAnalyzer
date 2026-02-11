/**
 * パフォーマンスモニターデータの共通型定義
 */

/** CSV インポート結果 */
export interface ImportResult {
  fileName: string;
  rowCount: number;
  counters: string[];
}

/** パフォーマンスカウンターのデータポイント */
export interface DataPoint {
  timestamp: string;
  value: number;
}

/** カウンターデータ */
export interface CounterData {
  counterName: string;
  dataPoints: DataPoint[];
}

/** 範囲選択 */
export interface TimeRange {
  start: string;
  end: string;
}

/** 傾き解析結果 */
export interface SlopeResult {
  counterName: string;
  slopeKBPer10Min: number;
  isWarning: boolean;
  rSquared: number;
}

/** API エラーレスポンス */
export interface ApiError {
  message: string;
  details?: string;
}
