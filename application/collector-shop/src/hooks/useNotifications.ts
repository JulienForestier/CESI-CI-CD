import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as notificationsApi from '../api/notifications'
import { useAuth } from '../context/AuthContext'

const POLL_INTERVAL_MS = 10000

export function useNotifications() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['notifications', user?.userId],
    queryFn: () => notificationsApi.getNotifications(),
    enabled: Boolean(user),
    refetchInterval: POLL_INTERVAL_MS,
  })
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => notificationsApi.markAllNotificationsRead(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] })
    },
  })
}
