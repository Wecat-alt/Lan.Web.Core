#!/bin/sh
set -eu

escape_js() {
  printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

api_base_url=$(escape_js "${VITE_APP_API_BASE_URL:-http://localhost:5197}")
signalr_url=$(escape_js "${VITE_SIGNALR_URL:-http://localhost:5197/hubs/stock}")
open_url=$(escape_js "${VITE_OPEN_URL:-http://localhost:5197}")
map_tile_url=$(escape_js "${VITE_MAP_TILE_MAP_URL:-/maptile_gaode/{z}/{x}/{y}.jpg}")
app_title=$(escape_js "${VUE_APP_TITLE:-生产环境}")

cat > /usr/share/nginx/html/config.js <<EOF
window.__APP_CONFIG__ = {
  VITE_APP_API_BASE_URL: "${api_base_url}",
  VITE_SIGNALR_URL: "${signalr_url}",
  VITE_OPEN_URL: "${open_url}",
  VITE_MAP_TILE_MAP_URL: "${map_tile_url}",
  VUE_APP_TITLE: "${app_title}",
}
EOF
