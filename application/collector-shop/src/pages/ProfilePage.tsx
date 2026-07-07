import { useState, type FormEvent } from 'react'
import { ApiError } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { useMyListings } from '../hooks/useCatalog'
import { useFavoriteListings } from '../hooks/useFavorites'
import { useProfile, useUpdateDisplayName } from '../hooks/useProfile'

const dateFormatter = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'long' })

export function ProfilePage() {
  const { logout, updateDisplayName: syncDisplayName } = useAuth()
  const profileQuery = useProfile()
  const listingsQuery = useMyListings()
  const favoritesQuery = useFavoriteListings()
  const updateDisplayName = useUpdateDisplayName()

  const [draftName, setDraftName] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const profile = profileQuery.data
  const displayName = draftName ?? profile?.displayName ?? ''

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setMessage(null)
    try {
      await updateDisplayName.mutateAsync(displayName)
      syncDisplayName(displayName)
      setMessage('Votre pseudo a été mis à jour.')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Impossible de mettre à jour votre pseudo.")
    }
  }

  if (profileQuery.isPending) {
    return <p className="font-ui text-brown-2">Chargement…</p>
  }

  if (profileQuery.isError || !profile) {
    return <p className="font-ui text-burnt">Impossible de charger votre profil pour le moment.</p>
  }

  return (
    <div>
      <h1 className="mb-8 font-display text-3xl">Mon profil</h1>

      <div className="flex flex-col gap-8 md:flex-row">
        <div className="flex-1 rounded-xl border-[1.5px] border-ink/15 bg-surface p-6">
          <div className="flex items-center gap-4">
            <span className="flex h-16 w-16 shrink-0 items-center justify-center rounded-full border-2 border-ink font-display text-2xl">
              {profile.displayName.charAt(0).toUpperCase()}
            </span>
            <div>
              <div className="font-display text-2xl">{profile.displayName}</div>
              <div className="font-ui text-sm text-brown-2">Acheteur · Vendeur</div>
              {profile.isAdmin && (
                <span className="mt-1 inline-block rounded-full bg-shipping px-2 py-0.5 font-ui text-[11px] font-semibold text-burnt">
                  Administrateur
                </span>
              )}
            </div>
          </div>

          <dl className="mt-6 flex flex-col gap-2 font-ui text-sm">
            <div className="flex justify-between border-t border-ink/10 pt-2">
              <dt className="text-brown-2">Email</dt>
              <dd className="font-medium text-ink">{profile.email}</dd>
            </div>
            <div className="flex justify-between border-t border-ink/10 pt-2">
              <dt className="text-brown-2">Membre depuis</dt>
              <dd className="font-medium text-ink">{dateFormatter.format(new Date(profile.createdAt))}</dd>
            </div>
          </dl>

          <form onSubmit={handleSubmit} className="mt-6 border-t border-ink/10 pt-6">
            <label className="flex flex-col gap-1.5 font-ui text-xs font-semibold text-brown-2">
              Pseudo
              <input
                value={displayName}
                onChange={(e) => setDraftName(e.target.value)}
                className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-2.5 font-ui text-sm text-ink"
              />
            </label>

            {error && <p className="mt-2 font-ui text-xs font-medium text-burnt">{error}</p>}
            {message && <p className="mt-2 font-ui text-xs font-medium text-teal">{message}</p>}

            <button
              type="submit"
              disabled={updateDisplayName.isPending || displayName.trim().length === 0}
              className="mt-3 rounded-lg bg-burnt px-4 py-2.5 font-ui text-sm font-semibold text-surface disabled:opacity-50"
            >
              Enregistrer
            </button>
          </form>

          <button
            type="button"
            onClick={logout}
            className="mt-6 w-full rounded-full border-[1.5px] border-ink px-4 py-2.5 font-ui text-sm font-semibold text-ink hover:bg-card"
          >
            Se déconnecter
          </button>
        </div>

        <div className="flex flex-1 flex-col gap-4">
          <div className="rounded-xl border-[1.5px] border-ink/15 bg-surface p-5">
            <div className="font-ui text-xs text-brown-2">Annonces publiées</div>
            <div className="font-display text-3xl">{listingsQuery.data?.length ?? '—'}</div>
          </div>
          <div className="rounded-xl border-[1.5px] border-ink/15 bg-surface p-5">
            <div className="font-ui text-xs text-brown-2">Favoris</div>
            <div className="font-display text-3xl">{favoritesQuery.data?.length ?? '—'}</div>
          </div>
        </div>
      </div>
    </div>
  )
}
