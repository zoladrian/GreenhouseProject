# Wykrywanie podlania i heurystyka deszczu

## Wykrywanie skoku (podlanie / zawilgocenie)

`WateringDetector` szuka **nagłego** wzrostu wilgotności między kolejnymi odczytami: domyślnie **Δ ≥ 5%** w oknie **≤ 30 min**.

## Epizody na wykresie

API `/api/chart/watering-events` zwraca **scalone epizody** (nie osobno każdy czujnik), żeby na wykresie była **jedna pionowa linia** na zdarzenie.

- **Pionowe `markLine`** na wykresie wilgotności: zielony = prawdopodobne podlanie, niebieski = możliwy deszcz.
- Lista pod wykresem na stronie nawy z etykietą i liczbą czujników biorących udział w epizodzie.

## Heurystyka: deszcz vs podlanie

Bez danych pogodowych **nie da się** rozróżnić z pewnością. Używana jest prosta reguła **w obrębie jednej nawy**:

| Warunek | `inferredKind` w API |
|--------|------------------------|
| W tym samym epizodzie czasowym skok widzi **tylko jeden** czujnik | `likelyManual` |
| W epizodzie uczestniczą **≥ 2 różne czujniki** (skoki w ciągu **45 min** od siebie) | `likelyRain` |

**Uzasadnienie:** deszcz / rosa zwykle podnoszą wilgotność w kilku punktach szklarni w podobnym czasie; podlewanie konewką częściej daje wyraźny skok na **jednym** czujniku.

**Ograniczenia:** podlanie dwóch skrzynek jednocześnie może zostać błędnie oznaczone jako „deszcz?”. Jedna doniczka pod dachem przy deszczu — jako „podlanie”.

## Możliwe rozszerzenia (niezaimplementowane)

- Korelacja z **temperaturą powietrza** (spadek przy deszczu) — wymaga stabilnych odczytów temp. i kalibracji.
- Integracja **API pogody** (opady mm w czasie epizodu).
- Ręczna etykieta użytkownika („to był deszcz”) do uczenia / korekty.
