import { useState } from 'react'
import { ApiError } from '../api/client'
import { useCategories } from '../hooks/useCatalog'
import { useInterests, useUpdateInterests } from '../hooks/useInterests'

const CATEGORY_ICONS: Record<string, string> = {
  Figurines: '🧸',
  'Vinyles & cassettes': '💿',
  Sneakers: '👟',
}

export function InterestsPage() {
  const categoriesQuery = useCategories()
  const interestsQuery = useInterests()
  const updateInterests = useUpdateInterests()
  const [override, setOverride] = useState<string[] | null>(null)
  const [message, setMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const selected = override ?? interestsQuery.data ?? []

  function toggle(categoryId: string) {
    setMessage(null)
    setOverride(selected.includes(categoryId) ? selected.filter((id) => id !== categoryId) : [...selected, categoryId])
  }

  async function handleSave() {
    setError(null)
    setMessage(null)
    try {
      await updateInterests.mutateAsync(selected)
      setMessage('Vos préférences ont été enregistrées.')
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Impossible d'enregistrer vos préférences.")
    }
  }

  const categories = categoriesQuery.data ?? []

  return (
    <div>
      <h1 className="mb-2 font-display text-3xl">Centres d'intérêt</h1>
      <p className="mb-8 max-w-2xl font-ui text-sm text-brown-2">
        Sélectionnez les catégories qui vous intéressent pour recevoir une notification à chaque nouvelle annonce
        correspondante.
      </p>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {categories.map((category) => {
          const isSelected = selected.includes(category.id)
          return (
            <button
              key={category.id}
              type="button"
              onClick={() => toggle(category.id)}
              aria-pressed={isSelected}
              className={`relative rounded-xl border-[1.5px] p-5 text-left font-ui transition ${
                isSelected ? 'border-burnt bg-shipping' : 'border-ink/15 bg-surface'
              }`}
            >
              {isSelected && (
                <span className="absolute top-3 right-3 flex h-5 w-5 items-center justify-center rounded-full bg-burnt text-xs text-surface">
                  ✓
                </span>
              )}
              <div className="mb-2 text-2xl">{CATEGORY_ICONS[category.name] ?? '📦'}</div>
              <div className="font-semibold text-ink">{category.name}</div>
            </button>
          )
        })}
      </div>

      {error && <p className="mt-4 font-ui text-sm font-medium text-burnt">{error}</p>}
      {message && <p className="mt-4 font-ui text-sm font-medium text-teal">{message}</p>}

      <button
        type="button"
        onClick={handleSave}
        disabled={updateInterests.isPending || interestsQuery.isPending}
        className="mt-6 rounded-xl bg-burnt px-6 py-3.5 font-ui text-sm font-bold text-surface shadow-[4px_4px_0_#29211b] disabled:opacity-50"
      >
        Enregistrer mes préférences
      </button>
    </div>
  )
}
