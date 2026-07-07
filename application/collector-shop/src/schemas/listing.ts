import { z } from 'zod'

export const createListingSchema = z.object({
  title: z.string().trim().min(1, 'Le titre est requis'),
  description: z.string().trim().min(1, 'La description est requise'),
  price: z.coerce.number().positive('Le prix doit être positif'),
  categoryId: z.string().min(1, 'Choisissez une catégorie'),
})

export type CreateListingFormValues = z.input<typeof createListingSchema>
export type CreateListingOutput = z.output<typeof createListingSchema>
