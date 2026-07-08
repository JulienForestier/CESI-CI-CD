import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as chatApi from '../api/chat'
import { useAuth } from '../context/AuthContext'

const POLL_INTERVAL_MS = 4000

export function useConversations() {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['conversations', user?.userId],
    queryFn: () => chatApi.getConversations(),
    enabled: Boolean(user),
    refetchInterval: POLL_INTERVAL_MS,
  })
}

export function useMessages(conversationId: string | undefined) {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['conversations', conversationId, 'messages'],
    queryFn: () => chatApi.getMessages(conversationId!),
    enabled: Boolean(user) && Boolean(conversationId),
    refetchInterval: POLL_INTERVAL_MS,
  })
}

export function useStartConversation() {
  return useMutation({
    mutationFn: (listingId: string) => chatApi.startConversation(listingId),
  })
}

export function useSendMessage(conversationId: string) {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (body: string) => chatApi.sendMessage(conversationId, body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['conversations', conversationId, 'messages'] })
      queryClient.invalidateQueries({ queryKey: ['conversations', user?.userId] })
    },
  })
}
