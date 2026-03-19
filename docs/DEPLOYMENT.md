# Wdrożenie na Raspberry Pi (Docker)

## Wymagania

- Raspberry Pi OS (64-bit zalecane), Docker + Docker Compose plugin
- Koordynator Zigbee podłączony przez USB
- Opcjonalnie: Wi‑Fi AP (`hostapd` + `dnsmasq`) — poza tym repozytorium; telefon łączy się z siecią Pi i otwiera `http://<IP>:5000`

## Architektura

- **Jeden kontener .NET** (`greenhouse-api`): REST API + statyczny frontend (`wwwroot`) + **MQTT ingestion** (subskrypcja `zigbee2mqtt/#`, zapis do SQLite)
- **Mosquitto** — broker MQTT
- **Zigbee2MQTT** — most Zigbee ↔ MQTT

Baza SQLite na wolumenie `greenhouse-data` (`/app/data/greenhouse.db`), tryb **WAL** włączany przy starcie aplikacji.

## Pierwsze uruchomienie

1. Sklonuj repozytorium na Pi.
2. Ustal ścieżkę urządzenia koordynatora na hoście, np.:
   ```bash
   ls -la /dev/ttyUSB* /dev/ttyACM*
   ```
3. Utwórz plik `.env` w katalogu projektu (opcjonalnie), np.:
   ```env
   ZIGBEE_DEVICE=/dev/ttyUSBZigbee
   ```
   Domyślnie compose używa `/dev/ttyUSBZigbee` → mapowane na `/dev/ttyUSBZigbee` w kontenerze (zgodnie z typowym `serial.port` w `configuration.yaml`).

4. **Zigbee2MQTT**: pierwszy start tworzy dane w wolumenie `zigbee2mqtt-data`. Jeśli masz już działającą konfigurację, możesz zamontować katalog z `configuration.yaml` zamiast czystego wolumenu — wtedy upewnij się, że `mqtt.server` wskazuje na `mqtt://mosquitto:1883`.

5. Build i start:
   ```bash
   docker compose build
   docker compose up -d
   ```

6. W przeglądarce (telefon w sieci Pi): `http://<adres_IP_malinki>:5000`

## Build z komputera (ARM64)

```bash
docker buildx build --platform linux/arm64 -t greenhouse-api:local -f Dockerfile .
```

## Zmiana schematu bazy

Obecnie używane jest `EnsureCreated`. Po zmianie encji usuń plik `greenhouse.db` na wolumenie lub przygotuj migracje EF (backlog).

## Zmienne środowiskowe aplikacji

| Zmienna | Opis |
|---------|------|
| `Infrastructure__DatabasePath` | Ścieżka do pliku SQLite (w compose: `/app/data/greenhouse.db`) |
| `Mqtt__Enabled` | `true`/`false` — wyłącza subskrypcję MQTT |
| `Mqtt__Host` | Host brokera (w sieci compose: `mosquitto`) |
| `Mqtt__Port` | Port (1883) |
| `Mqtt__TopicFilter` | Filtr subskrypcji (domyślnie `zigbee2mqtt/#`) |

## Lokalny development (bez Dockera)

- `Greenhouse.Api`: w `appsettings.Development.json` domyślnie `Mqtt:Enabled: false` (brak spamu logów bez brokera). Włącz `true`, gdy Mosquitto działa lokalnie.
- Opcjonalnie: osobny proces `Greenhouse.Workers` z MQTT — nadal dostępny w solution; w produkcji Docker używa tylko API.
