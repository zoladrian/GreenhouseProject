# Captive portal — automatyczne wskazanie panelu po połączeniu z Wi‑Fi Pi

Telefony (Android / iOS) po połączeniu z nową siecią sprawdzają, czy mają **dostęp do internetu**, odpytując znane adresy (np. `connectivitycheck.gstatic.com`, `captive.apple.com`). Jeśli zamiast „sukcesu” dostaną **przekierowanie HTTP**, system często pokazuje okno **„Zaloguj się do sieci”** z uproszczoną przeglądarką — tak działa Wi‑Fi w kawiarniach.

Możesz **naśladować** to zachowanie na malince z hotspotem, żeby użytkownik **szybciej trafił** na panel (`http://<IP>:5000`).

## Ograniczenia

- Działa głównie dla testów **HTTP** (port **80**). Część telefonów używa **HTTPS** — bez własnego CA na urządzeniach nie przechwycisz tego czysto.
- **Nie każdy** model pokaże okno automatycznie; czasem trzeba **stuknąć** w powiadomienie o sieci bez internetu.
- Konfiguracja **hotspotu** (`hostapd` / NetworkManager) i **DNS** (`dnsmasq`) jest na **hoście Raspberry Pi OS**, nie w kontenerach z `docker compose` (API nasłuchuje na **5000**).

## Architektura (schemat)

1. **dnsmasq** (lub inny DNS na AP) zwraca **adres IP malinki jako bramy AP** dla wybranych nazw używanych do testów captive.
2. Na malince działa lekki serwer **HTTP na porcie 80** (np. **nginx**), który dla **każdego** żądania odpowiada **`302`** na panel: `http://<IP_AP>:5000/` (dopasuj IP do swojej sieci hotspotu, często `192.168.4.1` przy udostępnianiu połączenia w NM).

Panel dalej serwuje **Docker** (`greenhouse-api` na `:5000`).

## Przykładowe pliki

- [`examples/captive-dnsmasq-address.conf`](examples/captive-dnsmasq-address.conf) — wpisy `address=/.../` (wklej do konfiguracji dnsmasq, **popraw IP**).
- [`examples/captive-nginx-default.conf`](examples/captive-nginx-default.conf) — wirtualny host na porcie 80 z przekierowaniem (**popraw URL** panelu).

### Instalacja nginx na hoście (skrót)

```bash
sudo apt install nginx
sudo cp docs/examples/captive-nginx-default.conf /etc/nginx/sites-available/captive-greenhouse
# Edytuj: return 302 http://TWOJE_IP_AP:5000/;
sudo ln -sf /etc/nginx/sites-available/captive-greenhouse /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t && sudo systemctl reload nginx
```

### dnsmasq

Jeśli używasz własnego `dnsmasq` przy AP, dołącz fragment z pliku `captive-dnsmasq-address.conf` i zrestartuj usługę. **Adres** w `address=/.../IP` musi być IP **interfejsu AP** (ten, który widzą klienci jako bramę).

## Po wdrożeniu

- Użytkownik łączy się z siecią szklarni → po chwili może pojawić się **okno captive** z przekierowaniem na UI.
- Nadal warto mieć **QR** z adresem `http://<IP>:5000` na obudowie (pewny plan B).

## PWA (wygląd aplikacji)

Front ma **manifest** + **service worker** (minimalny): po pierwszym wejściu użytkownik może **dodać skrót do ekranu głównego** — UI otwiera się w trybie **standalone** (bez paska adresu). Szczegóły: sekcja w [DEPLOYMENT.md](DEPLOYMENT.md).

**Uwaga:** pełny „Install” z Chrome na **czystym HTTP** (np. `http://192.168.x.x`) bywa ograniczony; **Safari (iOS)** → „Udostępnij → Dodaj do ekranu głównego” zwykle działa.
