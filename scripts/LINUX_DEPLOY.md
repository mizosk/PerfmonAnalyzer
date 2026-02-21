# Linux (Ubuntu) デプロイ - クイックリファレンス

## 前提条件

- Ubuntu 20.04 以降
- .NET Runtime 8.0 以降
- (オプション) Nginx

## 1. サーバー準備（初回のみ）

```bash
# .NET Runtime インストール
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt update
sudo apt install -y aspnetcore-runtime-8.0

# アプリ用ディレクトリ作成
sudo mkdir -p /opt/perfmonanalyzer

# 専用ユーザー作成
sudo useradd -r -s /bin/false perfmonapp
```

## 2. Windows でビルド

```powershell
# プロジェクトルートで実行
.\deploy-linux.ps1
```

または自動転送:
```powershell
.\deploy-linux.ps1 -AutoTransfer -ServerAddress '192.168.1.200' -ServerUser 'username'
```

## 3. ファイル転送（手動の場合）

```powershell
# Windows から実行
scp -r .\publish-linux username@192.168.1.200:/tmp/perfmonanalyzer
```

## 4. サーバーで配置

```bash
# Ubuntu で実行
cd /tmp
chmod +x perfmonanalyzer/scripts/deploy-server.sh
sudo ./perfmonanalyzer/scripts/deploy-server.sh
```

## 5. 確認

```bash
# ヘルスチェック
curl http://localhost:5000/api/health

# サービス状態確認
sudo systemctl status perfmonanalyzer

# ログ確認
sudo journalctl -u perfmonanalyzer -f
```

## よく使うコマンド

```bash
# サービス操作
sudo systemctl start perfmonanalyzer    # 起動
sudo systemctl stop perfmonanalyzer     # 停止
sudo systemctl restart perfmonanalyzer  # 再起動
sudo systemctl status perfmonanalyzer   # 状態確認

# ログ確認
sudo journalctl -u perfmonanalyzer -n 50        # 最新50行
sudo journalctl -u perfmonanalyzer -f           # リアルタイム表示
sudo journalctl -u perfmonanalyzer --since today # 今日のログ

# ファイアウォール
sudo ufw allow 5000/tcp  # 5000番ポート開放
sudo ufw allow 80/tcp    # 80番ポート開放（Nginx使用時）
sudo ufw status          # 状態確認
```

## Nginx セットアップ（オプション）

```bash
# Nginx インストール
sudo apt install -y nginx

# 設定ファイルを配置
sudo cp scripts/nginx-perfmonanalyzer.conf /etc/nginx/sites-available/perfmonanalyzer

# IPアドレス等を編集
sudo nano /etc/nginx/sites-available/perfmonanalyzer

# 有効化
sudo ln -s /etc/nginx/sites-available/perfmonanalyzer /etc/nginx/sites-enabled/

# 設定テスト
sudo nginx -t

# 再起動
sudo systemctl restart nginx

# ファイアウォール設定
sudo ufw allow 80/tcp
```

## トラブルシューティング

### タイムアウトエラーが出るが動作している

エラー: `Job for perfmonanalyzer.service failed because a timeout was exceeded`

```bash
# サービスファイルを編集
sudo nano /etc/systemd/system/perfmonanalyzer.service

# Type=notify → Type=simple に変更

# 保存後
sudo systemctl daemon-reload
sudo systemctl restart perfmonanalyzer
```

### サービスが起動しない

```bash
# 詳細ログ確認
sudo journalctl -u perfmonanalyzer -xe

# よくある原因:
# - 実行権限がない → chmod +x で解決
# - ポートが使用中 → lsof -i:5000 で確認
# - Runtimeがない → dotnet --list-runtimes で確認
```

### 他のPCからアクセスできない

```bash
# ファイアウォール確認
sudo ufw status

# ポート開放
sudo ufw allow 5000/tcp

# アプリがリッスンしているか確認
sudo netstat -tulpn | grep 5000
```

## アップデート手順

```bash
# 1. サービス停止
sudo systemctl stop perfmonanalyzer

# 2. Windows で新しいビルドを転送（SCP）

# 3. ファイル配置
sudo cp -r /tmp/perfmonanalyzer/* /opt/perfmonanalyzer/
sudo chmod +x /opt/perfmonanalyzer/PerfmonAnalyzer.Api

# 4. サービス再起動
sudo systemctl start perfmonanalyzer

# 5. 確認
sudo systemctl status perfmonanalyzer
```

## 詳細ドキュメント

- [完全ガイド](../docs/memo/2026-02-15-linux-deployment-guide.md)
- [デプロイスクリプト](../deploy-linux.ps1)
- [サーバー側スクリプト](deploy-server.sh)
- [Nginx設定サンプル](nginx-perfmonanalyzer.conf)
