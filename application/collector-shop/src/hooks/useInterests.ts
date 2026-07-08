import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as interestsApi from '../api/interests'
import { useAuth } from '../context/AuthContext'

export function useInterests() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['interests', user?.userId],
    queryFn: () => interestsApi.getInterests(),
    enabled: Boolean(user),
  })
}

export function useUpdateInterests() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (categoryIds: string[]) => interestsApi.updateInterests(categoryIds),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['interests'] })
    },
  })
}
