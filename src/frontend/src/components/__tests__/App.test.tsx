import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';

// Chart.js と react-chartjs-2 をモック
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

// API モジュールをモック
vi.mock('../services/api', () => ({
  uploadCsv: vi.fn(),
  getSessionData: vi.fn(),
}));

import App from '../../App';

describe('App', () => {
  it('ヘッダーとファイルアップロードセクションを表示する', () => {
    render(<App />);
    expect(screen.getByText('Perfmon Analyzer')).toBeInTheDocument();
    expect(screen.getByText('CSV ファイルアップロード')).toBeInTheDocument();
  });

  it('初期状態ではグラフセクションが表示されない', () => {
    render(<App />);
    expect(screen.queryByTestId('mock-chart')).not.toBeInTheDocument();
  });
});
