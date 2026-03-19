import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { Layout } from './components/Layout';
import { DashboardPage } from './pages/DashboardPage';
import { NawyListPage } from './pages/NawyListPage';
import { NawaDetailPage } from './pages/NawaDetailPage';
import { SensorsPage } from './pages/SensorsPage';
import { HealthPage } from './pages/HealthPage';

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path="/nawy" element={<NawyListPage />} />
          <Route path="/nawy/:id" element={<NawaDetailPage />} />
          <Route path="/sensory" element={<SensorsPage />} />
          <Route path="/zdrowie" element={<HealthPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
