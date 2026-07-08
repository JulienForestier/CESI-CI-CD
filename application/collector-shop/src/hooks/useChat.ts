import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as chatApi from '../api/chat'
import { useAuth } from '../context/AuthContext'

const POLL_INTERVAL_MS = 4000

export function useConversations() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['conversations', user?.userId],
    queryFn: () => chatApi.getConversations(user!.token),
    enabled: Boolean(user),
    refetchInterval: POLL_INTERVAL_MS,
  })
}

export function useMessages(conversationId: string | undefined) {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['conversations', conversationId, 'messages'],
    queryFn: () => chatApi.getMessages(user!.token, conversationId!),
    enabled: Boolean(user) && Boolean(conversationId),
    refetchInterval: POLL_INTERVAL_MS,
  })
}

export function useStartConversation() {
  const { user } = useAuth()

  return useMutation({
    mutationFn: (listingId: string) => chatApi.startConversation(user!.token, listingId),
  })
}

export function useSendMessage(conversationId: string) {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: string) => chatApi.sendMessage(user!.token, conversationId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversations', conversationId, 'messages'] })
      queryClient.invalidateQueries({ queryKey: ['conversations', user?.userId] })
    },
  })
}
