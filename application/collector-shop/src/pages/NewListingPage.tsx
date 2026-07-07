import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { PlaceholderImage } from '../components/PlaceholderImage'
import { useCategories, useCreateListing } from '../hooks/useCatalog'
import { createListingSchema, type CreateListingFormValues, type CreateListingOutput } from '../schemas/listing'
import type { Listing } from '../types'

const priceFormatter = new Intl.NumberFormat('fr-FR', { style: 'currency', currency: 'EUR' })

export function NewListingPage() {
  const categoriesQuery = useCategories()
  const createListingMutation = useCreateListing()
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<Listing | null>(null)

  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<CreateListingFormValues, unknown, CreateListingOutput>({
    resolver: zodResolver(createListingSchema),
    defaultValues: { title: '', description: '', price: '', categoryId: '' },
  })

  const categories = categoriesQuery.data ?? []
  // eslint-disable-next-line react-hooks/incompatible-library -- watch() drives the live preview card, safe here
  const preview = watch()
  const previewCategoryName = categories.find((category) => category.id === preview.categoryId)?.name

  async function onSubmit(values: CreateListingOutput) {
    setError(null)
    setResult(null)
    try {
      const listing = await createListingMutation.mutateAsync(values)
      setResult(listing)
      if (listing.status === 'Published') {
        reset()
      }
    } catch {
      setError('Impossible de publier cette annonce pour le moment.')
    }
  }

  return (
    <div>
      <h1 className="mb-8 font-display text-3xl">Vendre un objet</h1>

      <form
        onSubmit={handleSubmit(onSubmit)}
        className="grid grid-cols-1 gap-9 font-ui md:grid-cols-[1.2fr_0.8fr]"
      >
        <div className="flex flex-col gap-4">
          <label className="flex flex-col gap-1.5 text-xs font-bold text-ink">
            Titre de l'annonce
            <input
              {...register('title')}
              className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm"
            />
            {errors.title && <span className="text-xs font-medium text-burnt">{errors.title.message}</span>}
          </label>

          <div className="flex gap-3.5">
            <label className="flex flex-1 flex-col gap-1.5 text-xs font-bold text-ink">
              Catégorie
              <select
                {...register('categoryId')}
                className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm"
              >
                <option value="">Choisir une catégorie…</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
              {errors.categoryId && (
                <span className="text-xs font-medium text-burnt">{errors.categoryId.message}</span>
              )}
            </label>

            <label className="flex flex-1 flex-col gap-1.5 text-xs font-bold text-ink">
              Prix (€)
              <input
                type="number"
                step="0.01"
                {...register('price')}
                className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm"
              />
              {errors.price && <span className="text-xs font-medium text-burnt">{errors.price.message}</span>}
            </label>
          </div>

          <label className="flex flex-col gap-1.5 text-xs font-bold text-ink">
            Description
            <textarea
              rows={4}
              {...register('description')}
              className="rounded-lg border-[1.5px] border-ink bg-surface px-3.5 py-3 text-sm"
            />
            {errors.description && (
              <span className="text-xs font-medium text-burnt">{errors.description.message}</span>
            )}
          </label>
        </div>

        <div>
          <div className="mb-2.5 text-xs font-bold tracking-widest text-brown-2 uppercase">Aperçu en direct</div>
          <div className="overflow-hidden rounded-xl border-[1.5px] border-ink bg-surface shadow-[4px_4px_0_rgba(41,33,27,0.15)]">
            <PlaceholderImage label="aperçu photo" />
            <div className="p-3.5">
              <div className="mb-0.5 text-sm font-bold">{preview.title || 'Titre de votre annonce'}</div>
              <div className="mb-2.5 text-xs text-brown-2">{previewCategoryName ?? 'Catégorie'}</div>
              <div className="font-display text-xl">
                {preview.price ? priceFormatter.format(Number(preview.price)) : '— €'}
              </div>
            </div>
          </div>

          <div className="mt-5 rounded-xl border-[1.5px] border-ink/20 bg-surface p-4 text-xs leading-relaxed text-brown-2">
            <b className="text-ink">Contrôle qualité automatique :</b> titre, description et prix sont vérifiés
            avant mise en ligne.
          </div>

          {error && <p className="mt-4 text-xs font-medium text-burnt">{error}</p>}
          {result && result.status === 'Published' && (
            <p className="mt-4 text-xs font-medium text-teal">Annonce publiée avec succès !</p>
          )}
          {result && result.status === 'Rejected' && (
            <p className="mt-4 text-xs font-medium text-burnt">
              Votre annonce n'a pas passé le contrôle automatique et n'a pas été publiée. Vérifiez le titre, la
              description et le prix.
            </p>
          )}

          <button
            type="submit"
            disabled={isSubmitting}
            className="mt-5 w-full rounded-xl bg-burnt py-3.5 text-sm font-bold text-surface shadow-[4px_4px_0_#29211b] disabled:opacity-50"
          >
            {isSubmitting ? 'Publication…' : "Publier l'annonce"}
          </button>
        </div>
      </form>
    </div>
  )
}
