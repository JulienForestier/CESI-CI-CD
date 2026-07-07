import { Link, Outlet, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  function handleLogout() {
    logout()
    navigate('/')
  }

  return (
    <div className="min-h-screen bg-card font-body text-ink">
      <header className="border-b border-ink/10 bg-card">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <Link to="/" className="flex items-center gap-2.5">
            <span className="flex h-8 w-8 items-center justify-center rounded-full border-2 border-ink font-display text-base">
              C
            </span>
            <span className="font-display text-xl tracking-tight">
              Collector<span className="text-burnt">.shop</span>
            </span>
          </Link>

          <nav className="flex items-center gap-5 font-ui text-sm">
            <Link to="/" className="font-medium text-ink hover:text-burnt">
              Catalogue
            </Link>

            {user ? (
              <>
                <Link to="/mes-annonces" className="font-medium text-ink hover:text-burnt">
                  Mes annonces
                </Link>
                <Link
                  to="/annonces/nouvelle"
                  className="rounded-full bg-ink px-4 py-2 text-xs font-semibold text-card"
                >
                  Vendre un objet
                </Link>
                <span className="text-brown-2">{user.displayName}</span>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="rounded-full border-[1.5px] border-ink px-4 py-2 text-xs font-semibold text-ink hover:bg-surface"
                >
                  Se déconnecter
                </button>
              </>
            ) : (
              <>
                <Link to="/connexion" className="font-medium text-ink hover:text-burnt">
                  Connexion
                </Link>
                <Link
                  to="/inscription"
                  className="rounded-full bg-ink px-4 py-2 text-xs font-semibold text-card"
                >
                  Vendre un objet
                </Link>
              </>
            )}
          </nav>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-6 py-10">
        <Outlet />
      </main>
    </div>
  )
}
