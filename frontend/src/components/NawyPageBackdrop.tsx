/** Tło listy / szczegółu naw: dwa pliki w naturalnych proporcjach (bez rozciągania jak przy cover). */
export function NawyPageBackdrop() {
  return (
    <div className="nawy-page__backdrop" aria-hidden>
      <div className="nawy-page__photos">
        <img
          src="/images/nawy-aisle-bg.png"
          alt=""
          className="nawy-page__photo"
          decoding="async"
        />
        <img
          src="/images/nawy-beds-bg.png"
          alt=""
          className="nawy-page__photo"
          decoding="async"
        />
      </div>
      <div className="nawy-page__scrim" />
    </div>
  );
}
