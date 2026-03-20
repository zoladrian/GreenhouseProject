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

## Czujniki w aplikacji

- Odczyty trafiają z **Zigbee2MQTT** do **Mosquitto**; kontener **greenhouse-api** subskrybuje `zigbee2mqtt/#` i zapisuje tylko tematy **`zigbee2mqtt/<nazwa_przyjazna>`** z **JSON stanu** (pomijane są m.in. `.../availability`, `.../set`).
- Zakładka **Sensory** pokazuje zarejestrowane czujniki; **Przypisanie do nawy** powoduje, że wilgotność i wykresy pojawiają się na **Dashboardzie** i w szczegółach nawy.
- Grafiki marki: [`frontend/public/images/README.md`](../frontend/public/images/README.md) — domyślnie `kwiaty-polskie-hero.png` i `kwiaty-polskie-logo.png` (w repozytorium).

## Build z komputera (ARM64)

```bash
docker buildx build --platform linux/arm64 -t greenhouse-api:local -f Dockerfile .
```

## Build Docker — błąd MSB3202 (brak .csproj)

Jeśli `docker compose build` kończy się błędem w stylu „project file … Greenhouse.Workers … not found” przy `dotnet restore`, oznacza to stary Dockerfile przywracający **całe solution** przed skopiowaniem `src/`. Aktualny `Dockerfile` w repozytorium robi `dotnet restore` **wyłącznie** na `src/Greenhouse.Api/Greenhouse.Api.csproj`. Zrób `git pull` i buduj ponownie.

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
| `Voice__GreetingLeadin` | Początek wypowiedzi głosowej (np. „Dzień dobry Panie Czesławie”) |
| `Voice__TimeZoneId` | Strefa do „północy” przy średnich dziennych (domyślnie `Europe/Warsaw`) |

**Raport głosowy (offline):** na dashboardzie przycisk „Odczytaj dzienny raport” wywołuje `GET /api/voice/daily-report` — średnie wilgotności i temperatury z czujników przypisanych do nawy od **lokalnej północy** w `Voice:TimeZoneId`. Bez internetu — bez pogody z sieci.

## Lokalny development (bez Dockera)

- `Greenhouse.Api`: w `appsettings.Development.json` domyślnie `Mqtt:Enabled: false` (brak spamu logów bez brokera). Włącz `true`, gdy Mosquitto działa lokalnie.
- Opcjonalnie: osobny proces `Greenhouse.Workers` z MQTT — nadal dostępny w solution; w produkcji Docker używa tylko API.
