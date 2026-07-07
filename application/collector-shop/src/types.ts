export interface AuthResponse {
  token: string
  userId: string
  email: string
  displayName: string
}

export interface Category {
  id: string
  name: string
}

export type ListingStatus = 'Published' | 'Rejected'

export interface Listing {
  id: string
  title: string
  description: string
  price: number
  status: ListingStatus
  createdAt: string
  sellerId: string
  sellerDisplayName: string
  categoryId: string
  categoryName: string
}
