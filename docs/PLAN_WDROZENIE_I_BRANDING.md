# Plan: wdrożenie na Raspberry Pi + branding „Kwiaty Polskie”

Ten dokument jest zsynchronizowany z planem w Cursor (`branding_ui_kwiaty_polskie`).  
**Kolejność:** najpierw stabilny runtime (Faza A), potem UI (Faza B).

---

## Faza A — uruchomienie na Pi (Docker, MQTT, SQLite)

1. **`.dockerignore`** — wykluczyć `bin/`, `obj/`, `node_modules`, `.git`, `dist`, `*.db` z kontekstu buildu.
2. **SQLite WAL** — `PRAGMA journal_mode=WAL` przy starcie (Infrastructure / DbContext), bezpieczniejszy dostęp do pliku bazy.
3. **Jeden proces .NET** — przenieść subskrypcję MQTT z `Greenhouse.Workers` do **hosted service w `Greenhouse.Api`** (ta sama logika co `Worker.cs`, `IServiceScopeFactory`, `MqttOptions`).  
   - W compose: **usunąć** serwis `greenhouse-worker`.  
   - Opcja `Mqtt:Enabled` w appsettings (np. `false` w testach API).
4. **`Dockerfile`** — publikować tylko API (+ `wwwroot` z frontu); dodać zależność MQTTnet do Api jeśli potrzeba.
5. **`docker-compose.yml`** — jeden kontener aplikacji; Mosquitto; Zigbee2MQTT z **konfigurowalnym urządzeniem** (np. `/dev/ttyUSBZigbee` zgodnie z Twoją konfiguracją, nie sztywne `ttyACM0`); wolumen na dane Z2M z `configuration.yaml`.
6. **`docs/DEPLOYMENT.md`** — ARM64 build, pierwsze uruchomienie, port 5000, odniesienie do Wi‑Fi AP (hostapd/dnsmasq poza Dockerem).
7. **Weryfikacja** — `dotnet build`, `dotnet test`; w factory testów API ustawić `Mqtt:Enabled=false`.

---

## Faza B — branding UI (Kwiaty Polskie)

1. Assety w **`frontend/public/images/`** (logo + zdjęcie szklarni), skompresowane pod mobilny LCP.
2. **`frontend/src/theme.css`** — tokeny: `#45a249`, `#d32f2f`, powierzchnie, tekst.
3. **`DashboardHero.tsx`** — hero (zdjęcie + overlay) + pas z logo; wpięcie w `DashboardPage`.
4. **`Layout.tsx`** — logo w headerze, kolory z tokenów; bottom nav i przyciski spójne z motywem.
5. **`manifest.json` / `index.html`** — `theme-color`, tytuł.
6. **`npm run build`** + smoke pod `wwwroot` API.

---

## Checklist todo (implementacja)

- [x] `.dockerignore`
- [x] SQLite WAL
- [x] MQTT jako `IHostedService` w Api + compose bez workera
- [x] Dockerfile + docker-compose (ZIGBEE_DEVICE, volume Z2M)
- [x] `docs/DEPLOYMENT.md`
- [x] Testy: `Mqtt:Enabled=false` w WebApplicationFactory + test braku `MqttIngestionHostedService`
- [x] Assety SVG + `theme.css` + `DashboardHero` + `Layout` + manifest / favicon

---

## Ryzyka

| Problem | Działanie |
|---------|-----------|
| Dwa procesy na jednym SQLite | Eliminacja drugiego kontenera .NET + WAL |
| Zła ścieżka koordynatora | Zmienna środowiskowa + dokumentacja |
| MQTT w testach integracyjnych | Wyłączenie hosted service w konfiguracji testowej |
