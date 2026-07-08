import { apiFetch } from './client'
import type { Conversation, Message } from '../types'

export function getConversations() {
  return apiFetch<Conversation[]>('/conversations')
}

export function startConversation(listingId: string) {
  return apiFetch<{ id: string }>('/conversations', { method: 'POST', body: { listingId } })
}

export function getMessages(conversationId: string) {
  return apiFetch<Message[]>(`/conversations/${conversationId}/messages`)
}

export function sendMessage(conversationId: string, body: string) {
  return apiFetch<Message>(`/conversations/${conversationId}/messages`, {
    method: 'POST',
    body: { body },
  })
}
