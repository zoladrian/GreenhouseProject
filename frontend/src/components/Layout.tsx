import { NavLink, Outlet } from 'react-router-dom';
import { BrandedImg } from './BrandedImg';
import { DeployVersionBar } from './DeployVersionBar';

const logoPng = '/images/kwiaty-polskie-logo.png';
const logoFallback = '/images/kwiaty-polskie-logo.svg';

const navItems = [
  { to: '/', label: 'Pulpit' },
  { to: '/nawy', label: 'Nawy' },
  { to: '/sensory', label: 'Sensory' },
  { to: '/zdrowie', label: 'Zdrowie' },
];

export function Layout() {
  return (
    <div className="app-shell">
      <header className="app-header">
        <BrandedImg
          className="app-header__logo"
          src={logoPng}
          fallbackSrc={logoFallback}
          alt=""
          width={40}
          height={40}
          decoding="async"
        />
        <div className="app-header__titles">
          <span className="app-header__brand">Kwiaty Polskie</span>
          <span className="app-header__sub">Szklarnia — monitoring</span>
        </div>
      </header>
      <main className="app-main">
        <Outlet />
      </main>
      <nav className="app-nav">
        {navItems.map((item) => (
          <NavLink key={item.to} to={item.to} className={({ isActive }) => (isActive ? 'active' : '')}>
            {item.label}
          </NavLink>
        ))}
      </nav>
      <DeployVersionBar />
    </div>
  );
}
