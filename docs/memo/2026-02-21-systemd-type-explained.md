# systemd Type設定の解説

作成日: 2026年2月21日

## 問題の概要

Linuxでサービスを起動した際に以下のエラーが出るが、実際にはアプリは動作している：

```
Job for perfmonanalyzer.service failed because a timeout was exceeded.
```

## 原因

systemdサービスファイルの `Type=notify` 設定が原因です。

### systemdのTypeオプション

systemdサービスファイルの `[Service]` セクションにある `Type` は、systemdがサービスの「起動完了」をどう判断するかを指定します。

#### Type=simple（推奨）

```ini
[Service]
Type=simple
ExecStart=/path/to/app
```

**動作**:
- プロセスが起動した時点で「起動完了」とみなす
- 最もシンプルで、ほとんどのアプリに適している

**メリット**:
- ✅ 設定が簡単
- ✅ タイムアウトエラーが出ない
- ✅ 追加のコード不要

**デメリット**:
- ⚠️ アプリが実際に準備完了かは確認しない
- ⚠️ プロセスが起動してもエラーで止まる可能性

#### Type=notify

```ini
[Service]
Type=notify
ExecStart=/path/to/app
```

**動作**:
- アプリケーションが `sd_notify()` システムコールでsystemdに通知を送るのを待つ
- 通知を受け取った時点で「起動完了」とみなす
- デフォルト90秒以内に通知が来ないとタイムアウト

**メリット**:
- ✅ アプリが本当に準備完了してから「起動完了」になる
- ✅ 依存サービスが正しく待機できる

**デメリット**:
- ⚠️ アプリ側でsystemd通知機能を実装する必要がある
- ⚠️ 実装しないとタイムアウトエラーが出る

#### Type=forking

```ini
[Service]
Type=forking
PIDFile=/var/run/app.pid
ExecStart=/path/to/app
```

**動作**:
- メインプロセスがフォーク（子プロセスを作成）して終了するタイプ
- 古いデーモンプログラムで使われる

**ASP.NET Coreでは使用しません**。

---

## ASP.NET Core と systemd

### デフォルトの動作

ASP.NET Coreアプリは、そのままではsystemdに通知を送りません。

```
起動 → Kestrel起動 → リクエスト待機
                      ↑ この時点で準備完了だが、systemdに通知しない
```

systemdは通知を待ち続け、90秒後にタイムアウト。でもアプリは動き続ける。

### systemd通知を有効化する方法

`Microsoft.Extensions.Hosting.Systemd` パッケージを使用します。

#### 1. パッケージをインストール

```bash
dotnet add package Microsoft.Extensions.Hosting.Systemd
```

#### 2. Program.cs に追加

```csharp
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Linux上で動作している場合、systemd統合を有効化
builder.Host.UseSystemd();

// ... 以下既存のコード
builder.Services.AddControllers();
// etc.

var app = builder.Build();
app.Run();
```

#### 3. 動作

```
起動 → Kestrel起動 → リクエスト待機 → sd_notify() 実行
                                     ↓
                                systemdに通知
                                     ↓
                              「起動完了」
```

systemdはすぐに「起動完了」を認識し、タイムアウトエラーが出なくなります。

---

## 推奨される設定

### 小規模・個人プロジェクト

**`Type=simple` を使用**（簡単）

- 設定がシンプル
- コード変更不要
- ほとんどの場合これで十分

```ini
[Service]
Type=simple
ExecStart=/opt/perfmonanalyzer/PerfmonAnalyzer.Api
```

### 本番環境・複雑な依存関係

**`Type=notify` + systemd統合を使用**（厳密）

- 依存サービスとの連携が正確
- モニタリングツールとの統合が良好
- エンタープライズ向き

```ini
[Service]
Type=notify
ExecStart=/opt/perfmonanalyzer/PerfmonAnalyzer.Api
```

+ 

```csharp
builder.Host.UseSystemd();
```

---

## Use実装の内部動作

`UseSystemd()` は以下のことを行います：

1. **環境チェック**: systemd環境で動作しているか確認
   ```csharp
   if (IsSystemdService())  // 環境変数等をチェック
   ```

2. **ライフタイムイベントをフック**: アプリの起動・停止時にsystemdに通知
   ```csharp
   OnStarted: sd_notify("READY=1")      // 起動完了
   OnStopping: sd_notify("STOPPING=1")  // 停止開始
   ```

3. **ログ設定**: systemdの`journal`に適切に出力

4. **ウォッチドッグ統合**: systemdのヘルスチェック機能に対応（オプション）

---

## デバッグ方法

### タイムアウト時間を確認

```bash
# サービスファイルを確認
sudo systemctl cat perfmonanalyzer

# デフォルトは90秒
# TimeoutStartSec が設定されていなければ90秒
```

### タイムアウト時間を延長（一時対応）

```ini
[Service]
Type=notify
TimeoutStartSec=180  # 180秒に延長
ExecStart=/opt/perfmonanalyzer/PerfmonAnalyzer.Api
```

### 通知が送られているか確認

```bash
# ログで確認
sudo journalctl -u perfmonanalyzer -n 100

# systemd統合が有効なら以下のようなログが出る:
# "Hosting starting"
# "Hosting started"
```

### アプリが本当に動いているか確認

```bash
# プロセス確認
ps aux | grep PerfmonAnalyzer

# ポート確認
sudo netstat -tlnp | grep 5000

# ヘルスチェック
curl http://localhost:5000/api/health
```

---

## まとめ

| 項目 | Type=simple | Type=notify (通知なし) | Type=notify + UseSystemd() |
|---|---|---|---|
| **設定の複雑さ** | 簡単 | 簡単 | 中程度 |
| **コード変更** | 不要 | 不要 | 必要 |
| **タイムアウトエラー** | なし | **あり** | なし |
| **実際の動作** | 正常 | 正常 | 正常 |
| **起動完了判定** | プロセス起動時 | タイムアウト後 | 通知受信時 |
| **適用場面** | 一般的な用途 | ❌ 非推奨 | 本番環境 |

**結論**: 
- 簡単に済ませたい → `Type=simple`
- 厳密にやりたい → `Type=notify` + `UseSystemd()`
- **絶対にやってはいけない** → `Type=notify` のみ（今回の状況）

---

## 参考リンク

- [systemd.service - Type=](https://www.freedesktop.org/software/systemd/man/systemd.service.html#Type=)
- [Microsoft.Extensions.Hosting.Systemd](https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Systemd/)
- [ASP.NET Core systemd integration](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx#systemd-integration)
