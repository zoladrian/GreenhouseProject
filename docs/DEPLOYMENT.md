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
2. Ustal ścieżkę urządzenia koordynatora na hoście:
   ```bash
   ls -la /dev/ttyUSB* /dev/ttyACM* 2>/dev/null
   ls -la /dev/serial/by-id/
   ```
   Wiele dongli na Pi pokazuje się jako **`/dev/ttyACM0`** lub **`/dev/ttyUSB0`**. Ścieżka **`/dev/ttyUSBZigbee`** to zwykle **własny symlink udev** — jeśli go nie tworzyłeś, **nie istnieje** i Docker zgłosi błąd.
3. Utwórz plik **`.env`** w katalogu projektu (zalecane), np.:
   ```bash
   cp .env.example .env
   nano .env   # ustaw ZIGBEE_DEVICE= na to, co wyszło w kroku 2
   ```
   Domyślnie compose używa **`/dev/ttyACM0`** → w kontenerze zawsze **`/dev/ttyUSBZigbee`** (tak powinno być w `configuration.yaml` Zigbee2MQTT: `serial.port: /dev/ttyUSBZigbee`).

4. **Zigbee2MQTT**: pierwszy start tworzy dane w wolumenie `zigbee2mqtt-data`. W `docker-compose.yml` są m.in. **`ZIGBEE2MQTT_CONFIG_MQTT_SERVER`**, **`ZIGBEE2MQTT_CONFIG_SERIAL_PORT`** oraz **`ZIGBEE2MQTT_CONFIG_FRONTEND_ENABLED=true`** (bez tego po onboardingu UI bywa wyłączone i z komputera widać **ERR_CONNECTION_REFUSED** na `:8080` mimo mapowania portu). Interfejs: **`http://<IP_malinki>:8080`**.

5. Build i start:
   ```bash
   docker compose build
   docker compose up -d
   ```

6. W przeglądarce (telefon w sieci Pi): `http://<adres_IP_malinki>:5000`

## Panel na ekranie podłączonym do Malinki

Interfejs jest serwowany z kontenera **`greenhouse-api`** na porcie **5000** mapowanym na **host** (`5000:5000` w compose). Z samego systemu na Malince (monitor + Raspberry Pi OS **z pulpitem**) otwórz:

- **`http://127.0.0.1:5000`** lub **`http://localhost:5000`**

To ten sam frontend co z telefonu — bez dodatkowych kontenerów.

### Pełny ekran (kiosk) po starcie

1. Zainstaluj Chromium (Bookworm / nowsze często mają pakiet `chromium`):
   ```bash
   sudo apt update && sudo apt install -y chromium curl
   ```
2. Nadaj prawa wykonywania i test ręczny:
   ```bash
   cd /ścieżka/do/GreenhouseProject
   chmod +x scripts/raspberry-pi/greenhouse-kiosk.sh scripts/raspberry-pi/install-kiosk-autostart.sh
   ./scripts/raspberry-pi/greenhouse-kiosk.sh
   ```
   Skrypt **czeka**, aż `http://127.0.0.1:5000` odpowie (np. po `docker compose up -d`), potem uruchamia Chromium w trybie **kiosk** (z wyłączonym cache HTTP, żeby łatwiej łapać nowe assety).

3. **Autostart po zalogowaniu na pulpit** (zalecane — systemd użytkownika):
   ```bash
   ./scripts/raspberry-pi/install-kiosk-autostart.sh
   systemctl --user start greenhouse-kiosk.service
   ```
   Usługa startuje wraz z **sesją graficzną** (`graphical-session.target`). **Włącz automatyczne logowanie** użytkownika z pulpitem (np. *Raspberry Pi Configuration* / *raspi-config*) — inaczej po reboocie trzeba ręcznie się zalogować, żeby kiosk wystartował.

   Alternatywa bez systemd: plik [`greenhouse-kiosk.desktop`](../scripts/raspberry-pi/greenhouse-kiosk.desktop) do `~/.config/autostart/` (popraw ścieżkę `Exec=`).

### Aktualizacja kodu — odświeżenie panelu

- Przy każdym **`docker compose build`** obraz dostaje **nowy** identyfikator wdrożenia (plik `deploy-id` w kontenerze).
- Frontend w produkcji co ok. **45 s** wywołuje `GET /api/meta/deploy` i porównuje `deployId`. Gdy po **`docker compose up -d`** serwer zwraca **inny** identyfikator, strona robi **`location.reload()`** — bez ręcznego odświeżania na ekranie.
- Na dole UI (pod nawigacją) wyświetla się pasek **„Wersja serwera”** z tym samym identyfikatorem (łatwo zobaczyć, czy wdrożenie doszło).

**Wskazówki:** wyłączenie wygaszacza / blankowania ekranu: **Raspberry Pi Configuration** → **Display** lub ustawienia energii. Jeśli przy pierwszym starcie Chromium pojawi się **nowy keyring** (hasło pierścienia) — na kiosku zwykle ustaw **puste hasło** albo użyj aktualnego `greenhouse-kiosk.sh` (flaga `--password-store=basic`, żeby Chromium nie pytał o keyring). Szczegóły: [`scripts/raspberry-pi/README.md`](../scripts/raspberry-pi/README.md).

## PWA — wygląd jak aplikacja (bez sklepu Play)

Frontend ma [`manifest.json`](../frontend/public/manifest.json) (`display: standalone`), meta **Apple Web App** oraz minimalny **service worker** [`sw.js`](../frontend/public/sw.js) (rejestracja tylko w buildzie produkcyjnym).

- **iPhone / iPad (Safari):** Udostępnij → **Dodaj do ekranu głównego** — otwarcie bez paska adresu, obsługa **safe area** (notch) w CSS.
- **Android (Chrome):** menu → **Zainstaluj aplikację** / **Dodaj do ekranu głównego** (dostępność zależy od wersji i **HTTP** w LAN — pełna instalacja bywa łatwiejsza pod HTTPS).
- **QR na obudowie** z `http://<IP>:5000` nadal najszybszy pierwszy kontakt.

## Captive portal (opcjonalnie)

Żeby po połączeniu z Wi‑Fi szklarni **częściej** wyskakiwało okno z panelem (jak w publicznych sieciach), skonfiguruj **DNS + HTTP:80** na hoście Pi — opis i przykłady: [**CAPTIVE-PORTAL.md**](CAPTIVE-PORTAL.md).

## Czujniki w aplikacji

- Odczyty trafiają z **Zigbee2MQTT** do **Mosquitto**; kontener **greenhouse-api** subskrybuje `zigbee2mqtt/#` i zapisuje tylko tematy **`zigbee2mqtt/<nazwa_przyjazna>`** z **JSON stanu** (pomijane są m.in. `.../availability`, `.../set`).
- **Adres IEEE w JSON:** po zmianie *friendly name* w Z2M zmienia się tylko **temat** MQTT — bez IEEE w payloadzie aplikacja traktowałaby urządzenie jako **nowy** czujnik (duplikaty). Aktualny `docker-compose` włącza **`mqtt.include_device_information`** w Z2M; API **preferuje IEEE** z pola `ieee_address` / `device.ieeeAddr` jako stabilny `ExternalId` (scalanie z wcześniejszym rekordem `0xffff…` z topicu).
- Zakładka **Sensory** pokazuje zarejestrowane czujniki; **Przypisanie do nawy** powoduje, że wilgotność i wykresy pojawiają się na **Dashboardzie** i w szczegółach nawy.
- **Stare duplikaty** (np. wpis po nazwie + wpis po IEEE) po wdrożeniu możesz zostawić lub usunąć ręcznie z bazy — aktywne odczyty będą spinać się z jednym rekordem po IEEE.
- Grafiki marki: [`frontend/public/images/README.md`](../frontend/public/images/README.md) — domyślnie `kwiaty-polskie-hero.png` i `kwiaty-polskie-logo.png` (w repozytorium).

## Zigbee2MQTT — błąd `no such file ... /dev/ttyUSBZigbee`

Docker sprawdza **ścieżkę po lewej** w mapowaniu `devices` (to jest plik urządzenia **na hoście**). Komunikat w stylu *„adding custom device \"/dev/ttyUSBZigbee\": no such file\"* oznacza, że **na Pi nie ma takiego pliku** — nazwa `/dev/ttyUSBZigbee` bywa tylko wtedy, gdy sam dodasz **symlink udev**; typowe dongle to **`/dev/ttyACM0`** lub **`/dev/ttyUSB0`**.

**Naprawa:**

1. Sprawdź urządzenia: `ls -la /dev/ttyUSB* /dev/ttyACM* /dev/serial/by-id/`
2. Utwórz `.env` (wzór: [`.env.example`](../.env.example)):
   ```env
   ZIGBEE_DEVICE=/dev/ttyACM0
   ```
   (albo `/dev/ttyUSB0` albo stabilna ścieżka z `/dev/serial/by-id/...`)
3. `docker compose up -d`

W kontenerze port szeregowy ma nadal być **`/dev/ttyUSBZigbee`** (tak ustawia compose — zgodnie z `serial.port` w `configuration.yaml` Zigbee2MQTT).

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
| `ZIGBEE_DEVICE` | (Plik **`.env`** obok `docker-compose.yml`, nie kontener API) Ścieżka do dongla Zigbee **na hoście**. Domyślnie w compose: `/dev/ttyACM0`. Patrz [`.env.example`](../.env.example). |
| `Infrastructure__DatabasePath` | Ścieżka do pliku SQLite (w compose: `/app/data/greenhouse.db`) |
| `Mqtt__Enabled` | `true`/`false` — wyłącza subskrypcję MQTT |
| `Mqtt__Host` | Host brokera (w sieci compose: `mosquitto`) |
| `Mqtt__Port` | Port (1883) |
| `Mqtt__TopicFilter` | Filtr subskrypcji (domyślnie `zigbee2mqtt/#`) |
| `Voice__GreetingLeadin` | Początek wypowiedzi głosowej (np. „Dzień dobry Panie Czesławie”) |
| `Voice__TimeZoneId` | Strefa do „północy” przy średnich dziennych (domyślnie `Europe/Warsaw`) |
| `GREENHOUSE_DEPLOY_ID` | (Opcjonalnie) Nadpisuje identyfikator z obrazu; **zwykle nie ustawiaj** — wtedy każdy build Dockera ma unikalny `deploy-id` i panel sam się przeładuje po aktualizacji. |

**Raport głosowy (offline):** na dashboardzie przycisk „Odczytaj dzienny raport” wywołuje `GET /api/voice/daily-report` — średnie wilgotności i temperatury z czujników przypisanych do nawy od **lokalnej północy** w `Voice:TimeZoneId`. Bez internetu — bez pogody z sieci.

## Diagnostyka: `wiadomości z brokera=0` / brak ruchu na Mosquitto

Skoro **API jest połączone z brokerem** (`MQTT połączono`), a **`wiadomości z brokera=0`** i `mosquitto_sub -t 'zigbee2mqtt/#'` **nic nie pokazuje**, to **Zigbee2MQTT nie publikuje** do tego samego Mosquitto (najczęściej w `configuration.yaml` jest **`mqtt://localhost`** — w kontenerze Z2M to **nie** usługa `mosquitto` w sieci Dockera).

**Po `git pull`:** w aktualnym compose są zmienne `ZIGBEE2MQTT_CONFIG_MQTT_SERVER=mqtt://mosquitto:1883` oraz mapowanie **8080** — zrób:

```bash
cd ~/GreenhouseProject && git pull
docker compose up -d
docker compose logs zigbee2mqtt --tail 40
```

Szukaj w logach Z2M linii o połączeniu z MQTT / błędach. Potem:

```bash
timeout 20 docker exec greenhouseproject-mosquitto-1 mosquitto_sub -h localhost -t 'zigbee2mqtt/#' -v
```

(Po sparowaniu urządzeń powinny pojawić się komunikaty.)

**Konfiguracja w wolumenie (weryfikacja):**

```bash
docker compose exec zigbee2mqtt cat /app/data/configuration.yaml
```

W sekcji `mqtt:` powinno być **`server: mqtt://mosquitto:1883`** (lub równoważne), nie `localhost`.

## Wi‑Fi „szklarni” nie widać na telefonie

To **osobna** rzecz od Dockera: sieć AP (`hostapd` / hotspot) musi być włączona i skonfigurowana **na hoście** Raspberry Pi (np. *Raspberry Pi Connect* / ręczny AP). Sam stack **GreenhouseProject** jej nie tworzy — wskazówki: [**CAPTIVE-PORTAL.md**](CAPTIVE-PORTAL.md) oraz ustawienia Wi‑Fi w *Raspberry Pi OS*. Panel nadal działa po **LAN** pod `http://<IP_malinki>:5000`.

## Diagnostyka: czujniki nie pojawiają się w aplikacji

### Logi kontenera API

```bash
docker logs greenhouseproject-greenhouse-api-1 --tail 200
```

**Co szukać (poziomy domyślne — Information i wyżej):**

| Komunikat | Znaczenie |
|-----------|-----------|
| `MQTT połączono: ... filtr=zigbee2mqtt/#` | Broker osiągalny, subskrypcja ustawiona. |
| `MQTT podsumowanie (od startu procesu): wiadomości z brokera=…` | Co ~2 min — liczniki od startu API: **broker** (wszystkie wiadomości z MQTT), **pominięte tematy** (nie pasują do odczytu czujnika), **zapisane odczyty** (wiersze w bazie). **Broker=0** → brak ruchu MQTT do API (sieć/subskrypcja/broker). **Broker>0, zapisane=0, pominięcia≈broker** → prawie wszystko odfiltrowane (np. tylko `.../availability` lub tryb attribute). **Zapisane>0** → ingest działa; jeśli UI puste — sprawdź przypisanie czujnika do nawy. |
| `MQTT rozłączono` | Utrata sesji — sprawdź Mosquitto i sieć Docker. |
| `Zarejestrowano nowy czujnik z MQTT` | Pierwszy poprawny odczyt z tematu `zigbee2mqtt/<nazwa>` — czujnik powinien być w zakładce **Sensory**. |
| `MQTT ingest — nieoczekiwany błąd` | Wyjątek przy zapisie (np. baza); pełny stack w logu. |

**Uwaga:** w starszych obrazach mogła być angielska linia `MQTT connected to …` zamiast polskiego `MQTT połączono`. Po `docker compose build && up` powinien być aktualny komunikat + podsumowanie co ~2 min.

Domyślnie **nie** logujemy każdego zapytania SQL EF Core (`Microsoft.EntityFrameworkCore.Database.Command` = Warning), żeby `docker logs` nie były zalane `Executed DbCommand`. Aby je włączyć tymczasowo, ustaw w compose: `Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command=Information`.

**Szczegóły pominiętych tematów i każdy zapis odczytu** są na poziomie **Trace**; **podejrzenie trybu attribute / złego JSON** — **Debug** (tymczasowo, przez compose):

```yaml
environment:
  - Logging__LogLevel__Greenhouse.Application.Ingestion=Debug
  - Logging__LogLevel__Greenhouse.Api.Mqtt=Debug
```

Albo dla całego prefiksu:

```yaml
  - Logging__LogLevel__Greenhouse=Debug
```

(Wiele logów — tylko na czas diagnozy.)

**Dashboard (wilgotność wielu czujników, statusy Conflict / rozstrzał):** pełna linia na nawę jest na **Debug**; ostrzeżenia biznesowe na **Information** / **Warning**. Szczegóły: [**LOGGING.md**](LOGGING.md).

### Typowe przyczyny

1. **Brak linii `MQTT połączono`** — zły `Mqtt__Host` / port, broker nie działa, kontener API nie w tej samej sieci co `mosquitto`.
2. **Połączenie jest, brak `Zarejestrowano nowy czujnik`** — Z2M publikuje tylko podtematy (`.../availability`) albo **tryb attribute** (wtedy w **Debug** widać: `podtemat_lub_tryb_attribute` w Trace lub `JSON bez pól soil_moisture...`). Ustaw w Z2M publikację stanu jako **jeden temat** `zigbee2mqtt/<nazwa>` z JSON.
3. **Czujnik w aplikacji jest, dashboard pusty** — czujnik **nieprzypisany do nawy** (zakładka **Sensory** → przypisanie).

### Szybki test brokera (na Pi)

```bash
docker exec -it greenhouseproject-mosquitto-1 mosquitto_sub -h localhost -t 'zigbee2mqtt/#' -v -C 3
```

Powinny pojawić się tematy z **jednym** segmentem po `zigbee2mqtt/` i **JSON** stanu.

## Lokalny development (bez Dockera)

- `Greenhouse.Api`: w `appsettings.Development.json` domyślnie `Mqtt:Enabled: false` (brak spamu logów bez brokera). Włącz `true`, gdy Mosquitto działa lokalnie.
- Opcjonalnie: osobny proces `Greenhouse.Workers` z MQTT — nadal dostępny w solution; w produkcji Docker używa tylko API.
