# Logowanie i diagnostyka

## Poziomy (krótko)

| Poziom | Kiedy używać w kodzie | Typowe użycie w Greenhouse |
|--------|------------------------|----------------------------|
| **Trace** | Bardzo szczegóły (np. każda wiadomość) | MQTT / ingest — tylko na czas śledzenia pojedynczych wiadomości |
| **Debug** | Szczegóły pomocnicze przy diagnozie | Pełna linia agregacji dashboardu na nawę (`GetDashboardQueryService`) |
| **Information** | Zdarzenia biznesowe / stan | Sucho, za mokro, duży rozstrzał czujników |
| **Warning** | Coś wymaga uwagi operatora | Sprzeczne odczyty (sucho + za mokro jednocześnie) |
| **Error** | Błąd obsłużony, usługa działa dalej | — |
| **Critical** | Awaria krytyczna | — |

Domyślnie w `appsettings` / Docker często ustawione jest **Default: Information**, więc **Debug** i **Trace** nie trafiają do konsoli, dopóki nie podniesiesz poziomu dla wybranego namespace’u.

## Dashboard — agregacja wilgotności (`GetDashboardQueryService`)

Kategoria loggera: `Greenhouse.Application.Nawy.GetDashboardQueryService` (pełna nazwa typu dla `Logging:LogLevel`).

| Poziom | Kiedy | Przykładowa treść (skrót) |
|--------|--------|---------------------------|
| **Debug** | Każda aktywna nawa z czujnikami | `Dashboard nawa=… status=… czujniki=… zWilgotnoscia=… min=… max=… rozstrzal=… progi …` |
| **Information** | Status **Sucho**, **Za mokro**, **Rozstrzał** | Osobna linia z kontekstem (nawa, min/max, próg lub rozstrzał) |
| **Warning** | Status **Sprzeczne czujniki** (`Conflict`) | Min/max vs progi — wymaga weryfikacji czujników / kalibracji |

### Włączenie szczegółowych logów dashboardu (lokalnie)

W `src/Greenhouse.Api/appsettings.Development.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Greenhouse.Application.Nawy.GetDashboardQueryService": "Debug"
  }
}
```

### Docker Compose (tymczasowo)

```yaml
environment:
  - Logging__LogLevel__Greenhouse.Application.Nawy.GetDashboardQueryService=Debug
```

Albo cały prefiks aplikacji (dużo logów):

```yaml
  - Logging__LogLevel__Greenhouse=Debug
```

## Powiązane

- MQTT, ingest, EF — sekcja **Diagnostyka** w [DEPLOYMENT.md](DEPLOYMENT.md).
