#!/usr/bin/env bash
# Uruchamiany na hoście Raspberry Pi (nie w kontenerze). Wymaga NetworkManager (nmcli).
set -euo pipefail

log() { echo "[greenhouse-hotspot] $*"; }

DEFAULT_FILE=/etc/default/greenhouse-hotspot
if [[ -f "$DEFAULT_FILE" ]]; then
  # shellcheck source=/dev/null
  . "$DEFAULT_FILE"
fi

: "${GREENHOUSE_HOTSPOT_ENABLED:=1}"
if [[ "$GREENHOUSE_HOTSPOT_ENABLED" != "1" ]]; then
  log "wyłączone (GREENHOUSE_HOTSPOT_ENABLED!=1)"
  exit 0
fi

if ! command -v nmcli >/dev/null 2>&1; then
  log "nmcli nie znaleziony — potrzebny NetworkManager (ten skrypt nie obsługuje dhcpcd)"
  exit 0
fi

CONN="${GREENHOUSE_HOTSPOT_CONNECTION:-greenhouse-ap}"

nmcli radio wifi on || true

if nmcli -t -f NAME con show --active | grep -Fqx "$CONN"; then
  log "połączenie '$CONN' już aktywne"
  exit 0
fi

if nmcli connection show "$CONN" &>/dev/null; then
  log "podnoszenie '$CONN'…"
  nmcli connection up "$CONN"
  log "OK"
  exit 0
fi

if [[ "${GREENHOUSE_HOTSPOT_CREATE:-0}" == "1" ]]; then
  IFN="${GREENHOUSE_HOTSPOT_IFNAME:?ustaw GREENHOUSE_HOTSPOT_IFNAME, np. wlan0}"
  SSID="${GREENHOUSE_HOTSPOT_SSID:-Greenhouse}"
  PASS="${GREENHOUSE_HOTSPOT_PASSWORD:?ustaw GREENHOUSE_HOTSPOT_PASSWORD (min. 8 znaków)}"
  log "tworzenie hotspotu: ifname=$IFN con-name=$CONN ssid=$SSID"
  nmcli dev wifi hotspot ifname "$IFN" con-name "$CONN" ssid "$SSID" password "$PASS"
  log "utworzono i aktywowano"
  exit 0
fi

log "brak profilu NM '$CONN'. Utwórz hotspot (GUI / nmcli) i ustaw GREENHOUSE_HOTSPOT_CONNECTION albo GREENHOUSE_HOTSPOT_CREATE=1"
exit 1
