import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ChartView } from '../ChartView';
import type { CounterInfo } from '../../types';

// Chart.js と react-chartjs-2 をモック（jsdom には canvas がないため）
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

/** テスト用のカウンターデータを生成 */
function createMockCounter(name: string, dataCount: number): CounterInfo {
  return {
    machineName: 'TestMachine',
    category: 'TestCategory',
    instanceName: 'TestInstance',
    counterName: name,
    displayName: `\\TestMachine\\TestCategory(TestInstance)\\${name}`,
    dataPoints: Array.from({ length: dataCount }, (_, i) => ({
      timestamp: `2026-02-01T10:00:${String(i).padStart(2, '0')}`,
      value: Math.random() * 100,
    })),
  };
}

describe('ChartView', () => {
  it('カウンターが空の場合、データなしメッセージを表示する', () => {
    render(<ChartView counters={[]} />);
    expect(screen.getByText('データがありません。CSV ファイルをアップロードしてください。')).toBeInTheDocument();
  });

  it('カウンターデータがある場合、チャートを表示する', () => {
    const counters = [createMockCounter('Counter1', 5)];
    render(<ChartView counters={counters} />);
    expect(screen.getByTestId('mock-chart')).toBeInTheDocument();
    expect(screen.queryByText('データがありません。CSV ファイルをアップロードしてください。')).not.toBeInTheDocument();
  });

  it('複数カウンターでもチャートを表示する', () => {
    const counters = [
      createMockCounter('Counter1', 5),
      createMockCounter('Counter2', 5),
      createMockCounter('Counter3', 5),
    ];
    render(<ChartView counters={counters} />);
    expect(screen.getByTestId('mock-chart')).toBeInTheDocument();
  });
});
