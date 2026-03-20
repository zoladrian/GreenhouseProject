#!/usr/bin/env bash
# Uruchamia panel szklarni na pełnym ekranie (kiosk) — na hoście Raspberry Pi OS z pulpitem.
# Wymaga: Docker Compose włączony (`docker compose up -d`), port 5000 opublikowany na localhost.
# Zobacz: docs/DEPLOYMENT.md → „Panel na ekranie Malinki”.

set -euo pipefail

URL="${GREENHOUSE_KIOSK_URL:-http://127.0.0.1:5000}"
MAX_WAIT_SEC="${GREENHOUSE_KIOSK_WAIT_SEC:-120}"
SLEEP_SEC=2

if command -v chromium >/dev/null 2>&1; then
  BROWSER=(chromium)
elif command -v chromium-browser >/dev/null 2>&1; then
  BROWSER=(chromium-browser)
else
  echo "greenhouse-kiosk: brak Chromium. Zainstaluj: sudo apt install chromium" >&2
  exit 1
fi

echo "greenhouse-kiosk: czekam na ${URL} (max ${MAX_WAIT_SEC}s)..."
elapsed=0
while ! curl -sf -o /dev/null --connect-timeout 2 "${URL}/"; do
  if (( elapsed >= MAX_WAIT_SEC )); then
    echo "greenhouse-kiosk: serwer nie odpowiada — uruchom: docker compose up -d" >&2
    exit 1
  fi
  sleep "${SLEEP_SEC}"
  elapsed=$((elapsed + SLEEP_SEC))
done

echo "greenhouse-kiosk: start przeglądarki (kiosk) → ${URL}"

exec "${BROWSER[@]}" \
  --kiosk \
  "${URL}" \
  --password-store=basic \
  --disable-http-cache \
  --disable-infobars \
  --noerrdialogs \
  --disable-session-crashed-bubble \
  --disable-features=Translate,OptimizationHints \
  --check-for-update-interval=31536000
