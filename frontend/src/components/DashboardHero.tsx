/**
 * Baner: zdjęcie szklarni + pas z logo.
 * Aby użyć własnego JPG, dodaj plik do public/images i ustaw heroImageSrc poniżej.
 */
const heroImageSrc = '/images/kwiaty-polskie-greenhouse.svg';

export function DashboardHero() {
  return (
    <section className="dashboard-hero" aria-labelledby="dashboard-hero-title">
      <div className="dashboard-hero__image-wrap">
        <img
          className="dashboard-hero__image"
          src={heroImageSrc}
          alt="Szklarnia Kwiaty Polskie — widok ogrodowy"
          width={800}
          height={320}
          decoding="async"
        />
        <div className="dashboard-hero__overlay" aria-hidden="true" />
        <p id="dashboard-hero-title" className="dashboard-hero__tagline">
          Monitoring wilgotności gleby
        </p>
      </div>
      <div className="dashboard-hero__brand card-surface">
        <img
          className="dashboard-hero__logo"
          src="/images/kwiaty-polskie-logo.svg"
          alt="Logo Kwiaty Polskie"
          width={72}
          height={72}
          decoding="async"
        />
        <div className="dashboard-hero__brand-text">
          <span className="dashboard-hero__brand-name">Kwiaty Polskie</span>
          <span className="dashboard-hero__brand-sub">Szklarnia</span>
        </div>
      </div>
    </section>
  );
}
