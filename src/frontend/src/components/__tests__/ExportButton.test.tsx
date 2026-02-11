import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ExportButton } from '../ExportButton';
import { createRef } from 'react';
import type { Chart } from 'chart.js';

describe('ExportButton', () => {
  it('PNG エクスポートボタンを表示する', () => {
    const chartRef = createRef<Chart<'line'>>();
    render(<ExportButton chartRef={chartRef} />);
    expect(screen.getByRole('button', { name: /PNG/ })).toBeInTheDocument();
  });

  it('chartRef が null の場合、クリックしてもエラーにならない', () => {
    const chartRef = createRef<Chart<'line'>>();
    render(<ExportButton chartRef={chartRef} />);
    const button = screen.getByRole('button', { name: /PNG/ });
    // クリックしてもエラーが発生しないことを確認
    expect(() => fireEvent.click(button)).not.toThrow();
  });

  it('chartRef のインスタンスがある場合、toBase64Image を呼び出す', () => {
    const mockChart = {
      toBase64Image: vi.fn(() => 'data:image/png;base64,mockdata'),
    } as unknown as Chart<'line'>;

    const chartRef = { current: mockChart };
    render(<ExportButton chartRef={chartRef} />);

    // document.createElement('a') の click をモック
    const mockClick = vi.fn();
    const mockCreateElement = vi.spyOn(document, 'createElement');
    const mockAnchor = { href: '', download: '', click: mockClick } as unknown as HTMLAnchorElement;
    mockCreateElement.mockReturnValueOnce(mockAnchor);

    const button = screen.getByRole('button', { name: /PNG/ });
    fireEvent.click(button);

    expect(mockChart.toBase64Image).toHaveBeenCalled();
    expect(mockClick).toHaveBeenCalled();
    expect(mockAnchor.download).toBe('chart.png');

    mockCreateElement.mockRestore();
  });
});
