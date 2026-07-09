import { useMutation, useQuery } from '@tanstack/react-query'
import * as reportsApi from '../api/reports'
import { useAuth } from '../context/AuthContext'

export function useReportListing() {
  return useMutation({
    mutationFn: ({ listingId, reason, details }: { listingId: string; reason: string; details?: string }) =>
      reportsApi.reportListing(listingId, reason, details),
  })
}

export function useReports(search?: string) {
  const { user } = useAuth()

  return useQuery({
    queryKey: ['reports', search ?? ''],
    queryFn: () => reportsApi.getReports(search),
    enabled: Boolean(user?.isAdmin),
  })
}
