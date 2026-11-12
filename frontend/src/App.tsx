import { BrowserRouter, Navigate, Route, Routes } from 'react-router'
import { RequireAuth } from './auth/RequireAuth'
import { AppLayout } from './components/AppLayout'
import { ConnectionBanner } from './components/ConnectionBanner'
import { ClientDetailPage } from './pages/ClientDetailPage'
import { ClientsPage } from './pages/ClientsPage'
import { EditClientPage } from './pages/EditClientPage'
import { EditLoanPage } from './pages/EditLoanPage'
import { HomePage } from './pages/HomePage'
import { LoanDetailPage } from './pages/LoanDetailPage'
import { LoansPage } from './pages/LoansPage'
import { LoginPage } from './pages/LoginPage'
import { NewClientPage } from './pages/NewClientPage'
import { NewLoanPage } from './pages/NewLoanPage'
import { ProfilePage } from './pages/ProfilePage'
import { StatisticsPage } from './pages/StatisticsPage'

/** The screens of the app and their addresses. */
export default function App() {
  return (
    <BrowserRouter>
      {/* Above every screen, login included. */}
      <ConnectionBanner />

      <Routes>
        <Route path="/login" element={<LoginPage />} />

        {/* Everything inside needs a logged-in user, and shares the bottom navigation bar. */}
        <Route element={<RequireAuth />}>
          <Route element={<AppLayout />}>
            <Route path="/inicio" element={<HomePage />} />
            <Route path="/prestamos" element={<LoansPage />} />
            <Route path="/prestamos/nuevo" element={<NewLoanPage />} />
            <Route path="/prestamos/:id" element={<LoanDetailPage />} />
            <Route path="/prestamos/:id/editar" element={<EditLoanPage />} />
            <Route path="/clientes" element={<ClientsPage />} />
            <Route path="/clientes/nuevo" element={<NewClientPage />} />
            <Route path="/clientes/:id" element={<ClientDetailPage />} />
            <Route path="/clientes/:id/editar" element={<EditClientPage />} />
            <Route path="/estadistica" element={<StatisticsPage />} />
            <Route path="/perfil" element={<ProfilePage />} />
          </Route>
        </Route>

        {/* Any other address goes to the home screen (or to login, if there is no session). */}
        <Route path="*" element={<Navigate to="/inicio" replace />} />
      </Routes>
    </BrowserRouter>
  )
}
