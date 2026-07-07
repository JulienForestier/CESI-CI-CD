import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as catalogApi from '../api/catalog'
import type { CreateListingInput } from '../api/catalog'
import { useAuth } from '../context/AuthContext'

export function useCategories() {
  return useQuery({
    queryKey: ['categories'],
    queryFn: catalogApi.getCategories,
  })
}

export function useListings(categoryId?: string) {
  return useQuery({
    queryKey: ['listings', categoryId ?? null],
    queryFn: () => catalogApi.getListings(categoryId),
  })
}

export function useListing(id: string | undefined) {
  return useQuery({
    queryKey: ['listings', id],
    queryFn: () => catalogApi.getListing(id!),
    enabled: Boolean(id),
  })
}

export function useCreateListing() {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateListingInput) => catalogApi.createListing(user!.token, input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['listings'] })
    },
  })
}
