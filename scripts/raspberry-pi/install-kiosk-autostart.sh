#!/usr/bin/env bash
# Konfiguruje autostart panelu (Chromium kiosk) po zalogowaniu na pulpit — systemd --user.
# Wymaga: Raspberry Pi OS z pulpitem, chromium, curl (patrz docs/DEPLOYMENT.md).
set -euo pipefail

SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
KIOSK_SH="$SCRIPT_DIR/greenhouse-kiosk.sh"

if [[ ! -x "$KIOSK_SH" ]]; then
  echo "Nadaj prawa: chmod +x \"$KIOSK_SH\"" >&2
  exit 1
fi

mkdir -p "${XDG_CONFIG_HOME:-$HOME/.config}/systemd/user"

UNIT="${XDG_CONFIG_HOME:-$HOME/.config}/systemd/user/greenhouse-kiosk.service"

cat >"$UNIT" <<EOF
[Unit]
Description=Greenhouse — panel szklarni (Chromium kiosk, localhost:5000)
After=graphical-session.target

[Service]
Type=simple
ExecStart=$KIOSK_SH
Restart=on-failure
RestartSec=8

[Install]
WantedBy=graphical-session.target
EOF

systemctl --user daemon-reload
systemctl --user enable greenhouse-kiosk.service
echo "OK: włączono autostart użytkownika (greenhouse-kiosk.service)."
echo "    Pierwszy start:  systemctl --user start greenhouse-kiosk.service"
echo "    Status:          systemctl --user status greenhouse-kiosk.service"
echo ""
echo "Zalecane przy monitorze bez logowania ręcznego: włącz automatyczne logowanie użytkownika z pulpitem (raspi-config / ustawienia Raspberry Pi OS)."
echo "Opcjonalnie (usługi użytkownika bez sesji graficznej — zwykle NIE dla kiosku): sudo loginctl enable-linger \"$USER\""
