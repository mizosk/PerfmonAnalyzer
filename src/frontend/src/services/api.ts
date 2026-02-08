import axios from 'axios';
import type { ImportResult, CounterData, SlopeResult, TimeRange } from '../types';

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
export const uploadCsv = async (file: File): Promise<ImportResult> => {
  const formData = new FormData();
  formData.append('file', file);
  const response = await apiClient.post('/csv/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return response.data;
};

/** カウンターデータを取得 */
export const getCounterData = async (counterName: string): Promise<CounterData> => {
  const response = await apiClient.get(`/counters/${encodeURIComponent(counterName)}`);
  return response.data;
};

/** 傾き解析を実行 */
export const analyzeSlopeForCounter = async (
  counterName: string,
  range: TimeRange
): Promise<SlopeResult> => {
  const response = await apiClient.post('/analysis/slope', {
    counterName,
    ...range,
  });
  return response.data;
};

/** 傾き解析結果をエクスポート */
export const exportResults = async (format: 'csv' | 'json'): Promise<Blob> => {
  const response = await apiClient.get(`/export/${format}`, {
    responseType: 'blob',
  });
  return response.data;
};

export default apiClient;
