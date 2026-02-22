import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { snapTo30Min, findNearestLabelIndex, useChartDragSelect } from '../../hooks/useChartDragSelect';
import type { Chart as ChartJS } from 'chart.js';
import type { TimeRange } from '../../types';

// --- モック Chart.js インスタンス生成ヘルパー ---

/** テスト用ラベル（10:00〜11:00 の1分刻み） */
const TEST_LABELS = Array.from({ length: 61 }, (_, i) => {
  const h = Math.floor(i / 60) + 10;
  const m = i % 60;
  return `2026-02-01T${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}:00`;
});

/** chartAreaの定数 */
const CHART_AREA = { left: 50, right: 650, top: 10, bottom: 400 };

/**
 * モック Chart.js インスタンスと canvas を生成する。
 * pixel→index変換は chartArea の幅をラベル数で線形補間する。
 */
function createMockChart() {
  const canvas = document.createElement('canvas');
  // getBoundingClientRect のモック
  canvas.getBoundingClientRect = () => ({
    left: 0, top: 0, right: 700, bottom: 450,
    width: 700, height: 450, x: 0, y: 0, toJSON: () => ({}),
  });

  const pixelsPerLabel = (CHART_AREA.right - CHART_AREA.left) / (TEST_LABELS.length - 1);

  const chart = {
    canvas,
    chartArea: { ...CHART_AREA },
    data: { labels: [...TEST_LABELS] },
    scales: {
      x: {
        getValueForPixel: (px: number) => (px - CHART_AREA.left) / pixelsPerLabel,
        getPixelForValue: (idx: number) => CHART_AREA.left + idx * pixelsPerLabel,
      },
    },
    draw: vi.fn(),
    options: { plugins: {} },
  } as unknown as ChartJS<'line'>;

  return { chart, canvas };
}

describe('useChartDragSelect hook', () => {
  let mockChart: ReturnType<typeof createMockChart>;
  let chartRef: React.RefObject<ChartJS<'line'> | null>;
  let onDragSelect: ReturnType<typeof vi.fn> & ((range: TimeRange) => void);

  beforeEach(() => {
    mockChart = createMockChart();
    // MutableRefObject として作成
    chartRef = { current: mockChart.chart } as React.RefObject<ChartJS<'line'> | null>;
    onDragSelect = vi.fn() as ReturnType<typeof vi.fn> & ((range: TimeRange) => void);
  });

  /** mousedown → mousemove → 指定イベント のシーケンスを実行するヘルパー */
  function simulateDrag(
    canvas: HTMLCanvasElement,
    startX: number,
    moveX: number,
    finishEvent: 'mouseup' | 'mouseleave',
  ) {
    const downY = (CHART_AREA.top + CHART_AREA.bottom) / 2;
    canvas.dispatchEvent(new MouseEvent('mousedown', { clientX: startX, clientY: downY, bubbles: true }));
    canvas.dispatchEvent(new MouseEvent('mousemove', { clientX: moveX, clientY: downY, bubbles: true }));
    canvas.dispatchEvent(new MouseEvent(finishEvent, { clientX: moveX, clientY: downY, bubbles: true }));
  }

  it('mouseup でドラッグ範囲が確定する', () => {
    renderHook(() => useChartDragSelect({ chartRef, onDragSelect }));
    act(() => {
      simulateDrag(mockChart.canvas, 100, 400, 'mouseup');
    });
    expect(onDragSelect).toHaveBeenCalledTimes(1);
    const range: TimeRange = onDragSelect.mock.calls[0][0];
    expect(range.start < range.end).toBe(true);
  });

  it('mouseleave でドラッグ範囲が確定する（キャンセルされない）', () => {
    renderHook(() => useChartDragSelect({ chartRef, onDragSelect }));
    act(() => {
      simulateDrag(mockChart.canvas, 100, 400, 'mouseleave');
    });
    expect(onDragSelect).toHaveBeenCalledTimes(1);
    const range: TimeRange = onDragSelect.mock.calls[0][0];
    expect(range.start < range.end).toBe(true);
  });

  it('mouseleave 時に currentX がプロットエリア端にクランプされる', () => {
    renderHook(() => useChartDragSelect({ chartRef, onDragSelect }));

    // 右端を超える位置へドラッグ
    act(() => {
      simulateDrag(mockChart.canvas, 100, 800, 'mouseleave');
    });

    expect(onDragSelect).toHaveBeenCalledTimes(1);
    const range: TimeRange = onDragSelect.mock.calls[0][0];
    // クランプされて chartArea.right 相当のタイムスタンプが end になる
    expect(range.end).toBe(snapTo30Min(TEST_LABELS[TEST_LABELS.length - 1]));
  });

  it('ドラッグ距離が最小幅未満の場合は確定しない', () => {
    renderHook(() => useChartDragSelect({ chartRef, onDragSelect }));
    act(() => {
      simulateDrag(mockChart.canvas, 100, 102, 'mouseup');
    });
    expect(onDragSelect).not.toHaveBeenCalled();
  });
});

describe('snapTo30Min', () => {
  it('分が15未満の場合、:00に切り捨てる', () => {
    expect(snapTo30Min('2026-02-01T10:07:00')).toBe('2026-02-01T10:00:00');
    expect(snapTo30Min('2026-02-01T10:14:59')).toBe('2026-02-01T10:00:00');
    expect(snapTo30Min('2026-02-01T10:00:00')).toBe('2026-02-01T10:00:00');
  });

  it('分が15以上45未満の場合、:30に丸める', () => {
    expect(snapTo30Min('2026-02-01T10:15:00')).toBe('2026-02-01T10:30:00');
    expect(snapTo30Min('2026-02-01T10:29:00')).toBe('2026-02-01T10:30:00');
    expect(snapTo30Min('2026-02-01T10:44:00')).toBe('2026-02-01T10:30:00');
  });

  it('分が45以上の場合、次の時間の:00に切り上げる', () => {
    expect(snapTo30Min('2026-02-01T10:45:00')).toBe('2026-02-01T11:00:00');
    expect(snapTo30Min('2026-02-01T10:59:00')).toBe('2026-02-01T11:00:00');
  });

  it('23:45以上の場合、翌日の00:00に切り上げる', () => {
    expect(snapTo30Min('2026-02-01T23:45:00')).toBe('2026-02-02T00:00:00');
  });

  it('秒を0にリセットする', () => {
    expect(snapTo30Min('2026-02-01T10:07:35')).toBe('2026-02-01T10:00:00');
    expect(snapTo30Min('2026-02-01T10:20:45')).toBe('2026-02-01T10:30:00');
  });
});

describe('findNearestLabelIndex', () => {
  const labels = [
    '2026-02-01T10:00:00',
    '2026-02-01T10:01:00',
    '2026-02-01T10:02:00',
    '2026-02-01T10:03:00',
    '2026-02-01T10:04:00',
  ];

  it('完全一致する場合、そのインデックスを返す', () => {
    expect(findNearestLabelIndex(labels, '2026-02-01T10:02:00')).toBe(2);
  });

  it('先頭より前のタイムスタンプの場合、0を返す', () => {
    expect(findNearestLabelIndex(labels, '2026-02-01T09:00:00')).toBe(0);
  });

  it('末尾より後のタイムスタンプの場合、最後のインデックスを返す', () => {
    expect(findNearestLabelIndex(labels, '2026-02-01T11:00:00')).toBe(4);
  });

  it('中間のタイムスタンプの場合、最も近いインデックスを返す', () => {
    // 10:01:20 は 10:01:00（index 1）に近い
    expect(findNearestLabelIndex(labels, '2026-02-01T10:01:20')).toBe(1);
    // 10:01:40 は 10:02:00（index 2）に近い
    expect(findNearestLabelIndex(labels, '2026-02-01T10:01:40')).toBe(2);
  });

  it('空の配列の場合、-1を返す', () => {
    expect(findNearestLabelIndex([], '2026-02-01T10:00:00')).toBe(-1);
  });
});
