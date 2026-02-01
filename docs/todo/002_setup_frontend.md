# タスク002: フロントエンドプロジェクトのセットアップ

**ステータス**: 未着手  
**優先度**: 高  
**見積もり**: 1時間

---

## 概要

React + TypeScript + Vite でフロントエンドプロジェクトを作成し、基本的な構造を構築する。

## 前提条件

- Node.js 18.x 以上がインストールされていること
- npm または yarn が利用可能であること

## 作業内容

### 1. プロジェクト作成

```powershell
cd c:\workspace\010_programs\perfmonAnalyzer\src
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
```

### 2. 必要なパッケージ追加

```powershell
npm install axios chart.js react-chartjs-2
npm install -D @types/node
```

### 3. ディレクトリ構造作成

```
frontend/
├── src/
│   ├── components/
│   │   ├── FileUpload.tsx
│   │   ├── ChartView.tsx
│   │   ├── RangeSelector.tsx
│   │   ├── SlopeSummary.tsx
│   │   └── ExportButton.tsx
│   ├── services/
│   │   └── api.ts
│   ├── types/
│   │   └── index.ts
│   ├── App.tsx
│   └── main.tsx
├── package.json
└── vite.config.ts
```

### 4. API プロキシ設定

`vite.config.ts` にプロキシを設定（開発時の CORS 回避）。

## 受け入れ基準

- [ ] `npm run dev` で開発サーバーが起動する
- [ ] `http://localhost:5173` にアクセスできる
- [ ] TypeScript のコンパイルエラーがない

## 技術メモ

### Vite とは

**Vite**（ヴィート）は高速なフロントエンド開発ツールです。

- **高速起動**: ES モジュールを利用し、バンドル不要で起動
- **HMR**: Hot Module Replacement で即座に変更を反映
- **TypeScript 対応**: 設定不要で TypeScript を利用可能

### npm コマンド解説

| コマンド | 説明 |
|----------|------|
| `npm create vite@latest` | Vite プロジェクトを作成 |
| `npm install` | package.json の依存関係をインストール |
| `npm install <pkg>` | パッケージを追加（dependencies） |
| `npm install -D <pkg>` | パッケージを追加（devDependencies） |
| `npm run dev` | 開発サーバーを起動 |

### React + TypeScript の基本

```tsx
// コンポーネントの例
import { useState } from 'react';

interface Props {
  title: string;
}

export const MyComponent: React.FC<Props> = ({ title }) => {
  const [count, setCount] = useState(0);
  
  return (
    <div>
      <h1>{title}</h1>
      <button onClick={() => setCount(count + 1)}>
        Count: {count}
      </button>
    </div>
  );
};
```

---

## 完了日

（完了時に記入）
