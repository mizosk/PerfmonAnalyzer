# Linux (Ubuntu) でのデプロイ完全ガイド

作成日: 2026年2月15日

## 目次
1. [全体像](#全体像)
2. [前提条件](#前提条件)
3. [セットアップ手順](#セットアップ手順)
4. [systemdでサービス化](#systemdでサービス化)
5. [Nginxの設定（オプション）](#nginxの設定オプション)
6. [用語集](#用語集)

---

## 全体像

### Windows vs Linux の違い

| 項目 | Windows | Linux (Ubuntu) |
|---|---|---|
| **OS** | Windows 10/11/Server | Ubuntu 20.04/22.04/24.04 |
| **ランタイム** | .NET Runtime (Windows版) | .NET Runtime (Linux版) |
| **コマンド** | PowerShell | Bash |
| **サービス管理** | Windows Service / sc.exe | systemd |
| **リバースプロキシ** | IIS（オプション） | Nginx / Apache（推奨） |
| **ファイアウォール** | Windows Defender | ufw / iptables |
| **パッケージ管理** | なし（手動インストール） | apt / snap |

### デプロイの流れ

```
┌──────────────────────────────────────────────┐
│ 開発PC (Windows)                              │
│ ┌──────────────────────────────────────────┐ │
│ │ 1. フロントエンドをビルド                  │ │
│ │    npm run build → dist/                 │ │
│ └──────────────────────────────────────────┘ │
│ ┌──────────────────────────────────────────┐ │
│ │ 2. バックエンドを発行                      │ │
│ │    dotnet publish -r linux-x64           │ │
│ │    → publish/ (Linux用バイナリ)           │ │
│ └──────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
                    ↓ SCP / SFTP / Git
┌──────────────────────────────────────────────┐
│ サーバー (Ubuntu Linux)                        │
│ ┌──────────────────────────────────────────┐ │
│ │ 3. ファイルを配置                          │ │
│ │    /opt/perfmonanalyzer/                 │ │
│ └──────────────────────────────────────────┘ │
│ ┌──────────────────────────────────────────┐ │
│ │ 4. systemd でサービス登録                  │ │
│ │    → 自動起動、バックグラウンド実行        │ │
│ └──────────────────────────────────────────┘ │
│ ┌──────────────────────────────────────────┐ │
│ │ 5. Nginx でリバースプロキシ（オプション）   │ │
│ │    → ポート80で公開、HTTPS対応             │ │
│ └──────────────────────────────────────────┘ │
└──────────────────────────────────────────────┘
```

---

## 前提条件

### サーバー側（Ubuntu）に必要なもの

1. **Ubuntu 20.04 以降**
   - デスクトップ版でもサーバー版でもOK
   - 仮想環境（VirtualBox、VMware、Hyper-V等）でもOK

2. **固定IPアドレス**（ローカルネットワーク内）
   - 例: `192.168.1.200`
   - 動的IPだと毎回アドレスが変わって不便

3. **SSHアクセス**（リモート管理する場合）
   - Windows → Ubuntu にリモート接続できると便利

### 開発PC側（Windows）に必要なもの

- .NET SDK（既にインストール済み）
- Node.js（既にインストール済み）
- このプロジェクト

---

## セットアップ手順

### ステップ1: Ubuntu に .NET Runtime をインストール

**Linux用語**:
- `apt` = Ubuntuのパッケージマネージャー（アプリをインストールするツール）
- `sudo` = 管理者権限で実行（Windowsの「管理者として実行」と同じ）

```bash
# Ubuntu にSSHまたは直接ログインして実行

# 1. Microsoftのパッケージリポジトリを追加
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# 2. パッケージリストを更新
sudo apt update

# 3. .NET Runtime（ASP.NET Core）をインストール
sudo apt install -y aspnetcore-runtime-8.0

# 4. インストール確認
dotnet --list-runtimes
```

**出力例**:
```
Microsoft.AspNetCore.App 8.0.x [/usr/share/dotnet/shared/Microsoft.AspNetCore.App]
Microsoft.NETCore.App 8.0.x [/usr/share/dotnet/shared/Microsoft.NETCore.App]
```

**注意**: このプロジェクトは.NET 10を使っていますが、執筆時点ではまだリリースされていないため、.NET 8を例にしています。将来的には `aspnetcore-runtime-10.0` をインストールしてください。

---

### ステップ2: Windows でLinux用にビルド

Windows上で、Linux向けのバイナリをビルドします。

**クロスコンパイル**: 異なるOS向けにビルドすること
- `-r linux-x64` = Linux（64bit Intel/AMD CPU）向けにビルド
- `-r linux-arm64` = Linux（ARM CPU、Raspberry Pi等）向けにビルド

#### 新しいデプロイスクリプト（Linux版）を作成

プロジェクトルートに `deploy-linux.ps1` を作成します（後述）。

または、手動で以下を実行：

```powershell
# 1. フロントエンドをビルド
cd src\frontend
npm run build

# 2. ビルド結果をバックエンドにコピー
$distPath = "dist"
$wwwrootPath = "..\backend\PerfmonAnalyzer.Api\wwwroot"
if (Test-Path $wwwrootPath) {
    Remove-Item $wwwrootPath -Recurse -Force
}
Copy-Item $distPath $wwwrootPath -Recurse

# 3. Linux用にバックエンドを発行
cd ..\backend\PerfmonAnalyzer.Api
dotnet publish -c Release -r linux-x64 --self-contained false -o ..\..\..\publish-linux

# オプション説明:
#   -r linux-x64       : Linux 64bit向け
#   --self-contained false : .NET Runtimeは別途インストール（サイズ削減）
#   -o publish-linux   : 出力先
```

**self-contained の違い**:

| オプション | 意味 | サイズ | 必要なもの |
|---|---|---|---|
| `--self-contained false` | Runtimeなし | 小（数MB） | サーバーに.NET Runtime必要 |
| `--self-contained true` | Runtime含む | 大（50-80MB） | サーバーに何も不要 |

通常は `false` でOK（サーバーにRuntimeをインストール済みの場合）。

---

### ステップ3: ファイルをUbuntuに転送

#### 方法1: SCP（推奨）

**SCP**: SSH経由でファイルをコピーするコマンド

Windows（PowerShell）から実行:

```powershell
# publish-linux フォルダ全体をUbuntuに転送
# 192.168.1.200 = UbuntuのIPアドレス
# username = Ubuntuのユーザー名
scp -r .\publish-linux username@192.168.1.200:/tmp/perfmonanalyzer
```

パスワードを聞かれるので、Ubuntuのユーザーパスワードを入力。

#### 方法2: 共有フォルダ（VirtualBox等）

VirtualBoxの共有フォルダ機能を使う場合:

```bash
# Ubuntu側で実行
sudo cp -r /media/sf_SharedFolder/publish-linux /opt/perfmonanalyzer
```

#### 方法3: Git経由

```bash
# Ubuntu側で実行
git clone https://github.com/yourusername/PerfmonAnalyzer.git
cd PerfmonAnalyzer
# ビルド済みファイルはGitにないので、別途転送が必要
```

---

### ステップ4: Ubuntu で配置と権限設定

```bash
# Ubuntu にSSHでログイン、または直接操作

# 1. アプリ用のディレクトリを作成
sudo mkdir -p /opt/perfmonanalyzer

# 2. 転送したファイルを配置（/tmp から移動）
sudo cp -r /tmp/perfmonanalyzer/* /opt/perfmonanalyzer/

# 3. 実行権限を付与
sudo chmod +x /opt/perfmonanalyzer/PerfmonAnalyzer.Api

# 4. 専用ユーザーを作成（セキュリティのため）
sudo useradd -r -s /bin/false perfmonapp

# 5. 所有権を変更
sudo chown -R perfmonapp:perfmonapp /opt/perfmonanalyzer

# 6. 設定ファイルを確認
cat /opt/perfmonanalyzer/appsettings.Production.json
```

**用語説明**:
- `chmod +x` = 実行権限を付与
- `useradd -r` = システムユーザーを作成（ログイン不可）
- `chown` = ファイルの所有者を変更

---

### ステップ5: 手動で動作確認

```bash
# 環境変数を設定して実行
cd /opt/perfmonanalyzer
ASPNETCORE_ENVIRONMENT=Production ./PerfmonAnalyzer.Api
```

別のターミナルで確認:

```bash
curl http://localhost:5000/api/health
# 出力: {"status":"healthy", ...}
```

ブラウザでアクセス:
```
http://192.168.1.200:5000
```

**成功したら Ctrl+C で停止**。次のステップでサービス化します。

---

## systemdでサービス化

### systemd とは？

**systemd** = Linux のサービス管理システム

Windows の「サービス」と同じで：
- バックグラウンドで実行
- OS起動時に自動起動
- クラッシュしたら自動再起動

### サービスファイルの作成

```bash
# サービス設定ファイルを作成
sudo nano /etc/systemd/system/perfmonanalyzer.service
```

**ファイル内容**:

```ini
[Unit]
Description=Perfmon Analyzer Web Application
After=network.target

[Service]
Type=notify
# アプリを実行するユーザー
User=perfmonapp
Group=perfmonapp

# アプリの場所
WorkingDirectory=/opt/perfmonanalyzer
ExecStart=/opt/perfmonanalyzer/PerfmonAnalyzer.Api

# 環境変数
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# 再起動設定
Restart=always
RestartSec=10

# セキュリティ設定
NoNewPrivileges=true
PrivateTmp=true

# ログ設定
StandardOutput=journal
StandardError=journal
SyslogIdentifier=perfmonanalyzer

[Install]
WantedBy=multi-user.target
```

**セクション説明**:

- `[Unit]` = サービスの情報
  - `After=network.target` = ネットワークが起動してから開始

- `[Service]` = 実行設定
  - `Type=notify` = ASP.NET Core用の設定
  - `Restart=always` = クラッシュしたら自動再起動
  - `RestartSec=10` = 再起動まで10秒待つ

- `[Install]` = 自動起動の設定
  - `WantedBy=multi-user.target` = マルチユーザーモードで起動

保存: `Ctrl+O` → Enter → `Ctrl+X`

---

### サービスの有効化と起動

```bash
# 1. systemd に設定をリロード
sudo systemctl daemon-reload

# 2. サービスを有効化（自動起動ON）
sudo systemctl enable perfmonanalyzer

# 3. サービスを開始
sudo systemctl start perfmonanalyzer

# 4. ステータス確認
sudo systemctl status perfmonanalyzer
```

**期待される出力**:

```
● perfmonanalyzer.service - Perfmon Analyzer Web Application
     Loaded: loaded (/etc/systemd/system/perfmonanalyzer.service; enabled)
     Active: active (running) since ...
```

**よく使うコマンド**:

```bash
# サービスを停止
sudo systemctl stop perfmonanalyzer

# サービスを再起動
sudo systemctl restart perfmonanalyzer

# ログを表示
sudo journalctl -u perfmonanalyzer -f

# 自動起動を無効化
sudo systemctl disable perfmonanalyzer
```

---

### ファイアウォール設定

Ubuntu の **ufw**（Uncomplicated Firewall）でポートを開放:

```bash
# 1. ufw を有効化
sudo ufw enable

# 2. SSH（22番ポート）を許可（リモート接続が切れないように）
sudo ufw allow 22/tcp

# 3. アプリのポート（5000番）を許可
sudo ufw allow 5000/tcp

# 4. 状態確認
sudo ufw status
```

**出力例**:
```
Status: active

To                         Action      From
--                         ------      ----
22/tcp                     ALLOW       Anywhere
5000/tcp                   ALLOW       Anywhere
```

これで、他のPCから `http://192.168.1.200:5000` でアクセスできます。

---

## Nginxの設定（オプション）

### Nginx とは？

**Nginx** = 高性能なWebサーバー＆リバースプロキシ

**リバースプロキシ** = 外からのアクセスを内部のアプリに転送する仕組み

```
ユーザー → Nginx (ポート80) → ASP.NET Core (ポート5000)
```

### なぜNginxを使うのか？

1. **標準ポート（80/443）でアクセス可能**
   - `http://192.168.1.200:5000` → `http://192.168.1.200`
   - ポート番号を省略できる

2. **HTTPS対応が簡単**
   - SSL証明書の設定をNginxで一元管理

3. **静的ファイルの高速配信**
   - 画像、CSS、JSはNginxが直接配信

4. **複数アプリの管理**
   - `/app1` → アプリ1、`/app2` → アプリ2

### Nginxのインストール

```bash
# Nginx をインストール
sudo apt install -y nginx

# 起動
sudo systemctl start nginx
sudo systemctl enable nginx

# 確認
curl http://localhost
# Nginxのデフォルトページが表示されればOK
```

### Nginx設定ファイルの作成

```bash
# 設定ファイルを作成
sudo nano /etc/nginx/sites-available/perfmonanalyzer
```

**ファイル内容**:

```nginx
server {
    listen 80;
    server_name 192.168.1.200;  # サーバーのIPアドレス

    # アクセスログ
    access_log /var/log/nginx/perfmonanalyzer.access.log;
    error_log /var/log/nginx/perfmonanalyzer.error.log;

    location / {
        # ASP.NET Core アプリにプロキシ
        proxy_pass http://localhost:5000;
        
        # ヘッダーの設定
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

**設定の有効化**:

```bash
# シンボリックリンクを作成（有効化）
sudo ln -s /etc/nginx/sites-available/perfmonanalyzer /etc/nginx/sites-enabled/

# 設定ファイルのテスト
sudo nginx -t

# Nginx を再起動
sudo systemctl restart nginx
```

### ファイアウォールの変更

```bash
# 80番ポートを許可
sudo ufw allow 80/tcp

# 5000番ポートは外部に公開しない（ローカルのみ）
sudo ufw delete allow 5000/tcp
```

### appsettings.Production.json の変更

Nginxを使う場合、ASP.NET Coreはlocalhostのみでリッスンします：

```json
{
  "Urls": "http://localhost:5000"
}
```

`0.0.0.0` ではなく `localhost` にすることで、外部から直接5000番ポートにアクセスできなくなり、セキュリティが向上します。

### アクセス確認

ブラウザで：
```
http://192.168.1.200
```

ポート番号なしでアクセスできます！

---

## 完全な自動デプロイスクリプト

### Windows 用（Linux向けビルド）

`deploy-linux.ps1`:

```powershell
# ======================================
# Linux (Ubuntu) 向けデプロイスクリプト
# ======================================

param(
    [string]$OutputPath = ".\publish-linux",
    [string]$TargetRuntime = "linux-x64",
    [switch]$SelfContained = $false
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Linux向けビルド開始" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$rootPath = $PSScriptRoot
$frontendPath = Join-Path $rootPath "src\frontend"
$backendPath = Join-Path $rootPath "src\backend\PerfmonAnalyzer.Api"
$distPath = Join-Path $frontendPath "dist"
$wwwrootPath = Join-Path $backendPath "wwwroot"

# フロントエンドビルド
Write-Host "フロントエンドをビルド中..." -ForegroundColor Yellow
Push-Location $frontendPath
try {
    npm install --silent
    npm run build
}
finally {
    Pop-Location
}

# 静的ファイルコピー
Write-Host "静的ファイルをコピー中..." -ForegroundColor Yellow
if (Test-Path $wwwrootPath) {
    Remove-Item $wwwrootPath -Recurse -Force
}
Copy-Item $distPath $wwwrootPath -Recurse

# Linux用にビルド
Write-Host "Linux向けにビルド中..." -ForegroundColor Yellow
Push-Location $backendPath
try {
    $fullOutputPath = Join-Path $rootPath $OutputPath
    if (Test-Path $fullOutputPath) {
        Remove-Item $fullOutputPath -Recurse -Force
    }
    
    dotnet publish -c Release -r $TargetRuntime --self-contained:$SelfContained -o $fullOutputPath --nologo
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host " ビルド完了！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "次のステップ:" -ForegroundColor Yellow
Write-Host "  1. Ubuntu に転送:" -ForegroundColor White
Write-Host "     scp -r $OutputPath username@192.168.1.200:/tmp/" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Ubuntu 側で配置:" -ForegroundColor White
Write-Host "     sudo cp -r /tmp/publish-linux/* /opt/perfmonanalyzer/" -ForegroundColor Gray
Write-Host "     sudo chmod +x /opt/perfmonanalyzer/PerfmonAnalyzer.Api" -ForegroundColor Gray
Write-Host ""
Write-Host "  3. サービス再起動:" -ForegroundColor White
Write-Host "     sudo systemctl restart perfmonanalyzer" -ForegroundColor Gray
Write-Host ""
```

### Ubuntu 用（配置＆再起動）

`deploy.sh` （Ubuntu上で実行）:

```bash
#!/bin/bash
# ======================================
# Ubuntu サーバー側デプロイスクリプト
# ======================================

# エラー時に停止
set -e

# 変数
APP_DIR="/opt/perfmonanalyzer"
SERVICE_NAME="perfmonanalyzer"
TEMP_DIR="/tmp/perfmonanalyzer"

echo "========================================"
echo " Perfmon Analyzer デプロイ"
echo "========================================"
echo ""

# 1. ファイルの配置
if [ ! -d "$TEMP_DIR" ]; then
    echo "エラー: $TEMP_DIR が見つかりません"
    echo "先にSCPでファイルを転送してください"
    exit 1
fi

echo "ファイルを配置中..."
sudo cp -r "$TEMP_DIR"/* "$APP_DIR"/
sudo chmod +x "$APP_DIR/PerfmonAnalyzer.Api"
sudo chown -R perfmonapp:perfmonapp "$APP_DIR"

# 2. サービス再起動
echo "サービスを再起動中..."
sudo systemctl restart "$SERVICE_NAME"

# 3. ステータス確認
sleep 2
sudo systemctl status "$SERVICE_NAME" --no-pager

echo ""
echo "========================================"
echo " デプロイ完了！"
echo "========================================"
echo ""
echo "確認方法:"
echo "  curl http://localhost:5000/api/health"
echo ""
```

実行権限を付与:

```bash
chmod +x deploy.sh
```

---

## 用語集

### Linux関連

| 用語 | 説明 |
|---|---|
| **Ubuntu** | Linuxディストリビューションの1つ。初心者に優しい |
| **bash** | Linuxの標準シェル（コマンドライン） |
| **apt** | Ubuntuのパッケージマネージャー |
| **sudo** | 管理者権限でコマンドを実行 |
| **systemd** | サービス管理システム |
| **chmod** | ファイルのパーミッション（権限）を変更 |
| **chown** | ファイルの所有者を変更 |
| **SCP** | SSH経由でファイルをコピー |
| **SSH** | リモートログインプロトコル |

### ネットワーク関連

| 用語 | 説明 |
|---|---|
| **リバースプロキシ** | 外部からのアクセスを内部に転送する |
| **Nginx** | 高性能Webサーバー＆リバースプロキシ |
| **ufw** | Ubuntuのファイアウォール |
| **ポートフォワーディング** | ポート番号を変換して転送 |

### .NET関連

| 用語 | 説明 |
|---|---|
| **クロスコンパイル** | 異なるOS向けにビルド |
| **Runtime** | プログラムを実行する基盤 |
| **Self-contained** | Runtime込みでビルド |
| **RID** | Runtime Identifier（`linux-x64`等） |

---

## トラブルシューティング

### タイムアウトエラーが出るが動作している

**症状**: `Job for perfmonanalyzer.service failed because a timeout was exceeded` というエラーが出るが、アプリは正常に動作している

**原因**: systemdサービスファイルで `Type=notify` を使用している場合、ASP.NET Coreがsystemdに「起動完了」の通知を送る必要がありますが、この通知機能が有効になっていないためタイムアウトします。

**解決方法1（推奨）**: サービスファイルを修正

```bash
# サービスファイルを編集
sudo nano /etc/systemd/system/perfmonanalyzer.service

# Type=notify を Type=simple に変更
# [Service]
# Type=simple  ← これに変更

# 保存後
sudo systemctl daemon-reload
sudo systemctl restart perfmonanalyzer
sudo systemctl status perfmonanalyzer  # エラーが出ないことを確認
```

**解決方法2**: .NET側でsystemd通知を有効化

```bash
# Windows (開発PC) で
cd src/backend/PerfmonAnalyzer.Api
dotnet add package Microsoft.Extensions.Hosting.Systemd
```

Program.csに追加:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Linux上でsystemd統合を有効化
builder.Host.UseSystemd();

// ... 以下既存のコード
```

再ビルド・デプロイすれば `Type=notify` でもエラーが出なくなります。

### サービスが起動しない

```bash
# ログを確認
sudo journalctl -u perfmonanalyzer -n 50 --no-pager

# よくある原因:
# - 実行権限がない → chmod +x で解決
# - Runtimeがない → apt install aspnetcore-runtime-x.x
# - ポートが使用中 → appsettings.jsonでポート変更
```

### ファイアウォールでブロックされる

```bash
# ufwの状態確認
sudo ufw status verbose

# ポート開放
sudo ufw allow 5000/tcp
```

### Nginxでエラー

```bash
# Nginxのログ確認
sudo tail -f /var/log/nginx/perfmonanalyzer.error.log

# 設定テスト
sudo nginx -t
```

### プロキシヘッダーが正しくない

ASP.NET Core 側で ForwardedHeaders を設定:

```csharp
// Program.cs
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
```

---

## まとめ

### Windows との違い

| 作業 | Windows | Linux (Ubuntu) |
|---|---|---|
| **ビルド** | `dotnet publish` | `dotnet publish -r linux-x64` |
| **配置** | フォルダコピー | SCP + 権限設定 |
| **サービス化** | `sc.exe create` | systemd サービスファイル |
| **自動起動** | スタートアップに登録 | `systemctl enable` |
| **ログ確認** | イベントビューアー | `journalctl` |
| **ファイアウォール** | Windows Defender | ufw |
| **リバースプロキシ** | IIS（オプション） | Nginx（推奨） |

### 推奨構成

```
ネットワーク
│
├── 開発PC (Windows)
│   └── Visual Studio / VS Code で開発
│
└── サーバー (Ubuntu VM)
    ├── systemd で自動起動
    ├── Nginx でリバースプロキシ (ポート80)
    └── ASP.NET Core (ポート5000)
```

この構成なら：
- ✅ 開発はWindowsで快適に
- ✅ 本番はLinuxで安定稼働
- ✅ コストも低い（無料のUbuntu）

---

## 参考リンク

- [Ubuntu Server ダウンロード](https://ubuntu.com/download/server)
- [.NET on Linux](https://learn.microsoft.com/ja-jp/dotnet/core/install/linux-ubuntu)
- [ASP.NET Core on Linux with Nginx](https://learn.microsoft.com/ja-jp/aspnet/core/host-and-deploy/linux-nginx)
- [systemd サービスの作成](https://www.freedesktop.org/software/systemd/man/systemd.service.html)
