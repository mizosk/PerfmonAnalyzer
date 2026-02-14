# Perfmon Analyzer デプロイガイド

## クイックスタート

### 1. デプロイ準備（開発PCで実行）

```powershell
# プロジェクトのルートフォルダでスクリプトを実行
.\deploy.ps1
```

このコマンドで以下が自動的に実行されます：
- フロントエンドのビルド
- 静的ファイルのコピー
- バックエンドの発行

結果として `publish` フォルダが作成されます。

### 2. サーバーへの配置

`publish` フォルダをサーバーPCにコピーします。

**方法1**: USBメモリ等でコピー
**方法2**: ネットワーク共有フォルダ経由
**方法3**: リモートデスクトップでコピー&ペースト

### 3. サーバーで実行

```powershell
# publish フォルダに移動
cd C:\path\to\publish

# アプリを起動
dotnet PerfmonAnalyzer.Api.dll
```

### 4. アクセス確認

ブラウザで以下にアクセス：
- サーバー自身から: `http://localhost:5000`
- 他のPCから: `http://192.168.1.100:5000` （サーバーのIPアドレス）

---

## オプション設定

### ポート番号を変更する

`appsettings.Production.json` を編集：

```json
{
  "Urls": "http://0.0.0.0:8080"
}
```

### ファイアウォール設定

他のPCからアクセスできない場合は、ファイアウォールでポートを開放：

```powershell
# 管理者権限のPowerShellで実行
New-NetFirewallRule -DisplayName "PerfmonAnalyzer" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

### Windowsサービスとして登録（自動起動）

PC起動時に自動で起動するようにするには：

```powershell
# publish フォルダに移動
cd C:\path\to\publish

# サービス登録
sc.exe create PerfmonAnalyzer binPath="C:\path\to\publish\PerfmonAnalyzer.Api.exe" start=auto

# サービス開始
sc.exe start PerfmonAnalyzer
```

サービスの停止：
```powershell
sc.exe stop PerfmonAnalyzer
```

サービスの削除：
```powershell
sc.exe delete PerfmonAnalyzer
```

---

## トラブルシューティング

### 他のPCからアクセスできない

1. **ファイアウォールを確認**
   ```powershell
   Get-NetFirewallRule -DisplayName "PerfmonAnalyzer"
   ```

2. **サーバーのIPアドレスを確認**
   ```powershell
   ipconfig
   ```
   `IPv4 Address` の値を使用

3. **appsettings.Production.json の Urls を確認**
   `0.0.0.0` になっているか確認

### ポートが既に使用されている

エラーメッセージ: `Address already in use`

別のプログラムが同じポートを使用している場合は、ポート番号を変更：
```json
{
  "Urls": "http://0.0.0.0:5001"
}
```

### 404 エラーが出る

React のルート（例: `/about`）で 404 エラーが出る場合：

→ `Program.cs` に `app.MapFallbackToFile("index.html");` があるか確認

### アップロードしたファイルが消える

アプリを再起動するとデータが消える仕様です（現在はメモリ内保存）。

データを永続化したい場合は、データベースや外部ストレージの実装が必要です。

---

## アップデート手順

新しいバージョンをデプロイする場合：

1. **サーバーでアプリを停止**
   - Ctrl+C で停止（コンソール実行の場合）
   - `sc.exe stop PerfmonAnalyzer`（サービスの場合）

2. **開発PCでビルド**
   ```powershell
   .\deploy.ps1
   ```

3. **publish フォルダをサーバーにコピー**
   既存のフォルダを上書き

4. **サーバーでアプリを再起動**
   ```powershell
   dotnet PerfmonAnalyzer.Api.dll
   ```

---

## デプロイスクリプトのオプション

### 出力先を変更

```powershell
.\deploy.ps1 -OutputPath "C:\MyApp"
```

### フロントエンドのビルドをスキップ

```powershell
.\deploy.ps1 -SkipFrontend
```

### バックエンドの発行をスキップ

```powershell
.\deploy.ps1 -SkipBackend
```

---

## セキュリティに関する注意

このアプリは **ローカルネットワーク内での使用** を想定しています。

インターネットに公開する場合は、以下の対策が必要です：
- HTTPS化（SSL証明書の取得）
- 認証・認可の実装
- ファイルアップロードの厳格なチェック
- セキュリティヘッダーの追加
- レート制限の実装

**推奨**: インターネット公開は避け、ローカルネットワーク内のみで使用してください。

---

## 参考情報

- サーバーのログを見る: コンソール出力を確認
- 設定ファイル: `appsettings.Production.json`
- 静的ファイル: `wwwroot` フォルダ内
