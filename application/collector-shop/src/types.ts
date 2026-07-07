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
