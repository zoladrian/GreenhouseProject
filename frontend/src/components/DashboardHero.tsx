import { BrandedImg } from './BrandedImg';

/** Baner z petuniami + „KWIATY POLSKIE”; potem poprzednie zdjęcie szklarni (to samo co tło pulpitu), na końcu SVG. */
const heroPng = '/images/kwiaty-polskie-petunias-banner.png';
const heroFallback = '/images/kwiaty-polskie-hero.png';
const heroSvgFallback = '/images/kwiaty-polskie-greenhouse.svg';
const logoPng = '/images/kwiaty-polskie-logo.png';
const logoFallback = '/images/kwiaty-polskie-logo.svg';

export function DashboardHero() {
  return (
    <section className="dashboard-hero" aria-labelledby="dashboard-hero-title">
      <div className="dashboard-hero__image-wrap">
        <BrandedImg
          className="dashboard-hero__image"
          src={heroPng}
          fallbackSrc={heroFallback}
          fallbackSrc2={heroSvgFallback}
          alt="Kwiaty Polskie — petunie"
          decoding="async"
        />
        <div className="dashboard-hero__overlay" aria-hidden="true" />
        <p id="dashboard-hero-title" className="dashboard-hero__tagline">
          Monitoring wilgotności gleby
        </p>
      </div>
      <div className="dashboard-hero__brand card-surface">
        <BrandedImg
          className="dashboard-hero__logo"
          src={logoPng}
          fallbackSrc={logoFallback}
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
