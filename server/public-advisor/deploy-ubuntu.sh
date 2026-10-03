#!/usr/bin/env bash
set -euo pipefail

APP_DIR="${APP_DIR:-$HOME/guandu-public-advisor}"
PORT="${PORT:-8080}"
PUBLIC_ORIGIN="${PUBLIC_ORIGIN:-http://81.70.40.146:${PORT}}"
SOURCE_DIR="${SOURCE_DIR:-$PWD}"

if ! command -v node >/dev/null 2>&1; then
  sudo apt-get update
  sudo apt-get install -y nodejs npm
fi

mkdir -p "$APP_DIR"
source_abs="$(cd "$SOURCE_DIR" && pwd)"
app_abs="$(cd "$APP_DIR" && pwd)"
if [ "$source_abs" != "$app_abs" ]; then
  cp -R "$SOURCE_DIR"/. "$APP_DIR"/
fi
cd "$APP_DIR"
npm install --omit=dev

sudo tee /etc/systemd/system/guandu-public-advisor.service >/dev/null <<SERVICE
[Unit]
Description=Guandu public advisor relay
After=network.target

[Service]
Type=simple
User=$USER
WorkingDirectory=$APP_DIR
Environment=NODE_ENV=production
Environment=PORT=$PORT
Environment=PUBLIC_ORIGIN=$PUBLIC_ORIGIN
ExecStart=$(command -v node) $APP_DIR/server.js
Restart=always
RestartSec=3

[Install]
WantedBy=multi-user.target
SERVICE

sudo systemctl daemon-reload
sudo systemctl enable --now guandu-public-advisor
if command -v ufw >/dev/null 2>&1; then
  sudo ufw allow "${PORT}/tcp" || true
fi
curl --fail --silent "http://127.0.0.1:${PORT}/healthz"
echo
echo "public advisor is running at ${PUBLIC_ORIGIN}"
