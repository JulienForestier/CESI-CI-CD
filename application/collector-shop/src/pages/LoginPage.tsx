import { useEffect } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function LoginPage() {
  const { login, user, isLoading } = useAuth()

  // login() renvoie ici (returnUrl = chemin courant) une fois le flow OIDC terminé — si on
  // redéclenchait login() inconditionnellement à chaque montage, un utilisateur déjà authentifié
  // atterrissant sur /connexion relancerait indéfiniment le flow (boucle de rechargement infinie).
  useEffect(() => {
    if (!isLoading && !user) {
      login()
    }
  }, [login, user, isLoading])

  if (user) {
    return <Navigate to="/" replace />
  }

  return (
    <div className="mx-auto max-w-md rounded-2xl border-[1.5px] border-ink/15 bg-card p-10 text-center">
      <span
        role="status"
        aria-label="Redirection en cours"
        className="mx-auto mb-5 block h-11 w-11 animate-spin rounded-full border-[3px] border-ink/15 border-t-ink"
      />
      <h1 className="font-display text-3xl">Bon retour parmi nous</h1>
      <p className="mt-1.5 mb-7 font-ui text-sm text-brown-2">Redirection vers la connexion…</p>

      <button
        type="button"
        onClick={login}
        className="rounded-xl bg-burnt px-6 py-3.5 font-ui text-[15px] font-bold text-surface shadow-[4px_4px_0_#29211b] transition"
      >
        Continuer
      </button>
    </div>
  )
}
