import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export function ProtectedRoute({ children, adminOnly = false }: { children: ReactNode; adminOnly?: boolean }) {
  const { user } = useAuth()

  if (!user) {
    return <Navigate to="/connexion" replace />
  }

  if (adminOnly && !user.isAdmin) {
    return <Navigate to="/" replace />
  }

  return children
}
