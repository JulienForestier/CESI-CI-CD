import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as interestsApi from '../api/interests'
import { useAuth } from '../context/AuthContext'

export function useInterests() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['interests', user?.userId],
    queryFn: () => interestsApi.getInterests(user!.token),
    enabled: Boolean(user),
  })
}

export function useUpdateInterests() {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (categoryIds: string[]) => interestsApi.updateInterests(user!.token, categoryIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interests'] })
    },
  })
}
