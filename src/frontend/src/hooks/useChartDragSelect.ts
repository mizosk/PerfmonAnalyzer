import { useEffect, useRef, useCallback } from 'react';
import type { Chart as ChartJS } from 'chart.js';
import type { TimeRange } from '../types';
import type { DragState } from '../plugins/chartDragSelectPlugin';

/** 最小ドラッグ幅（px）。これ未満のドラッグは無視 */
const MIN_DRAG_DISTANCE_PX = 5;
/** デフォルトのスナップ間隔（分） */
const DEFAULT_SNAP_MINUTES = 30;
/** スナップの境界閾値（分）: この値未満なら切り捨て */
const SNAP_THRESHOLD_LOW = 15;
/** スナップの境界閾値（分）: この値以上なら次の時間に切り上げ */
const SNAP_THRESHOLD_HIGH = 45;

/** hook のオプション */
interface UseChartDragSelectOptions {
  chartRef: React.RefObject<ChartJS<'line'> | null>;
  onDragSelect: (range: TimeRange) => void;
  snapMinutes?: number;
  enabled?: boolean;
}

/**
 * タイムスタンプを指定分数単位にスナップする（文字列操作ベース、タイムゾーン問題を回避）
 *
 * - minutes < 15  → :00 に切り捨て
 * - minutes < 45  → :30 に丸め
 * - minutes >= 45 → 次の時間の :00 に切り上げ
 */
export function snapTo30Min(isoString: string, _snapMinutes: number = DEFAULT_SNAP_MINUTES): string {
  // "YYYY-MM-DDTHH:MM:SS" 形式を前提とする
  const parts = isoString.slice(0, 19);
  const datePart = parts.slice(0, 10); // YYYY-MM-DD
  let hours = parseInt(parts.slice(11, 13), 10);
  const minutes = parseInt(parts.slice(14, 16), 10);

  let snappedMinutes: string;
  if (minutes < SNAP_THRESHOLD_LOW) {
    snappedMinutes = '00';
  } else if (minutes < SNAP_THRESHOLD_HIGH) {
    snappedMinutes = '30';
  } else {
    // 次の時間の :00 に切り上げ
    hours += 1;
    snappedMinutes = '00';
  }

  if (hours >= 24) {
    // 日をまたぐ場合: Date オブジェクトで正しく日付を繰り上げる
    const [year, month, day] = datePart.split('-').map(Number);
    const nextDate = new Date(year, month - 1, day + 1);
    const y = nextDate.getFullYear();
    const m = String(nextDate.getMonth() + 1).padStart(2, '0');
    const d = String(nextDate.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}T00:00:00`;
  }

  const hh = String(hours).padStart(2, '0');
  return `${datePart}T${hh}:${snappedMinutes}:00`;
}

/**
 * ラベル配列から指定タイムスタンプに最も近いインデックスを二分探索で取得する
 */
export function findNearestLabelIndex(labels: string[], timestamp: string): number {
  if (labels.length === 0) return -1;
  if (timestamp <= labels[0]) return 0;
  if (timestamp >= labels[labels.length - 1]) return labels.length - 1;

  let low = 0;
  let high = labels.length - 1;

  while (low <= high) {
    const mid = Math.floor((low + high) / 2);
    if (labels[mid] === timestamp) return mid;
    if (labels[mid] < timestamp) {
      low = mid + 1;
    } else {
      high = mid - 1;
    }
  }

  // low と high の間に挟まれているので、近い方を返す
  if (high < 0) return 0;
  if (low >= labels.length) return labels.length - 1;

  const diffLow = Math.abs(new Date(labels[low]).getTime() - new Date(timestamp).getTime());
  const diffHigh = Math.abs(new Date(labels[high]).getTime() - new Date(timestamp).getTime());
  return diffLow <= diffHigh ? low : high;
}

/**
 * チャートドラッグ選択 hook
 * Canvas の mousedown/mousemove/mouseup/mouseleave イベントを管理し、
 * ドラッグ終了時に pixel → timestamp → スナップ変換を行って onDragSelect を呼び出す。
 * 内部状態は useRef で管理し、mousemove 中の React re-render を発生させない。
 */
export function useChartDragSelect({
  chartRef,
  onDragSelect,
  snapMinutes = DEFAULT_SNAP_MINUTES,
  enabled = true,
}: UseChartDragSelectOptions): void {
  const dragStateRef = useRef<DragState>({
    isDragging: false,
    startX: 0,
    currentX: 0,
  });
  const rafIdRef = useRef<number | null>(null);

  /** pixel X → ラベルインデックス → タイムスタンプ文字列の変換 */
  const pixelToTimestamp = useCallback((chart: ChartJS<'line'>, pixelX: number): string | null => {
    const xScale = chart.scales.x;
    if (!xScale) return null;

    const rawIndex = xScale.getValueForPixel(pixelX);
    if (rawIndex == null) return null;

    const index = Math.round(rawIndex);
    const labels = chart.data.labels as string[] | undefined;
    if (!labels || index < 0 || index >= labels.length) return null;

    return labels[index];
  }, []);

  useEffect(() => {
    if (!enabled) return;

    const chart = chartRef.current;
    if (!chart) return;

    const canvas = chart.canvas;
    if (!canvas) return;

    // ドラッグ状態をチャートインスタンスに紐付ける（プラグインからアクセス用）
    const chartWithDragState = chart as ChartJS<'line'> & { $dragState?: DragState };
    chartWithDragState.$dragState = dragStateRef.current;

    const handleMouseDown = (e: MouseEvent) => {
      const chartArea = chart.chartArea;
      if (!chartArea) return;

      const rect = canvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const y = e.clientY - rect.top;

      // プロットエリア内のみ有効
      if (x < chartArea.left || x > chartArea.right || y < chartArea.top || y > chartArea.bottom) {
        return;
      }

      dragStateRef.current.isDragging = true;
      dragStateRef.current.startX = x;
      dragStateRef.current.currentX = x;
    };

    const handleMouseMove = (e: MouseEvent) => {
      if (!dragStateRef.current.isDragging) return;

      const rect = canvas.getBoundingClientRect();
      const x = e.clientX - rect.left;
      const chartArea = chart.chartArea;

      // chartArea にクランプ
      dragStateRef.current.currentX = Math.max(
        chartArea.left,
        Math.min(x, chartArea.right)
      );

      // requestAnimationFrame でスロットル
      if (rafIdRef.current == null) {
        rafIdRef.current = requestAnimationFrame(() => {
          rafIdRef.current = null;
          chart.draw();
        });
      }
    };

    const handleMouseUp = () => {
      if (!dragStateRef.current.isDragging) return;

      const { startX, currentX } = dragStateRef.current;
      dragStateRef.current.isDragging = false;

      // 最小ドラッグ幅チェック
      if (Math.abs(currentX - startX) < MIN_DRAG_DISTANCE_PX) {
        chart.draw();
        return;
      }

      // 右→左ドラッグ対応: start/end を正規化
      const leftX = Math.min(startX, currentX);
      const rightX = Math.max(startX, currentX);

      const startTimestamp = pixelToTimestamp(chart, leftX);
      const endTimestamp = pixelToTimestamp(chart, rightX);

      if (startTimestamp && endTimestamp) {
        const snappedStart = snapTo30Min(startTimestamp, snapMinutes);
        const snappedEnd = snapTo30Min(endTimestamp, snapMinutes);

        // スナップ後も start < end であることを保証
        if (snappedStart < snappedEnd) {
          onDragSelect({ start: snappedStart, end: snappedEnd });
        }
      }

      chart.draw();
    };

    const handleMouseLeave = () => {
      if (dragStateRef.current.isDragging) {
        dragStateRef.current.isDragging = false;
        chart.draw();
      }
    };

    canvas.addEventListener('mousedown', handleMouseDown);
    canvas.addEventListener('mousemove', handleMouseMove);
    canvas.addEventListener('mouseup', handleMouseUp);
    canvas.addEventListener('mouseleave', handleMouseLeave);

    return () => {
      canvas.removeEventListener('mousedown', handleMouseDown);
      canvas.removeEventListener('mousemove', handleMouseMove);
      canvas.removeEventListener('mouseup', handleMouseUp);
      canvas.removeEventListener('mouseleave', handleMouseLeave);
      if (rafIdRef.current != null) {
        cancelAnimationFrame(rafIdRef.current);
        rafIdRef.current = null;
      }
    };
  }, [chartRef, onDragSelect, snapMinutes, enabled, pixelToTimestamp]);
}
