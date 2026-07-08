import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as catalogApi from '../api/catalog'
import type { CreateListingInput, ListingsFilter } from '../api/catalog'
import { useAuth } from '../context/AuthContext'

export function useCategories() {
  return useQuery({
    queryKey: ['categories'],
    queryFn: catalogApi.getCategories,
  })
}

export function useListings(filter: ListingsFilter = {}) {
  return useQuery({
    queryKey: ['listings', filter.categoryId ?? null, filter.search ?? null],
    queryFn: () => catalogApi.getListings(filter),
  })
}

export function useListing(id: string | undefined) {
  return useQuery({
    queryKey: ['listings', id],
    queryFn: () => catalogApi.getListing(id!),
    enabled: Boolean(id),
  })
}

export function useMyListings() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['listings', 'mine', user?.userId],
    queryFn: () => catalogApi.getMyListings(),
    enabled: Boolean(user),
  })
}

export function useCreateListing() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (input: CreateListingInput) => catalogApi.createListing(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['listings'] })
    },
  })
}
