/**
 * パフォーマンスモニターデータの共通型定義
 */

/** データポイント */
export interface DataPoint {
  timestamp: string;
  value: number;
}

/** カウンター情報（バックエンド CounterInfo に対応） */
export interface CounterInfo {
  machineName: string;
  category: string;
  instanceName: string;
  counterName: string;
  displayName: string;
  dataPoints: DataPoint[];
}

/** CSV アップロード結果（バックエンド UploadResult に対応） */
export interface UploadResult {
  sessionId: string;
  counters: CounterInfo[];
}

/** データ取得レスポンス（バックエンド DataResponse に対応） */
export interface DataResponse {
  counters: CounterInfo[];
}

/** 範囲選択 */
export interface TimeRange {
  start: string;
  end: string;
}

/** 傾き分析リクエスト（バックエンド SlopeRequest に対応） */
export interface SlopeRequest {
  sessionId: string;
  startTime: string;
  endTime: string;
  thresholdKBPer10Min?: number;
}

/** 傾き分析結果（バックエンド SlopeResult に対応） */
export interface SlopeResult {
  counterName: string;
  slopeKBPer10Min: number;
  isWarning: boolean;
  rSquared: number;
}

/** 傾き分析レスポンス（バックエンド SlopeResponse に対応） */
export interface SlopeResponse {
  results: SlopeResult[];
}

/** API エラーレスポンス */
export interface ApiError {
  error: string;
}

/** レポート生成リクエスト（バックエンド ReportRequest に対応） */
export interface ReportRequest {
  sessionId: string;
  startTime: string;
  endTime: string;
  thresholdKBPer10Min: number;
  chartImageBase64: string;
  format: 'html' | 'md';
}
