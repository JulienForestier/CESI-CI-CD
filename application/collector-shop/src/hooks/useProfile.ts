import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as usersApi from '../api/users'
import { useAuth } from '../context/AuthContext'

export function useProfile() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['profile', user?.userId],
    queryFn: () => usersApi.getMyProfile(),
    enabled: Boolean(user),
  })
}

export function useUpdateDisplayName() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (displayName: string) => usersApi.updateDisplayName(displayName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['profile'] })
    },
  })
}
