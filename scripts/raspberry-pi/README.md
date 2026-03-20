# Skrypty — Raspberry Pi (pulpit)

## `greenhouse-kiosk.sh`

Otwiera interfejs szklarni w **Chromium** w trybie **kiosk** (`http://127.0.0.1:5000`).

- Wymaga **Raspberry Pi OS z pulpitem** (nie headless bez X11/Wayland).
- Przed startem skrypt **czeka**, aż API odpowie (Docker: `greenhouse-api` z mapowaniem `5000:5000`).
- Chromium uruchamiany z **`--disable-http-cache`** (łatwiejsze ładowanie nowych plików po deployu).
- Zmienne opcjonalne:
  - `GREENHOUSE_KIOSK_URL` — inny adres (np. `http://192.168.1.10:5000`).
  - `GREENHOUSE_KIOSK_WAIT_SEC` — timeout oczekiwania w sekundach (domyślnie 120).

```bash
chmod +x scripts/raspberry-pi/greenhouse-kiosk.sh
./scripts/raspberry-pi/greenhouse-kiosk.sh
```

## `install-kiosk-autostart.sh`

Instaluje **systemd --user** jednostkę `greenhouse-kiosk.service`, żeby kiosk **startował automatycznie po zalogowaniu na pulpit**.

```bash
chmod +x scripts/raspberry-pi/install-kiosk-autostart.sh
./scripts/raspberry-pi/install-kiosk-autostart.sh
systemctl --user start greenhouse-kiosk.service
```

**Ważne:** włącz **automatyczne logowanie** użytkownika z GUI — inaczej po reboocie sesja graficzna nie wstanie i kiosk nie wystartuje sam.

## `greenhouse-kiosk.desktop`

Przykład wpisu **autostartu** (alternatywa dla systemd). Skopiuj i popraw ścieżkę `Exec=…`:

```bash
mkdir -p ~/.config/autostart
cp scripts/raspberry-pi/greenhouse-kiosk.desktop ~/.config/autostart/
nano ~/.config/autostart/greenhouse-kiosk.desktop
```

Instalacja Chromium (jeśli brak):

```bash
sudo apt update && sudo apt install -y chromium curl
```

## Odświeżanie po aktualizacji Dockera

Obraz API zawiera unikalny plik `deploy-id`. Frontend w produkcji co ~45 s sprawdza `GET /api/meta/deploy` i przy zmianie **przeładowuje stronę** — wystarczy na Pi: `git pull && docker compose build && docker compose up -d`.

Pełna instrukcja: [`docs/DEPLOYMENT.md`](../../docs/DEPLOYMENT.md).
