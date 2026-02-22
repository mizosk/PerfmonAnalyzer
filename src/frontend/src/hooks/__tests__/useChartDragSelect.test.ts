import { describe, it, expect } from 'vitest';
import { snapTo30Min, findNearestLabelIndex } from '../../hooks/useChartDragSelect';

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
