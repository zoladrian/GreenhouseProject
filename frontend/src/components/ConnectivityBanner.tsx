import { useOnline } from '../hooks/useOnline';

/**
 * Czerwony pasek u góry ekranu, gdy przeglądarka nie ma sieci.
 *
 * Po co to istnieje:
 *  - Service worker zwraca z precache TYLKO "shell" (HTML/CSS/JS).
 *    `/api/*` jest celowo NetworkOnly — UI musi pokazać użytkownikowi, że dane są nieświeże.
 *  - Bez tego bannera użytkownik widziałby pustą siatkę naw albo "—" i nie wiedział dlaczego.
 *
 * Render:
 *  - role="status" + aria-live="polite" = czytniki ekranu zauważą zmianę bez przerywania.
 */
export function ConnectivityBanner() {
  const online = useOnline();
  if (online) return null;

  return (
    <div
      role="status"
      aria-live="polite"
      data-testid="connectivity-banner"
      style={{
        position: 'sticky',
        top: 0,
        zIndex: 1000,
        background: '#b91c1c',
        color: '#fff',
        textAlign: 'center',
        padding: '6px 12px',
        fontSize: 13,
        fontWeight: 600,
        boxShadow: '0 1px 3px rgba(0,0,0,.25)',
      }}
    >
      Brak sieci — dane mogą być nieaktualne. Sprawdź połączenie z malinką.
    </div>
  );
}
