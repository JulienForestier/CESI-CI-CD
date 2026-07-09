import { useEffect } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function RegisterPage() {
  const { register, user, isLoading } = useAuth()

  // Même garde que LoginPage : register() (alias de login()) renvoie ici une fois le flow OIDC
  // terminé — sans cette garde, un utilisateur déjà authentifié relancerait indéfiniment le flow.
  useEffect(() => {
    if (!isLoading && !user) {
      register()
    }
  }, [register, user, isLoading])

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
      <h1 className="font-display text-3xl">Créer un compte</h1>
      <p className="mt-1.5 mb-7 font-ui text-sm text-brown-2">Redirection vers la création de compte…</p>

      <button
        type="button"
        onClick={register}
        className="rounded-xl bg-burnt px-6 py-3.5 font-ui text-[15px] font-bold text-surface shadow-[4px_4px_0_#29211b] transition"
      >
        Continuer
      </button>
    </div>
  )
}
