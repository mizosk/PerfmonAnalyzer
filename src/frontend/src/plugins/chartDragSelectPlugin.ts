import type { Plugin, Chart } from 'chart.js';

/** ドラッグ中の状態を保持するための拡張プロパティ */
export interface DragState {
  isDragging: boolean;
  startX: number;
  currentX: number;
}

/** プラグインオプション */
export interface DragSelectPluginOptions {
  /** 確定済みの選択範囲（ラベルインデックスベース） */
  selectedStartIndex?: number;
  /** 確定済みの選択範囲（ラベルインデックスベース） */
  selectedEndIndex?: number;
}

/** ドラッグ中オーバーレイの色 */
const DRAG_OVERLAY_COLOR = 'rgba(54, 162, 235, 0.2)';
/** 確定後の遮蔽色 */
const CONFIRMED_MASK_COLOR = 'rgba(0, 0, 0, 0.1)';

/**
 * Chart.js カスタムプラグイン
 * - ドラッグ中: 青色半透明の矩形をドラッグ領域に表示
 * - 確定後: 選択範囲外をグレー半透明で遮蔽し、選択範囲を強調
 */
export const dragSelectPlugin: Plugin<'line'> = {
  id: 'dragSelect',

  afterDraw(chart: Chart<'line'>) {
    const dragState = (chart as Chart<'line'> & { $dragState?: DragState }).$dragState;
    const ctx = chart.ctx;
    const chartArea = chart.chartArea;

    if (!chartArea) return;

    // 1. ドラッグ中のオーバーレイ描画
    if (dragState?.isDragging) {
      const left = Math.max(Math.min(dragState.startX, dragState.currentX), chartArea.left);
      const right = Math.min(Math.max(dragState.startX, dragState.currentX), chartArea.right);
      const top = chartArea.top;
      const height = chartArea.bottom - chartArea.top;

      ctx.save();
      ctx.fillStyle = DRAG_OVERLAY_COLOR;
      ctx.fillRect(left, top, right - left, height);
      ctx.restore();
      return;
    }

    // 2. 確定済み選択範囲のオーバーレイ描画
    const pluginOpts = chart.options.plugins?.dragSelect as DragSelectPluginOptions | undefined;
    if (pluginOpts?.selectedStartIndex != null && pluginOpts?.selectedEndIndex != null) {
      const xScale = chart.scales.x;
      if (!xScale) return;

      const startPixel = xScale.getPixelForValue(pluginOpts.selectedStartIndex);
      const endPixel = xScale.getPixelForValue(pluginOpts.selectedEndIndex);
      const left = Math.min(startPixel, endPixel);
      const right = Math.max(startPixel, endPixel);
      const top = chartArea.top;
      const height = chartArea.bottom - chartArea.top;

      ctx.save();
      ctx.fillStyle = CONFIRMED_MASK_COLOR;
      // 左側の遮蔽
      if (left > chartArea.left) {
        ctx.fillRect(chartArea.left, top, left - chartArea.left, height);
      }
      // 右側の遮蔽
      if (right < chartArea.right) {
        ctx.fillRect(right, top, chartArea.right - right, height);
      }
      ctx.restore();
    }
  },
};

// Chart.js のプラグインオプション型を拡張
declare module 'chart.js' {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface PluginOptionsByType<TType extends import('chart.js').ChartType> {
    dragSelect?: DragSelectPluginOptions;
  }
}
