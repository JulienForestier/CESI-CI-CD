import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as moderationApi from '../api/moderation'
import { useAuth } from '../context/AuthContext'

export function usePendingListings() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['moderation', 'pending'],
    queryFn: () => moderationApi.getPendingListings(),
    enabled: Boolean(user?.isAdmin),
  })
}

export function useApproveListing() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (listingId: string) => moderationApi.approveListing(listingId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['moderation', 'pending'] })
    },
  })
}

export function useRejectListing() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ listingId, reason }: { listingId: string; reason: string }) =>
      moderationApi.rejectListing(listingId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['moderation', 'pending'] })
    },
  })
}
