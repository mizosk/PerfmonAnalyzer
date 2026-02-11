import { forwardRef, useMemo, type ForwardedRef } from 'react';
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
} from 'chart.js';
import { Line } from 'react-chartjs-2';
import type { ChartData, ChartOptions } from 'chart.js';
import type { CounterInfo } from '../types';

// Chart.js コンポーネントを登録
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend
);

/** カウンターごとの色パレット */
const COLOR_PALETTE = [
  { border: 'rgb(75, 192, 192)', background: 'rgba(75, 192, 192, 0.5)' },
  { border: 'rgb(255, 99, 132)', background: 'rgba(255, 99, 132, 0.5)' },
  { border: 'rgb(54, 162, 235)', background: 'rgba(54, 162, 235, 0.5)' },
  { border: 'rgb(255, 205, 86)', background: 'rgba(255, 205, 86, 0.5)' },
  { border: 'rgb(153, 102, 255)', background: 'rgba(153, 102, 255, 0.5)' },
  { border: 'rgb(255, 159, 64)', background: 'rgba(255, 159, 64, 0.5)' },
  { border: 'rgb(201, 203, 207)', background: 'rgba(201, 203, 207, 0.5)' },
  { border: 'rgb(0, 204, 102)', background: 'rgba(0, 204, 102, 0.5)' },
  { border: 'rgb(255, 0, 255)', background: 'rgba(255, 0, 255, 0.5)' },
  { border: 'rgb(0, 128, 255)', background: 'rgba(0, 128, 255, 0.5)' },
];

/**
 * グラフ表示コンポーネントの props
 */
interface ChartViewProps {
  counters: CounterInfo[];
}

/** グラフ表示オプション（再レンダリング時に再生成されないようコンポーネント外で定義） */
const CHART_OPTIONS: ChartOptions<'line'> = {
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    mode: 'index',
    intersect: false,
  },
  plugins: {
    legend: {
      position: 'top',
      labels: {
        usePointStyle: true,
      },
    },
    tooltip: {
      mode: 'index',
      intersect: false,
    },
  },
  scales: {
    x: {
      ticks: {
        maxTicksLimit: 20,
        maxRotation: 45,
      },
    },
  },
};

/**
 * グラフ表示コンポーネント
 * Chart.js を使って複数カウンターデータを折れ線グラフで表示する
 * ref 転送により、外部から Chart インスタンスを参照可能（PNG出力用）
 */
export const ChartView = forwardRef<ChartJS<'line'>, ChartViewProps>(
  function ChartView({ counters }, ref) {
    // chartData をメモ化して不要な再計算を防止
    const chartData = useMemo<ChartData<'line'> | null>(() => {
      if (!counters || counters.length === 0) return null;

      // 全カウンターのタイムスタンプを統合してラベルを作成
      const allTimestamps = new Set<string>();
      for (const counter of counters) {
        for (const dp of counter.dataPoints) {
          allTimestamps.add(dp.timestamp);
        }
      }
      const labels = Array.from(allTimestamps).sort();

      // 各カウンターをデータセットに変換
      const datasets = counters.map((counter, index) => {
        const color = COLOR_PALETTE[index % COLOR_PALETTE.length];

        // タイムスタンプをキーにしたマップを作成してラベルに合わせる
        const valueMap = new Map<string, number>();
        for (const dp of counter.dataPoints) {
          valueMap.set(dp.timestamp, dp.value);
        }

        return {
          label: counter.displayName,
          data: labels.map((ts) => valueMap.get(ts) ?? null),
          borderColor: color.border,
          backgroundColor: color.background,
          tension: 0.1,
          spanGaps: true,
        };
      });

      return { labels, datasets };
    }, [counters]);

    // データがない場合のメッセージ表示
    if (!chartData) {
      return (
        <div className="chart-view chart-view--empty">
          <p>データがありません。CSV ファイルをアップロードしてください。</p>
        </div>
      );
    }

    return (
      <div className="chart-view">
        <Line ref={ref as ForwardedRef<ChartJS<'line'> | undefined>} data={chartData} options={CHART_OPTIONS} />
      </div>
    );
  }
);

ChartView.displayName = 'ChartView';
