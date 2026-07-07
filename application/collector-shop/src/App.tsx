import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { ProtectedRoute } from './components/ProtectedRoute'
import { CatalogPage } from './pages/CatalogPage'
import { ListingDetailPage } from './pages/ListingDetailPage'
import { LoginPage } from './pages/LoginPage'
import { MyListingsPage } from './pages/MyListingsPage'
import { NewListingPage } from './pages/NewListingPage'
import { RegisterPage } from './pages/RegisterPage'

function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<CatalogPage />} />
        <Route path="annonces/:id" element={<ListingDetailPage />} />
        <Route path="connexion" element={<LoginPage />} />
        <Route path="inscription" element={<RegisterPage />} />
        <Route
          path="annonces/nouvelle"
          element={
            <ProtectedRoute>
              <NewListingPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="mes-annonces"
          element={
            <ProtectedRoute>
              <MyListingsPage />
            </ProtectedRoute>
          }
        />
      </Route>
    </Routes>
  )
}

export default App
