# Grafiki marki

Pliki w tym katalogu trafiają do `wwwroot` (Vite `public/`) i są dostępne pod `/images/...`.

- **`kwiaty-polskie-logo.png`** — logo marki (nagłówek + pas na dashboardzie). Przy braku pliku UI użyje `kwiaty-polskie-logo.svg`.
- **`kwiaty-polskie-hero.png`** — zdjęcie baneru (ekspozycja / szklarnia). Przy braku pliku UI użyje `kwiaty-polskie-greenhouse.svg`.
- Pliki `.svg` — lekkie placeholdery z repozytorium.

Aby podmienić grafiki: zastąp powyższe PNG własnymi plikami **o tych samych nazwach** i zrób ponowny build frontu / obrazu Docker.
