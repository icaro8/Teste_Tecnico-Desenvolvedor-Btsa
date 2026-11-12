import { BrowserRouter, Route, Routes } from 'react-router'
import { Layout } from './components/Layout'
import { AccountDetailsPage } from './pages/AccountDetailsPage'
import { AccountsPage } from './pages/AccountsPage'
import { NewAccountPage } from './pages/NewAccountPage'
import { NewTransferPage } from './pages/NewTransferPage'
import { TransferDetailsPage } from './pages/TransferDetailsPage'
import { TransfersPage } from './pages/TransfersPage'

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route element={<Layout />}>
          <Route index element={<AccountsPage />} />
          <Route path="contas/nova" element={<NewAccountPage />} />
          <Route path="contas/:id" element={<AccountDetailsPage />} />
          <Route path="transferencias" element={<TransfersPage />} />
          <Route path="transferencias/nova" element={<NewTransferPage />} />
          <Route path="transferencias/:id" element={<TransferDetailsPage />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
