/** Tło pulpitu — jedno zdjęcie w proporcjach pliku, bez przycinania cover (max rozmiar = kontener, bez sztucznego powiększania). */
export function DashboardPageBackdrop() {
  return (
    <div className="dashboard-page__bg" aria-hidden>
      <img
        src="/images/kwiaty-polskie-hero.png"
        alt=""
        className="dashboard-page__bg-img"
        decoding="async"
      />
    </div>
  );
}
