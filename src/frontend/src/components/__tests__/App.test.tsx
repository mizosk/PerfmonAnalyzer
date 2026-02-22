import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

// Chart.js と react-chartjs-2 をモック
vi.mock('chart.js', () => ({
  Chart: { register: vi.fn() },
  CategoryScale: class {},
  LinearScale: class {},
  PointElement: class {},
  LineElement: class {},
  Title: class {},
  Tooltip: class {},
  Legend: class {},
}));

vi.mock('react-chartjs-2', () => ({
  Line: function MockLine() {
    return <div data-testid="mock-chart">Mock Chart</div>;
  },
}));

// プラグインと hook をモック
vi.mock('../../plugins/chartDragSelectPlugin', () => ({
  dragSelectPlugin: { id: 'dragSelect', afterDraw: vi.fn() },
}));

vi.mock('../../hooks/useChartDragSelect', () => ({
  useChartDragSelect: vi.fn(),
  findNearestLabelIndex: vi.fn().mockReturnValue(0),
}));

// API モジュールをモック
vi.mock('../../services/api', () => ({
  uploadCsv: vi.fn(),
  analyzeSlopeForSession: vi.fn().mockResolvedValue({ results: [] }),
}));

import App from '../../App';
import { uploadCsv, analyzeSlopeForSession } from '../../services/api';
import type { UploadResult, SlopeResponse } from '../../types';

const mockUploadCsv = vi.mocked(uploadCsv);
const mockAnalyzeSlopeForSession = vi.mocked(analyzeSlopeForSession);

/** ファイル入力要素を取得するヘルパー */
function getFileInput(): HTMLInputElement {
  const input = document.querySelector('input[type="file"]');
  if (!input) throw new Error('File input not found');
  return input as HTMLInputElement;
}

describe('App', () => {
  it('ヘッダーとファイルアップロードセクションを表示する', () => {
    render(<App />);
    expect(screen.getByText('Perfmon Analyzer')).toBeInTheDocument();
    expect(screen.getByText('CSV ファイルアップロード')).toBeInTheDocument();
  });

  it('初期状態ではグラフセクションが表示されない', () => {
    render(<App />);
    expect(screen.queryByTestId('mock-chart')).not.toBeInTheDocument();
  });

  it('ファイルアップロード成功後にグラフ表示領域が表示される', async () => {
    // アップロード結果のモックデータ
    const mockResult: UploadResult = {
      sessionId: 'test-session-123',
      counters: [
        {
          machineName: 'SERVER01',
          category: 'Memory',
          instanceName: '',
          counterName: 'Available MBytes',
          displayName: 'Memory - Available MBytes',
          dataPoints: [
            { timestamp: '2026-02-01T10:00:00', value: 4096 },
            { timestamp: '2026-02-01T10:01:00', value: 4000 },
          ],
        },
      ],
    };
    mockUploadCsv.mockResolvedValueOnce(mockResult);

    render(<App />);

    // ファイルを選択
    const input = getFileInput();
    const file = new File(['test-csv-content'], 'perfmon.csv', { type: 'text/csv' });
    fireEvent.change(input, { target: { files: [file] } });

    // アップロードボタンをクリック
    const uploadButton = screen.getByRole('button', { name: 'アップロード' });
    fireEvent.click(uploadButton);

    // グラフが表示されることを確認
    await waitFor(() => {
      expect(screen.getByTestId('mock-chart')).toBeInTheDocument();
    });

    // API が正しく呼ばれたことを確認
    expect(mockUploadCsv).toHaveBeenCalledWith(file);
  });

  it('ファイルアップロード成功後に傾きサマリセクションが表示される', async () => {
    const mockResult: UploadResult = {
      sessionId: 'session-slope-test',
      counters: [
        {
          machineName: 'SERVER01',
          category: 'Memory',
          instanceName: '',
          counterName: 'Available MBytes',
          displayName: 'Memory - Available MBytes',
          dataPoints: [
            { timestamp: '2026-02-01T10:00:00', value: 4096 },
            { timestamp: '2026-02-01T10:01:00', value: 4000 },
          ],
        },
      ],
    };
    mockUploadCsv.mockResolvedValueOnce(mockResult);
    mockAnalyzeSlopeForSession.mockResolvedValueOnce({ results: [] });

    render(<App />);

    const input = getFileInput();
    const file = new File(['csv'], 'perfmon.csv', { type: 'text/csv' });
    fireEvent.change(input, { target: { files: [file] } });
    fireEvent.click(screen.getByRole('button', { name: 'アップロード' }));

    await waitFor(() => {
      expect(screen.getByText('傾き解析結果')).toBeInTheDocument();
    });
  });

  it('analyzeSlopeForSession が結果を返した場合、テーブルにカウンタ名が表示される', async () => {
    const mockResult: UploadResult = {
      sessionId: 'session-slope-table',
      counters: [
        {
          machineName: 'SERVER01',
          category: 'Memory',
          instanceName: '',
          counterName: 'Available MBytes',
          displayName: 'Memory - Available MBytes',
          dataPoints: [
            { timestamp: '2026-02-01T10:00:00', value: 4096 },
            { timestamp: '2026-02-01T10:01:00', value: 4000 },
          ],
        },
      ],
    };
    const slopeResponse: SlopeResponse = {
      results: [
        {
          counterName: '\\SERVER01\\Memory\\Available MBytes',
          slopeKBPer10Min: 75.5,
          isWarning: true,
          rSquared: 0.95,
        },
      ],
    };
    mockUploadCsv.mockResolvedValueOnce(mockResult);
    mockAnalyzeSlopeForSession.mockResolvedValueOnce(slopeResponse);

    render(<App />);

    const input = getFileInput();
    const file = new File(['csv'], 'perfmon.csv', { type: 'text/csv' });
    fireEvent.change(input, { target: { files: [file] } });
    fireEvent.click(screen.getByRole('button', { name: 'アップロード' }));

    await waitFor(() => {
      expect(
        screen.getByText('\\SERVER01\\Memory\\Available MBytes'),
      ).toBeInTheDocument();
    });
    // テーブルが aria-label 付きで表示されていることも確認
    expect(screen.getByRole('table', { name: '傾き解析結果一覧' })).toBeInTheDocument();
  });
});
