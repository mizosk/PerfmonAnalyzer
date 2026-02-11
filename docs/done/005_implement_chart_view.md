# タスク005: グラフ表示機能の実装

**ステータス**: 完了  
**優先度**: 高  
**見積もり**: 3時間

---

## 概要

React + Chart.js を使って時系列グラフを表示し、範囲選択・画像エクスポート機能を実装する。

## 前提条件

- タスク002（フロントエンドセットアップ）が完了していること
- タスク003（CSV インポータ）が完了していること

## 作業内容

### 1. 型定義作成

`types/index.ts`:

```typescript
export interface DataPoint {
  timestamp: string;
  value: number;
}

export interface CounterInfo {
  name: string;
  processName: string;
  counterType: string;
  data: DataPoint[];
}

export interface SlopeResult {
  counterName: string;
  slopeKBPer10Min: number;
  isWarning: boolean;
  rSquared: number;
}
```

### 2. API クライアント作成

`services/api.ts`:

```typescript
import axios from 'axios';

const api = axios.create({
  baseURL: '/api',
});

export const uploadFile = (file: File) => {
  const formData = new FormData();
  formData.append('file', file);
  return api.post('/file/upload', formData);
};

export const getData = (sessionId: string, startTime?: string, endTime?: string) => {
  return api.get(`/data/${sessionId}`, { params: { startTime, endTime } });
};

export const calculateSlope = (request: SlopeRequest) => {
  return api.post('/analysis/slope', request);
};
```

### 3. ChartView コンポーネント実装

`components/ChartView.tsx`:

```typescript
import { Line } from 'react-chartjs-2';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

export const ChartView: React.FC<Props> = ({ counters }) => {
  const chartRef = useRef<Chart>(null);
  
  const data = {
    labels: counters[0]?.data.map(d => d.timestamp),
    datasets: counters.map((counter, i) => ({
      label: counter.name,
      data: counter.data.map(d => d.value),
      borderColor: colors[i % colors.length],
      fill: false,
    })),
  };
  
  return <Line ref={chartRef} data={data} options={options} />;
};
```

### 4. RangeSelector コンポーネント実装

時間範囲を選択する UI（日時入力フィールド）。

### 5. ExportButton コンポーネント実装

```typescript
const handleExport = () => {
  const chart = chartRef.current;
  if (chart) {
    const url = chart.toBase64Image();
    const link = document.createElement('a');
    link.download = 'chart.png';
    link.href = url;
    link.click();
  }
};
```

### 6. スタイリング

基本的な CSS で見やすいレイアウトに調整。

## 受け入れ基準

- [x] アップロードした CSV のデータがグラフ表示される
- [x] 複数カウンタが凡例付きで表示される
- [x] カウンタごとに表示/非表示を切り替えられる
- [x] 時間範囲を指定してグラフを絞り込める
- [x] グラフを PNG 画像としてダウンロードできる
- [x] マウスオーバーで値が表示される

## 技術メモ

### Chart.js + React の基本

**react-chartjs-2** は Chart.js を React で使うためのラッパーです。

```typescript
import { Line } from 'react-chartjs-2';

// Chart.js v4 では registerables の登録が必要
import { Chart, registerables } from 'chart.js';
Chart.register(...registerables);
```

### データ構造

```typescript
const data = {
  labels: ['10:00', '10:01', '10:02'],  // X軸ラベル
  datasets: [
    {
      label: 'Process A',               // 凡例名
      data: [100, 150, 200],            // Y軸データ
      borderColor: 'rgb(75, 192, 192)', // 線の色
      fill: false,                      // 塗りつぶしなし
    },
  ],
};
```

### オプション設定

```typescript
const options = {
  responsive: true,              // レスポンシブ対応
  maintainAspectRatio: false,    // アスペクト比固定解除
  plugins: {
    legend: { position: 'top' }, // 凡例位置
    tooltip: { mode: 'index' },  // ツールチップ設定
  },
  scales: {
    x: { title: { display: true, text: '時刻' } },
    y: { title: { display: true, text: '値' } },
  },
};
```

### 画像出力

```typescript
// Chart インスタンスから Base64 画像を取得
const base64Image = chart.toBase64Image();

// ダウンロードリンクを生成
const link = document.createElement('a');
link.href = base64Image;
link.download = 'chart.png';
link.click();
```

---

## 完了日

2026-02-11
