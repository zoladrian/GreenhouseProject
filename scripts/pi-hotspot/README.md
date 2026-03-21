# Hotspot Wi‑Fi na Raspberry Pi — utrzymanie włączone (host, nie Docker)

## Dlaczego nie w kontenerze `greenhouse-api`?

Kontener **nie widzi** interfejsu `wlan0`, nie ma uprawnień do sterowania radiem Wi‑Fi ani **NetworkManager** na hoście.  
Żeby „aplikacja w Dockerze” robiła hotspot, trzeba by `privileged`, `network_mode: host`, urządzenia radiowe w kontenerze i integrację z D-Bus — to **kruche**, **nieprzenośne** i **ryzykowne** bezpieczeństwowo.

Ten katalog zawiera **skrypt + systemd na hoście Pi** (obok Dockera), które:

- włączają radio Wi‑Fi;
- podnoszą zapisany profil hotspotu **NetworkManager**;
- opcjonalnie co kilka minut sprawdzają, czy nadal jest aktywny.

## Wymagania

- Raspberry Pi OS z **NetworkManager** (typowe na *Desktop*; na *Lite* może być `dhcpcd` — wtedy ten skrypt **nie** zadziała bez przejścia na NM).
- Jednorazowo: utworzony profil hotspotu (GUI *Wireless hotspot* albo `nmcli dev wifi hotspot …`) **albo** ustawienie `GREENHOUSE_HOTSPOT_CREATE=1` w pliku konfiguracyjnym (patrz niżej).

## Instalacja

Na malince (ścieżki przykładowe — dostosuj katalog repo):

```bash
sudo install -m 0755 scripts/pi-hotspot/ensure-hotspot.sh /usr/local/bin/greenhouse-ensure-hotspot.sh
sudo install -m 0644 scripts/pi-hotspot/greenhouse-hotspot.service /etc/systemd/system/
sudo install -m 0644 scripts/pi-hotspot/greenhouse-hotspot.timer /etc/systemd/system/
sudo install -m 0644 scripts/pi-hotspot/etc-default.example /etc/default/greenhouse-hotspot
sudo nano /etc/default/greenhouse-hotspot   # ustaw GREENHOUSE_HOTSPOT_CONNECTION=nazwa_profilu_NM
sudo systemctl daemon-reload
sudo systemctl enable --now greenhouse-hotspot.timer
```

**Nazwa profilu** — to, co widzisz w `nmcli connection show` (kolumna `NAME`), np. `Hotspot`, `greenhouse-ap`.

Sprawdzenie:

```bash
systemctl status greenhouse-hotspot.timer
journalctl -u greenhouse-hotspot.service -n 30 --no-pager
```

## Konfiguracja `/etc/default/greenhouse-hotspot`

| Zmienna | Znaczenie |
|--------|-----------|
| `GREENHOUSE_HOTSPOT_ENABLED` | `1` (domyślnie) — działaj; `0` — wyłącz skrypt |
| `GREENHOUSE_HOTSPOT_CONNECTION` | Nazwa połączenia NM do `nmcli connection up` |
| `GREENHOUSE_HOTSPOT_CREATE` | `1` — jeśli profil nie istnieje, utwórz hotspot (wymaga poniższych) |
| `GREENHOUSE_HOTSPOT_IFNAME` | Interfejs, np. `wlan0` |
| `GREENHOUSE_HOTSPOT_SSID` | SSID sieci |
| `GREENHOUSE_HOTSPOT_PASSWORD` | Hasło (min. 8 znaków dla WPA2) |

Pierwszy raz **łatwiej** utworzyć hotspot w GUI lub ręcznie `nmcli`, potem tylko podać `GREENHOUSE_HOTSPOT_CONNECTION`.

## Captive portal / DNS

Nadal osobno: [**docs/CAPTIVE-PORTAL.md**](../../docs/CAPTIVE-PORTAL.md).

## Timer

`greenhouse-hotspot.timer` uruchamia usługę **po starcie** (z opóźnieniem) i **cyklicznie** (domyślnie co 5 min), żeby po sporadycznych zrzutach połączenia hotspot wrócił.
