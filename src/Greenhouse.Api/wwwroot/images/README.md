# Grafiki marki

Pliki w tym katalogu trafiają do `wwwroot` (Vite `public/`) i są dostępne pod `/images/...`.

- **`kwiaty-polskie-logo.png`** — logo marki (nagłówek + pas na dashboardzie). Przy braku pliku UI użyje `kwiaty-polskie-logo.svg`.
- **`kwiaty-polskie-hero.png`** — **tło całego pulpitu** (szklarnia / ekspozycja, `background-size: cover`). Powinno zostać w repozytorium.
- **`kwiaty-polskie-petunias-banner.png`** — **górny baner na pulpicie** (petunie + napis KWIATY POLSKIE). Przy braku pliku hero użyje `kwiaty-polskie-hero.png`, potem `kwiaty-polskie-greenhouse.svg`.
- **`nawy-aisle-bg.png`** — lewa połowa tła widoku **Nawy** (alejki / lawenda).
- **`nawy-beds-bg.png`** — prawa połowa tła (grządki z chryzantemami); razem z `nawy-aisle-bg.png` składa się panoramę pod listę i szczegół nawy.
- Pliki `.svg` — lekkie placeholdery z repozytorium.

Na pulpicie i w **Nawy** obrazy są wyświetlane z **`object-fit: contain`** (pełny kadr pliku, bez przycinania jak przy `cover`). Tło pulpitu to osobny `<img>` z `max-width` / `max-height` 100% — nie powiększa się ponad rozdzielczość bitmapy w pikselach.

Aby podmienić grafiki: zastąp powyższe PNG własnymi plikami **o tych samych nazwach** i zrób ponowny build frontu / obrazu Docker.
