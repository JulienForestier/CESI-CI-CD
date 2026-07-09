import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as purchasesApi from '../api/purchases'
import { useAuth } from '../context/AuthContext'

export function useMyPurchases() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['purchases', 'mine', user?.userId],
    queryFn: () => purchasesApi.getMyPurchases(),
    enabled: Boolean(user),
  })
}

export function usePurchaseListing() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (listingId: string) => purchasesApi.purchaseListing(listingId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['purchases'] })
      queryClient.invalidateQueries({ queryKey: ['listings'] })
    },
  })
}
