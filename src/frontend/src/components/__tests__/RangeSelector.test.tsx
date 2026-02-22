import { render, screen, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RangeSelector } from '../RangeSelector';
import type { TimeRange } from '../../types';

describe('RangeSelector', () => {
  const mockOnRangeChange = vi.fn();
  const mockOnReset = vi.fn();

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

  it('開始日時が終了日時以降の場合、エラーメッセージを表示し onRangeChange を呼ばない', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} />);

    const startInput = screen.getByLabelText('開始:');
    const endInput = screen.getByLabelText('終了:');

    // 開始 > 終了
    fireEvent.change(startInput, { target: { value: '2026-02-01T20:00' } });
    fireEvent.change(endInput, { target: { value: '2026-02-01T08:00' } });

    const button = screen.getByRole('button', { name: '適用' });
    fireEvent.click(button);

    expect(screen.getByRole('alert')).toHaveTextContent('開始日時は終了日時より前に設定してください');
    expect(mockOnRangeChange).not.toHaveBeenCalled();
  });

  it('開始日時と終了日時が同じ場合もエラーメッセージを表示する', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} />);

    const startInput = screen.getByLabelText('開始:');
    const endInput = screen.getByLabelText('終了:');

    fireEvent.change(startInput, { target: { value: '2026-02-01T12:00' } });
    fireEvent.change(endInput, { target: { value: '2026-02-01T12:00' } });

    const button = screen.getByRole('button', { name: '適用' });
    fireEvent.click(button);

    expect(screen.getByRole('alert')).toHaveTextContent('開始日時は終了日時より前に設定してください');
    expect(mockOnRangeChange).not.toHaveBeenCalled();
  });

  it('バリデーションエラー後に正しい範囲で適用するとエラーがクリアされる', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} />);

    const startInput = screen.getByLabelText('開始:');
    const endInput = screen.getByLabelText('終了:');

    // まず不正な範囲で適用
    fireEvent.change(startInput, { target: { value: '2026-02-01T20:00' } });
    fireEvent.change(endInput, { target: { value: '2026-02-01T08:00' } });
    fireEvent.click(screen.getByRole('button', { name: '適用' }));
    expect(screen.getByRole('alert')).toBeInTheDocument();

    // 正しい範囲に修正して再適用
    fireEvent.change(startInput, { target: { value: '2026-02-01T08:00' } });
    fireEvent.change(endInput, { target: { value: '2026-02-01T20:00' } });
    fireEvent.click(screen.getByRole('button', { name: '適用' }));

    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(mockOnRangeChange).toHaveBeenCalledWith({
      start: '2026-02-01T08:00',
      end: '2026-02-01T20:00',
    });
  });

  it('range prop が変更されると入力フィールドが同期される', () => {
    const range1: TimeRange = {
      start: '2026-02-01T10:00:00',
      end: '2026-02-01T12:00:00',
    };
    const range2: TimeRange = {
      start: '2026-02-01T14:00:00',
      end: '2026-02-01T16:00:00',
    };

    const { rerender } = render(<RangeSelector range={range1} onRangeChange={mockOnRangeChange} />);

    const startInput = screen.getByLabelText('開始:') as HTMLInputElement;
    const endInput = screen.getByLabelText('終了:') as HTMLInputElement;
    expect(startInput.value).toMatch(/^2026-02-01T10:00/);
    expect(endInput.value).toMatch(/^2026-02-01T12:00/);

    // range prop を変更して再レンダリング
    rerender(<RangeSelector range={range2} onRangeChange={mockOnRangeChange} />);

    expect(startInput.value).toMatch(/^2026-02-01T14:00/);
    expect(endInput.value).toMatch(/^2026-02-01T16:00/);
  });

  it('onReset が提供されている場合、リセットボタンを表示する', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} onReset={mockOnReset} />);
    expect(screen.getByRole('button', { name: 'リセット' })).toBeInTheDocument();
  });

  it('onReset が未提供の場合、リセットボタンを表示しない', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} />);
    expect(screen.queryByRole('button', { name: 'リセット' })).not.toBeInTheDocument();
  });

  it('リセットボタンをクリックすると onReset が呼び出される', () => {
    render(<RangeSelector onRangeChange={mockOnRangeChange} onReset={mockOnReset} />);

    const resetButton = screen.getByRole('button', { name: 'リセット' });
    fireEvent.click(resetButton);
    expect(mockOnReset).toHaveBeenCalledTimes(1);
  });
});
