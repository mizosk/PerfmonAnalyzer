#!/bin/bash
# ======================================
# Ubuntu サーバー側デプロイスクリプト
# ======================================
# 使い方:
#   1. Windows から publish-linux をサーバーに転送
#   2. このスクリプトを実行
# ======================================

set -e  # エラー時に停止

# 設定
APP_DIR="/opt/perfmonanalyzer"
SERVICE_NAME="perfmonanalyzer"
TEMP_DIR="/tmp/perfmonanalyzer"
APP_USER="perfmonapp"
APP_GROUP="perfmonapp"

# 色付き出力
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN} Perfmon Analyzer デプロイ${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

# ========================================
# 1. 権限チェック
# ========================================
if [ "$EUID" -ne 0 ]; then 
    echo -e "${RED}エラー: このスクリプトは sudo で実行してください${NC}"
    echo "例: sudo ./deploy-server.sh"
    exit 1
fi

# ========================================
# 2. ファイルの存在確認
# ========================================
if [ ! -d "$TEMP_DIR" ]; then
    echo -e "${RED}エラー: $TEMP_DIR が見つかりません${NC}"
    echo ""
    echo "先に Windows から SCP でファイルを転送してください:"
    echo "  scp -r publish-linux username@サーバーIP:/tmp/perfmonanalyzer"
    echo ""
    exit 1
fi

# ========================================
# 3. アプリディレクトリの準備
# ========================================
echo -e "${YELLOW}アプリディレクトリを準備中...${NC}"

if [ ! -d "$APP_DIR" ]; then
    echo "  → $APP_DIR を作成"
    mkdir -p "$APP_DIR"
fi

# ========================================
# 4. サービス停止
# ========================================
echo -e "${YELLOW}サービスを停止中...${NC}"

if systemctl is-active --quiet "$SERVICE_NAME"; then
    systemctl stop "$SERVICE_NAME"
    echo "  ✓ サービスを停止しました"
else
    echo "  → サービスは実行されていません"
fi

# ========================================
# 5. ファイルのコピー
# ========================================
echo -e "${YELLOW}ファイルをコピー中...${NC}"
echo "  コピー元: $TEMP_DIR"
echo "  コピー先: $APP_DIR"

cp -r "$TEMP_DIR"/* "$APP_DIR"/
echo "  ✓ ファイルをコピーしました"

# ========================================
# 6. 権限設定
# ========================================
echo -e "${YELLOW}権限を設定中...${NC}"

# 実行権限を付与
chmod +x "$APP_DIR/PerfmonAnalyzer.Api"
echo "  ✓ 実行権限を付与"

# ユーザーが存在するか確認
if ! id -u "$APP_USER" > /dev/null 2>&1; then
    echo "  → ユーザー $APP_USER を作成"
    useradd -r -s /bin/false "$APP_USER"
fi

# 所有権を変更
chown -R "$APP_USER:$APP_GROUP" "$APP_DIR"
echo "  ✓ 所有権を変更（$APP_USER:$APP_GROUP）"

# ========================================
# 7. systemd サービスファイルの確認/作成
# ========================================
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

if [ ! -f "$SERVICE_FILE" ]; then
    echo -e "${YELLOW}systemd サービスファイルを作成中...${NC}"
    
    cat > "$SERVICE_FILE" << EOF
[Unit]
Description=Perfmon Analyzer Web Application
After=network.target

[Service]
# Type=simple: プロセス起動時点でサービス準備完了とみなす
# Type=notify: アプリからの通知を待つ（要:Microsoft.Extensions.Hosting.Systemd）
Type=simple
User=$APP_USER
Group=$APP_GROUP
WorkingDirectory=$APP_DIR
ExecStart=$APP_DIR/PerfmonAnalyzer.Api

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

Restart=always
RestartSec=10

NoNewPrivileges=true
PrivateTmp=true

StandardOutput=journal
StandardError=journal
SyslogIdentifier=$SERVICE_NAME

[Install]
WantedBy=multi-user.target
EOF
    
    echo "  ✓ サービスファイルを作成: $SERVICE_FILE"
    
    # systemd リロード
    systemctl daemon-reload
    echo "  ✓ systemd をリロード"
    
    # サービス有効化
    systemctl enable "$SERVICE_NAME"
    echo "  ✓ サービスを有効化（自動起動ON）"
fi

# ========================================
# 8. サービス起動
# ========================================
echo -e "${YELLOW}サービスを起動中...${NC}"
systemctl start "$SERVICE_NAME"
sleep 2

# ========================================
# 9. ステータス確認
# ========================================
echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN} サービスのステータス${NC}"
echo -e "${CYAN}========================================${NC}"
systemctl status "$SERVICE_NAME" --no-pager || true

# ========================================
# 10. 動作確認
# ========================================
echo ""
echo -e "${YELLOW}動作確認中...${NC}"
sleep 1

if curl -s http://localhost:5000/api/health > /dev/null; then
    echo -e "  ${GREEN}✓ アプリは正常に動作しています${NC}"
else
    echo -e "  ${RED}✗ アプリが応答しません${NC}"
    echo "  ログを確認してください:"
    echo "    journalctl -u $SERVICE_NAME -n 50"
fi

# ========================================
# 完了
# ========================================
echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${GREEN} デプロイ完了！${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo "確認方法:"
echo "  • ヘルスチェック:"
echo "      curl http://localhost:5000/api/health"
echo ""
echo "  • ブラウザでアクセス:"
echo "      http://$(hostname -I | awk '{print $1}'):5000"
echo ""
echo "よく使うコマンド:"
echo "  • ログ表示:"
echo "      journalctl -u $SERVICE_NAME -f"
echo ""
echo "  • サービス再起動:"
echo "      systemctl restart $SERVICE_NAME"
echo ""
echo "  • サービス停止:"
echo "      systemctl stop $SERVICE_NAME"
echo ""
echo "  • サービス状態確認:"
echo "      systemctl status $SERVICE_NAME"
echo ""

# クリーンアップ
echo -e "${YELLOW}一時ファイルをクリーンアップしますか？ (y/n)${NC}"
read -r answer
if [ "$answer" = "y" ] || [ "$answer" = "Y" ]; then
    rm -rf "$TEMP_DIR"
    echo "  ✓ $TEMP_DIR を削除しました"
fi

echo ""
echo -e "${GREEN}すべて完了しました！${NC}"
