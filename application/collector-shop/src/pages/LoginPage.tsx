import { useEffect } from 'react'
import { useAuth } from '../context/AuthContext'

export function LoginPage() {
  const { login } = useAuth()

  useEffect(() => {
    login()
  }, [login])

  return (
    <div className="mx-auto max-w-md rounded-2xl border-[1.5px] border-ink/15 bg-card p-10 text-center">
      <span className="mx-auto mb-5 flex h-11 w-11 items-center justify-center rounded-full border-2 border-ink font-display text-xl">
        C
      </span>
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
