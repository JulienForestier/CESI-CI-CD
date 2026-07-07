export interface AuthResponse {
  token: string
  userId: string
  email: string
  displayName: string
  isAdmin: boolean
}

export interface Category {
  id: string
  name: string
}

export type ListingStatus = 'Published' | 'Rejected' | 'Pending'

export interface Listing {
  id: string
  title: string
  description: string
  price: number
  status: ListingStatus
  qualityScore: number
  moderationReason: string
  createdAt: string
  sellerId: string
  sellerDisplayName: string
  categoryId: string
  categoryName: string
}

export interface Conversation {
  id: string
  listingId: string
  listingTitle: string
  counterpartId: string
  counterpartDisplayName: string
  lastMessageBody: string | null
  lastMessageAt: string | null
  hasUnread: boolean
}

export interface Message {
  id: string
  conversationId: string
  senderId: string
  senderDisplayName: string
  body: string
  sentAt: string
}

export type NotificationType = 'NewListingMatch' | 'ListingApproved' | 'ListingRejected'

export interface AppNotification {
  id: string
  title: string
  message: string
  type: NotificationType
  isRead: boolean
  createdAt: string
  listingId: string | null
}

export interface UserProfile {
  id: string
  email: string
  displayName: string
  isAdmin: boolean
  createdAt: string
}
