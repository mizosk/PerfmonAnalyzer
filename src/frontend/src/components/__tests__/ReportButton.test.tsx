import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ReportButton } from '../ReportButton';
import { createRef } from 'react';
import type { Chart } from 'chart.js';

// api モジュールをモック
vi.mock('../../services/api', () => ({
  generateReport: vi.fn(),
}));

import { generateReport } from '../../services/api';

describe('ReportButton', () => {
  const defaultProps = {
    chartRef: createRef<Chart<'line'>>(),
    sessionId: 'test-session',
    startTime: '2026-01-01T00:00:00',
    endTime: '2026-01-02T00:00:00',
    threshold: 50,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('レポート生成ボタンを表示する', () => {
    render(<ReportButton {...defaultProps} />);
    expect(screen.getByRole('button', { name: /レポート生成/ })).toBeInTheDocument();
  });

  it('出力形式の選択ドロップダウンが表示される', () => {
    render(<ReportButton {...defaultProps} />);
    const select = screen.getByRole('combobox');
    expect(select).toBeInTheDocument();
  });

  it('HTML と Markdown の選択肢がある', () => {
    render(<ReportButton {...defaultProps} />);
    const options = screen.getAllByRole('option');
    expect(options).toHaveLength(2);
    expect(options[0]).toHaveTextContent('HTML');
    expect(options[1]).toHaveTextContent('Markdown');
  });

  it('sessionId が null の場合、ボタンが無効になる', () => {
    render(<ReportButton {...defaultProps} sessionId={null} />);
    const button = screen.getByRole('button', { name: /レポート生成/ });
    expect(button).toBeDisabled();
  });

  it('chartRef が null の場合でもクリックしてエラーにならない', async () => {
    const mockGenerateReport = vi.mocked(generateReport);
    mockGenerateReport.mockResolvedValueOnce(new Blob(['<html>test</html>'], { type: 'text/html' }));

    render(<ReportButton {...defaultProps} />);
    const button = screen.getByRole('button', { name: /レポート生成/ });
    // chartRef.current が null の場合は空文字がチャート画像として送信される
    expect(() => fireEvent.click(button)).not.toThrow();
  });

  it('出力形式を変更できる', () => {
    render(<ReportButton {...defaultProps} />);
    const select = screen.getByRole('combobox');
    fireEvent.change(select, { target: { value: 'md' } });
    expect(select).toHaveValue('md');
  });

  it('レポート生成中はローディング表示になる', async () => {
    const mockGenerateReport = vi.mocked(generateReport);
    // 解決を遅延させる
    let resolvePromise: (value: Blob) => void;
    const promise = new Promise<Blob>((resolve) => { resolvePromise = resolve; });
    mockGenerateReport.mockReturnValueOnce(promise);

    const mockChart = {
      toBase64Image: vi.fn(() => 'data:image/png;base64,mockdata'),
    } as unknown as Chart<'line'>;

    render(<ReportButton {...defaultProps} chartRef={{ current: mockChart }} />);
    const button = screen.getByRole('button', { name: /レポート生成/ });
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /生成中/ })).toBeDisabled();
    });

    // Promise を解決
    resolvePromise!(new Blob(['test'], { type: 'text/html' }));
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /レポート生成/ })).toBeEnabled();
    });
  });

  it('chartRef のインスタンスがある場合、toBase64Image を使用する', async () => {
    const mockGenerateReport = vi.mocked(generateReport);
    mockGenerateReport.mockResolvedValueOnce(new Blob(['<html>test</html>'], { type: 'text/html' }));

    const mockChart = {
      toBase64Image: vi.fn(() => 'data:image/png;base64,mockdata'),
    } as unknown as Chart<'line'>;

    // URL.createObjectURL と URL.revokeObjectURL をモック
    const mockCreateObjectURL = vi.fn(() => 'blob:mock-url');
    const mockRevokeObjectURL = vi.fn();
    global.URL.createObjectURL = mockCreateObjectURL;
    global.URL.revokeObjectURL = mockRevokeObjectURL;

    render(<ReportButton {...defaultProps} chartRef={{ current: mockChart }} />);
    const button = screen.getByRole('button', { name: /レポート生成/ });
    fireEvent.click(button);

    await waitFor(() => {
      expect(mockChart.toBase64Image).toHaveBeenCalled();
      expect(mockGenerateReport).toHaveBeenCalledWith(expect.objectContaining({
        chartImageBase64: 'data:image/png;base64,mockdata',
        format: 'html',
        sessionId: 'test-session',
      }));
    });
  });

  it('レポート生成失敗時にエラーメッセージを表示する', async () => {
    const mockGenerateReport = vi.mocked(generateReport);
    mockGenerateReport.mockRejectedValueOnce(new Error('Server Error'));

    render(<ReportButton {...defaultProps} />);
    const button = screen.getByRole('button', { name: /レポート生成/ });
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('レポート生成に失敗しました。もう一度お試しください。');
    });
  });

  it('再度レポート生成を試みるとエラーメッセージがクリアされる', async () => {
    const mockGenerateReport = vi.mocked(generateReport);
    mockGenerateReport.mockRejectedValueOnce(new Error('Server Error'));

    render(<ReportButton {...defaultProps} />);
    const button = screen.getByRole('button', { name: /レポート生成/ });
    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });

    // 2回目はBlobを返す
    mockGenerateReport.mockResolvedValueOnce(new Blob(['<html>test</html>'], { type: 'text/html' }));
    const mockCreateObjectURL = vi.fn(() => 'blob:mock-url');
    const mockRevokeObjectURL = vi.fn();
    global.URL.createObjectURL = mockCreateObjectURL;
    global.URL.revokeObjectURL = mockRevokeObjectURL;

    fireEvent.click(button);

    await waitFor(() => {
      expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    });
  });
});
