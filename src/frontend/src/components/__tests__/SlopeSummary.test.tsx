import { render, screen, within, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { SlopeSummary } from '../SlopeSummary';
import type { SlopeResult } from '../../types';

/** テスト用のSlopeResultデータを生成 */
function createMockResult(
  counterName: string,
  slopeKBPer10Min: number,
  rSquared: number,
): SlopeResult {
  return {
    counterName,
    slopeKBPer10Min,
    rSquared,
    isWarning: slopeKBPer10Min > 50, // バックエンド側の判定（テスト内では使わない）
  };
}

describe('SlopeSummary', () => {
  const defaultThreshold = 50;

  describe('テーブル表示', () => {
    it('結果が空の場合、データなしメッセージを表示する', () => {
      render(<SlopeSummary results={[]} threshold={defaultThreshold} />);
      expect(screen.getByText('解析結果がありません。')).toBeInTheDocument();
    });

    it('テーブルのカラムヘッダーが正しく表示される', () => {
      const results = [createMockResult('Counter1', 60, 0.95)];
      render(<SlopeSummary results={results} threshold={defaultThreshold} />);

      expect(screen.getByText('カウンタ名')).toBeInTheDocument();
      expect(screen.getByText('傾き (KB/10min)')).toBeInTheDocument();
      expect(screen.getByText('R²')).toBeInTheDocument();
      expect(screen.getByText('判定')).toBeInTheDocument();
    });

    it('結果データがテーブルに正しく表示される', () => {
      const results = [createMockResult('\\Machine\\Memory\\Available MBytes', 75.123, 0.9567)];
      render(<SlopeSummary results={results} threshold={defaultThreshold} />);

      expect(screen.getByText('\\Machine\\Memory\\Available MBytes')).toBeInTheDocument();
      expect(screen.getByText('75.12')).toBeInTheDocument();
      expect(screen.getByText('0.957')).toBeInTheDocument();
    });
  });

  describe('ソート', () => {
    it('傾き降順でソートされて表示される', () => {
      const results = [
        createMockResult('Counter_Low', 10, 0.8),
        createMockResult('Counter_High', 100, 0.95),
        createMockResult('Counter_Mid', 50, 0.9),
      ];
      render(<SlopeSummary results={results} threshold={defaultThreshold} />);

      const rows = screen.getAllByRole('row');
      // rows[0] はヘッダー行
      const dataRows = rows.slice(1);
      expect(dataRows).toHaveLength(3);

      // 降順: Counter_High(100) > Counter_Mid(50) > Counter_Low(10)
      expect(within(dataRows[0]).getByText('Counter_High')).toBeInTheDocument();
      expect(within(dataRows[1]).getByText('Counter_Mid')).toBeInTheDocument();
      expect(within(dataRows[2]).getByText('Counter_Low')).toBeInTheDocument();
    });
  });

  describe('閾値判定', () => {
    it('閾値超過時に「⚠️ 警告」が表示される', () => {
      const results = [createMockResult('Counter1', 60, 0.95)];
      render(<SlopeSummary results={results} threshold={50} />);

      expect(screen.getByText('⚠️ 警告')).toBeInTheDocument();
    });

    it('閾値以下の場合は「✓ OK」が表示される', () => {
      const results = [createMockResult('Counter1', 30, 0.85)];
      render(<SlopeSummary results={results} threshold={50} />);

      expect(screen.getByText('✓ OK')).toBeInTheDocument();
    });

    it('閾値ちょうどの場合は「✓ OK」が表示される', () => {
      const results = [createMockResult('Counter1', 50, 0.9)];
      render(<SlopeSummary results={results} threshold={50} />);

      expect(screen.getByText('✓ OK')).toBeInTheDocument();
    });

    it('警告行にwarningクラスが適用される', () => {
      const results = [createMockResult('Counter_Warn', 60, 0.95)];
      render(<SlopeSummary results={results} threshold={50} />);

      const rows = screen.getAllByRole('row');
      const dataRow = rows[1]; // ヘッダー行の次
      expect(dataRow).toHaveClass('warning');
    });

    it('OK行にokクラスが適用される', () => {
      const results = [createMockResult('Counter_OK', 30, 0.85)];
      render(<SlopeSummary results={results} threshold={50} />);

      const rows = screen.getAllByRole('row');
      const dataRow = rows[1];
      expect(dataRow).toHaveClass('ok');
    });

    it('異なる閾値で正しく判定が変わる', () => {
      const results = [
        createMockResult('Counter1', 60, 0.95),
        createMockResult('Counter2', 30, 0.85),
      ];

      // 閾値 25 → 両方とも警告
      const { unmount } = render(<SlopeSummary results={results} threshold={25} />);
      expect(screen.getAllByText('⚠️ 警告')).toHaveLength(2);
      unmount();

      // 閾値 100 → 両方とも OK
      render(<SlopeSummary results={results} threshold={100} />);
      expect(screen.getAllByText('✓ OK')).toHaveLength(2);
    });
  });

  describe('閾値変更コールバック', () => {
    it('onThresholdChange が呼ばれるとき、正しい値が渡される', () => {
      const onThresholdChange = vi.fn();
      const results = [createMockResult('Counter1', 60, 0.95)];

      render(
        <SlopeSummary
          results={results}
          threshold={50}
          onThresholdChange={onThresholdChange}
        />
      );

      const input = screen.getByLabelText('閾値 (KB/10min)');
      expect(input).toBeInTheDocument();
      expect(input).toHaveValue(50);

      fireEvent.change(input, { target: { value: '75' } });
      expect(onThresholdChange).toHaveBeenCalledWith(75);
    });

    it('onThresholdChange が未指定の場合、閾値入力は表示されない', () => {
      const results = [createMockResult('Counter1', 60, 0.95)];
      render(<SlopeSummary results={results} threshold={50} />);

      expect(screen.queryByLabelText('閾値 (KB/10min)')).not.toBeInTheDocument();
    });
  });

  describe('元データが変更されないこと', () => {
    it('ソート後も props の results は変更されない', () => {
      const results = [
        createMockResult('Counter_Low', 10, 0.8),
        createMockResult('Counter_High', 100, 0.95),
      ];
      const originalOrder = [...results];

      render(<SlopeSummary results={results} threshold={50} />);

      // props の配列は変更されていないこと
      expect(results[0].counterName).toBe(originalOrder[0].counterName);
      expect(results[1].counterName).toBe(originalOrder[1].counterName);
    });
  });
});
