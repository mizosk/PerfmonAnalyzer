# タスク006: 傾きサマリ表示機能の実装

**ステータス**: 未着手  
**優先度**: 中  
**見積もり**: 2時間

---

## 概要

各カウンタの傾き計算結果をテーブル形式で表示し、閾値超過時に警告を表示する機能を実装する。

## 前提条件

- タスク004（傾き分析機能）が完了していること
- タスク005（グラフ表示機能）が完了していること

## 作業内容

### 1. SlopeSummary コンポーネント実装

`components/SlopeSummary.tsx`:

```typescript
interface Props {
  results: SlopeResult[];
  threshold: number;
}

export const SlopeSummary: React.FC<Props> = ({ results, threshold }) => {
  // 傾き降順でソート
  const sorted = [...results].sort((a, b) => b.slopeKBPer10Min - a.slopeKBPer10Min);
  
  return (
    <table>
      <thead>
        <tr>
          <th>カウンタ名</th>
          <th>傾き (KB/10min)</th>
          <th>R²</th>
          <th>判定</th>
        </tr>
      </thead>
      <tbody>
        {sorted.map(result => (
          <tr key={result.counterName} className={result.isWarning ? 'warning' : ''}>
            <td>{result.counterName}</td>
            <td>{result.slopeKBPer10Min.toFixed(2)}</td>
            <td>{result.rSquared.toFixed(3)}</td>
            <td>{result.isWarning ? '⚠️ 警告' : '✓ OK'}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};
```

### 2. 閾値設定 UI 実装

閾値を変更できる入力フィールドを追加。

```typescript
const [threshold, setThreshold] = useState(50);

<input
  type="number"
  value={threshold}
  onChange={(e) => setThreshold(Number(e.target.value))}
/>
```

### 3. スタイリング

警告行の背景色を赤系に、OK 行は緑系にするなど、視覚的にわかりやすくする。

```css
.warning {
  background-color: #ffebee;
  color: #c62828;
}

.ok {
  background-color: #e8f5e9;
  color: #2e7d32;
}
```

### 4. App.tsx への統合

グラフと傾きサマリを並べて表示するレイアウトを実装。

## 受け入れ基準

- [ ] 傾き一覧がテーブル形式で表示される
- [ ] 傾き降順でソートされている
- [ ] 閾値超過時に警告色・アイコンで表示される
- [ ] 閾値を UI から変更できる
- [ ] 閾値変更後、即座に警告表示が更新される

## 技術メモ

### React での条件付きスタイル

```typescript
// className で切り替え
<tr className={result.isWarning ? 'warning' : 'ok'}>

// style で直接指定
<tr style={{ backgroundColor: result.isWarning ? '#ffebee' : '#e8f5e9' }}>
```

### テーブルのソート

```typescript
// 降順ソート
const sorted = [...results].sort((a, b) => b.slopeKBPer10Min - a.slopeKBPer10Min);

// 昇順ソート
const sorted = [...results].sort((a, b) => a.slopeKBPer10Min - b.slopeKBPer10Min);
```

**注意**: `sort()` は元の配列を変更するため、`[...results]` でコピーを作成してからソートする。

### 数値のフォーマット

```typescript
// 小数点以下2桁
result.slopeKBPer10Min.toFixed(2)  // "75.30"

// 小数点以下3桁
result.rSquared.toFixed(3)  // "0.920"
```

---

## 完了日

2026-02-11
