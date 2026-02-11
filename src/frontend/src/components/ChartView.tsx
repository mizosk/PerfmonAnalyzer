import type React from 'react';
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

/**
 * グラフ表示コンポーネント
 * Chart.js を使ってカウンターデータを折れ線グラフで表示する
 */

interface ChartViewProps {
  data?: CounterInfo;
}

export const ChartView: React.FC<ChartViewProps> = ({ data }) => {
  if (!data || data.dataPoints.length === 0) {
    return (
      <div className="chart-view">
        <h2>グラフ表示</h2>
        <p>データがありません。CSV ファイルをアップロードしてください。</p>
      </div>
    );
  }

  const chartData = {
    labels: data.dataPoints.map((dp) => dp.timestamp),
    datasets: [
      {
        label: data.displayName,
        data: data.dataPoints.map((dp) => dp.value),
        borderColor: 'rgb(75, 192, 192)',
        backgroundColor: 'rgba(75, 192, 192, 0.5)',
        tension: 0.1,
      },
    ],
  };

  const options = {
    responsive: true,
    plugins: {
      legend: { position: 'top' as const },
      title: {
        display: true,
        text: data.displayName,
      },
    },
  };

  return (
    <div className="chart-view">
      <h2>グラフ表示</h2>
      <Line data={chartData} options={options} />
    </div>
  );
};
