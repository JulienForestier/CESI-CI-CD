import { useState } from 'react'
import { ApiError } from '../api/client'
import { useApproveListing, usePendingListings, useRejectListing } from '../hooks/useModeration'
import { useReports } from '../hooks/useReports'
import type { Listing } from '../types'

const priceFormatter = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR' })
const dateFormatter = new Intl.DateTimeFormat('fr-FR', { dateStyle: 'medium', timeStyle: 'short' })

export function ModerationPage() {
  const pendingQuery = usePendingListings()
  const listings = pendingQuery.data ?? []

  return (
    <div>
      <h1 className="mb-2 font-display text-3xl">Modération des annonces</h1>
      <p className="mb-8 font-ui text-sm text-brown-2">
        {listings.length} annonce(s) en attente de validation manuelle.
      </p>

      {pendingQuery.isPending && <p className="font-ui text-brown-2">Chargement…</p>}
      {pendingQuery.isError && (
        <p className="font-ui text-burnt">Impossible de charger la file de modération.</p>
      )}
      {pendingQuery.isSuccess && listings.length === 0 && (
        <p className="font-ui text-brown-2">Aucune annonce en attente, tout est à jour.</p>
      )}

      <div className="flex flex-col gap-4">
        {listings.map((listing) => (
          <ModerationCard key={listing.id} listing={listing} />
        ))}
      </div>

      <ReportsSection />
    </div>
  )
}

function ReportsSection() {
  const [search, setSearch] = useState('')
  const reportsQuery = useReports(search || undefined)
  const reports = reportsQuery.data ?? []

  return (
    <div className="mt-12 border-t border-ink/10 pt-8">
      <h2 className="mb-2 font-display text-2xl">Signalements</h2>
      <p className="mb-4 font-ui text-sm text-brown-2">
        {reports.length} signalement(s){search ? ' correspondant à la recherche' : ''}.
      </p>

      <input
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Rechercher dans les motifs…"
        className="mb-4 w-full max-w-sm rounded-lg border-[1.5px] border-ink bg-surface px-3 py-2 font-ui text-sm text-ink"
      />

      {reportsQuery.isPending && <p className="font-ui text-brown-2">Chargement…</p>}
      {reportsQuery.isError && <p className="font-ui text-burnt">Impossible de charger les signalements.</p>}
      {reportsQuery.isSuccess && reports.length === 0 && (
        <p className="font-ui text-brown-2">Aucun signalement à traiter.</p>
      )}

      <div className="flex flex-col gap-3">
        {reports.map((report) => (
          <div key={report.id} className="rounded-xl border-[1.5px] border-ink/15 bg-surface p-4">
            <div className="flex items-start justify-between gap-4">
              <div>
                <div className="font-display text-lg">{report.listingTitle}</div>
                <div className="font-ui text-xs text-brown-2">
                  Signalé par {report.reporterDisplayName} · {dateFormatter.format(new Date(report.createdAt))}
                </div>
              </div>
            </div>
            <p className="mt-2 font-ui text-sm font-semibold text-burnt">{report.reason}</p>
            {report.details && <p className="mt-1 font-body text-sm text-brown-1">{report.details}</p>}
          </div>
        ))}
      </div>
    </div>
  )
}

function ModerationCard({ listing }: { listing: Listing }) {
  const approve = useApproveListing()
  const reject = useRejectListing()
  const [showRejectForm, setShowRejectForm] = useState(false)
  const [reason, setReason] = useState('')
  const [error, setError] = useState<string | null>(null)

  async function handleApprove() {
    setError(null)
    try {
      await approve.mutateAsync(listing.id)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Impossible de valider l'annonce.")
    }
  }

  async function handleReject() {
    setError(null)
    try {
      await reject.mutateAsync({ listingId: listing.id, reason })
      setShowRejectForm(false)
      setReason('')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Impossible de rejeter l'annonce.")
    }
  }

  return (
    <div className="rounded-xl border-[1.5px] border-ink/15 bg-surface p-4">
      <div className="flex items-start justify-between gap-4">
        <div>
          <div className="font-ui text-xs font-bold tracking-widest text-burnt uppercase">
            {listing.categoryName}
          </div>
          <div className="font-display text-xl">{listing.title}</div>
          <div className="font-ui text-xs text-brown-2">
            {listing.sellerDisplayName} · {priceFormatter.format(listing.price)}
          </div>
        </div>
        <span className="shrink-0 rounded-full bg-shipping px-3 py-1 font-ui text-xs font-bold text-burnt">
          {listing.qualityScore}/100 — {listing.moderationReason}
        </span>
      </div>

      <p className="mt-3 font-body text-sm text-brown-1">{listing.description}</p>

      {error && <p className="mt-2 font-ui text-xs font-medium text-burnt">{error}</p>}

      {!showRejectForm ? (
        <div className="mt-4 flex gap-2">
          <button
            type="button"
            onClick={handleApprove}
            disabled={approve.isPending}
            className="rounded-lg bg-teal px-4 py-2 font-ui text-sm font-semibold text-surface disabled:opacity-50"
          >
            Valider
          </button>
          <button
            type="button"
            onClick={() => setShowRejectForm(true)}
            className="rounded-lg border-[1.5px] border-ink px-4 py-2 font-ui text-sm font-semibold text-ink"
          >
            Rejeter
          </button>
        </div>
      ) : (
        <div className="mt-4 flex flex-col gap-2">
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            placeholder="Motif du rejet…"
            className="rounded-lg border-[1.5px] border-ink bg-surface px-3 py-2 font-ui text-sm text-ink"
          />
          <div className="flex gap-2">
            <button
              type="button"
              onClick={handleReject}
              disabled={reject.isPending || reason.trim().length === 0}
              className="rounded-lg bg-burnt px-4 py-2 font-ui text-sm font-semibold text-surface disabled:opacity-50"
            >
              Confirmer le rejet
            </button>
            <button
              type="button"
              onClick={() => setShowRejectForm(false)}
              className="rounded-lg border-[1.5px] border-ink/30 px-4 py-2 font-ui text-sm font-semibold text-brown-2"
            >
              Annuler
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
