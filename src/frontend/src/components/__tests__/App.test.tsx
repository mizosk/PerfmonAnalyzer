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

// API モジュールをモック
vi.mock('../../services/api', () => ({
  uploadCsv: vi.fn(),
  getSessionData: vi.fn(),
}));

import App from '../../App';
import { uploadCsv, getSessionData } from '../../services/api';
import type { UploadResult, DataResponse } from '../../types';

const mockUploadCsv = vi.mocked(uploadCsv);
const mockGetSessionData = vi.mocked(getSessionData);

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
});
