import axios from 'axios';
import type { UploadResult, DataResponse, SlopeResponse, SlopeRequest, TimeRange } from '../types';

/**
 * API クライアント
 * Vite のプロキシ設定により、/api へのリクエストはバックエンドに転送される
 */
const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

/** ヘルスチェック */
export const getHealth = async (): Promise<{ status: string }> => {
  const response = await apiClient.get('/health');
  return response.data;
};

/** CSV ファイルをアップロード */
export const uploadCsv = async (file: File): Promise<UploadResult> => {
  const formData = new FormData();
  formData.append('file', file);
  const response = await apiClient.post('/file/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return response.data;
};

/** セッションのカウンターデータを取得 */
export const getSessionData = async (
  sessionId: string,
  startTime?: string,
  endTime?: string
): Promise<DataResponse> => {
  const params: Record<string, string> = {};
  if (startTime) params.startTime = startTime;
  if (endTime) params.endTime = endTime;

  const response = await apiClient.get(`/data/${encodeURIComponent(sessionId)}`, { params });
  return response.data;
};

/** 傾き解析を実行 */
export const analyzeSlope = async (request: SlopeRequest): Promise<SlopeResponse> => {
  const response = await apiClient.post('/analysis/slope', request);
  return response.data;
};

/** 傾き解析のヘルパー（sessionId + TimeRange から SlopeRequest を組み立て） */
export const analyzeSlopeForSession = async (
  sessionId: string,
  range: TimeRange,
  thresholdKBPer10Min?: number
): Promise<SlopeResponse> => {
  return analyzeSlope({
    sessionId,
    startTime: range.start,
    endTime: range.end,
    thresholdKBPer10Min,
  });
};

/** 傾き解析結果をエクスポート */
export const exportResults = async (format: 'csv' | 'json'): Promise<Blob> => {
  const response = await apiClient.get(`/export/${format}`, {
    responseType: 'blob',
  });
  return response.data;
};

export default apiClient;
