import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RangeSelector } from '../RangeSelector';
import type { TimeRange } from '../../types';

describe('RangeSelector', () => {
  const mockOnRangeChange = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('開始・終了の入力フィールドと適用ボタンを表示する', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} />);
    expect(screen.getByLabelText('開始:')).toBeInTheDocument();
    expect(screen.getByLabelText('終了:')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '適用' })).toBeInTheDocument();
  });

  it('初期値が設定された場合、入力フィールドに反映される', () => {
    const initialRange: TimeRange = {
      start: '2026-02-01T10:00:00',
      end: '2026-02-01T12:00:00',
    };
    render(<RangeSelector initialRange={initialRange} onRangeChange={mockOnRangeChange} />);

    const startInput = screen.getByLabelText('開始:') as HTMLInputElement;
    const endInput = screen.getByLabelText('終了:') as HTMLInputElement;
    // jsdom の datetime-local は秒が 00 の場合に省略する場合がある
    expect(startInput.value).toMatch(/^2026-02-01T10:00/);
    expect(endInput.value).toMatch(/^2026-02-01T12:00/);
  });

  it('開始・終了が未入力の場合、適用ボタンが無効', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} />);
    expect(screen.getByRole('button', { name: '適用' })).toBeDisabled();
  });

  it('適用ボタンをクリックすると onRangeChange が呼び出される', () => {
    const initialRange: TimeRange = {
      start: '2026-02-01T10:00:00',
      end: '2026-02-01T12:00:00',
    };
    render(<RangeSelector initialRange={initialRange} onRangeChange={mockOnRangeChange} />);

    const button = screen.getByRole('button', { name: '適用' });
    fireEvent.click(button);
    expect(mockOnRangeChange).toHaveBeenCalledWith({
      start: '2026-02-01T10:00:00',
      end: '2026-02-01T12:00:00',
    });
  });

  it('入力値を変更して適用すると新しい範囲が返される', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} />);

    const startInput = screen.getByLabelText('開始:');
    const endInput = screen.getByLabelText('終了:');

    fireEvent.change(startInput, { target: { value: '2026-02-01T08:30' } });
    fireEvent.change(endInput, { target: { value: '2026-02-01T20:45' } });

    const button = screen.getByRole('button', { name: '適用' });
    fireEvent.click(button);
    expect(mockOnRangeChange).toHaveBeenCalledWith({
      start: '2026-02-01T08:30',
      end: '2026-02-01T20:45',
    });
  });
});
